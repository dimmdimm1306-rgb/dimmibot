# AI Bot Data Context Guide

**Last Updated:** 2026-05-10 21:01 WIB

## 📊 Apa itu Data Context?

Data Context adalah fitur yang memungkinkan AI bot (Claw) untuk **mengakses dan memahami data project kamu** dari Google Sheets, termasuk:

- 📈 **Progress Resume** - Progress kabel, tiang per segment
- 📦 **Stok Gudang** - Inventory material di setiap gudang
- 📄 **Surat Jalan** - Riwayat keluar-masuk barang
- 🏗️ **Project Info** - Nama project, deskripsi, segment

Dengan data context, bot bisa menjawab pertanyaan seperti:
- "Berapa progress kabel di segment 1?"
- "Stok kabel fiber di gudang berapa?"
- "Kapan terakhir ada surat jalan masuk?"

---

## 🔧 Cara Kerja

### 1. **Auto-Inject Context**
Saat aplikasi dibuka, context otomatis di-inject ke AI service:

```csharp
// Di RootTabbedPage.xaml.cs
var aiService = Services.GetRequiredService<AiChatService>();
aiService.SetProjectContext(sheets, project);
```

### 2. **Lazy Loading**
Data di-fetch **hanya saat pertama kali chat**, bukan saat app dibuka. Ini menghemat bandwidth dan mempercepat startup.

### 3. **Caching**
Setelah data di-fetch, disimpan di cache. Chat berikutnya menggunakan cache tanpa fetch ulang.

### 4. **Manual Refresh**
User bisa refresh data kapan saja dengan perintah khusus.

---

## 💬 Perintah untuk User

### Refresh Data
Ketik salah satu perintah ini untuk muat ulang data terbaru:

```
refresh data
reload data
update data
muat ulang data
```

**Response:**
```
✅ Data berhasil di-refresh! Sekarang aku punya data terbaru dari project.

📊 Info: 2847 karakter data ter-load.
```

### Contoh Pertanyaan tentang Data

**Progress:**
- "Berapa progress kabel di segment 1?"
- "Segment mana yang paling lambat progressnya?"
- "Sudah berapa persen total progress?"

**Stok:**
- "Berapa stok kabel fiber di gudang?"
- "Material apa aja yang hampir habis?"
- "Gudang mana yang paling banyak stok?"

**Surat Jalan:**
- "Kapan terakhir ada barang masuk?"
- "Barang apa yang paling sering keluar?"
- "Berapa total surat jalan bulan ini?"

**Analisis:**
- "Segment mana yang butuh tambahan material?"
- "Estimasi kapan project selesai?"
- "Ada masalah di progress segment berapa?"

---

## 🐛 Troubleshooting

### Bot Bilang "Data belum ter-load"

**Penyebab:**
1. Belum pernah chat sejak buka app
2. Data belum di-sync dari Google Sheets
3. Context belum di-inject

**Solusi:**
1. Ketik `refresh data` untuk muat data
2. Pastikan sudah buka project (bukan di home screen)
3. Pastikan internet aktif untuk sync Google Sheets

### Bot Tidak Tahu Data Terbaru

**Penyebab:**
Data di-cache, belum di-refresh setelah ada perubahan di Sheets.

**Solusi:**
Ketik `refresh data` untuk muat ulang data terbaru.

### Bot Jawab "Data fetch error"

**Penyebab:**
1. Tidak ada koneksi internet
2. Google Sheets API error
3. Spreadsheet tidak bisa diakses

**Solusi:**
1. Cek koneksi internet
2. Coba sync manual di halaman Progress/Stok
3. Cek apakah spreadsheet masih bisa dibuka di browser

---

## 🔍 Debug Logging

Untuk developer, ada logging di debug console:

```
[AiChatService] Using cached context
[AiChatService] Building fresh context...
[AiChatService] Fetching data from sheets...
[AiChatService] Found 6 progress items
[AiChatService] Found 12 warehouses
[AiChatService] Found 45 surat jalan
[AiChatService] Context built, length: 2847 chars
[AiChatService] Context length: 2847 chars
[AiChatService] System prompt length: 3456 chars
```

Cek log ini untuk diagnosa masalah data context.

---

## 📋 Data yang Di-Include

### 1. Project Info
```
PROJECT: FTTH Jawa Tengah
Deskripsi: Instalasi fiber optik 6 segment
Segments: 6
  Segment 1: Semarang Utara
  Segment 2: Semarang Selatan
  ...
```

### 2. Progress Resume
```
── PROGRESS RESUME ──
  Seg 1 (Semarang Utara): Kabel 1200/1500m (80%), T7 45/50btg (90%), T9 30/35btg (86%)
  Seg 2 (Semarang Selatan): Kabel 800/1200m (67%), T7 30/40btg (75%), T9 20/30btg (67%)
  ...
```

### 3. Total Resume
```
── TOTAL RESUME ──
  Kabel: 5,400/7,200m
  Tiang 7m: 180/240btg
  Tiang 9m: 120/180btg
```

### 4. Stok Gudang (Top 10)
```
── STOK GUDANG ──
  Gudang Pusat (Semarang): Kabel Fiber: sisa 500, Tiang 7m: sisa 20, ...
  Gudang Segment 1: Kabel Fiber: sisa 150, Tiang 7m: sisa 5, ...
  ...
```

### 5. Surat Jalan Terbaru (Top 10)
```
── SURAT JALAN TERBARU ──
  2026-05-10 | Masuk | Kabel Fiber 200 meter | Segment 1
  2026-05-09 | Keluar | Tiang 7m 10 batang | Segment 2
  ...
```

---

## ⚙️ Konfigurasi untuk Developer

### Invalidate Cache Programmatically

```csharp
// Force refresh context on next message
aiService.InvalidateContext();
```

### Clear Chat History

```csharp
// Clear conversation history (also clears cache)
aiService.ClearHistory();
```

### Set Project Context

```csharp
// Inject project context
aiService.SetProjectContext(sheetsService, projectConfig);
```

---

## 🎯 Best Practices

### Untuk User:
1. **Refresh data setelah update besar** - Setelah input banyak surat jalan atau progress
2. **Tanya spesifik** - "Progress segment 1" lebih baik dari "gimana progress?"
3. **Gunakan bahasa natural** - Bot paham bahasa Indonesia santai

### Untuk Developer:
1. **Jangan fetch terlalu sering** - Gunakan cache untuk performa
2. **Limit data yang di-include** - Top 10 saja untuk setiap kategori
3. **Handle error gracefully** - Jangan crash kalau Sheets error
4. **Log untuk debugging** - Gunakan Debug.WriteLine untuk trace

---

## 📈 Performance

### Data Size
- **Typical context:** 2,000 - 5,000 characters
- **Max context:** ~10,000 characters (untuk project besar)
- **Fetch time:** 1-3 detik (tergantung koneksi)

### Token Usage
- **System prompt:** ~500-800 tokens
- **Context data:** ~400-1,000 tokens
- **Total per message:** ~1,000-2,000 tokens

### Cost Impact (GPT-4o-mini)
- **Input:** $0.15 per 1M tokens
- **Per message with context:** ~$0.0003 (0.03 sen)
- **Negligible cost increase** dibanding tanpa context

---

## 🚀 Future Enhancements

### Planned Features:
- [ ] Auto-refresh setiap X menit
- [ ] Real-time data sync (WebSocket)
- [ ] Filter data by segment
- [ ] Include foto/attachment dari Drive
- [ ] Historical data comparison
- [ ] Predictive analytics

---

## 📞 Support

Jika bot tidak bisa akses data:
1. Cek log debug di console
2. Coba `refresh data`
3. Restart aplikasi
4. Cek koneksi internet dan Google Sheets access

**Masih bermasalah?** Hubungi developer (Dimas).
