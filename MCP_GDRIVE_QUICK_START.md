# 🚀 MCP Google Drive - Quick Start

## ✅ Status Setup

### 1. Dependencies
- [x] Python packages installed
- [x] MCP config created
- [x] Credentials folder created
- [x] .gitignore updated

### 2. Yang Perlu Anda Lakukan

#### A. Setup Google Cloud (5 menit)
📖 **Ikuti:** `GOOGLE_DRIVE_SETUP_STEP_BY_STEP.md`

**Ringkasan:**
1. Buka https://console.cloud.google.com/
2. Buat project baru
3. Enable Google Drive API
4. Buat Service Account
5. Download credentials JSON
6. Simpan di: `credentials/gdrive-credentials.json`

#### B. Share Drive Files
1. Copy email service account (dari Google Cloud Console)
2. Share file/folder Drive dengan email tersebut
3. Permission: **Viewer**

#### C. Restart Kiro
- Restart VS Code / Kiro

## 🎯 Test Commands

### 1. List Files
```
AI, list files di Google Drive saya
```

### 2. Get Headers (Super Hemat!)
```
AI, ambil header dari file BOQ FWA
File ID: [paste file ID]
```

### 3. Filter Data
```
AI, dari file BOQ FWA ambil data:
- Segment: FWA
- Tanggal: setelah 2024-01-01
- Limit: 20 baris
```

## 📊 Cara Dapat File ID

### Dari URL:
```
https://drive.google.com/file/d/1abc...xyz/view
                              ^^^^^^^^^ File ID
```

### Atau tanya AI:
```
AI, list files yang namanya mengandung "BOQ"
```

## 💡 Tips Hemat Token

### ❌ JANGAN (Boros Token):
```
AI, baca semua data dari BOQ FWA
→ Load 10,000 baris = ~500,000 tokens
```

### ✅ LAKUKAN (Hemat Token):
```
1. AI, ambil header dari BOQ FWA
   → ~100 tokens

2. AI, ambil summary dari kolom segment dan rute
   → ~500 tokens

3. AI, filter data: segment=FWA, limit=50
   → ~2,500 tokens

Total: ~3,100 tokens (hemat 99%!)
```

## 🔧 Troubleshooting Cepat

### MCP Server tidak muncul?
```bash
# Cek config valid
cat .kiro/settings/mcp.json

# Restart Kiro
```

### Error "Permission denied"?
- Pastikan file di-share dengan service account email
- Tunggu 1-2 menit setelah share

### Error "Credentials not found"?
- Cek path di `mcp.json` benar
- Gunakan double backslash: `D:\\path\\to\\file.json`

## 📚 Dokumentasi Lengkap

- **Setup Detail:** `GOOGLE_DRIVE_SETUP_STEP_BY_STEP.md`
- **Cara Pakai:** `SETUP_MCP_GDRIVE.md`
- **Code:** `mcp_gdrive_filter/server.py`

## 🎉 Ready to Go!

Setelah setup Google Cloud selesai, Anda bisa langsung:
1. List files di Drive
2. Baca Excel/Sheets dengan filter
3. Hemat token hingga 99%!

**Next Step:** Buka `GOOGLE_DRIVE_SETUP_STEP_BY_STEP.md` dan ikuti langkah-langkahnya.
