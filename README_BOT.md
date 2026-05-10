# 🤖 OPENCLAW WhatsApp Bot - Quick Reference

**Version**: 2.0.0  
**Last Updated**: 9 Mei 2026

---

## 🚀 Quick Start (5 Menit)

### 1. Install APK
```bash
adb install com.companyname.stokbarangmaui-Signed.apk
```

### 2. Setup Bot
```bash
# Di folder OPENCLAW
cp .env.example .env
nano .env  # Edit konfigurasi
npm install
```

### 3. Start Bot
1. Buka aplikasi → Klik **🤖 Bot**
2. Klik **▶ Start**
3. Scan QR code dengan WhatsApp
4. ✅ Bot siap!

---

## 📱 Cara Pakai Bot

### Input Barang
```
masuk 100 kabel fiber
keluar 50 kabel fiber
dibawa 25 kabel fiber
```
*Bisa kirim foto barang*

### Cek Progress
```
cek Jakarta
cek 2026-05-09
cek
```

### Konsultasi AI
```
Apa itu fiber optik?
Bagaimana cara splicing fiber?
```

---

## 🎮 Kontrol Bot di Aplikasi

### Tombol Utama
- **▶ Start** - Mulai bot
- **■ Stop** - Hentikan bot
- **🔄 Restart** - Restart bot

### Menu Tambahan
- **⚙️ Konfigurasi** - Setup & config
- **📄 Logs** - Log management
- **❓ Help** - Panduan lengkap

### Statistik Real-time
- **Status** - Online/Offline
- **Uptime** - Durasi bot berjalan
- **Messages** - Jumlah pesan diproses

---

## ⚙️ Konfigurasi (.env)

```env
# Google Sheets
SPREADSHEET_ID=1abc...xyz
SHEET_NAME=Data Barang

# Google Drive
DRIVE_FOLDER_ID=1def...uvw

# OpenRouter AI
OPENROUTER_API_KEY=sk-or-v1-...

# Bot akan auto-generate session WhatsApp
```

---

## 🔧 Troubleshooting

### Bot Tidak Start
```
✓ Cek Node.js terinstall: node --version
✓ Cek dependencies: npm install
✓ Cek .env file ada dan terisi
✓ Cek console log untuk error
```

### QR Code Tidak Muncul
```
✓ Tunggu 10-15 detik
✓ Scroll console log ke atas
✓ Restart bot jika perlu
```

### Bot Disconnect
```
✓ Klik tombol Restart
✓ Cek koneksi internet
✓ Cek WhatsApp tidak logout
```

---

## 📊 Status Indicators

| Icon | Status | Arti |
|------|--------|------|
| ✅ | Online | Bot aktif & terhubung |
| ⭕ | Offline | Bot tidak aktif |
| 🟡 | Starting | Bot sedang start |
| 🔴 | Error | Ada error, cek log |

---

## 💡 Tips & Tricks

### Best Practices
1. ✅ Selalu cek status sebelum kirim pesan
2. ✅ Clear console secara berkala
3. ✅ Restart bot jika ada masalah
4. ✅ Backup .env file

### Keyboard Shortcuts
- Tidak ada (mobile app)

### Performance Tips
- Restart bot setiap 24 jam
- Clear console jika >500 lines
- Monitor uptime & messages

---

## 📚 Dokumentasi Lengkap

- **CHANGELOG_OPENCLAW_BOT_INTEGRATION.md** - Detail integrasi
- **CHANGELOG_BOT_UI_IMPROVEMENTS.md** - Perbaikan UI
- **BOT_UI_GUIDE.md** - Panduan UI lengkap
- **RELEASE_NOTES_v2.0.md** - Release notes

---

## �� Need Help?

### In-App
- Klik **❓ Help** di console
- Klik **⚙️ Konfigurasi** → **📋 Setup Guide**

### Console Log
- `[System]` - Pesan sistem
- `[INFO]` - Output bot
- `[ERROR]` - Error messages

---

## 🎯 Fitur Utama

✅ WhatsApp bot integration  
✅ Real-time statistics  
✅ Dark theme console  
✅ 6 control buttons  
✅ Configuration menu  
✅ Logs management  
✅ Help system  
✅ Uptime timer  
✅ Message counter  
✅ Auto-scroll console  

---

## 📦 APK Info

**File**: `com.companyname.stokbarangmaui-Signed.apk`  
**Size**: 38.7 MB  
**Location**: `bin\Release\net9.0-android\publish\`  
**Build**: 2026-05-09 20:52 WIB  

---

**Happy Bot-ing! 🚀**
