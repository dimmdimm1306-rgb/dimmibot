"""
MCP Server untuk Google Drive dengan filtering data Excel/Sheets
Hemat token dengan pre-filter data sebelum dikirim ke AI

Support:
- Excel (.xlsx, .xls) - native download
- Google Sheets - auto export ke Excel
- CSV (.csv) - native download
- Folder browsing - list semua file di folder
"""

import os
import json
from typing import Any, Dict, List, Optional
import pandas as pd
from google.oauth2 import service_account
from googleapiclient.discovery import build
from googleapiclient.http import MediaIoBaseDownload
import io
from mcp.server import Server
from mcp.types import Tool, TextContent
import mcp.server.stdio

# Inisialisasi MCP Server
app = Server("gdrive-filter-server")

# =====================================================================
# READ-ONLY ENFORCEMENT
# =====================================================================
# Server ini dikonfigurasi 100% read-only dengan 3 lapisan proteksi:
#   1. OAuth scopes hanya .readonly (Google akan tolak semua write)
#   2. Whitelist tools - hanya baca/list yang di-expose
#   3. Runtime guard - block HTTP method selain GET (lihat bawah)
# =====================================================================

# Google API scopes - HANYA READ-ONLY!
# Jangan tambahkan scope tanpa .readonly di sini.
SCOPES = [
    'https://www.googleapis.com/auth/drive.readonly',
    'https://www.googleapis.com/auth/drive.metadata.readonly',
    'https://www.googleapis.com/auth/spreadsheets.readonly'
]

# Whitelist HTTP methods - hanya GET dan POST untuk batch/search yang di-allow
# (Google Drive API pakai POST untuk beberapa read operation seperti search)
_ALLOWED_READ_METHODS = {'GET'}
_ALLOWED_READ_URIS = (
    'files',  # list/get files (GET)
    'export', # export file content (GET)
    'values', # sheets values.get (GET)
    'spreadsheets',  # sheets metadata (GET)
)

service = None
sheets_service = None

# MIME types
MIME_GOOGLE_SHEET = 'application/vnd.google-apps.spreadsheet'
MIME_GOOGLE_FOLDER = 'application/vnd.google-apps.folder'
MIME_EXCEL = 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
MIME_EXCEL_OLD = 'application/vnd.ms-excel'
MIME_CSV = 'text/csv'

def _assert_readonly_request(http, *args, **kwargs):
    """
    Runtime guard: intercept setiap HTTP request ke Google API.
    Block method apapun selain GET (kecuali POST untuk :batchGet yang read-only).
    Ini lapisan pertahanan kedua kalau ada bug/regresi di kode.
    """
    # args format dari googleapiclient: (uri, method, body, headers)
    method = None
    uri = ''
    if len(args) >= 2:
        uri = str(args[0])
        method = str(args[1]).upper()
    elif 'method' in kwargs:
        method = str(kwargs['method']).upper()
        uri = str(kwargs.get('uri', ''))
    
    if method is None:
        return  # can't determine, let it through (signature mismatch)
    
    # Allow GET always
    if method == 'GET':
        return
    
    # Allow POST only for known read-only endpoints (batchGet, search)
    if method == 'POST':
        read_only_post_endpoints = (':batchGet', ':batchGetByDataFilter')
        if any(ep in uri for ep in read_only_post_endpoints):
            return
    
    # Block everything else
    raise PermissionError(
        f"READ-ONLY VIOLATION: method={method} uri={uri[:100]}. "
        f"This server is locked to read-only mode."
    )


def init_services():
    """Initialize Google Drive & Sheets services (READ-ONLY)"""
    global service, sheets_service
    creds_path = os.getenv('GOOGLE_APPLICATION_CREDENTIALS')
    if not creds_path:
        raise ValueError("GOOGLE_APPLICATION_CREDENTIALS not set")
    
    credentials = service_account.Credentials.from_service_account_file(
        creds_path, scopes=SCOPES)
    
    # Verify scopes are read-only (sanity check)
    for scope in credentials.scopes or []:
        if not scope.endswith('.readonly'):
            raise PermissionError(
                f"REFUSED TO START: scope '{scope}' is not read-only!"
            )
    
    service = build('drive', 'v3', credentials=credentials)
    sheets_service = build('sheets', 'v4', credentials=credentials)
    
    # Install runtime guard - intercept semua HTTP request
    _original_request_drive = service._http.request
    _original_request_sheets = sheets_service._http.request
    
    def _guarded_drive(*args, **kwargs):
        _assert_readonly_request(service._http, *args, **kwargs)
        return _original_request_drive(*args, **kwargs)
    
    def _guarded_sheets(*args, **kwargs):
        _assert_readonly_request(sheets_service._http, *args, **kwargs)
        return _original_request_sheets(*args, **kwargs)
    
    service._http.request = _guarded_drive
    sheets_service._http.request = _guarded_sheets

def _sanitize_for_json(obj):
    """Replace NaN/Inf floats with None, recursively. json.dumps default tolak nan/inf."""
    if isinstance(obj, float):
        if obj != obj or obj == float('inf') or obj == float('-inf'):  # NaN check
            return None
        return obj
    if isinstance(obj, dict):
        return {k: _sanitize_for_json(v) for k, v in obj.items()}
    if isinstance(obj, (list, tuple)):
        return [_sanitize_for_json(v) for v in obj]
    return obj


def get_file_metadata(file_id: str) -> Dict:
    """Get file metadata (name, mimeType, dll)"""
    return service.files().get(
        fileId=file_id,
        fields="id, name, mimeType, modifiedTime, size, parents"
    ).execute()


def _flatten_multi_header(df: pd.DataFrame) -> pd.DataFrame:
    """
    Ketika pakai pd.read_excel(header=[0,1]) atau sheets dengan 2-row header,
    kolom jadi MultiIndex. Flatten jadi string: ('Kabel 24c', 'Progress') -> 'Kabel 24c - Progress'.
    Kolom Unnamed di level atas (karena merged cell kosong) di-drop.
    """
    if not isinstance(df.columns, pd.MultiIndex):
        return df

    flat_cols = []
    for tup in df.columns:
        parts = []
        for p in tup:
            s = str(p).strip() if p is not None else ''
            # Skip pandas "Unnamed: X" placeholders yang dihasilkan dari merged cells
            if s.lower().startswith('unnamed:') or s == 'nan' or s == '':
                continue
            parts.append(s)
        flat_cols.append(' - '.join(parts) if parts else '_unnamed')
    df = df.copy()
    df.columns = flat_cols
    return df


def _merge_multi_row_header(df: pd.DataFrame, header_rows: int) -> pd.DataFrame:
    """
    Untuk Google Sheets / CSV yang sudah di-read flat: gabungkan N baris pertama
    jadi header tunggal dengan pola "Group - Sub" (copy forward untuk merged cells).
    """
    if header_rows <= 1 or len(df) < header_rows:
        return df

    # Ambil N baris pertama sebagai header rows
    header_values = []
    for i in range(header_rows):
        row = df.iloc[i].fillna('').astype(str).tolist()
        # Forward-fill level atas (merged cells di Excel/Sheets biasa ninggalin kosong)
        if i < header_rows - 1:
            prev = ''
            filled = []
            for v in row:
                s = v.strip()
                if s and not s.lower().startswith('unnamed'):
                    prev = s
                    filled.append(s)
                else:
                    filled.append(prev)
            header_values.append(filled)
        else:
            # baris terakhir: jangan forward-fill (sub-header biasanya lengkap)
            header_values.append([v.strip() for v in row])

    # Combine
    new_cols = []
    for col_idx in range(len(df.columns)):
        parts = []
        for lvl in header_values:
            s = lvl[col_idx] if col_idx < len(lvl) else ''
            if s and s.lower() != 'nan' and not s.lower().startswith('unnamed'):
                if not parts or parts[-1] != s:
                    parts.append(s)
        new_cols.append(' - '.join(parts) if parts else f'_col_{col_idx+1}')

    df = df.iloc[header_rows:].copy()
    df.columns = new_cols
    df = df.reset_index(drop=True)
    return df


def _dedupe_columns(df: pd.DataFrame) -> pd.DataFrame:
    """
    Spreadsheet sering punya header duplikat (mis. 'HOMEBASE' muncul 2x, atau kolom kosong '').
    Pandas tangani ini dengan biarin nama sama -> df[col] return DataFrame, bukan Series,
    yang bikin banyak operasi (.dtype, .astype, .str, ...) crash.
    Kita rename duplicate jadi 'name', 'name_2', 'name_3', dst.
    Kolom kosong dirubah jadi '_col_N'.
    """
    if df is None or df.empty and len(df.columns) == 0:
        return df

    new_cols = []
    seen: Dict[str, int] = {}
    for i, col in enumerate(df.columns):
        name = str(col) if col is not None else ''
        name = name.strip()
        if name == '' or name.lower() == 'nan':
            name = f'_col_{i+1}'
        if name in seen:
            seen[name] += 1
            new_cols.append(f'{name}_{seen[name]}')
        else:
            seen[name] = 1
            new_cols.append(name)
    df = df.copy()
    df.columns = new_cols
    return df


def download_file_to_df(
    file_id: str,
    sheet_name: Optional[str] = None,
    header_rows: int = 1,
    header_row_start: int = 1,
) -> pd.DataFrame:
    """
    Download file dari Google Drive dan convert ke DataFrame.
    Support: Excel, Google Sheets, CSV. Header duplikat otomatis di-dedupe.

    Args:
        header_rows:       jumlah baris header (default 1; pakai 2 untuk group+sub).
        header_row_start:  baris berapa (1-indexed) yang pertama jadi header.
                           Default 1. Pakai 3 kalau 2 baris pertama adalah judul/merged title
                           yang bukan header asli.
    """
    metadata = get_file_metadata(file_id)
    mime_type = metadata.get('mimeType', '')
    file_name = metadata.get('name', '')

    # Google Sheets - pakai Sheets API (lebih efisien, sudah dedupe)
    if mime_type == MIME_GOOGLE_SHEET:
        return read_google_sheet(file_id, sheet_name, header_rows=header_rows,
                                  header_row_start=header_row_start)

    # Excel atau CSV - download langsung
    if mime_type in [MIME_EXCEL, MIME_EXCEL_OLD, MIME_CSV]:
        request = service.files().get_media(fileId=file_id)
        file_buffer = io.BytesIO()
        downloader = MediaIoBaseDownload(file_buffer, request)

        done = False
        while not done:
            status, done = downloader.next_chunk()

        file_buffer.seek(0)

        if mime_type == MIME_CSV or file_name.endswith('.csv'):
            if header_rows > 1 or header_row_start > 1:
                raw = pd.read_csv(file_buffer, header=None)
                # skip rows before header
                if header_row_start > 1:
                    raw = raw.iloc[header_row_start - 1:].reset_index(drop=True)
                return _dedupe_columns(_merge_multi_row_header(raw, header_rows))
            return _dedupe_columns(pd.read_csv(file_buffer))
        else:
            # Excel: header parameter 0-indexed. Baris 1 (user-facing) = index 0.
            skip_count = header_row_start - 1
            if header_rows > 1:
                header_arg = [skip_count + i for i in range(header_rows)]
            else:
                header_arg = skip_count
            if sheet_name:
                df = pd.read_excel(file_buffer, sheet_name=sheet_name, header=header_arg)
            else:
                df = pd.read_excel(file_buffer, header=header_arg)
            if header_rows > 1:
                df = _flatten_multi_header(df)
            return _dedupe_columns(df)

    # Fallback - coba export sebagai Excel
    try:
        request = service.files().export_media(
            fileId=file_id,
            mimeType='application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
        )
        file_buffer = io.BytesIO()
        downloader = MediaIoBaseDownload(file_buffer, request)
        done = False
        while not done:
            status, done = downloader.next_chunk()
        file_buffer.seek(0)
        skip_count = header_row_start - 1
        if header_rows > 1:
            header_arg = [skip_count + i for i in range(header_rows)]
        else:
            header_arg = skip_count
        if sheet_name:
            df = pd.read_excel(file_buffer, sheet_name=sheet_name, header=header_arg)
        else:
            df = pd.read_excel(file_buffer, header=header_arg)
        if header_rows > 1:
            df = _flatten_multi_header(df)
        return _dedupe_columns(df)
    except Exception as e:
        raise ValueError(f"Unsupported file type: {mime_type}. Error: {e}")

def read_google_sheet(
    file_id: str,
    sheet_name: Optional[str] = None,
    header_rows: int = 1,
    header_row_start: int = 1,
) -> pd.DataFrame:
    """Baca Google Sheets via Sheets API (lebih efisien untuk native Sheets)"""
    # Get sheet metadata
    spreadsheet = sheets_service.spreadsheets().get(spreadsheetId=file_id).execute()
    sheets = spreadsheet.get('sheets', [])
    
    if not sheets:
        raise ValueError("Spreadsheet tidak punya sheet")
    
    # Pilih sheet
    if sheet_name:
        target_sheet = None
        for s in sheets:
            if s['properties']['title'].lower() == sheet_name.lower():
                target_sheet = s
                break
        if not target_sheet:
            raise ValueError(f"Sheet '{sheet_name}' tidak ditemukan")
        range_name = target_sheet['properties']['title']
    else:
        # Default: sheet pertama
        range_name = sheets[0]['properties']['title']
    
    # Get data
    result = sheets_service.spreadsheets().values().get(
        spreadsheetId=file_id,
        range=range_name
    ).execute()
    
    values = result.get('values', [])
    
    if not values:
        return pd.DataFrame()

    # Skip baris judul di awal (header_row_start>1)
    skip = max(0, header_row_start - 1)
    values = values[skip:]
    if not values:
        return pd.DataFrame()
    
    if header_rows > 1 and len(values) > header_rows:
        # Pad semua rows ke panjang max yang ditemukan di header area
        max_cols = max(len(r) for r in values[:header_rows + 5])
        padded = [list(r) + [None] * (max_cols - len(r)) for r in values]
        raw_df = pd.DataFrame(padded)
        merged = _merge_multi_row_header(raw_df, header_rows)
        df = _dedupe_columns(merged)
    else:
        # Row 1 (setelah skip) = header, sisanya = data
        headers = values[0]
        data = values[1:]
        # Pad rows yang pendek (Sheets API skip empty trailing cells)
        max_cols = len(headers)
        padded_data = [row + [None] * (max_cols - len(row)) for row in data]
        df = pd.DataFrame(padded_data, columns=headers)
        df = _dedupe_columns(df)

    # Auto-convert numeric columns (after dedupe, aman pakai df[col])
    for col in df.columns:
        try:
            df[col] = pd.to_numeric(df[col], errors='ignore')
        except Exception:
            pass

    return df


def read_raw_rows(file_id: str, sheet_name: Optional[str] = None, max_rows: int = 10) -> List[List[Any]]:
    """Baca N baris pertama tanpa parsing header. Berguna untuk explore struktur sheet."""
    metadata = get_file_metadata(file_id)
    mime_type = metadata.get('mimeType', '')

    if mime_type == MIME_GOOGLE_SHEET:
        spreadsheet = sheets_service.spreadsheets().get(spreadsheetId=file_id).execute()
        sheets = spreadsheet.get('sheets', [])
        if not sheets:
            return []
        if sheet_name:
            target = next((s for s in sheets if s['properties']['title'].lower() == sheet_name.lower()), None)
            if not target:
                raise ValueError(f"Sheet '{sheet_name}' tidak ditemukan")
            rng = f"{target['properties']['title']}!A1:ZZ{max_rows}"
        else:
            rng = f"{sheets[0]['properties']['title']}!A1:ZZ{max_rows}"
        result = sheets_service.spreadsheets().values().get(
            spreadsheetId=file_id, range=rng
        ).execute()
        return result.get('values', [])

    # Excel / CSV: download then peek
    request = service.files().get_media(fileId=file_id) if mime_type in [MIME_EXCEL, MIME_EXCEL_OLD, MIME_CSV] \
        else service.files().export_media(fileId=file_id, mimeType=MIME_EXCEL)
    file_buffer = io.BytesIO()
    downloader = MediaIoBaseDownload(file_buffer, request)
    done = False
    while not done:
        status, done = downloader.next_chunk()
    file_buffer.seek(0)

    file_name = metadata.get('name', '')
    if mime_type == MIME_CSV or file_name.endswith('.csv'):
        df = pd.read_csv(file_buffer, header=None, nrows=max_rows)
    else:
        if sheet_name:
            df = pd.read_excel(file_buffer, sheet_name=sheet_name, header=None, nrows=max_rows)
        else:
            df = pd.read_excel(file_buffer, header=None, nrows=max_rows)

    # Convert to list of lists, replace NaN with ''
    df = df.fillna('')
    return df.values.tolist()

def get_sheet_names(file_id: str) -> List[str]:
    """Get list sheet names dari file"""
    metadata = get_file_metadata(file_id)
    mime_type = metadata.get('mimeType', '')
    
    if mime_type == MIME_GOOGLE_SHEET:
        spreadsheet = sheets_service.spreadsheets().get(spreadsheetId=file_id).execute()
        return [s['properties']['title'] for s in spreadsheet.get('sheets', [])]
    
    elif mime_type in [MIME_EXCEL, MIME_EXCEL_OLD]:
        request = service.files().get_media(fileId=file_id)
        file_buffer = io.BytesIO()
        downloader = MediaIoBaseDownload(file_buffer, request)
        done = False
        while not done:
            status, done = downloader.next_chunk()
        file_buffer.seek(0)
        xl = pd.ExcelFile(file_buffer)
        return xl.sheet_names
    
    return ['Sheet1']  # CSV default

def fuzzy_match_column(df: pd.DataFrame, search_term: str) -> Optional[str]:
    """
    Cari kolom dengan nama mirip (case-insensitive, partial match).
    Return nama kolom pertama yang match.
    """
    search_lower = search_term.lower().strip()

    # 1. Exact match (case-insensitive)
    for col in df.columns:
        if str(col).lower().strip() == search_lower:
            return col

    # 2. Kalau search term "SITE ID" dan ada dedupe "SITE ID_2", tetap match
    for col in df.columns:
        base = str(col).lower().strip()
        # Strip dedupe suffix: "site id_2" → "site id"
        if '_' in base and base.rsplit('_', 1)[-1].isdigit():
            base = base.rsplit('_', 1)[0].strip()
        if base == search_lower:
            return col

    # 3. Partial match
    for col in df.columns:
        if search_lower in str(col).lower():
            return col

    # 4. Reverse partial match (kolom ada di search)
    for col in df.columns:
        cl = str(col).lower()
        if cl and cl in search_lower:
            return col

    return None


def fuzzy_match_columns_all(df: pd.DataFrame, search_term: str) -> List[str]:
    """
    Return SEMUA kolom yang match (handle header duplikat yang sudah di-dedupe).
    Contoh: 'SITE ID' akan balikin ['SITE ID', 'SITE ID_2'] kalau keduanya ada.
    """
    search_lower = search_term.lower().strip()
    matches: List[str] = []

    def _base_name(col: str) -> str:
        base = str(col).lower().strip()
        if '_' in base and base.rsplit('_', 1)[-1].isdigit():
            base = base.rsplit('_', 1)[0].strip()
        return base

    # Exact + dedupe siblings
    for col in df.columns:
        if _base_name(col) == search_lower or str(col).lower().strip() == search_lower:
            matches.append(col)

    # Kalau belum ketemu, coba partial
    if not matches:
        for col in df.columns:
            if search_lower in str(col).lower():
                matches.append(col)

    return matches

def _ensure_series(obj, col_name: str) -> pd.Series:
    """Kalau obj adalah DataFrame (karena duplicate columns), ambil kolom pertama."""
    if isinstance(obj, pd.DataFrame):
        return obj.iloc[:, 0]
    return obj


def filter_dataframe(df: pd.DataFrame, filters: Dict[str, Any]) -> pd.DataFrame:
    """
    Filter DataFrame berdasarkan kriteria dengan fuzzy column matching.
    Kalau search term match ke beberapa kolom (karena dedupe), pakai OR across columns.
    """
    filtered_df = df.copy()

    for search_col, value in filters.items():
        candidate_cols = fuzzy_match_columns_all(filtered_df, search_col)
        if not candidate_cols:
            continue  # skip kalau kolom tidak ditemukan

        # Build combined mask across all matching columns (OR)
        combined_mask = None

        for actual_col in candidate_cols:
            col_data = _ensure_series(filtered_df[actual_col], actual_col)

            try:
                if isinstance(value, list):
                    lookup = [str(v).lower() for v in value]
                    mask = col_data.astype(str).str.lower().isin(lookup)
                elif isinstance(value, dict):
                    mask = pd.Series(True, index=filtered_df.index)
                    if 'min' in value:
                        try:
                            mask &= (col_data >= value['min'])
                        except Exception:
                            mask &= (col_data.astype(str) >= str(value['min']))
                    if 'max' in value:
                        try:
                            mask &= (col_data <= value['max'])
                        except Exception:
                            mask &= (col_data.astype(str) <= str(value['max']))
                else:
                    # Partial case-insensitive match untuk string, exact untuk numeric
                    if pd.api.types.is_numeric_dtype(col_data):
                        try:
                            mask = (col_data == value)
                        except Exception:
                            mask = col_data.astype(str).str.contains(
                                str(value), case=False, na=False, regex=False)
                    else:
                        mask = col_data.astype(str).str.contains(
                            str(value), case=False, na=False, regex=False)
            except Exception as e:
                # Skip kolom yang error, jangan crash seluruh filter
                print(f"[filter] warning: skip column '{actual_col}': {e}")
                continue

            combined_mask = mask if combined_mask is None else (combined_mask | mask)

        if combined_mask is not None:
            filtered_df = filtered_df[combined_mask]

    return filtered_df

@app.list_tools()
async def list_tools() -> list[Tool]:
    """List available tools"""
    return [
        Tool(
            name="list_drive_files",
            description="List files di Google Drive. Bisa filter by name, folder, atau file type (Excel/Sheets/Folder).",
            inputSchema={
                "type": "object",
                "properties": {
                    "query": {
                        "type": "string",
                        "description": "Search query. Contoh: 'name contains \"BOQ\"' atau 'mimeType=\"application/vnd.google-apps.spreadsheet\"'"
                    },
                    "folder_id": {
                        "type": "string",
                        "description": "Folder ID untuk browse isi folder tertentu"
                    },
                    "file_type": {
                        "type": "string",
                        "enum": ["all", "sheets", "excel", "folder", "spreadsheet"],
                        "description": "Filter by tipe: 'sheets' (Google Sheets), 'excel' (xlsx), 'spreadsheet' (semua tipe spreadsheet), 'folder', 'all'"
                    },
                    "page_size": {
                        "type": "integer",
                        "description": "Jumlah file yang di-return (default 30, max 100)"
                    }
                }
            }
        ),
        Tool(
            name="list_folder_contents",
            description="List semua file & subfolder di dalam folder tertentu. Cocok untuk explore folder yang di-share.",
            inputSchema={
                "type": "object",
                "properties": {
                    "folder_id": {
                        "type": "string",
                        "description": "Folder ID. Kalau kosong, list semua file yang di-share ke service account."
                    },
                    "recursive": {
                        "type": "boolean",
                        "description": "Kalau true, include subfolders juga (default false)"
                    }
                }
            }
        ),
        Tool(
            name="get_sheet_headers",
            description="Ambil HANYA header/kolom + info dasar dari file (super hemat token!). Support Excel, Google Sheets, CSV.",
            inputSchema={
                "type": "object",
                "properties": {
                    "file_id": {
                        "type": "string",
                        "description": "Google Drive file ID"
                    },
                    "sheet_name": {
                        "type": "string",
                        "description": "Nama sheet (optional, untuk file multi-sheet)"
                    },
                    "header_rows": {
                        "type": "integer",
                        "description": "Jumlah baris header (default 1). Pakai 2+ kalau sheet punya group+sub header (mis. 'Kabel 24c' di baris 1, 'Plan/Progress/%' di baris 2)."
                    },
                    "header_row_start": {
                        "type": "integer",
                        "description": "Baris pertama (1-indexed) yang jadi header. Default 1. Pakai 3 kalau 2 baris atas cuma judul/merged title, header sebenarnya mulai dari baris 3."
                    }
                },
                "required": ["file_id"]
            }
        ),
        Tool(
            name="preview_raw_rows",
            description="Baca N baris pertama MENTAH (tanpa parsing header). Cara cepat ngintip struktur sheet — identifikasi di baris mana header sebenarnya berada.",
            inputSchema={
                "type": "object",
                "properties": {
                    "file_id": {"type": "string"},
                    "sheet_name": {"type": "string"},
                    "max_rows": {"type": "integer", "description": "default 10, max 50"}
                },
                "required": ["file_id"]
            }
        ),
        Tool(
            name="list_sheet_tabs",
            description="List semua tab/sheet di dalam file Excel atau Google Sheets.",
            inputSchema={
                "type": "object",
                "properties": {
                    "file_id": {
                        "type": "string",
                        "description": "Google Drive file ID"
                    }
                },
                "required": ["file_id"]
            }
        ),
        Tool(
            name="get_sheet_summary",
            description="Ambil summary statistik dari file (unique values, min/max, count). Hemat token untuk eksplorasi data.",
            inputSchema={
                "type": "object",
                "properties": {
                    "file_id": {"type": "string"},
                    "sheet_name": {"type": "string"},
                    "columns": {"type": "array", "items": {"type": "string"}},
                    "header_rows": {"type": "integer", "description": "default 1"},
                    "header_row_start": {"type": "integer", "description": "default 1"}
                },
                "required": ["file_id"]
            }
        ),
        Tool(
            name="read_and_filter_sheet",
            description="Baca file Excel/Sheets/CSV dari Drive dan filter data. HEMAT TOKEN dengan pre-filtering di server!",
            inputSchema={
                "type": "object",
                "properties": {
                    "file_id": {"type": "string"},
                    "sheet_name": {"type": "string"},
                    "filters": {"type": "object", "description": "mis. {'tanggal': {'min': '2024-01-01'}, 'segment': 'FWA'}"},
                    "columns": {"type": "array", "items": {"type": "string"}},
                    "limit": {"type": "integer", "description": "default 100"},
                    "header_rows": {"type": "integer", "description": "default 1"},
                    "header_row_start": {"type": "integer", "description": "default 1"}
                },
                "required": ["file_id"]
            }
        )
    ]

@app.call_tool()
async def call_tool(name: str, arguments: Any) -> list[TextContent]:
    """Handle tool calls"""
    
    if service is None:
        init_services()
    
    if name == "list_drive_files":
        query = arguments.get("query", "")
        folder_id = arguments.get("folder_id")
        file_type = arguments.get("file_type", "all")
        page_size = min(arguments.get("page_size", 30), 100)
        
        q_parts = []
        if query:
            q_parts.append(query)
        if folder_id:
            q_parts.append(f"'{folder_id}' in parents")
        
        # Filter by file type
        if file_type == "sheets":
            q_parts.append(f"mimeType='{MIME_GOOGLE_SHEET}'")
        elif file_type == "excel":
            q_parts.append(f"(mimeType='{MIME_EXCEL}' or mimeType='{MIME_EXCEL_OLD}')")
        elif file_type == "spreadsheet":
            q_parts.append(f"(mimeType='{MIME_GOOGLE_SHEET}' or mimeType='{MIME_EXCEL}' or mimeType='{MIME_EXCEL_OLD}' or mimeType='{MIME_CSV}')")
        elif file_type == "folder":
            q_parts.append(f"mimeType='{MIME_GOOGLE_FOLDER}'")
        
        # Exclude trashed
        q_parts.append("trashed=false")
        
        q = " and ".join(q_parts) if q_parts else "trashed=false"
        
        results = service.files().list(
            q=q,
            pageSize=page_size,
            fields="files(id, name, mimeType, modifiedTime, size, parents)"
        ).execute()
        
        files = results.get('files', [])
        
        # Tambah label tipe file yang mudah dibaca
        for f in files:
            mime = f.get('mimeType', '')
            if mime == MIME_GOOGLE_SHEET:
                f['type'] = 'Google Sheets'
            elif mime == MIME_GOOGLE_FOLDER:
                f['type'] = 'Folder'
            elif mime in [MIME_EXCEL, MIME_EXCEL_OLD]:
                f['type'] = 'Excel'
            elif mime == MIME_CSV:
                f['type'] = 'CSV'
            else:
                f['type'] = mime
        
        return [TextContent(
            type="text",
            text=json.dumps({
                "total": len(files),
                "files": files
            }, indent=2)
        )]
    
    elif name == "list_folder_contents":
        folder_id = arguments.get("folder_id")
        recursive = arguments.get("recursive", False)
        
        if folder_id:
            q = f"'{folder_id}' in parents and trashed=false"
        else:
            # List semua file yang di-share (tidak di folder tertentu)
            q = "trashed=false"
        
        results = service.files().list(
            q=q,
            pageSize=100,
            fields="files(id, name, mimeType, modifiedTime, size, parents)"
        ).execute()
        
        files = results.get('files', [])
        
        # Separate folders and files
        folders = []
        sheets_files = []
        other_files = []
        
        for f in files:
            mime = f.get('mimeType', '')
            if mime == MIME_GOOGLE_FOLDER:
                f['type'] = 'Folder'
                folders.append(f)
            elif mime == MIME_GOOGLE_SHEET:
                f['type'] = 'Google Sheets'
                sheets_files.append(f)
            elif mime in [MIME_EXCEL, MIME_EXCEL_OLD]:
                f['type'] = 'Excel'
                sheets_files.append(f)
            elif mime == MIME_CSV:
                f['type'] = 'CSV'
                sheets_files.append(f)
            else:
                f['type'] = mime
                other_files.append(f)
        
        result = {
            "folder_id": folder_id or "root (shared files)",
            "summary": {
                "folders": len(folders),
                "spreadsheets": len(sheets_files),
                "other_files": len(other_files)
            },
            "folders": folders,
            "spreadsheets": sheets_files,
            "other_files": other_files
        }
        
        # Recursive: fetch subfolders
        if recursive and folders:
            result["subfolders_contents"] = {}
            for folder in folders[:5]:  # Max 5 subfolders
                sub_results = service.files().list(
                    q=f"'{folder['id']}' in parents and trashed=false",
                    pageSize=50,
                    fields="files(id, name, mimeType)"
                ).execute()
                result["subfolders_contents"][folder['name']] = sub_results.get('files', [])
        
        return [TextContent(
            type="text",
            text=json.dumps(result, indent=2)
        )]
    
    elif name == "list_sheet_tabs":
        file_id = arguments["file_id"]
        metadata = get_file_metadata(file_id)
        sheet_names = get_sheet_names(file_id)
        
        return [TextContent(
            type="text",
            text=json.dumps({
                "file_name": metadata.get('name'),
                "file_type": metadata.get('mimeType'),
                "sheets": sheet_names,
                "total_sheets": len(sheet_names)
            }, indent=2)
        )]

    elif name == "preview_raw_rows":
        file_id = arguments["file_id"]
        sheet_name = arguments.get("sheet_name")
        max_rows = min(int(arguments.get("max_rows", 10) or 10), 50)
        rows = read_raw_rows(file_id, sheet_name, max_rows)
        return [TextContent(
            type="text",
            text=json.dumps(_sanitize_for_json({"rows": rows, "total_returned": len(rows)}), default=str)
        )]
    
    elif name == "get_sheet_headers":
        file_id = arguments["file_id"]
        sheet_name = arguments.get("sheet_name")
        header_rows = int(arguments.get("header_rows", 1) or 1)
        header_row_start = int(arguments.get("header_row_start", 1) or 1)
        
        metadata = get_file_metadata(file_id)
        df = download_file_to_df(file_id, sheet_name, header_rows=header_rows,
                                  header_row_start=header_row_start)
        
        headers = {
            "file_name": metadata.get('name'),
            "sheet_name": sheet_name or "default",
            "columns": list(df.columns),
            "total_rows": len(df),
            "total_columns": len(df.columns),
            "dtypes": {str(col): str(dtype) for col, dtype in df.dtypes.items()},
            "sample_first_row": _sanitize_for_json(df.iloc[0].to_dict()) if len(df) > 0 else {}
        }
        
        return [TextContent(
            type="text",
            text=json.dumps(headers, indent=2, default=str)
        )]
    
    elif name == "get_sheet_summary":
        file_id = arguments["file_id"]
        sheet_name = arguments.get("sheet_name")
        header_rows = int(arguments.get("header_rows", 1) or 1)
        header_row_start = int(arguments.get("header_row_start", 1) or 1)
        df = download_file_to_df(file_id, sheet_name, header_rows=header_rows,
                                  header_row_start=header_row_start)
        
        columns = arguments.get("columns")
        if columns:
            actual_cols = [fuzzy_match_column(df, c) for c in columns]
            actual_cols = [c for c in actual_cols if c is not None]
            if actual_cols:
                df = df[actual_cols]
        
        summary = {
            "shape": {"rows": len(df), "columns": len(df.columns)},
            "columns": {}
        }
        
        for col in df.columns:
            series = _ensure_series(df[col], col)
            try:
                col_summary = {
                    "dtype": str(series.dtype),
                    "non_null": int(series.count()),
                    "null": int(series.isna().sum())
                }
            except Exception:
                col_summary = {"dtype": "unknown", "non_null": 0, "null": 0, "error": "could not compute basic stats"}
                summary["columns"][str(col)] = col_summary
                continue

            try:
                if pd.api.types.is_numeric_dtype(series):
                    col_summary["min"] = float(series.min())
                    col_summary["max"] = float(series.max())
                    col_summary["mean"] = float(series.mean())
                    col_summary["sum"] = float(series.sum())
                else:
                    unique_vals = series.dropna().unique()
                    col_summary["unique_count"] = len(unique_vals)
                    if len(unique_vals) <= 30:
                        col_summary["unique_values"] = [str(v) for v in unique_vals]
                    else:
                        top = series.value_counts().head(10)
                        col_summary["top_values"] = {str(k): int(v) for k, v in top.items()}
            except Exception:
                pass

            summary["columns"][str(col)] = col_summary
        
        return [TextContent(
            type="text",
            text=json.dumps(_sanitize_for_json(summary), indent=2, default=str)
        )]
    
    elif name == "read_and_filter_sheet":
        file_id = arguments["file_id"]
        sheet_name = arguments.get("sheet_name")
        filters = arguments.get("filters", {})
        columns = arguments.get("columns")
        limit = arguments.get("limit", 100)
        header_rows = int(arguments.get("header_rows", 1) or 1)
        header_row_start = int(arguments.get("header_row_start", 1) or 1)
        
        df = download_file_to_df(file_id, sheet_name, header_rows=header_rows,
                                  header_row_start=header_row_start)
        original_count = len(df)
        
        if filters:
            df = filter_dataframe(df, filters)
        
        filtered_count = len(df)
        
        if columns:
            actual_columns = []
            for col in columns:
                actual_col = fuzzy_match_column(df, col)
                if actual_col:
                    actual_columns.append(actual_col)
            if actual_columns:
                df = df[actual_columns]
        
        df = df.head(limit)
        
        result = {
            "original_rows": original_count,
            "rows_after_filter": filtered_count,
            "rows_returned": len(df),
            "columns": [str(c) for c in df.columns],
            "data": _sanitize_for_json(df.to_dict(orient='records'))
        }
        
        return [TextContent(
            type="text",
            text=json.dumps(result, indent=2, default=str)
        )]
    
    else:
        raise ValueError(f"Unknown tool: {name}")

async def main():
    """Run the MCP server"""
    async with mcp.server.stdio.stdio_server() as (read_stream, write_stream):
        await app.run(
            read_stream,
            write_stream,
            app.create_initialization_options()
        )

if __name__ == "__main__":
    import asyncio
    asyncio.run(main())
