# Quick Test Guide - AI Chat Bot di HP

## ✅ Status Saat Ini (10 Mei 2026, 20:51 WIB)

**Semua sistem SIAP DIGUNAKAN!**

- ✅ Server berjalan di laptop (port 8080)
- ✅ Cloudflare tunnel aktif
- ✅ GPT-4o-mini bekerja dengan baik
- ✅ Config sudah di-push ke GitHub

## 📱 Cara Test di HP

### 1. Buka Aplikasi StokBarangMAUI
   - Pastikan HP terkoneksi internet
   - Buka aplikasi

### 2. Restart Aplikasi (Jika Perlu)
   - Close aplikasi sepenuhnya
   - Buka lagi
   - Aplikasi akan auto-download config terbaru dari GitHub

### 3. Buka AI Chat
   - Cari menu/tombol AI Chat atau OpenClaw Bot
   - Klik untuk membuka halaman chat

### 4. Test Chat
   Coba kirim pesan:
   - "Halo, siapa kamu?"
   - "Jelaskan apa itu FTTH"
   - "Berapa stok kabel fiber di gudang?" (jika bot sudah terintegrasi dengan data)

### 5. Expected Response
   - Bot akan menjawab dalam Bahasa Indonesia
   - Response time: 1-2 detik
   - Bot menggunakan GPT-4o-mini (model OpenAI)

## ⚠️ Troubleshooting

### Jika Bot Tidak Merespons:

1. **Cek koneksi internet HP**
   - Pastikan HP terkoneksi WiFi/data

2. **Cek laptop masih nyala**
   - Server berjalan di laptop, jadi laptop harus tetap ON
   - Jangan sleep/hibernate

3. **Cek tunnel URL masih aktif**
   - Buka browser di HP
   - Akses: https://sao-phases-consultants-arising.trycloudflare.com/health
   - Harus muncul: `{"status":"ok","service":"OpenClaw API Server",...}`

4. **Restart aplikasi**
   - Close app sepenuhnya
   - Buka lagi

5. **Cek log di laptop**
   - Lihat Terminal 7 (API Server)
   - Harus ada log: `[API] Chat request | ... | requested: gpt-4o-mini`

## 🔍 Verifikasi Config di App

Jika app punya halaman settings/config, cek:
- **Base URL:** `https://sao-phases-consultants-arising.trycloudflare.com/v1`
- **Model:** `gpt-4o-mini`
- **API Key Required:** `false`

## 💡 Tips

1. **Laptop harus tetap ON** selama test
2. **Jangan close Terminal 4 dan 7** di laptop
3. **Tunnel URL berubah setiap restart** - jika restart tunnel, harus update config lagi
4. **Bot bisa Bahasa Indonesia** - chat pakai bahasa Indonesia aja

## 📊 Biaya

- GPT-4o-mini: $0.15 per 1 juta token input
- Estimasi: ~$0.01 per 100 pesan chat
- Sangat murah untuk testing

## ✅ Checklist Sebelum Test

- [ ] Laptop ON dan terkoneksi internet
- [ ] Terminal 7 (API Server) masih running
- [ ] Terminal 4 (Cloudflare Tunnel) masih running
- [ ] HP terkoneksi internet
- [ ] Aplikasi sudah di-restart (untuk fetch config terbaru)

---

**Siap test! Coba buka app di HP dan chat dengan bot! 🚀**
