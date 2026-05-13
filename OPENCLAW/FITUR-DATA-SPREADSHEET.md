# Fitur Data Spreadsheet

## Deskripsi
Bot sekarang bisa membaca data dari Google Spreadsheet eksternal dan menjawab query tentang data tersebut.

## Konfigurasi

Tambahkan di file `.env`:

```env
DATA_SHEET_ID=1RC2Ylo4DjIAJkNMLe6v0jMnupJMcrP2v5aFTauhhcsg
# Multi-tab: comma-separated. Override DATA_SHEET_NAME.
DATA_SHEET_TABS=Surat Jalan,Progress,Stok,Aktual Stok
DATA_SHEET_NAME=Progress
DATA_UPDATE_INTERVAL=86400000

# Reference (belum dipakai aktif, untuk dokumentasi/konteks)
RESUME_SHEET_ID=1d9GKDxcYGwURcVp-BvSYW4W0YQiNVZt_
SJ_DRIVE_FOLDER_ID=1LCfHKCK5hm_f4iqyOXuUo5o4xtBgmMAE
```

### Penjelasan:
- `DATA_SHEET_ID`: ID spreadsheet yang berisi data (dari URL spreadsheet)
- `DATA_SHEET_TABS`: Daftar tab yang dibaca, dipisah koma. Kalau kosong, fallback ke `DATA_SHEET_NAME`.
- `DATA_SHEET_NAME`: (legacy) Nama tab tunggal, dipakai kalau `DATA_SHEET_TABS` kosong.
- `DATA_UPDATE_INTERVAL`: Interval update dalam milidetik (default: 86400000 = 24 jam)
- `RESUME_SHEET_ID`: Spreadsheet resume (master site list) — belum dipakai aktif, tersimpan untuk referensi.
- `SJ_DRIVE_FOLDER_ID`: Drive folder untuk foto SJ — belum dipakai aktif.

## Format Spreadsheet

Bot membaca tiap tab di `DATA_SHEET_TABS` secara generik (kolom A–Z), baris pertama dianggap header. Hasil search di-group per tab dan tiap baris ditampilkan dengan label header dinamis — jadi tidak ada skema kaku.

Tab default yang dibaca dari spreadsheet utama:
- **Surat Jalan** (GID 1213940465) — Tanggal, Segment, Nama Barang, QTY, Jenis, NO_SJ, Pengirim, Penerima, Keterangan, Drive
- **Progress** (GID 1637178585) — Tanggal, Segment, Rute, Nama Barang, Progres, Keterangan, Homebase, Kab/Kota, Site ID
- **Stok** (GID 1736939395) — Stok MRF
- **Aktual Stok** (GID 1692657123) — Aktual stok gudang

Catatan: kalau tab name di `.env` salah (case-sensitive!), bot akan log `❌ Tab "X" gagal: ...` saat startup — koreksi nama tab di `.env` sesuai persis dengan tab di Google Sheets.

## Cara Menggunakan

### 1. Query Data
Tag bot dan gunakan kata kunci berikut diikuti dengan query:
- `cek [query]`
- `cari [query]`
- `data [query]`
- `info [query]`
- `status [query]`
- `progress [query]`
- `site [query]`
- `rute [query]`
- `segment [query]`

### Contoh:
```
@bot cek brebes
@bot cari site 12345
@bot info rute A
@bot status kabel
```

### 2. Respon Bot
Bot akan mencari di semua kolom dan menampilkan hasil yang cocok:
```
Ditemukan 2 hasil untuk "brebes":

1. 2026-05-09 | Segment A | Rute 1
   Barang: Kabel Fiber
   Progress: 80%
   Kab/Kota: Brebes | Site: 12345
   Ket: Dalam proses

2. 2026-05-08 | Segment B | Rute 2
   Barang: Tiang
   Progress: 100%
   Kab/Kota: Brebes | Site: 12346
```

## Update Data

- Bot otomatis update data setiap 24 jam (atau sesuai `DATA_UPDATE_INTERVAL`)
- Data juga di-update saat ada query baru untuk memastikan data terbaru
- Cache digunakan untuk mengurangi API calls ke Google Sheets

## Troubleshooting

### Bot tidak merespon query
1. Pastikan bot di-mention (tag) jika `REQUIRE_MENTION=true`
2. Cek apakah `DATA_SHEET_ID` sudah diisi dengan benar
3. Pastikan Service Account atau OAuth memiliki akses ke spreadsheet

### Data tidak update
1. Cek log terminal untuk error
2. Pastikan spreadsheet ID benar
3. Verifikasi permission Google Sheets (harus bisa dibaca oleh Service Account)

### Error "Data belum tersedia"
Bot masih melakukan initial fetch. Tunggu beberapa detik dan coba lagi.

## Catatan Penting

1. **Permission**: Spreadsheet harus di-share ke Service Account email atau OAuth account
2. **Format**: Pastikan header di baris pertama sesuai format
3. **Performance**: Jika data >1000 baris, pertimbangkan untuk filter di spreadsheet atau gunakan query lebih spesifik
4. **Rate Limit**: Google Sheets API memiliki rate limit, jangan set interval terlalu kecil

