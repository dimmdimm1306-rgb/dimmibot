# WA Arsip Agent

Bot WhatsApp untuk menerima teks atau foto dengan caption seperti `muhadi t7 33`, lalu:

- Upload foto ke Google Drive.
- Menambah baris ke Google Spreadsheet.
- Mengisi kolom foto dengan preview gambar dan link Drive.

Jika ingin dipakai dari OpenClaw, baca `OPENCLAW.md`.

## Format Pesan Rapi

Bot hanya memproses pesan yang mention/tag bot. Kalau tidak di-tag, pesan diabaikan supaya chat grup biasa tidak masuk spreadsheet.

Contoh kirim ke grup:

```text
@bot jenis: keluar
sj: SJ-001
dari: Gudang Dimas
ke: Brebes
barang: Kabel Dropcore
jumlah: 24000 meter
ket: kurang 2 roll jika ada
```

Format paling rapi untuk barang masuk/keluar:

```text
jenis: keluar
sj: SJ-001
dari: Gudang Dimas
ke: Brebes
barang: Kabel Dropcore
jumlah: 24000 meter
ket: kurang 2 roll jika ada
```

Field yang bisa dipakai:
- `jenis`: `masuk` atau `keluar`
- `sj`: nomor surat jalan
- `dari`: pengirim/gudang/mandor
- `ke`: penerima/tujuan
- `barang`: nama barang
- `kode`: kode barang jika ada
- `jumlah`: angka dan satuan
- `ket`: keterangan, misalnya barang kurang/rusak

## Format Cepat Lapangan

Kalau orang lapangan/mandor chatnya singkat, bot tetap mencoba membaca:

```text
laporan 24000 ke brebes
```

Data itu akan masuk spreadsheet sebagai `KELUAR`, jumlah `24000`, tujuan `brebes`, dan status `PERLU DILENGKAPI` karena nama barang, surat jalan, dan pengirim belum jelas.

Contoh cepat lain:

```text
kabel 24000 ke brebes
```

```text
mandor gilang bawa kabel 24000 ke brebes
```

## Kolom Spreadsheet

Siapkan sheet dengan nama `Arsip` dan header berikut:

```text
Waktu Input | Tanggal Transaksi | Jenis | No Surat Jalan | Pengirim | Penerima/Tujuan | Nama Barang | Kode Barang | Jumlah | Satuan | Keterangan | Status Input | Validasi Admin | Teks Asli | Foto | Link Foto | Pengirim WA
```

Kolom `Validasi Admin` otomatis diisi `PENDING`. Setelah dicek manual, ubah menjadi `OK` atau `TIDAK` di spreadsheet.

Bot juga cek duplikat sebelum menyimpan. Jika tanggal, jenis, surat jalan, pengirim, penerima, barang, kode, dan jumlah sama, bot tidak akan menambah baris baru.

## Setup Google

1. Buat project di Google Cloud Console.
2. Aktifkan API berikut:
   - Google Sheets API
   - Google Drive API
3. Pilih salah satu metode login:
   - Service Account: buat Service Account, lalu buat key JSON.
   - OAuth: gunakan file OAuth client dan token, pastikan token punya scope Drive dan Sheets.
4. Jika memakai Service Account, share Google Spreadsheet dan folder Drive ke email service account sebagai Editor.
6. Ambil:
   - `client_email` untuk `GOOGLE_SERVICE_ACCOUNT_EMAIL` jika memakai Service Account
   - `private_key` untuk `GOOGLE_PRIVATE_KEY` jika memakai Service Account
   - path OAuth client JSON untuk `GOOGLE_OAUTH_CLIENT_FILE` jika memakai OAuth
   - path token JSON untuk `GOOGLE_OAUTH_TOKEN_FILE` jika memakai OAuth
   - ID spreadsheet dari URL Google Sheets
   - ID folder dari URL Google Drive

## Setup Bot

```bash
npm install
copy .env.example .env
npm run check
```

Isi `.env` sesuai data Google Anda.

Jalankan bot:

```bash
npm start
```

Scan QR WhatsApp yang muncul di terminal.

## Membatasi Chat yang Boleh Masuk

Bot bisa dibuat hanya menerima arsip dari 1 grup WhatsApp.

Cara mengambil ID grup:

1. Pastikan `LOG_CHAT_ID=true` di `.env`.
2. Jalankan bot dengan `npm start`.
3. Kirim pesan apa saja di grup yang akan dipakai.
4. Lihat terminal, akan muncul teks seperti:

```text
Pesan masuk dari chatId: 120363xxxxxxxx@g.us
```

5. Copy nilai itu ke `.env`:

```text
ALLOWED_GROUP_OR_NUMBER=120363xxxxxxxx@g.us
```

Setelah `ALLOWED_GROUP_OR_NUMBER` diisi, bot akan mengabaikan chat pribadi dan grup lain.

## Balasan WhatsApp

Secara default bot tidak mengirim chat/balasan apa pun ke WhatsApp:

```text
SEND_REPLY=false
```

Dengan setting ini, bot hanya membaca pesan foto yang masuk dari grup yang diizinkan, upload foto, lalu menulis ke spreadsheet. Status berhasil/gagal hanya tampil di terminal.

Jika suatu saat ingin bot membalas `Tersimpan`, ubah menjadi:

```text
SEND_REPLY=true
```

Jika ingin bot membalas singkat untuk teks biasa yang bukan format arsip, aktifkan:

```text
CASUAL_REPLY=true
```

Format arsip seperti `Gilang kabel 24000` tetap disimpan ke spreadsheet. Teks biasa seperti `halo`, `tes`, atau `gimana caranya` akan dibalas singkat dan tidak masuk spreadsheet.

## Fitur Query Data dari Spreadsheet

Bot sekarang bisa membaca data dari Google Spreadsheet eksternal dan menjawab pertanyaan tentang data tersebut.

### Setup Data Spreadsheet

Tambahkan di `.env`:

```env
DATA_SHEET_ID=1RC2Ylo4DjIAJkNMLe6v0jMnupJMcrP2v5aFTauhhcsg
DATA_SHEET_NAME=Sheet1
DATA_UPDATE_INTERVAL=86400000
```

- `DATA_SHEET_ID`: ID spreadsheet yang berisi data project/site
- `DATA_SHEET_NAME`: Nama sheet (default: Sheet1)
- `DATA_UPDATE_INTERVAL`: Update interval dalam milidetik (default: 24 jam)

### Format Spreadsheet Data

Spreadsheet harus memiliki kolom A-I dengan header:
- **A**: Tanggal
- **B**: Segment
- **C**: Rute
- **D**: Nama Barang
- **E**: Progress
- **F**: Keterangan
- **G**: Homebase
- **H**: Kab/Kota
- **I**: Site ID

### Cara Query Data

Tag bot dan gunakan kata kunci:

```text
@bot cek brebes
@bot cari site 12345
@bot info rute A
@bot status kabel
@bot progress segment B
```

Bot akan mencari di semua kolom dan menampilkan hasil yang cocok dengan format:

```text
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

### Update Data

- Bot otomatis update data setiap 24 jam (atau sesuai interval yang diset)
- Data juga di-refresh saat ada query baru
- Pastikan spreadsheet di-share ke Service Account atau OAuth account

Lihat `FITUR-DATA-SPREADSHEET.md` untuk dokumentasi lengkap.

## Catatan

- Bot ini menggunakan Baileys, bukan WhatsApp Business Cloud API resmi.
- Session login tersimpan di folder `auth_info`.
- Jangan commit file `.env` dan folder `auth_info`.