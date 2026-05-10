# Integrasi Dengan OpenClaw

Karena format project OpenClaw Anda belum tersedia di folder ini, pendekatan paling mudah adalah menjadikan bot ini sebagai **tool lokal** yang dijalankan oleh OpenClaw.

Alurnya:

```text
OpenClaw Agent -> menjalankan command lokal -> npm start -> WA bot aktif
```

Bot Node.js tetap menangani bagian teknis:

- Login WhatsApp lewat QR.
- Menerima foto dengan caption `muhadi t7 33`.
- Membatasi input hanya dari grup yang diisi di `ALLOWED_GROUP_OR_NUMBER`.
- Tidak mengirim balasan WhatsApp jika `SEND_REPLY=false`.
- Upload foto ke Google Drive.
- Menulis data dan foto ke Google Spreadsheet.

## Command Untuk OpenClaw

Jika OpenClaw meminta command/tool, gunakan:

```bash
npm start
```

Working directory:

```text
D:\!FTTH\Program\UPLOAD DOKUMEN\OPENCLAW
```

## Instruksi Agent OpenClaw

Pakai instruksi berikut di OpenClaw:

```text
Kamu adalah agent arsip barang.
Tugasmu menjalankan tool lokal WA Arsip Agent.
Pastikan bot aktif dengan command `npm start` dari folder project.
User akan mengirim foto WhatsApp dengan caption format `nama kode jumlah`, contoh `muhadi t7 33`.
Bot akan menyimpan data ke Google Sheets dan foto ke Google Drive.
Jika bot gagal jalan, minta user cek `.env`, koneksi internet, dan dependency npm.
```

## Setup Pertama Kali

Jalankan manual dulu di terminal:

```bash
npm install
copy .env.example .env
npm run check
```

Setelah `.env` lengkap:

```bash
npm start
```

Scan QR WhatsApp yang tampil.

## Kenapa Tetap Pakai Node.js?

WhatsApp membutuhkan koneksi realtime dan penyimpanan session. Bagian ini lebih stabil sebagai proses lokal yang terus hidup. OpenClaw bisa menjadi pengendali/launcher, sedangkan bot lokal menjadi eksekutor integrasi WhatsApp, Drive, dan Sheets.