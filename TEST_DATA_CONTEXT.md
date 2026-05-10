# Test Data Context - Panduan Cepat

**Dibuat:** 10 Mei 2026, 21:01 WIB

## 🎯 Apa yang Baru?

Bot sekarang bisa **akses data project kamu**! Progress, stok gudang, surat jalan - semua bisa ditanya ke bot.

---

## �� Cara Test di HP

### 1. Buka Aplikasi
- Pastikan sudah buka **project** (bukan di home screen)
- Buka halaman **AI Chat**

### 2. Muat Data Dulu
Ketik perintah ini untuk muat data:
```
refresh data
```

Bot akan jawab:
```
✅ Data berhasil di-refresh! Sekarang aku punya data terbaru dari project.

📊 Info: 2847 karakter data ter-load.
```

### 3. Tanya tentang Data
Coba pertanyaan ini:

**Progress:**
```
Berapa progress kabel di segment 1?
```

**Stok:**
```
Berapa stok kabel fiber di gudang?
```

**Surat Jalan:**
```
Kapan terakhir ada barang masuk?
```

**Analisis:**
```
Segment mana yang paling lambat progressnya?
```

---

## ✅ Expected Results

### Jika Data Ter-Load dengan Baik:
Bot akan jawab dengan **data spesifik** dari project kamu:

```
📊 Progress Segment 1 (Semarang Utara):
- Kabel: 1,200/1,500m (80%)
- Tiang 7m: 45/50 batang (90%)
- Tiang 9m: 30/35 batang (86%)

Progress segment 1 udah lumayan bagus nih! Kabel udah 80%, tiang juga hampir selesai. Tinggal dikit lagi! 💪
```

### Jika Data Belum Ter-Load:
Bot akan minta kamu refresh data:

```
Data belum ter-load. Coba ketik "refresh data" dulu ya!
```

---

## 🐛 Troubleshooting

### Bot Bilang "Data belum ter-load"
**Solusi:** Ketik `refresh data`

### Bot Tidak Tahu Data Terbaru
**Solusi:** Ketik `refresh data` lagi untuk reload

### Bot Jawab "Data fetch error"
**Solusi:** 
1. Cek koneksi internet
2. Pastikan sudah buka project
3. Coba sync manual di halaman Progress

---

## 🔍 Cek Debug Log (Untuk Developer)

Jika pakai Visual Studio, cek Output window untuk log:

```
[AiChatService] Building fresh context...
[AiChatService] Fetching data from sheets...
[AiChatService] Found 6 progress items
[AiChatService] Found 12 warehouses
[AiChatService] Found 45 surat jalan
[AiChatService] Context built, length: 2847 chars
```

Jika tidak ada log ini, berarti data tidak ter-fetch.

---

## 📋 Checklist Test

- [ ] Buka aplikasi dan login
- [ ] Buka project (bukan di home screen)
- [ ] Buka AI Chat
- [ ] Ketik `refresh data`
- [ ] Bot jawab "✅ Data berhasil di-refresh"
- [ ] Tanya "Berapa progress kabel di segment 1?"
- [ ] Bot jawab dengan data spesifik (angka progress)
- [ ] Tanya "Berapa stok kabel fiber di gudang?"
- [ ] Bot jawab dengan data stok
- [ ] Tanya "Kapan terakhir ada barang masuk?"
- [ ] Bot jawab dengan tanggal surat jalan

---

## 🎉 Jika Semua Berhasil

Bot sekarang bisa:
- ✅ Akses data progress real-time
- ✅ Cek stok gudang
- ✅ Lihat riwayat surat jalan
- ✅ Analisis progress per segment
- ✅ Kasih saran berdasarkan data

**Ini yang kamu mau kemarin - bot bisa jawab data progress!** 🚀

---

## 💡 Tips

1. **Refresh data setelah input banyak** - Setelah tambah surat jalan atau update progress
2. **Tanya spesifik** - "Progress segment 1" lebih baik dari "gimana progress?"
3. **Gunakan bahasa santai** - Bot paham bahasa Indonesia natural

---

## 📝 Catatan Penting

- **Data di-cache** - Setelah refresh, data disimpan sampai kamu refresh lagi
- **Tidak real-time** - Harus manual refresh untuk data terbaru
- **Butuh internet** - Untuk fetch data dari Google Sheets
- **Laptop harus ON** - Server GPT-4o-mini berjalan di laptop

---

**Selamat mencoba! Sekarang bot bisa bantu analisis data project kamu! 🎯**
