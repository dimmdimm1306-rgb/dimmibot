# 🤖 Panduan Lengkap Perintah AI Bot - StokBarangMAUI

**Tanggal:** 11 Mei 2026  
**Bot Name:** Claw  
**Version:** 2.0

---

## 📋 DAFTAR ISI

1. [Perintah Umum](#perintah-umum)
2. [Query Progress](#query-progress)
3. [Query Surat Jalan](#query-surat-jalan)
4. [Query Stok](#query-stok)
5. [Query Site & Lokasi](#query-site--lokasi)
6. [Perintah Admin](#perintah-admin)
7. [Obrolan Santai](#obrolan-santai)

---

## 1️⃣ PERINTAH UMUM

### 📊 Cek Status Project
```
"status project"
"gimana progress project?"
"overview project"
```
**Output:** Ringkasan progress semua segment, stok, dan status terkini

### 📅 Cek Tanggal & Waktu
```
"hari ini tanggal berapa?"
"sekarang jam berapa?"
```
**Output:** Informasi tanggal dan waktu saat ini

### 🔄 Refresh Data
**TIDAK PERLU!** Data otomatis refresh setiap kali buka chat bot

---

## 2️⃣ QUERY PROGRESS

### 📅 Progress Hari Ini
```
"progress hari ini"
"apa yang dikerjakan hari ini?"
"pekerjaan hari ini"
```
**Output:** Detail progress semua segment hari ini

### 📅 Progress Kemarin
```
"progress kemarin"
"apa yang dikerjakan kemarin?"
"pekerjaan kemarin"
```
**Output:** Detail progress semua segment kemarin

### 📅 Progress Tanggal Tertentu
```
"progress tanggal 10 Mei"
"progress 10/05/2026"
"pekerjaan tanggal 5 Mei"
```
**Output:** Detail progress pada tanggal yang diminta

### 📅 Progress Minggu Ini
```
"progress minggu ini"
"pekerjaan minggu ini"
"progress 7 hari terakhir"
```
**Output:** Ringkasan progress 7 hari terakhir

### 📊 Progress Per Segment
```
"progress segment 1"
"progress segment Brebes"
"progress Surakarta"
```
**Output:** Detail progress segment tertentu

### 📈 Progress Total
```
"total progress"
"berapa persen progress?"
"progress keseluruhan"
```
**Output:** Persentase progress total project

### 🏆 Segment Tercepat/Terlambat
```
"segment mana yang paling cepat?"
"segment mana yang paling lambat?"
"ranking segment"
```
**Output:** Perbandingan progress antar segment

---

## 3️⃣ QUERY SURAT JALAN

### 📦 Surat Jalan Per Barang
```
"surat jalan tiang"
"surat jalan kabel"
"surat jalan cable"
"surat jalan pole"
```
**Output:** 
```
📦 Surat Jalan Tiang:

�� 10/05/2026 | Pole 7m 10 btg | Pengirim: PT ABC → Penerima: Gudang MRF | No.SJ: SJ-001
📄 09/05/2026 | Pole 9m 5 btg | Pengirim: PT XYZ → Penerima: Gudang MRF | No.SJ: SJ-002

Total: 2 surat jalan
```

### 📦 Surat Jalan Terakhir Masuk
```
"surat jalan terakhir masuk"
"surat jalan terbaru"
"3 surat jalan terakhir"
```
**Output:** 3 surat jalan paling baru dengan format rapi

### 📦 Surat Jalan Per Lokasi
```
"surat jalan surakarta"
"surat jalan brebes"
"surat jalan segment 1"
```
**Output:** Semua surat jalan untuk lokasi/segment tertentu

### 📸 Foto Surat Jalan
```
"foto surat jalan baris 5"
"link foto surat jalan nomor 10"
"foto surat jalan SJ-001"
```
**Output:** Link foto dari Google Drive + detail surat jalan

### 🔗 Link Spreadsheet Surat Jalan
```
"link surat jalan"
"spreadsheet surat jalan"
```
**Output:** Link langsung ke Google Sheets Surat Jalan

---

## 4️⃣ QUERY STOK

### 📦 Stok Gudang
```
"stok gudang"
"stok MRF"
"berapa stok kabel di gudang?"
"stok tiang"
```
**Output:** Detail stok per barang di gudang

### 📊 Stok Aktual
```
"stok aktual"
"stok real"
"stok sebenarnya"
```
**Output:** Stok aktual setelah dikurangi pemakaian

### 📈 Stok Diterima
```
"stok diterima"
"barang yang sudah diterima"
"material masuk"
```
**Output:** List barang yang sudah diterima dari vendor

### ⚠️ Stok Menipis
```
"stok yang menipis"
"stok hampir habis"
"barang yang perlu di-order"
```
**Output:** List barang dengan stok di bawah threshold

### 🔍 Cek Stok Barang Tertentu
```
"stok kabel 24c"
"stok tiang 7m"
"berapa cable 48c?"
```
**Output:** Detail stok barang spesifik

---

## 5️⃣ QUERY SITE & LOKASI

### 🗺️ Site Yang Belum Dikerjakan
```
"site yang belum dikerjakan"
"site mana yang belum?"
"pekerjaan yang belum dimulai"
```
**Output:** List site dengan progress 0

### 🏙️ Pekerjaan Per Kota
```
"cek pekerjaan di Surakarta"
"site yang belum di Brebes"
"progress Tegal"
```
**Output:** Detail site yang belum dikerjakan di kota tertentu

### �� Detail Site Tertentu
```
"cek site 0244"
"detail site 0244"
"info site 0244"
```
**Output:** Detail lengkap site tertentu (otomatis lookup)

### 🛣️ Cek Rute
```
"cek rute Brebes-Tegal"
"detail rute segment 1"
```
**Output:** Detail rute dan progress

### 🏠 Homebase
```
"homebase segment 1"
"lokasi homebase Surakarta"
```
**Output:** Informasi homebase per segment

---

## 6️⃣ PERINTAH ADMIN

### 🔐 Login Admin
```
"dimmi13"
"kevingoblok"
```
**Output:** Akses mode admin (berlaku 24 jam)

### 📝 Lihat Perintah Admin
```
"dimmi13 help"
"dimmi13 ?"
```
**Output:** List semua perintah admin yang tersedia

### 🎭 Set Personality Bot
```
"dimmi13 personality [teks personality]"
```
**Contoh:**
```
"dimmi13 personality Kamu sekarang lebih santai dan suka bercanda"
```
**Output:** Bot personality berubah sesuai instruksi

### 🧠 Tambah Memory Bot
```
"dimmi13 ingat [informasi]"
```
**Contoh:**
```
"dimmi13 ingat Eko pindah ke Segment 2"
"dimmi13 ingat Kevin sekarang di Brebes"
```
**Output:** Bot menyimpan informasi ke memory

### 📋 Lihat Memory Bot
```
"dimmi13 memory"
"dimmi13 ingatan"
```
**Output:** List semua memory yang tersimpan

### 🗑️ Hapus Memory Bot
```
"dimmi13 lupa [nomor]"
"dimmi13 hapus memory [nomor]"
```
**Output:** Memory tertentu dihapus

### 🌐 Set Instruksi Global (Sync ke GitHub)
```
"dimmi13 instruksi [teks instruksi]"
```
**Contoh:**
```
"dimmi13 instruksi jangan pernah bilang 'mungkin maksudnya'"
```
**Output:** Instruksi tersimpan di GitHub, semua device dapat update

### 📖 Lihat Instruksi Global
```
"dimmi13 instruksi"
```
**Output:** Instruksi global yang aktif

### 🗑️ Hapus Instruksi Global
```
"dimmi13 hapus instruksi"
"dimmi13 reset instruksi"
```
**Output:** Instruksi global dihapus

### 📊 Status Bot
```
"dimmi13 status"
```
**Output:** Info personality, memory count, instruksi global

### 📝 Catat Perpindahan Tim
```
"dimmi13 catat [nama] pindah ke [lokasi]"
```
**Contoh:**
```
"dimmi13 catat Eko pindah ke Segment 2"
"dimmi13 catat Kevin sekarang di Brebes"
```
**Output:** Informasi tersimpan di memory bot

---

## 7️⃣ OBROLAN SANTAI

### 👋 Salam & Sapaan
```
"halo"
"hai"
"apa kabar?"
"gimana kabarmu?"
```
**Output:** Bot balas dengan ramah

### 😂 Bercanda
```
"cerita lucu dong"
"ada joke?"
"bikin ketawa"
```
**Output:** Bot kasih joke atau respons lucu

### 🤔 Tanya Tentang Bot
```
"siapa kamu?"
"kamu bot apa?"
"siapa yang buat aplikasi ini?"
```
**Output:** 
- Nama: Claw
- Developer: Dimas
- Fungsi: AI Assistant untuk FTTH project

### 👥 Tanya Tentang Tim
```
"siapa Pak Nova?"
"cerita tentang Eko"
"Kevin orangnya gimana?"
"karakteristik Pak Teguh"
```
**Output:** Karakteristik dan info tentang tim lapangan

### 📚 Tanya Tentang FTTH
```
"apa itu ODP?"
"jelaskan fiber optik"
"apa beda kabel aerial sama underground?"
```
**Output:** Penjelasan teknis tentang FTTH

### ❓ Pertanyaan Di Luar Scope
```
"siapa presiden Indonesia?"
"bagaimana cara membuat kue?"
"apa arti kehidupan?"
```
**Output:** Respons lucu! 😂
- "Wah, ini di luar keahlianku bro 😅 Cari sendiri ya, apa gunanya aplikasi kalo chat pertanyaan yang ada jawabannya bisa di cek pake bot terus! 😂"
- "Aduh, otak AI-ku nge-lag nih 🤖💨 Coba googling aja deh!"
- Dan 3 variasi lucu lainnya

---

## 💡 TIPS & TRIK

### 1. Bahasa Indonesia & English
Bot bisa pakai istilah Indonesia atau English:
- ✅ "kabel" atau "cable"
- ✅ "tiang" atau "pole"
- ✅ Bot otomatis translate ke English untuk search spreadsheet

### 2. Format Tanggal Fleksibel
Bot paham berbagai format tanggal:
- ✅ "10 Mei"
- ✅ "10/05/2026"
- ✅ "kemarin"
- ✅ "hari ini"
- ✅ "minggu ini"

### 3. Copy Text
Tap dan tahan pesan chat untuk copy ke clipboard!

### 4. Typo Friendly
Bot bisa pahami typo ringan:
- "progres" → "progress"
- "kbel" → "kabel"
- "cak" → "cek"

### 5. Pertanyaan Natural
Tidak perlu format kaku, tanya dengan natural:
- ✅ "eh, progress kemarin gimana?"
- ✅ "coba cek stok kabel dong"
- ✅ "ada surat jalan tiang gak?"

---

## 🎯 CONTOH PERCAKAPAN

### Contoh 1: Cek Progress
```
User: "progress kemarin gimana?"

Bot: "📅 Progress Kemarin (Sabtu, 10 Mei 2026):

🔹 Segment 1 - Brebes-Tegal:
   • Cable 24c: 150m
   • Pole 7m: 5 btg

🔹 Segment 4 - Surakarta:
   • Cable 48c: 200m
   • Pole 9m: 8 btg

Total: 2 segment aktif kemarin"
```

### Contoh 2: Surat Jalan
```
User: "surat jalan tiang terakhir"

Bot: "📦 3 Surat Jalan Tiang Terakhir:

📄 10/05/2026 | Pole 7m 10 btg | PT ABC → Gudang MRF | Segment 1
📄 09/05/2026 | Pole 9m 5 btg | PT XYZ → Gudang MRF | Segment 4
📄 08/05/2026 | Pole 7m 8 btg | PT DEF → Gudang MRF | Segment 2

Link: [spreadsheet URL]"
```

### Contoh 3: Admin Command
```
User: "dimmi13 ingat Eko pindah ke Segment 2"

Bot: "✅ Tersimpan di memory!
📝 Eko pindah ke Segment 2

Memory sekarang: 1 item"
```

### Contoh 4: Pertanyaan Lucu
```
User: "siapa presiden Indonesia?"

Bot: "Wah, ini di luar keahlianku bro 😅 Cari sendiri ya, apa gunanya aplikasi kalo chat pertanyaan yang ada jawabannya bisa di cek pake bot terus! 😂"
```

---

## ⚠️ CATATAN PENTING

### ✅ Yang Bisa Bot Lakukan:
- Jawab pertanyaan tentang progress, stok, surat jalan
- Query data dari spreadsheet
- Analisis dan summary data
- Kasih saran teknis FTTH
- Obrolan santai
- Respons lucu kalau tidak bisa jawab

### ❌ Yang TIDAK Bisa Bot Lakukan:
- Edit/hapus data di spreadsheet (hanya baca)
- Upload foto/dokumen
- Kirim notifikasi
- Akses data di luar project ini
- Jawab pertanyaan di luar scope FTTH (tapi kasih respons lucu!)

### 🔄 Auto-Refresh:
- Data otomatis refresh setiap kali buka chat
- Tidak perlu ketik "refresh data"

### 🔐 Admin Mode:
- Login dengan password: `dimmi13` atau `kevingoblok`
- Berlaku 24 jam
- Bisa set personality, memory, instruksi global

---

## 📞 SUPPORT

Kalau ada pertanyaan atau masalah:
1. Coba tanya bot dulu dengan berbagai cara
2. Cek dokumentasi ini
3. Hubungi developer: Dimas

---

**Selamat menggunakan AI Bot! 🚀**

*Last Updated: 11 Mei 2026*
