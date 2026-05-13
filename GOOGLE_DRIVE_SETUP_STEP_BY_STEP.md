# 🚀 Setup Google Drive API - Step by Step

## Langkah 1: Buat Project di Google Cloud Console

1. **Buka Google Cloud Console**
   - Kunjungi: https://console.cloud.google.com/
   - Login dengan akun Google Anda

2. **Buat Project Baru**
   - Klik dropdown project di atas (sebelah logo Google Cloud)
   - Klik **"NEW PROJECT"**
   - Nama project: `MCP Drive Reader` (atau nama lain)
   - Klik **"CREATE"**
   - Tunggu beberapa detik sampai project dibuat

## Langkah 2: Enable Google Drive API

1. **Buka API Library**
   - Di menu kiri, klik **"APIs & Services"** → **"Library"**
   - Atau langsung ke: https://console.cloud.google.com/apis/library

2. **Cari dan Enable Drive API**
   - Ketik "Google Drive API" di search box
   - Klik **"Google Drive API"**
   - Klik tombol **"ENABLE"**
   - Tunggu sampai enabled (biasanya cepat)

## Langkah 3: Buat Service Account

1. **Buka Service Accounts**
   - Menu kiri: **"IAM & Admin"** → **"Service Accounts"**
   - Atau: https://console.cloud.google.com/iam-admin/serviceaccounts

2. **Create Service Account**
   - Klik **"+ CREATE SERVICE ACCOUNT"** di atas
   
3. **Service Account Details**
   - **Service account name**: `mcp-gdrive-reader`
   - **Service account ID**: akan auto-generate (mcp-gdrive-reader)
   - **Description**: `MCP server untuk baca Google Drive`
   - Klik **"CREATE AND CONTINUE"**

4. **Grant Access (Skip)**
   - Di bagian "Grant this service account access to project"
   - **SKIP** - tidak perlu role khusus
   - Klik **"CONTINUE"**

5. **Grant Users Access (Skip)**
   - **SKIP** juga
   - Klik **"DONE"**

## Langkah 4: Download Credentials JSON

1. **Buka Service Account yang Baru Dibuat**
   - Klik email service account yang baru dibuat
   - Format: `mcp-gdrive-reader@your-project-id.iam.gserviceaccount.com`

2. **Create Key**
   - Tab **"KEYS"**
   - Klik **"ADD KEY"** → **"Create new key"**
   - Pilih **"JSON"**
   - Klik **"CREATE"**
   - File JSON akan otomatis terdownload

3. **Simpan File JSON**
   - Rename file ke nama yang mudah: `gdrive-credentials.json`
   - Pindahkan ke folder aman, misalnya:
     ```
     D:\!FTTH\Program\UPLOAD DOKUMEN\StokBarangMAUI\credentials\gdrive-credentials.json
     ```
   - **JANGAN commit ke Git!** (sudah ada di .gitignore)

4. **Copy Email Service Account**
   - Copy email service account ini, contoh:
     ```
     mcp-gdrive-reader@your-project-123456.iam.gserviceaccount.com
     ```
   - Kita akan pakai untuk share Drive files

## Langkah 5: Share Google Drive Files

Sekarang share file/folder Drive Anda dengan service account:

### Cara 1: Share Specific File
1. Buka Google Drive
2. Klik kanan file yang ingin diakses (misalnya "BOQ FWA.xlsx")
3. Klik **"Share"**
4. Paste email service account
5. Permission: **Viewer** (read-only)
6. Klik **"Send"**

### Cara 2: Share Folder (Lebih Praktis)
1. Buka Google Drive
2. Klik kanan folder yang berisi semua file kerja
3. Klik **"Share"**
4. Paste email service account
5. Permission: **Viewer**
6. Klik **"Send"**
7. Semua file di folder ini bisa diakses!

## Langkah 6: Update MCP Config

Edit file `.kiro/settings/mcp.json`:

```json
{
  "mcpServers": {
    "gdrive-reader": {
      "command": "python",
      "args": ["-m", "mcp_gdrive_filter.server"],
      "env": {
        "GOOGLE_APPLICATION_CREDENTIALS": "D:\\!FTTH\\Program\\UPLOAD DOKUMEN\\StokBarangMAUI\\credentials\\gdrive-credentials.json"
      },
      "disabled": false,
      "autoApprove": ["read_and_filter_sheet", "get_sheet_headers", "get_sheet_summary", "list_drive_files"]
    }
  }
}
```

**⚠️ PENTING:**
- Ganti path dengan lokasi file JSON Anda
- Gunakan double backslash `\\` untuk Windows path
- Atau gunakan forward slash `/`

## Langkah 7: Test Connection

1. **Restart Kiro**
   - Restart VS Code / Kiro

2. **Cek MCP Server**
   - Buka Command Palette (Ctrl+Shift+P)
   - Ketik "MCP"
   - Pilih "MCP: Show Server Status"
   - Pastikan `gdrive-reader` status **Connected** ✅

3. **Test dengan Chat**
   ```
   AI, list files di Google Drive saya
   ```

## 🎯 Quick Test Commands

### Test 1: List Files
```
AI, list semua file Excel di Drive saya
```

### Test 2: Get Headers
```
AI, ambil header dari file "BOQ FWA.xlsx"
```

### Test 3: Filter Data
```
AI, dari file BOQ FWA, ambil data:
- Segment: FWA
- Limit: 10 baris
```

## ⚠️ Troubleshooting

### Error: "GOOGLE_APPLICATION_CREDENTIALS not set"
**Solusi:**
- Cek path di `mcp.json` benar
- Gunakan absolute path
- Gunakan double backslash `\\`

### Error: "Permission denied" atau "File not found"
**Solusi:**
- Pastikan file sudah di-share dengan service account email
- Cek permission minimal **Viewer**
- Tunggu 1-2 menit setelah share (propagation)

### MCP Server tidak muncul
**Solusi:**
- Cek syntax `mcp.json` valid (gunakan JSON validator)
- Restart Kiro
- Cek log di MCP Server view

### Error: "Module not found: mcp_gdrive_filter"
**Solusi:**
- Pastikan folder `mcp_gdrive_filter` ada
- Pastikan ada file `__init__.py` di dalamnya
- Coba install ulang dependencies

## 📝 Cara Dapat File ID

### Dari URL Google Drive:
```
https://drive.google.com/file/d/1abc...xyz/view
                              ^^^^^^^^^ 
                              ini File ID
```

### Atau dari Sheets:
```
https://docs.google.com/spreadsheets/d/1abc...xyz/edit
                                       ^^^^^^^^^
                                       ini File ID
```

### Atau List via AI:
```
AI, list files di Drive yang namanya mengandung "BOQ"
```

## ✅ Checklist Setup

- [ ] Project dibuat di Google Cloud Console
- [ ] Google Drive API enabled
- [ ] Service Account dibuat
- [ ] Credentials JSON downloaded
- [ ] File JSON disimpan di folder aman
- [ ] Email service account di-copy
- [ ] Drive files/folder di-share dengan service account
- [ ] Path credentials di `mcp.json` sudah benar
- [ ] Kiro sudah di-restart
- [ ] MCP server status **Connected**
- [ ] Test command berhasil

## 🎉 Selesai!

Sekarang Anda bisa:
- List files di Drive
- Baca Excel/Sheets dengan filter
- Hemat token hingga 99%!

**Next:** Lihat `SETUP_MCP_GDRIVE.md` untuk cara pakai lengkap.
