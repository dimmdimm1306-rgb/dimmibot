# Changelog

## [2.0.0] - 2026-05-09

### ✨ Fitur Baru: Query Data dari Spreadsheet

Bot sekarang bisa membaca dan mencari data dari Google Spreadsheet eksternal secara otomatis.

#### Fitur yang Ditambahkan:

1. **Auto-fetch data dari spreadsheet**
   - Bot membaca spreadsheet Progress dengan 329 baris data
   - Kolom: Tanggal, Segment, Rute, Nama Barang, Progress, Keterangan, Homebase, Kab/Kota, Site ID
   - Update otomatis setiap 24 jam (configurable)

2. **Search/Query functionality**
   - Cari data berdasarkan keyword di semua kolom
   - Kata kunci: `cek`, `cari`, `data`, `info`, `status`, `progress`, `site`, `rute`, `segment`
   - Contoh: `@bot cek brebes`, `@bot cari site 1437221023`

3. **Smart caching**
   - Data di-cache untuk mengurangi API calls
   - Auto-refresh saat ada query baru
   - Interval update configurable via `DATA_UPDATE_INTERVAL`

4. **Response formatting**
   - Hasil ditampilkan dengan format rapi
   - Maksimal 10 hasil per query
   - Informasi lengkap: tanggal, segment, rute, barang, progress, lokasi, site ID

#### File yang Ditambahkan:

- `FITUR-DATA-SPREADSHEET.md` - Dokumentasi lengkap fitur data spreadsheet
- `CARA-PAKAI-BOT.md` - Panduan penggunaan bot untuk user
- `src/test-data-fetch.js` - Script untuk test koneksi ke spreadsheet
- `CHANGELOG.md` - File ini

#### File yang Dimodifikasi:

- `src/index.js` - Tambah fungsi fetch, cache, dan search data
- `.env` - Tambah konfigurasi `DATA_SHEET_ID`, `DATA_SHEET_NAME`, `DATA_UPDATE_INTERVAL`
- `.env.example` - Update template konfigurasi
- `package.json` - Tambah script `test-data`
- `README.md` - Update dokumentasi dengan fitur baru

#### Konfigurasi Baru:

```env
DATA_SHEET_ID=1RC2Ylo4DjIAJkNMLe6v0jMnupJMcrP2v5aFTauhhcsg
DATA_SHEET_NAME=Progress
DATA_UPDATE_INTERVAL=86400000
```

#### Breaking Changes:

Tidak ada breaking changes. Fitur lama (input arsip barang) tetap berfungsi normal.

#### Testing:

- ✅ Test koneksi ke spreadsheet berhasil
- ✅ Berhasil fetch 329 baris data
- ✅ Format data sesuai dengan kolom A-I
- ✅ Bot bisa membedakan query data vs input arsip

#### Next Steps:

1. Jalankan `npm run test-data` untuk verifikasi koneksi
2. Update `.env` dengan `DATA_SHEET_ID` yang benar
3. Restart bot dengan `npm start`
4. Test query dengan `@bot cek brebes`

---

## [1.0.0] - Sebelumnya

### Fitur Awal:

- Input arsip barang ke Google Spreadsheet
- Upload foto ke Google Drive
- Support format lengkap dan format cepat
- Duplicate detection
- OAuth dan Service Account authentication
- Group chat filtering
- Mention/tag requirement
