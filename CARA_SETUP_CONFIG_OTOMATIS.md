# 🚀 Cara Setup Konfigurasi Otomatis untuk Semua User

## Masalah
Setiap orang yang install APK harus setup konfigurasi project sendiri-sendiri.

## Solusi
Dengan Remote Config, semua user otomatis dapat konfigurasi yang sama!

---

## 📝 Langkah Setup (5 Menit)

### 1️⃣ Upload Config ke Google Drive

1. **Buka file `project_config_ready.json`** (sudah berisi konfigurasi Anda saat ini)

2. **Upload ke Google Drive**:
   - Buka https://drive.google.com
   - Upload file `project_config_ready.json`
   - Klik kanan file → **Share** → **Anyone with the link**
   - Copy link (contoh: `https://drive.google.com/file/d/1ABC123xyz/view`)

### 2️⃣ Masukkan URL ke Kode

1. **Buka file**: `Services/ProjectService.cs`

2. **Cari baris 20** (ada tulisan `HARDCODED_REMOTE_CONFIG_URL`):
   ```csharp
   private const string HARDCODED_REMOTE_CONFIG_URL = "";
   ```

3. **Ganti dengan URL Google Drive**:
   ```csharp
   private const string HARDCODED_REMOTE_CONFIG_URL = "https://drive.google.com/file/d/1ABC123xyz/view";
   ```
   *(Ganti dengan URL yang Anda copy dari Google Drive)*

4. **Save file**

### 3️⃣ Build APK

```cmd
dotnet build -c Release -f net9.0-android
```

APK ada di: `bin\Release\net9.0-android\com.companyname.stokbarangmaui-Signed.apk`

### 4️⃣ Distribusikan APK

Kirim APK ke semua user. **Selesai!** ✅

Semua user otomatis dapat konfigurasi yang sama saat buka app!

---

## 🔄 Update Konfigurasi (Tanpa Build Ulang APK!)

Jika ingin ubah konfigurasi setelah APK didistribusikan:

1. **Edit file JSON** di Google Drive (file yang sama)
2. **Upload ulang** (replace file lama)
3. **User otomatis dapat update** saat buka app berikutnya!

**Tidak perlu build ulang APK!** 🎉

---

## 📋 Isi File Config

File `project_config_ready.json` berisi:
- ✅ Spreadsheet ID utama
- ✅ GID semua sheet (Stok, Progress, Surat Jalan, dll)
- ✅ Drive Folder ID untuk foto
- ✅ Konfigurasi 6 segment
- ✅ Nama project & icon

Semua sudah sesuai dengan konfigurasi Anda saat ini!

---

## 🎯 Keuntungan

✅ **User tidak perlu setup apapun** - Langsung bisa pakai
✅ **Konfigurasi selalu sama** - Tidak ada perbedaan antar user
✅ **Update mudah** - Edit file di Drive, semua user dapat update
✅ **Tidak perlu build ulang APK** - Cukup edit file JSON

---

## ⚠️ Penting!

- Pastikan file JSON di Google Drive di-set **"Anyone with the link can view"**
- Jangan ubah `"Id": "ftth-jawatengah-2024"` setelah APK didistribusikan
- Format JSON harus valid (cek di https://jsonlint.com)

---

## 🆘 Troubleshooting

**Config tidak ter-sync?**
- Cek koneksi internet
- Pastikan URL Google Drive benar
- Pastikan file di-set "Anyone with the link can view"

**User masih dapat config lama?**
- Tutup dan buka ulang app
- Sync berjalan di background, tunggu beberapa saat

---

## 📞 Kontak

Admin: dimmdimm1306@gmail.com
