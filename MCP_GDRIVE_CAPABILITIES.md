# 📂 MCP Google Drive - Yang Bisa Dibaca

## ✅ Support Penuh:

### 1. **Google Sheets** (Native)
- File spreadsheet native Google Drive
- Multi-sheet (beberapa tab)
- Pakai Google Sheets API (efisien!)
- Contoh: `https://docs.google.com/spreadsheets/d/xxx`

### 2. **Excel** (.xlsx, .xls)
- File Excel yang di-upload ke Drive
- Multi-sheet support
- Auto-parse dengan pandas
- Contoh: `BOQ FWA.xlsx`

### 3. **CSV** (.csv)
- File CSV di Drive
- Auto-detect encoding

### 4. **Folder**
- Browse isi folder yang di-share
- List semua file di dalamnya
- Support recursive (include subfolder)

## 📋 Cara Share untuk Akses Penuh:

### Option 1: Share Folder (Recommended!)
```
1. Buat folder khusus di Drive, misal "AI Access"
2. Masukkan semua file kerja ke folder itu
3. Share folder dengan service account email
4. Permission: Viewer
5. Done! Semua file dalam folder otomatis terakses
```

### Option 2: Share Per File
```
1. Klik kanan file
2. Share dengan service account email
3. Permission: Viewer
```

### Option 3: Share Shared Drive
```
1. Buat Shared Drive (untuk tim)
2. Add service account sebagai member
3. Permission: Viewer
```

## 🎯 Tools Tersedia:

### 1. `list_drive_files`
List semua file dengan filter.

**Filter by type:**
- `all` - Semua file
- `sheets` - Google Sheets saja
- `excel` - Excel saja
- `spreadsheet` - Semua spreadsheet (Sheets + Excel + CSV)
- `folder` - Folder saja

**Contoh:**
```
AI, list semua Google Sheets di Drive
AI, list file Excel yang namanya "BOQ"
AI, list folder di Drive
```

### 2. `list_folder_contents` ⭐ BARU!
Browse isi folder yang di-share.

**Contoh:**
```
AI, list isi folder [folder_id]
AI, list folder "AI Access" secara recursive
```

**Response:**
```json
{
  "summary": {
    "folders": 3,
    "spreadsheets": 15,
    "other_files": 2
  },
  "folders": [...],
  "spreadsheets": [...]
}
```

### 3. `list_sheet_tabs` ⭐ BARU!
List semua tab/sheet di file multi-sheet.

**Contoh:**
```
AI, ada sheet apa saja di BOQ FWA?

Response:
Sheets: ["Summary", "Data FWA", "Data FTTH", "Pivot"]
```

### 4. `get_sheet_headers`
Ambil header + info dasar (~100 tokens).

**Contoh:**
```
AI, header dari BOQ FWA sheet "Data FWA"
```

### 5. `get_sheet_summary`
Statistik per kolom (~500 tokens).

**Contoh:**
```
AI, summary kolom Segment dan Rute dari BOQ FWA
```

### 6. `read_and_filter_sheet`
Baca dengan filter (hemat token!).

**Contoh:**
```
AI, dari BOQ FWA sheet "Data FWA":
- Segment: FWA
- Rute: A, B
- Limit: 50
```

## 🔄 Workflow Lengkap:

### Skenario: Eksplorasi Folder Kerja

```
1. User: AI, list folder di Drive saya
   → Lihat folder apa saja yang di-share

2. User: AI, list isi folder "BOQ Projects"
   → Lihat file apa saja di folder

3. User: AI, ada sheet apa di file "BOQ FWA Q1 2024"
   → Lihat tab-tab di file

4. User: AI, header dari sheet "Data Detail"
   → Lihat kolom apa saja

5. User: AI, summary kolom Segment, Rute, Material
   → Lihat distribusi data

6. User: AI, filter data: segment=FWA, rute=A, limit=50
   → Ambil data yang diperlukan
```

**Total token: ~5,000** (vs 500,000 tanpa MCP!)

## 📊 Jenis File Support Matrix:

| File Type | Read | Multi-Sheet | Filter | Notes |
|-----------|------|-------------|--------|-------|
| Google Sheets | ✅ | ✅ | ✅ | Via Sheets API |
| Excel (.xlsx) | ✅ | ✅ | ✅ | Via download |
| Excel (.xls) | ✅ | ✅ | ✅ | Via download |
| CSV (.csv) | ✅ | ❌ | ✅ | Single sheet |
| Folder | ✅ | - | - | Browse only |
| Google Docs | ❌ | - | - | Tidak support |
| PDF | ❌ | - | - | Tidak support |
| Images | ❌ | - | - | Tidak support |

## 🎯 Tips Pakai:

### Tip 1: Gunakan Folder untuk Organize
```
Drive/
├── AI Access/          ← Share folder ini
│   ├── BOQ Projects/
│   │   ├── BOQ FWA.xlsx
│   │   └── BOQ FTTH.xlsx
│   ├── Reports/
│   │   └── Monthly Report.gsheet
│   └── Templates/
```

### Tip 2: Multi-Sheet File
```
AI, list sheet di BOQ FWA
→ ["Summary", "Data", "Pivot"]

AI, header dari sheet "Data"
→ [Tanggal, Segment, Rute, ...]

AI, filter sheet "Data": segment=FWA, limit=50
```

### Tip 3: Kombinasi Filter
```
AI, dari "Monthly Report" sheet "Januari":
- Kolom: Product, Qty, Revenue
- Filter: Revenue > 1000000
- Limit: 20
```

## ⚠️ Limitasi:

1. **Service Account tidak punya Drive sendiri**
   - Harus share file/folder ke service account email
   - Tidak bisa buat/edit file (read-only)

2. **File size limit**
   - Excel besar (>50MB) bisa lambat
   - Gunakan filter untuk hemat bandwidth

3. **Google Docs tidak support**
   - Hanya spreadsheet (Sheets/Excel/CSV)

4. **Rate limit Google API**
   - 1,000 request/100 detik per user
   - Biasanya cukup untuk penggunaan normal

## 🚀 Ready!

Setelah setup:
- ✅ Baca Google Sheets
- ✅ Baca Excel di Drive
- ✅ Baca CSV di Drive
- ✅ Browse folder yang di-share
- ✅ Filter data hemat token
- ✅ Multi-sheet support

**Next:** Ikuti `GOOGLE_DRIVE_SETUP_STEP_BY_STEP.md` untuk setup!
