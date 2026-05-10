# Setup Remote Config - Konfigurasi Otomatis untuk Semua User

## 📋 Ringkasan

Dengan fitur Remote Config, semua user yang install APK akan otomatis mendapat konfigurasi project yang sama. Anda hanya perlu:
1. Upload file JSON config ke Google Drive
2. Masukkan URL-nya ke dalam kode
3. Build APK
4. Semua user otomatis sync config saat buka app!

---

## 🚀 Cara Setup (Step-by-Step)

### Step 1: Upload Config ke Google Drive

1. **Edit file `project_config_template.json`** sesuai konfigurasi project Anda:
   - `SpreadsheetId`: ID spreadsheet utama
   - `GidStok`, `GidProgress`, dll: GID sheet-sheet yang digunakan
   - `DriveFolderIdSuratJalan`: Folder Drive untuk foto surat jalan
   - `SegmentGids` dan `SegmentNames`: Konfigurasi segment

2. **Upload file JSON ke Google Drive**:
   - Buka Google Drive
   - Upload file `project_config_template.json`
   - Klik kanan file → "Get link" → "Anyone with the link can view"
   - Copy link-nya (contoh: `https://drive.google.com/file/d/1ABC123xyz/view`)

### Step 2: Masukkan URL ke Kode

1. **Buka file `Services/ProjectService.cs`**

2. **Cari baris ini** (sekitar baris 20):
   ```csharp
   private const string HARDCODED_REMOTE_CONFIG_URL = "";
   ```

3. **Ganti dengan URL Google Drive Anda**:
   ```csharp
   private const string HARDCODED_REMOTE_CONFIG_URL = "https://drive.google.com/file/d/1ABC123xyz/view";
   ```

### Step 3: Build APK

```cmd
dotnet build -c Release -f net9.0-android
```

APK akan tersimpan di:
```
bin\Release\net9.0-android\com.companyname.stokbarangmaui-Signed.apk
```

### Step 4: Distribusikan APK

Kirim APK ke semua user. Saat mereka buka app pertama kali, config akan otomatis ter-sync dari Google Drive!

---

## 🔄 Cara Update Config (Setelah APK Didistribusikan)

Jika Anda ingin mengubah konfigurasi setelah APK sudah didistribusikan:

1. **Edit file JSON di Google Drive** (file yang sama yang sudah di-upload)
2. **Upload ulang** file JSON yang sudah diedit (replace file lama)
3. **User otomatis dapat update** saat mereka buka app berikutnya!

**Tidak perlu build ulang APK!** 🎉

---

## 📝 Format JSON Config

File JSON harus mengikuti format ini:

```json
{
  "Id": "unique-project-id",
  "Name": "Nama Project",
  "Description": "Deskripsi project",
  "Icon": "📡",
  "Color": "#1D4ED8",
  "SpreadsheetId": "ID_SPREADSHEET_UTAMA",
  "GidStok": "GID_SHEET_STOK",
  "GidAktualStok": "GID_SHEET_AKTUAL_STOK",
  "GidSuratJalan": "GID_SHEET_SURAT_JALAN",
  "GidProgress": "GID_SHEET_PROGRESS",
  "ResumeSpreadsheetId": "ID_SPREADSHEET_RESUME",
  "GidResume": "GID_SHEET_RESUME",
  "DriveFolderIdSuratJalan": "FOLDER_ID_DRIVE",
  "DriveFolderIdAbsensi": "",
  "GidConfig": "",
  "GidMasterBarang": "",
  "SheetNameSuratJalan": "Surat Jalan",
  "SheetNameProgress": "Progress",
  "SheetNameAbsensi": "Absensi",
  "SegmentGids": {
    "1": "GID_SEGMENT_1",
    "2": "GID_SEGMENT_2",
    "3": "GID_SEGMENT_3",
    "4": "GID_SEGMENT_4",
    "5": "GID_SEGMENT_5",
    "6": "GID_SEGMENT_6"
  },
  "SegmentNames": {
    "1": "NAMA RUTE SEGMENT 1",
    "2": "NAMA RUTE SEGMENT 2",
    "3": "NAMA RUTE SEGMENT 3",
    "4": "NAMA RUTE SEGMENT 4",
    "5": "NAMA RUTE SEGMENT 5",
    "6": "NAMA RUTE SEGMENT 6"
  }
}
```

---

## 🔍 Cara Mendapatkan ID & GID

### Spreadsheet ID
Dari URL spreadsheet:
```
https://docs.google.com/spreadsheets/d/1kWcBTcjSQIhQtmvUTyvszBii0c4BRkd5/edit
                                      ↑ INI SPREADSHEET ID
```

### GID (Sheet ID)
Dari URL saat buka sheet tertentu:
```
https://docs.google.com/spreadsheets/d/1kWcBTcjSQIhQtmvUTyvszBii0c4BRkd5/edit#gid=1736939395
                                                                              ↑ INI GID
```

### Drive Folder ID
Dari URL folder di Google Drive:
```
https://drive.google.com/drive/folders/1LCfHKCK5hm_f4iqyOXuUo5o4xtBgmMAE
                                        ↑ INI FOLDER ID
```

---

## ⚙️ Alternatif: Gunakan GitHub (Gratis & Lebih Mudah)

Jika tidak ingin pakai Google Drive, bisa pakai GitHub:

1. **Buat repository GitHub** (bisa private atau public)
2. **Upload file JSON** ke repository
3. **Gunakan URL raw**:
   ```
   https://raw.githubusercontent.com/username/repo-name/main/project_config.json
   ```
4. **Masukkan URL raw ke kode**:
   ```csharp
   private const string HARDCODED_REMOTE_CONFIG_URL = 
       "https://raw.githubusercontent.com/username/repo-name/main/project_config.json";
   ```

**Keuntungan GitHub:**
- Gratis
- Ada version history (bisa rollback ke config lama)
- Bisa edit langsung di web
- Lebih cepat diakses

---

## 🛠️ Troubleshooting

### Config tidak ter-sync
- Pastikan URL Google Drive sudah di-set "Anyone with the link can view"
- Pastikan format JSON valid (gunakan https://jsonlint.com untuk validasi)
- Cek koneksi internet di device

### User masih dapat config lama
- App sync config saat dibuka, jadi user perlu tutup dan buka ulang app
- Atau tunggu beberapa saat, sync berjalan di background

### Ingin disable remote config
Kosongkan URL di kode:
```csharp
private const string HARDCODED_REMOTE_CONFIG_URL = "";
```

---

## 📱 Cara Kerja

1. **Saat app dibuka**, `ProjectService.TrySyncFromRemoteAsync()` dipanggil
2. **App download JSON** dari URL yang di-hardcode
3. **Config di-parse** dan disimpan lokal
4. **Jika ada project dengan ID sama**, config di-update
5. **Jika belum ada**, project baru ditambahkan
6. **User langsung dapat config terbaru!**

---

## 🎯 Best Practices

1. **Gunakan ID yang unik** untuk setiap project (jangan ganti ID setelah didistribusikan)
2. **Test config JSON** sebelum upload (pastikan format valid)
3. **Backup config lama** sebelum update
4. **Gunakan GitHub** untuk version control yang lebih baik
5. **Dokumentasikan perubahan** setiap kali update config

---

## 📞 Support

Jika ada masalah dengan remote config, hubungi admin: dimmdimm1306@gmail.com
