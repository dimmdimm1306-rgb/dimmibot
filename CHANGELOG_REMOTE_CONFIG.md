# Changelog - Remote Config Feature

**Tanggal**: 8 Mei 2026
**Fitur**: Konfigurasi Otomatis untuk Semua User

---

## 🎯 Tujuan

Agar semua user yang install APK otomatis mendapat konfigurasi project yang sama, tanpa perlu setup manual.

---

## ✅ Perubahan yang Dilakukan

### 1. **File yang Dimodifikasi**

#### `Services/ProjectService.cs`
- ✅ Menambahkan konstanta `HARDCODED_REMOTE_CONFIG_URL` (baris 30)
- ✅ Mengupdate method `TrySyncFromRemoteAsync()` untuk prioritaskan hardcoded URL
- ✅ Jika hardcoded URL kosong, fallback ke user-defined URL

**Cara Kerja:**
1. Saat app dibuka, `TrySyncFromRemoteAsync()` dipanggil
2. Cek apakah ada hardcoded URL → jika ada, gunakan itu
3. Jika tidak ada, cek user-defined URL
4. Download JSON config dari URL
5. Parse dan simpan ke local storage
6. Update project jika ID sama, atau tambah project baru

### 2. **File yang Dibuat**

#### `project_config_template.json`
- Template kosong untuk konfigurasi project
- Berisi semua field yang diperlukan
- Bisa digunakan untuk project baru

#### `project_config_ready.json`
- Config yang sudah terisi dengan konfigurasi Anda saat ini
- Siap upload ke Google Drive
- Berisi:
  - Spreadsheet ID: `1kWcBTcjSQIhQtmvUTyvszBii0c4BRkd5`
  - Resume Spreadsheet ID: `1d9GKDxcYGwURcVp-BvSYW4W0YQiNVZt_`
  - Drive Folder ID: `1LCfHKCK5hm_f4iqyOXuUo5o4xtBgmMAE`
  - 6 Segment dengan GID dan nama rute

#### `REMOTE_CONFIG_SETUP.md`
- Dokumentasi lengkap (Bahasa Inggris)
- Step-by-step setup
- Troubleshooting guide
- Best practices

#### `CARA_SETUP_CONFIG_OTOMATIS.md`
- Dokumentasi singkat (Bahasa Indonesia)
- Panduan cepat untuk setup
- Cara update config tanpa build ulang APK

---

## 🚀 Cara Menggunakan

### Setup Awal (Sekali Saja)

1. **Upload `project_config_ready.json` ke Google Drive**
   - Set sharing: "Anyone with the link can view"
   - Copy URL (contoh: `https://drive.google.com/file/d/1ABC123xyz/view`)

2. **Edit `Services/ProjectService.cs` baris 30**
   ```csharp
   private const string HARDCODED_REMOTE_CONFIG_URL = "URL_GOOGLE_DRIVE_ANDA";
   ```

3. **Build APK**
   ```cmd
   dotnet build -c Release -f net9.0-android
   ```

4. **Distribusikan APK**
   - Semua user otomatis dapat config yang sama!

### Update Config (Tanpa Build Ulang APK)

1. Edit file JSON di Google Drive
2. Upload ulang (replace file lama)
3. User otomatis dapat update saat buka app berikutnya

---

## 🎁 Keuntungan

✅ **User tidak perlu setup** - Install APK langsung bisa pakai
✅ **Konfigurasi konsisten** - Semua user dapat config yang sama
✅ **Update mudah** - Edit file di Drive, tidak perlu build ulang APK
✅ **Fleksibel** - Bisa pakai Google Drive atau GitHub
✅ **Backward compatible** - User lama tetap bisa pakai config lokal

---

## 🔧 Technical Details

### Priority System
1. **Hardcoded URL** (di kode) - Prioritas tertinggi
2. **User-defined URL** (dari remote_config_ref.json) - Fallback
3. **Default config** (di kode) - Jika tidak ada remote config

### Sync Behavior
- Sync berjalan di background saat app dibuka
- Tidak blocking UI
- Jika gagal, app tetap pakai config lokal
- Config di-cache untuk performa

### Security
- URL harus HTTPS
- Google Drive link otomatis dikonversi ke direct download URL
- JSON di-validate sebelum di-parse

---

## 📋 Struktur JSON Config

```json
{
  "Id": "unique-id",                    // Jangan ubah setelah distribusi
  "Name": "Nama Project",
  "Description": "Deskripsi",
  "Icon": "📡",
  "Color": "#1D4ED8",
  "SpreadsheetId": "...",               // ID spreadsheet utama
  "GidStok": "...",                     // GID sheet Stok
  "GidAktualStok": "...",               // GID sheet Aktual Stok
  "GidSuratJalan": "...",               // GID sheet Surat Jalan
  "GidProgress": "...",                 // GID sheet Progress
  "ResumeSpreadsheetId": "...",         // ID spreadsheet resume
  "GidResume": "...",                   // GID sheet Resume
  "DriveFolderIdSuratJalan": "...",     // Folder ID untuk foto
  "DriveFolderIdAbsensi": "",
  "GidConfig": "",
  "GidMasterBarang": "",
  "SheetNameSuratJalan": "Surat Jalan",
  "SheetNameProgress": "Progress",
  "SheetNameAbsensi": "Absensi",
  "SegmentGids": {                      // GID per segment
    "1": "...",
    "2": "...",
    ...
  },
  "SegmentNames": {                     // Nama rute per segment
    "1": "...",
    "2": "...",
    ...
  }
}
```

---

## 🆘 Troubleshooting

### Config tidak ter-sync
- ✅ Cek URL Google Drive benar
- ✅ Cek sharing setting: "Anyone with the link can view"
- ✅ Cek format JSON valid (gunakan https://jsonlint.com)
- ✅ Cek koneksi internet

### User masih dapat config lama
- ✅ Tutup dan buka ulang app
- ✅ Tunggu beberapa saat (sync berjalan di background)
- ✅ Cek file di Google Drive sudah ter-update

### Ingin disable remote config
```csharp
private const string HARDCODED_REMOTE_CONFIG_URL = "";
```

---

## 📞 Support

Admin: dimmdimm1306@gmail.com

---

## 🔄 Version History

### v1.0 (8 Mei 2026)
- ✅ Initial implementation
- ✅ Hardcoded URL support
- ✅ Google Drive integration
- ✅ Auto-sync on app start
- ✅ Documentation (ID & EN)
