# Setup AI Consultant (Groq API)

Bot sekarang bisa menjawab **SEMUA pertanyaan bebas** tentang fiber optik menggunakan AI!

## 🚀 Cara Setup (GRATIS)

### 1. Daftar Groq API (Gratis & Cepat)

1. Buka: https://console.groq.com/
2. Sign up dengan Google/GitHub
3. Setelah login, klik **API Keys** di sidebar
4. Klik **Create API Key**
5. Copy API key yang muncul

### 2. Tambahkan ke .env

Buka file `.env` dan isi:

```env
AI_ENABLED=true
AI_PROVIDER=groq
GROQ_API_KEY=gsk_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
```

### 3. Restart Bot

```bash
npm start
```

## ✨ Fitur AI

Bot sekarang bisa menjawab pertanyaan bebas seperti:

### Pertanyaan Teknis:
```
ODC itu apa?
Apa bedanya ODP dan ODC?
Jelaskan cara splicing fiber
Bagaimana cara ukur loss dengan OTDR?
Kenapa sinyal ONT lemah?
Berapa loss normal untuk splitter 1:8?
```

### Troubleshooting:
```
Cara troubleshoot fiber putus?
ONT tidak dapat sinyal, apa penyebabnya?
Bagaimana cara cek kualitas fiber?
Loss terlalu tinggi, solusinya apa?
```

### Best Practice:
```
Cara instalasi dropcore yang benar?
Tips maintenance ODC?
Standar power level untuk ONT?
Cara proteksi sambungan fiber?
```

### Pertanyaan Umum:
```
Apa itu GPON?
Bedanya single mode dan multi mode?
Fungsi splice closure?
Kapan pakai OTDR vs Power Meter?
```

## 🎯 Cara Kerja

1. **Knowledge Base** (Cepat): Bot cek dulu di knowledge base lokal untuk istilah umum (ODC, ODP, ONT, dll)
2. **AI Consultant** (Pintar): Jika tidak ada di knowledge base, bot tanya ke AI untuk jawaban detail
3. **Data Query** (Spreadsheet): Jika pakai kata "cek", "cari", bot cari di spreadsheet Progress
4. **Input Arsip** (Inventory): Jika ada angka + kata "ke", bot simpan ke spreadsheet Arsip

## 📊 Prioritas Jawaban

```
1. Knowledge Base (instant) → ODC, ODP, ONT, dll
2. AI Consultant (2-3 detik) → Pertanyaan bebas
3. Data Query → cek sragen, cari site 123
4. Input Arsip → kabel 24000 ke brebes
5. Casual Reply → halo, tes, thanks
```

## 💡 Tips

1. **Pertanyaan spesifik** = jawaban lebih akurat
2. **Bahasa Indonesia** atau **English** sama-sama bisa
3. **AI gratis** tapi ada rate limit (30 request/menit)
4. **Offline mode**: Set `AI_ENABLED=false` jika tidak perlu AI

## ⚙️ Konfigurasi

### Disable AI:
```env
AI_ENABLED=false
```

### Model AI:
Default: `llama-3.3-70b-versatile` (paling pintar & cepat)

Bisa diganti di `src/ai-consultant.js`:
- `llama-3.3-70b-versatile` (recommended)
- `llama-3.1-70b-versatile`
- `mixtral-8x7b-32768`

## 🔒 Keamanan

- API key disimpan di `.env` (jangan commit ke git)
- `.env` sudah ada di `.gitignore`
- Groq API gratis tapi ada rate limit
- Tidak ada data sensitif dikirim ke AI

## 📈 Rate Limit Groq (Free Tier)

- **30 requests per minute**
- **14,400 requests per day**
- Cukup untuk grup WhatsApp kecil-menengah

Jika limit terlewati, bot akan skip AI dan pakai knowledge base saja.

## ❓ Troubleshooting

### Bot tidak jawab pertanyaan:
1. Cek `AI_ENABLED=true` di `.env`
2. Cek `GROQ_API_KEY` sudah diisi
3. Cek log terminal untuk error
4. Test API key di: https://console.groq.com/playground

### Error "API key invalid":
1. Generate API key baru di Groq console
2. Copy paste dengan benar (tanpa spasi)
3. Restart bot

### Jawaban lambat:
- Normal, AI butuh 2-3 detik
- Groq sudah paling cepat dibanding OpenAI/Claude
- Knowledge base tetap instant

## 🎉 Selesai!

Bot sekarang pintar dan bisa jawab pertanyaan apa saja tentang fiber optik! 🚀
