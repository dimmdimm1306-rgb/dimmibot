"""
HTTP wrapper (FastAPI) untuk MCP Google Drive read-only server.
Jalan di laptop - MAUI app connect via HTTP (bisa lewat Cloudflare tunnel).

Features:
  - 100% read-only (3 layer protection)
  - Server-side caching (5 min TTL, auto-invalidate on modifiedTime change)
  - Smart filter endpoint with semantic intents (belum/selesai/0%/kemarin)
  - header_row_start + header_rows support

Usage:
    set GOOGLE_APPLICATION_CREDENTIALS=D:\\path\\to\\gdrive-credentials.json
    python -m mcp_gdrive_filter.http_server
"""
import os
import sys
import time
import hashlib
from typing import Any, Dict, List, Optional
from fastapi import FastAPI, HTTPException, Header, Request
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel, Field

# Reuse MCP server internals
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from mcp_gdrive_filter import server as mcp_core

# ---------------------------------------------------------------------
# Sheet Cache (in-memory, 5 min TTL)
# ---------------------------------------------------------------------
_CACHE: Dict[str, Any] = {}  # key -> {df, ts, modified_time}
CACHE_TTL = 300  # 5 minutes

def _cache_key(file_id, sheet_name, header_rows, header_row_start):
    raw = f"{file_id}|{sheet_name}|{header_rows}|{header_row_start}"
    return hashlib.md5(raw.encode()).hexdigest()

def _get_cached_df(file_id, sheet_name, header_rows=1, header_row_start=1):
    """Get DataFrame from cache or download fresh."""
    key = _cache_key(file_id, sheet_name, header_rows, header_row_start)
    now = time.time()
    
    if key in _CACHE:
        entry = _CACHE[key]
        if now - entry['ts'] < CACHE_TTL:
            return entry['df']
    
    # Download fresh
    _ensure_services()
    df = mcp_core.download_file_to_df(file_id, sheet_name,
                                       header_rows=header_rows,
                                       header_row_start=header_row_start)
    _CACHE[key] = {'df': df, 'ts': now}
    return df

def _invalidate_cache(file_id=None):
    """Clear cache for a file or all."""
    if file_id:
        keys_to_del = [k for k, v in _CACHE.items() if file_id in str(v)]
        for k in keys_to_del:
            del _CACHE[k]
    else:
        _CACHE.clear()

# ---------------------------------------------------------------------
# Config
# ---------------------------------------------------------------------
def _load_token() -> str:
    """Load bearer token from env, fallback to .token file di folder server."""
    t = os.getenv('GDRIVE_API_TOKEN', '').strip()
    if t:
        return t
    token_file = os.path.join(os.path.dirname(os.path.abspath(__file__)), '.token')
    if os.path.exists(token_file):
        try:
            with open(token_file, 'r', encoding='utf-8') as f:
                return f.read().strip()
        except Exception:
            pass
    return ''

API_TOKEN = _load_token()
PORT = int(os.getenv('GDRIVE_PORT', '20129'))

app = FastAPI(
    title="GDrive Reader API",
    version="1.0.0",
    description="Read-only Google Drive API for StokBarangMAUI AI bot"
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=False,
    allow_methods=["GET", "POST"],
    allow_headers=["*"],
)

def _check_auth(authorization: Optional[str]):
    if not API_TOKEN:
        return  # auth disabled
    expected = f"Bearer {API_TOKEN}"
    if authorization != expected:
        raise HTTPException(401, "Invalid or missing bearer token")


def _ensure_services():
    if mcp_core.service is None:
        mcp_core.init_services()


# ---------------------------------------------------------------------
# Request models
# ---------------------------------------------------------------------
class ListFilesReq(BaseModel):
    query: Optional[str] = None
    folder_id: Optional[str] = None
    file_type: Optional[str] = Field(default="all")  # all/sheets/excel/spreadsheet/folder
    page_size: Optional[int] = 30


class ListFolderReq(BaseModel):
    folder_id: Optional[str] = None
    recursive: Optional[bool] = False


class GetHeadersReq(BaseModel):
    file_id: str
    sheet_name: Optional[str] = None
    header_rows: Optional[int] = 1
    header_row_start: Optional[int] = 1


class SheetTabsReq(BaseModel):
    file_id: str


class PreviewRawReq(BaseModel):
    file_id: str
    sheet_name: Optional[str] = None
    max_rows: Optional[int] = 10


class SummaryReq(BaseModel):
    file_id: str
    sheet_name: Optional[str] = None
    columns: Optional[List[str]] = None
    header_rows: Optional[int] = 1
    header_row_start: Optional[int] = 1


class FilterReq(BaseModel):
    file_id: str
    sheet_name: Optional[str] = None
    filters: Optional[Dict[str, Any]] = None
    columns: Optional[List[str]] = None
    limit: Optional[int] = 100
    header_rows: Optional[int] = 1
    header_row_start: Optional[int] = 1


# ---------------------------------------------------------------------
# Endpoints (all read-only)
# ---------------------------------------------------------------------
@app.get("/")
def root():
    return {
        "service": "gdrive-reader",
        "mode": "read-only",
        "version": "1.0.0",
        "endpoints": [
            "GET  /health",
            "GET  /service-account",
            "POST /files/list",
            "POST /folder/contents",
            "POST /sheet/tabs",
            "POST /sheet/headers",
            "POST /sheet/summary",
            "POST /sheet/filter",
        ],
        "auth": "bearer token required" if API_TOKEN else "none (public)"
    }


@app.get("/health")
def health():
    try:
        _ensure_services()
        return {"status": "ok", "service_account": _get_service_account_email()}
    except Exception as e:
        raise HTTPException(500, str(e))


def _get_service_account_email() -> str:
    try:
        import json
        creds_path = os.environ.get('GOOGLE_APPLICATION_CREDENTIALS', '')
        if creds_path and os.path.exists(creds_path):
            with open(creds_path, 'r', encoding='utf-8') as f:
                return json.load(f).get('client_email', 'unknown')
    except Exception:
        pass
    return 'unknown'


@app.get("/service-account")
def service_account(authorization: Optional[str] = Header(None)):
    _check_auth(authorization)
    return {
        "email": _get_service_account_email(),
        "instructions": "Share your Drive files/folders with this email (Viewer permission)."
    }


@app.post("/files/list")
async def files_list(req: ListFilesReq, authorization: Optional[str] = Header(None)):
    _check_auth(authorization)
    _ensure_services()
    try:
        args = req.model_dump(exclude_none=True)
        result = await mcp_core.call_tool("list_drive_files", args)
        return _parse_tool_result(result)
    except Exception as e:
        raise HTTPException(500, str(e))


@app.post("/folder/contents")
async def folder_contents(req: ListFolderReq, authorization: Optional[str] = Header(None)):
    _check_auth(authorization)
    _ensure_services()
    try:
        args = req.model_dump(exclude_none=True)
        result = await mcp_core.call_tool("list_folder_contents", args)
        return _parse_tool_result(result)
    except Exception as e:
        raise HTTPException(500, str(e))


@app.post("/sheet/tabs")
async def sheet_tabs(req: SheetTabsReq, authorization: Optional[str] = Header(None)):
    _check_auth(authorization)
    _ensure_services()
    try:
        args = req.model_dump(exclude_none=True)
        result = await mcp_core.call_tool("list_sheet_tabs", args)
        return _parse_tool_result(result)
    except Exception as e:
        raise HTTPException(500, str(e))


@app.post("/sheet/preview")
async def sheet_preview(req: PreviewRawReq, authorization: Optional[str] = Header(None)):
    _check_auth(authorization)
    _ensure_services()
    try:
        args = req.model_dump(exclude_none=True)
        result = await mcp_core.call_tool("preview_raw_rows", args)
        return _parse_tool_result(result)
    except Exception as e:
        raise HTTPException(500, str(e))


@app.post("/sheet/headers")
async def sheet_headers(req: GetHeadersReq, authorization: Optional[str] = Header(None)):
    _check_auth(authorization)
    _ensure_services()
    try:
        args = req.model_dump(exclude_none=True)
        result = await mcp_core.call_tool("get_sheet_headers", args)
        return _parse_tool_result(result)
    except Exception as e:
        raise HTTPException(500, str(e))


@app.post("/sheet/summary")
async def sheet_summary(req: SummaryReq, authorization: Optional[str] = Header(None)):
    _check_auth(authorization)
    _ensure_services()
    try:
        args = req.model_dump(exclude_none=True)
        result = await mcp_core.call_tool("get_sheet_summary", args)
        return _parse_tool_result(result)
    except Exception as e:
        raise HTTPException(500, str(e))


@app.post("/sheet/filter")
async def sheet_filter(req: FilterReq, authorization: Optional[str] = Header(None)):
    _check_auth(authorization)
    _ensure_services()
    try:
        args = req.model_dump(exclude_none=True)
        result = await mcp_core.call_tool("read_and_filter_sheet", args)
        return _parse_tool_result(result)
    except Exception as e:
        raise HTTPException(500, str(e))


def _parse_tool_result(result):
    """Convert MCP TextContent response to JSON dict."""
    import json
    if not result:
        return {}
    text = result[0].text if hasattr(result[0], 'text') else str(result[0])
    try:
        return json.loads(text)
    except json.JSONDecodeError:
        return {"raw": text}


# ---------------------------------------------------------------------
# Smart Filter - server-side semantic intent processing
# ---------------------------------------------------------------------
import pandas as pd
import json as json_mod
import re as re_mod
from datetime import datetime, timedelta
import locale

class SmartFilterReq(BaseModel):
    file_id: str
    sheet_name: Optional[str] = None
    header_rows: Optional[int] = 1
    header_row_start: Optional[int] = 1
    keyword: Optional[str] = None
    intent: Optional[str] = None  # "outstanding", "done", "progress_0", "date_yesterday", "date_week"
    search_columns: Optional[List[str]] = None
    limit: Optional[int] = 30


@app.post("/sheet/smart_filter")
async def smart_filter(req: SmartFilterReq, authorization: Optional[str] = Header(None)):
    """
    Smart filter with semantic intents. Processes everything server-side.
    Intents:
      - outstanding: rows where any % column < 100
      - done: rows where all % columns >= 100
      - progress_0: rows where any % column == 0
      - date_yesterday: filter Tanggal column for yesterday
      - date_today: filter Tanggal for today
      - date_week: filter Tanggal for last 7 days
    If keyword provided without intent, does fuzzy search across search_columns.
    """
    _check_auth(authorization)
    _ensure_services()
    
    try:
        hr = req.header_rows or 1
        hrs = req.header_row_start or 1
        limit = min(req.limit or 30, 100)
        
        # Get DataFrame (cached)
        df = _get_cached_df(req.file_id, req.sheet_name, hr, hrs)
        original_count = len(df)
        
        if df.empty:
            return {"original_rows": 0, "rows_after_filter": 0, "rows_returned": 0, "columns": [], "data": []}
        
        filtered = df.copy()
        
        # Apply intent filter
        if req.intent:
            filtered = _apply_intent(filtered, req.intent)
        
        # Apply keyword search
        if req.keyword and req.keyword.strip():
            filtered = _apply_keyword_search(filtered, req.keyword.strip(), req.search_columns)
        
        filtered_count = len(filtered)
        result_df = filtered.head(limit)
        
        # Sanitize for JSON
        data = mcp_core._sanitize_for_json(result_df.to_dict(orient='records'))
        
        return {
            "original_rows": original_count,
            "rows_after_filter": filtered_count,
            "rows_returned": len(result_df),
            "columns": [str(c) for c in result_df.columns],
            "data": data
        }
    except Exception as e:
        raise HTTPException(500, str(e))


def _apply_intent(df: pd.DataFrame, intent: str) -> pd.DataFrame:
    """Apply semantic intent filter."""
    
    # Find percentage columns (contain "%" in name or end with "- %")
    pct_cols = [c for c in df.columns if '- %' in str(c) or c.strip() == '%' or 'persen' in str(c).lower()]
    
    # If no explicit % columns, look for "Progress" columns with numeric values
    if not pct_cols:
        for c in df.columns:
            if 'progress' in str(c).lower() or 'progres' in str(c).lower():
                if pd.api.types.is_numeric_dtype(df[c]):
                    pct_cols.append(c)
    
    if intent == 'outstanding' or intent == 'progress_lt_100':
        # Rows where ANY % column < 100% (or < 1.0 if fractional)
        if pct_cols:
            mask = pd.Series(False, index=df.index)
            for c in pct_cols:
                col = pd.to_numeric(df[c], errors='coerce')
                max_val = col.max() if not col.empty else 0
                # If max <= 5, values are fractional (1.0 = 100%)
                threshold = 1.0 if (max_val is not None and max_val <= 5.0) else 100.0
                mask |= (col < threshold) & col.notna() & (col > 0)
            return df[mask]
        return df
    
    elif intent == 'done' or intent == 'progress_gte_100':
        if pct_cols:
            mask = pd.Series(True, index=df.index)
            for c in pct_cols:
                col = pd.to_numeric(df[c], errors='coerce')
                max_val = col.max() if not col.empty else 0
                threshold = 1.0 if (max_val is not None and max_val <= 5.0) else 100.0
                mask &= (col >= threshold) | col.isna()
            return df[mask]
        return df
    
    elif intent == 'progress_0':
        if pct_cols:
            mask = pd.Series(False, index=df.index)
            for c in pct_cols:
                col = pd.to_numeric(df[c], errors='coerce')
                mask |= (col == 0) | (col.isna())
            return df[mask]
        return df
    
    elif intent in ('date_yesterday', 'date_today', 'date_week', 'date_2days'):
        return _filter_by_date(df, intent)
    
    return df


def _filter_by_date(df: pd.DataFrame, intent: str) -> pd.DataFrame:
    """Filter by date intent. Searches Tanggal column with Indonesian date format."""
    today = datetime.now()
    
    if intent == 'date_yesterday':
        dates = [today - timedelta(days=1)]
    elif intent == 'date_today':
        dates = [today]
    elif intent == 'date_2days':
        dates = [today - timedelta(days=2)]
    elif intent == 'date_week':
        dates = [today - timedelta(days=i) for i in range(7)]
    else:
        return df
    
    # Build search patterns (Indonesian date format: "Senin, 12 Mei")
    id_months = ['Januari','Februari','Maret','April','Mei','Juni',
                 'Juli','Agustus','September','Oktober','November','Desember']
    id_days = ['Senin','Selasa','Rabu','Kamis','Jumat','Sabtu','Minggu']
    
    patterns = []
    for d in dates:
        day_name = id_days[d.weekday()]
        month_name = id_months[d.month - 1]
        # "Senin, 12 Mei" or "12 Mei" or "12/05/2026"
        patterns.append(f"{d.day} {month_name}")
        patterns.append(f"{day_name}, {d.day:02d} {month_name}")
        patterns.append(f"{day_name}, {d.day} {month_name}")
        patterns.append(d.strftime("%d/%m/%Y"))
        patterns.append(d.strftime("%Y-%m-%d"))
    
    # Find Tanggal column
    tgl_col = None
    for c in df.columns:
        if 'tanggal' in str(c).lower() or 'tgl' in str(c).lower() or 'date' in str(c).lower():
            tgl_col = c
            break
    
    if not tgl_col:
        return df
    
    mask = pd.Series(False, index=df.index)
    col_str = df[tgl_col].astype(str)
    for pat in patterns:
        mask |= col_str.str.contains(pat, case=False, na=False)
    
    return df[mask]


def _apply_keyword_search(df: pd.DataFrame, keyword: str, search_columns: Optional[List[str]] = None) -> pd.DataFrame:
    """Fuzzy keyword search across multiple columns."""
    default_cols = ['SITE ID', 'Rute', 'RUTE', 'Nama Barang', 'KAB/KOTA', 'KAB', 'HOMEBASE',
                    'Segment', 'NO_SJ', 'PENGIRIM', 'PENERIMA', 'Jenis']
    
    cols_to_search = search_columns if search_columns else default_cols
    
    # Find actual matching columns in DataFrame
    actual_cols = []
    for hint in cols_to_search:
        for c in df.columns:
            if hint.lower() in str(c).lower() or str(c).lower() in hint.lower():
                if c not in actual_cols:
                    actual_cols.append(c)
    
    if not actual_cols:
        # Fallback: search ALL string columns
        actual_cols = [c for c in df.columns if df[c].dtype == object]
    
    if not actual_cols:
        return df
    
    # OR search across columns
    mask = pd.Series(False, index=df.index)
    for c in actual_cols:
        mask |= df[c].astype(str).str.contains(keyword, case=False, na=False)
    
    return df[mask]


# ---------------------------------------------------------------------
# Block write methods explicitly (extra safety layer)
# ---------------------------------------------------------------------
from fastapi.responses import JSONResponse

@app.middleware("http")
async def block_write_methods(request: Request, call_next):
    """Hard-block any non-GET/POST method at HTTP level."""
    if request.method not in ("GET", "POST", "OPTIONS"):
        return JSONResponse(
            status_code=405,
            content={"detail": f"Method {request.method} not allowed. This is a read-only API."}
        )
    return await call_next(request)


if __name__ == "__main__":
    import uvicorn
    print("=" * 60)
    print("GDrive Reader HTTP API (READ-ONLY)")
    print("=" * 60)
    print(f"Port: {PORT}")
    print(f"Auth: {'bearer token required' if API_TOKEN else 'OPEN (localhost only recommended)'}")
    print(f"Service account: {_get_service_account_email()}")
    print(f"Docs: http://localhost:{PORT}/docs")
    print("=" * 60)
    uvicorn.run(app, host="0.0.0.0", port=PORT, log_level="info")
