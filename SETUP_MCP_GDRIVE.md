# Setup MCP Google Drive Filter Server

## 🎯 Tujuan
Hemat token dengan pre-filtering data Excel/Sheets dari Google Drive sebelum dikirim ke AI.

## 📋 Fitur
1. **Fuzzy Column Matching** - Cari kolom dengan nama mirip (case-insensitive)
2. **Smart Filtering** - Filter data sebelum kirim ke AI
3. **Header Only Mode** - Ambil header saja (super hemat!)
4. **Summary Stats** - Lihat statistik tanpa load semua data

## 🚀 Cara Setup

### 1. Install Dependencies
```bash
cd "d:\!FTTH\Program\UPLOAD DOKUMEN\StokBarangMAUI\mcp_gdrive_filter"
pip install -r requirements.txt
```

### 2. Setup Google Drive API

#### A. Buat Service Account
1. Buka [Google Cloud Console](https://console.cloud.google.com/)
2. Buat project baru atau pilih existing
3. Enable **Google Drive API**
4. Buat **Service Account**:
   - IAM & Admin → Service Accounts → Create Service Account
   - Nama: `mcp-gdrive-reader`
   - Role: Tidak perlu role khusus
5. Create Key (JSON) dan download

#### B. Share Drive Files
- Share folder/file Google Drive Anda dengan email service account
- Email format: `mcp-gdrive-reader@your-project.iam.gserviceaccount.com`
- Permission: **Viewer** (read-only)

### 3. Konfigurasi Credentials

Edit file `.kiro/settings/mcp.json`:
```json
{
  "mcpServers": {
    "gdrive-reader": {
      "command": "python",
      "args": ["-m", "mcp_gdrive_filter.server"],
      "env": {
        "GOOGLE_APPLICATION_CREDENTIALS": "D:\\path\\to\\your\\credentials.json"
      },
      "disabled": false,
      "autoApprove": ["read_and_filter_sheet", "get_sheet_headers", "get_sheet_summary"]
    }
  }
}
```

**Ganti path credentials dengan lokasi file JSON Anda!**

### 4. Restart Kiro
- Restart Kiro atau reconnect MCP server dari MCP Server view

## 📖 Cara Pakai

### Contoh 1: Ambil Header Saja (Hemat Token!)
```
AI, tolong ambil header dari file BOQ FWA di Drive
File ID: 1abc...xyz
```

AI akan panggil:
```json
{
  "tool": "get_sheet_headers",
  "file_id": "1abc...xyz"
}
```

### Contoh 2: Filter Data Sebelum Kirim
```
AI, ambil data dari BOQ FWA:
- Segment: FWA
- Tanggal: setelah 2024-01-01
- Rute: A atau B
- Limit: 50 baris
```

AI akan panggil:
```json
{
  "tool": "read_and_filter_sheet",
  "file_id": "1abc...xyz",
  "filters": {
    "segment": "FWA",
    "tanggal": {"min": "2024-01-01"},
    "rute": ["A", "B"]
  },
  "limit": 50
}
```

### Contoh 3: Summary Statistik
```
AI, berikan summary dari kolom segment dan rute
```

AI akan panggil:
```json
{
  "tool": "get_sheet_summary",
  "file_id": "1abc...xyz",
  "columns": ["segment", "rute"]
}
```

## 🔍 Cara Dapat File ID

### Dari URL Google Drive:
```
https://drive.google.com/file/d/1abc...xyz/view
                              ^^^^^^^^^ ini File ID
```

### Atau List Files:
```
AI, list semua file Excel di Drive saya
```

## 💡 Tips Hemat Token

1. **Selalu ambil header dulu** - Lihat struktur data sebelum query
2. **Filter di server** - Jangan load semua data lalu filter di AI
3. **Limit rows** - Default 100, sesuaikan kebutuhan
4. **Pilih kolom** - Hanya ambil kolom yang diperlukan
5. **Summary first** - Lihat statistik dulu sebelum ambil data detail

## 🎯 Workflow Efisien

```
1. get_sheet_headers → Lihat struktur
2. get_sheet_summary → Lihat statistik
3. read_and_filter_sheet → Ambil data yang difilter
```

## ⚠️ Troubleshooting

### Error: "GOOGLE_APPLICATION_CREDENTIALS not set"
- Pastikan path di `mcp.json` benar
- Gunakan absolute path dengan double backslash `\\`

### Error: "Permission denied"
- Share file/folder dengan service account email
- Cek permission minimal **Viewer**

### Error: "File not found"
- Cek File ID benar
- Pastikan file sudah di-share

### MCP Server tidak muncul
- Restart Kiro
- Cek `mcp.json` syntax valid
- Lihat log di MCP Server view

## 📊 Perbandingan Token Usage

### Tanpa MCP (Load semua):
```
File 10,000 baris × 20 kolom = ~500,000 tokens
```

### Dengan MCP (Filter dulu):
```
Header: ~100 tokens
Summary: ~500 tokens
Filtered data (50 baris): ~2,500 tokens
Total: ~3,100 tokens (hemat 99%!)
```

## 🔗 Resources
- [Google Drive API Docs](https://developers.google.com/drive/api/v3/about-sdk)
- [MCP Protocol](https://modelcontextprotocol.io/)
- [Pandas Filtering](https://pandas.pydata.org/docs/user_guide/indexing.html)
