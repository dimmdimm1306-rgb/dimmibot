"""
Schema Scanner - Scan all shared spreadsheets and build a knowledge base.

Output: schemas.json containing metadata for each sheet:
  - Header position (row_start, rows count)
  - Column names + types
  - Sample values
  - Crosstab detection (2D pivot: group header x sub header)
  - Row count

This schema is loaded by the AI bot as KNOWLEDGE BASE so it knows the
structure without having to fetch data every query. Massive token savings.

Usage:
    python -m mcp_gdrive_filter.schema_scanner
    # Or as module:
    from mcp_gdrive_filter.schema_scanner import scan_all, build_kb_summary
"""
import os
import sys
import json
import re
from typing import Any, Dict, List, Optional, Tuple
from datetime import datetime

# Reuse MCP core
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from mcp_gdrive_filter import server as mcp_core
import pandas as pd

# =====================================================================
# Helpers
# =====================================================================

def _is_numeric_like(val: str) -> bool:
    """Check if a string looks like a number."""
    if not val or not isinstance(val, str):
        return False
    s = val.strip().replace(',', '').replace('.', '').replace('%', '').replace('-', '')
    return s.isdigit()

def _detect_header_rows(rows: List[List[Any]]) -> Tuple[int, int]:
    """
    Heuristic: find header row start (1-indexed) and count.
    Returns (header_row_start, header_rows).

    Rules:
      - Skip leading rows that are pure titles (few cells, mostly empty)
      - Header row = first row with >=3 non-empty text cells AND distinct values
      - Multi-row header if next row ALSO has text (not data)
    """
    def nonempty(row):
        return [str(c).strip() for c in row if c and str(c).strip()]

    def is_text_row(row):
        cells = nonempty(row)
        if len(cells) < 2:
            return False
        text_count = sum(1 for c in cells if not _is_numeric_like(c))
        return text_count >= max(2, int(len(cells) * 0.5))

    def is_title_row(row):
        """A title row has 1-2 cells, usually centered/merged."""
        cells = nonempty(row)
        return 0 < len(cells) <= 2

    for i, row in enumerate(rows[:10]):
        cells = nonempty(row)

        # Skip title rows
        if len(cells) < 3:
            continue

        # Check if it's text row (header candidate)
        if not is_text_row(row):
            continue

        header_rows = 1

        # Check if next row is also text (multi-row header)
        if i + 1 < len(rows):
            next_cells = nonempty(rows[i + 1])
            if len(next_cells) > 0 and is_text_row(rows[i + 1]):
                # Check that row AFTER it has data (numeric or mixed)
                if i + 2 < len(rows):
                    data_row = nonempty(rows[i + 2])
                    has_numeric = any(_is_numeric_like(c) for c in data_row)
                    if has_numeric or len(data_row) >= 3:
                        header_rows = 2

        return i + 1, header_rows

    return 1, 1


def _detect_crosstab(df: pd.DataFrame, raw_rows: List[List[Any]], header_row_start: int, header_rows: int) -> Optional[Dict[str, Any]]:
    """
    Detect if a sheet is a 2D crosstab (pivot-like).
    Example: Aktual Stok has:
      Row 1: STOK DITERIMA | ... | STOK KELUAR | ... | Implementasi Progress
      Row 2: KOTA, [6 kota for each group]
      Row 3: (optional 3rd header for sub-group)
      Row 4+: Material (Cable 24C, Tiang 7M, ...)

    Heuristic: if header_rows >= 2 AND first column has few unique text values
    (looks like row labels), might be crosstab.
    """
    if header_rows < 2:
        return None
    if df.empty or len(df.columns) < 5:
        return None

    # Check if first column is short text categories (row labels)
    first_col = df.iloc[:, 0] if len(df.columns) > 0 else None
    if first_col is None:
        return None

    uniques = first_col.dropna().astype(str).unique()
    uniques = [u for u in uniques if u.strip() and len(u) < 50]
    if len(uniques) < 2 or len(uniques) > 30:
        return None

    # Extract group headers from raw rows (before header_rows merged)
    if len(raw_rows) < header_row_start + header_rows:
        return None

    group_row = raw_rows[header_row_start - 1]  # 0-indexed
    sub_row = raw_rows[header_row_start] if header_rows >= 2 and len(raw_rows) > header_row_start else []

    # Find distinct groups (non-empty cells forward-filled)
    groups = []
    last_group = None
    for idx, cell in enumerate(group_row):
        c = str(cell).strip() if cell else ''
        if c:
            last_group = c
            groups.append((idx, c))

    if len(groups) < 2:
        return None

    # Find sub-headers per group
    sub_headers = []
    for idx, cell in enumerate(sub_row):
        c = str(cell).strip() if cell else ''
        if c:
            sub_headers.append(c)

    return {
        "type": "crosstab",
        "row_label_column": str(df.columns[0]) if len(df.columns) > 0 else None,
        "row_values": uniques[:20],  # first 20 for KB
        "groups": [g[1] for g in groups[:10]],
        "sub_headers": list(dict.fromkeys(sub_headers))[:15],  # unique, ordered
    }


def _extract_column_info(df: pd.DataFrame, max_unique: int = 30) -> List[Dict[str, Any]]:
    """Extract per-column metadata for KB."""
    info = []
    for col in df.columns:
        col_str = str(col).strip()
        if not col_str or col_str.startswith('_col_') or col_str == '_unnamed':
            continue
        # Skip dedupe duplicates like "SITE ID_2"
        if re.search(r'_\d+$', col_str):
            continue

        # Handle multi-column matches after dedupe (same base name)
        try:
            series = df[col]
            if hasattr(series, 'iloc') and not isinstance(series, pd.Series):
                series = series.iloc[:, 0]
        except Exception:
            continue

        try:
            dtype = str(series.dtype)
            non_null = int(series.count())

            col_info = {"name": col_str, "dtype": dtype, "non_null": non_null}

            # For text: unique values (if not too many)
            if dtype == 'object' or dtype == 'string':
                uniques = series.dropna().astype(str).str.strip().unique()
                uniques = [u for u in uniques if u and u.lower() not in ('nan', 'null', '')]
                col_info["unique_count"] = len(uniques)
                if 0 < len(uniques) <= max_unique:
                    col_info["unique_values"] = [str(u) for u in uniques][:max_unique]
                elif len(uniques) > max_unique:
                    # Top 15 most common
                    top = series.value_counts().head(15)
                    col_info["top_values"] = {str(k): int(v) for k, v in top.items()}
            elif pd.api.types.is_numeric_dtype(series):
                try:
                    col_info["min"] = float(series.min())
                    col_info["max"] = float(series.max())
                except Exception:
                    pass

            info.append(col_info)
        except Exception as e:
            info.append({"name": col_str, "error": str(e)[:80]})

    return info


# =====================================================================
# Main scan functions
# =====================================================================

def scan_sheet(file_id: str, sheet_name: str, file_name: str = None) -> Dict[str, Any]:
    """Scan a single sheet and return its schema."""
    mcp_core.init_services() if mcp_core.service is None else None

    # 1. Peek raw rows to detect header structure
    raw_rows = mcp_core.read_raw_rows(file_id, sheet_name, max_rows=10)
    if not raw_rows:
        return {"error": "empty sheet"}

    header_row_start, header_rows = _detect_header_rows(raw_rows)

    # 2. Load full DataFrame with detected header
    try:
        df = mcp_core.download_file_to_df(
            file_id, sheet_name,
            header_rows=header_rows,
            header_row_start=header_row_start,
        )
    except Exception as e:
        return {"error": f"load failed: {e}"}

    # 3. Detect crosstab
    crosstab = _detect_crosstab(df, raw_rows, header_row_start, header_rows)

    # 4. Column info
    columns = _extract_column_info(df, max_unique=30)

    # 5. Sample first data row
    sample_row = {}
    if len(df) > 0:
        first = df.iloc[0].to_dict()
        for k, v in first.items():
            if v is not None and str(v).strip() and str(v).lower() != 'nan':
                sample_row[str(k)] = str(v)[:60]

    return {
        "sheet_name": sheet_name,
        "header_row_start": header_row_start,
        "header_rows": header_rows,
        "total_rows": int(len(df)),
        "total_cols": int(len(df.columns)),
        "crosstab": crosstab,
        "columns": columns,
        "sample_first_row": sample_row,
    }


def scan_file(file_id: str, file_name: str = None) -> Dict[str, Any]:
    """Scan all sheets in a file."""
    mcp_core.init_services() if mcp_core.service is None else None
    metadata = mcp_core.get_file_metadata(file_id)
    mime_type = metadata.get('mimeType', '')
    name = file_name or metadata.get('name', '')

    try:
        sheet_names = mcp_core.get_sheet_names(file_id)
    except Exception as e:
        return {"file_name": name, "error": f"cannot list sheets: {e}"}

    sheets = {}
    for sn in sheet_names:
        try:
            sheets[sn] = scan_sheet(file_id, sn, name)
        except Exception as e:
            sheets[sn] = {"error": str(e)[:200]}

    return {
        "file_id": file_id,
        "file_name": name,
        "mime_type": mime_type,
        "sheets": sheets,
    }


def scan_all(output_path: str = None) -> Dict[str, Any]:
    """Scan all shared spreadsheets and write schemas.json."""
    mcp_core.init_services()

    # List all shared files
    q_parts = [
        f"(mimeType='{mcp_core.MIME_GOOGLE_SHEET}' or mimeType='{mcp_core.MIME_EXCEL}' or mimeType='{mcp_core.MIME_EXCEL_OLD}' or mimeType='{mcp_core.MIME_CSV}')",
        "trashed=false",
    ]
    q = " and ".join(q_parts)

    result = mcp_core.service.files().list(
        q=q,
        pageSize=100,
        fields="files(id, name, mimeType, modifiedTime)"
    ).execute()
    files = result.get('files', [])

    print(f"Found {len(files)} spreadsheet file(s) to scan")

    files_scanned = {}
    for i, f in enumerate(files, 1):
        print(f"  [{i}/{len(files)}] Scanning: {f['name']}")
        try:
            files_scanned[f['id']] = scan_file(f['id'], f['name'])
        except Exception as e:
            print(f"    ERROR: {e}")
            files_scanned[f['id']] = {"file_name": f['name'], "error": str(e)[:200]}

    kb = {
        "scanned_at": datetime.utcnow().isoformat() + "Z",
        "total_files": len(files_scanned),
        "files": files_scanned,
    }

    if output_path:
        with open(output_path, 'w', encoding='utf-8') as f:
            json.dump(kb, f, indent=2, ensure_ascii=False, default=str)
        print(f"\nSaved schema KB to: {output_path}")

    return kb


# =====================================================================
# KB summary generator (compact for LLM system prompt)
# =====================================================================

def build_kb_summary(kb: Dict[str, Any], max_chars: int = 3000) -> str:
    """
    Build a COMPACT summary of the schema KB suitable for LLM system prompt.
    Target: keep under max_chars but include all sheet names, key columns, and searchable values.
    """
    lines = []
    lines.append("# KNOWLEDGE BASE SPREADSHEET")
    lines.append(f"Scanned {kb.get('total_files', 0)} file pada {kb.get('scanned_at', '')[:19]}")
    lines.append("")

    for file_id, f in kb.get('files', {}).items():
        if 'error' in f:
            continue
        name = f.get('file_name', file_id)
        lines.append(f"## {name}")
        lines.append(f"ID: {file_id}")

        for sheet_name, s in f.get('sheets', {}).items():
            if 'error' in s:
                continue

            hrs = s.get('header_rows', 1)
            hrstart = s.get('header_row_start', 1)
            rows = s.get('total_rows', 0)

            header_tag = ""
            if hrs > 1 or hrstart > 1:
                header_tag = f" [header_rows={hrs} header_row_start={hrstart}]"

            lines.append(f"### Sheet: {sheet_name} ({rows} baris){header_tag}")

            ct = s.get('crosstab')
            if ct:
                lines.append(f"  Type: CROSSTAB 2D")
                lines.append(f"  Row labels (kolom: {ct.get('row_label_column')}): {', '.join(ct.get('row_values', [])[:10])}")
                lines.append(f"  Groups: {', '.join(ct.get('groups', [])[:8])}")
                if ct.get('sub_headers'):
                    lines.append(f"  Sub-headers: {', '.join(ct.get('sub_headers', [])[:10])}")
            else:
                # Key columns + values
                for col in s.get('columns', []):
                    n = col.get('name', '')
                    unique_vals = col.get('unique_values', [])
                    if unique_vals and len(unique_vals) <= 15:
                        vals_str = ', '.join([v for v in unique_vals if v][:10])
                        lines.append(f"  - {n}: {vals_str}")
                    elif col.get('top_values'):
                        top_vals = list(col.get('top_values', {}).keys())[:6]
                        lines.append(f"  - {n}: (banyak nilai, sample: {', '.join(top_vals)})")
                    else:
                        dtype = col.get('dtype', '')
                        lines.append(f"  - {n} [{dtype}]")

        lines.append("")

    summary = '\n'.join(lines)
    if len(summary) > max_chars:
        summary = summary[:max_chars - 50] + "\n... (truncated)"
    return summary


if __name__ == "__main__":
    output = os.getenv('SCHEMAS_OUT', os.path.join(os.path.dirname(os.path.abspath(__file__)), 'schemas.json'))
    kb = scan_all(output)

    # Also generate summary.txt for LLM
    summary_path = output.replace('schemas.json', 'schemas_summary.txt')
    summary = build_kb_summary(kb)
    with open(summary_path, 'w', encoding='utf-8') as f:
        f.write(summary)
    print(f"Saved LLM-friendly summary to: {summary_path}")
    print(f"Summary size: {len(summary)} chars")
