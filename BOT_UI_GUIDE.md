# 🤖 OPENCLAW Bot - User Interface Guide

**Version**: 2.0
**Last Updated**: 2026-05-09

---

## 📱 Tampilan Halaman Bot

### Layout Overview
```
╔═══════════════════════════════════════════════════════════╗
║  ← | 🤖 OPENCLAW Bot              | 🔄 | ☀              ║
║      ⭕ Bot tidak aktif                                   ║
╠═══════════════════════════════════════════════════════════╣
║  ┌─────────┐  ┌─────────┐  ┌─────────┐                  ║
║  │   ⭕    │  │   ⏱️    │  │   💬    │                  ║
║  │ Status  │  │ Uptime  │  │Messages │                  ║
║  │Offline  │  │00:00:00 │  │    0    │                  ║
║  └─────────┘  └─────────┘  └─────────┘                  ║
╠═══════════════════════════════════════════════════════════╣
║  📋 Console Log              [🗑️ Clear] [❓ Help]        ║
║  ┌───────────────────────────────────────────────────┐   ║
║  │ [System] Belum ada log...                         │   ║
║  │                                                    │   ║
║  │                                                    │   ║
║  │                                                    │   ║
║  │                                                    │   ║
║  └───────────────────────────────────────────────────┘   ║
║  ┌───────────────────────────────────────────────────┐   ║
║  │ 💡 Tips:                                          │   ║
║  │ • Bot akan scan QR WhatsApp saat pertama kali     │   ║
║  │ • Setelah tersambung, bot berjalan otomatis       │   ║
║  │ • Kirim pesan ke bot untuk input data             │   ║
║  └───────────────────────────────────────────────────┘   ║
╠═══════════════════════════════════════════════════════════╣
║  ┌──────────┐  ┌──────────┐  ┌──────────┐              ║
║  │ ▶ Start  │  │ ■ Stop   │  │🔄 Restart│              ║
║  └──────────┘  └──────────┘  └──────────┘              ║
║  ┌─────────────────────┐  ┌──────────┐                  ║
║  │  ⚙️ Konfigurasi     │  │📄 Logs   │                  ║
║  └─────────────────────┘  └──────────┘                  ║
╚═══════════════════════════════════════════════════════════╝
```

---

## 🎯 Fitur-Fitur Utama

### 1. **Header Section**

#### Tombol Back (←)
- **Fungsi**: Kembali ke halaman sebelumnya
- **Behavior**: 
  - Jika bot running → Tampil konfirmasi
  - Jika bot offline → Langsung kembali
- **Warna**: Biru gelap (#1E3A6E)

#### Status Label
- **Format**: "✅ Bot aktif dan terhubung" atau "⭕ Bot tidak aktif"
- **Update**: Otomatis saat status berubah

#### Tombol Refresh (🔄)
- **Fungsi**: Refresh status bot manual
- **Warna**: Abu-abu gelap (#1E293B)

#### Tombol Theme (☀/🌙)
- **Fungsi**: Toggle dark/light mode
- **Icon**: ☀ (light mode) / 🌙 (dark mode)

---

### 2. **Statistics Cards**

#### Card 1: Status
```
┌─────────┐
│   ⭕    │  ← Icon (⭕ offline / ✅ online)
│ Status  │  ← Label
│ Offline │  ← Status text (abu-abu/hijau)
└─────────┘
```
- **Offline**: Icon ⭕, Text abu-abu (#6B7280)
- **Online**: Icon ✅, Text hijau (#10B981)

#### Card 2: Uptime
```
┌─────────┐
│   ⏱️    │  ← Icon timer
│ Uptime  │  ← Label
│00:00:00 │  ← Format HH:MM:SS
└─────────┘
```
- **Update**: Setiap 1 detik
- **Reset**: Saat bot di-stop
- **Start**: Saat bot di-start

#### Card 3: Messages
```
┌─────────┐
│   💬    │  ← Icon chat
│Messages │  ← Label
│    0    │  ← Counter
└─────────┘
```
- **Increment**: Otomatis dari log
- **Reset**: Saat bot di-restart
- **Detection**: Kata "message" atau "pesan"

---

### 3. **Console Log Section**

#### Header Console
- **Label**: "📋 Console Log"
- **Button Clear**: Bersihkan semua log
- **Button Help**: Tampilkan panduan

#### Console Display
- **Background**: Hitam (#0F172A)
- **Text Color**: Abu-abu terang (#94A3B8)
- **Font**: Courier New (monospace)
- **Max Lines**: 500 baris
- **Auto-scroll**: Ya, ke bawah
- **Format Log**: `[HH:MM:SS] [TYPE] message`

#### Log Types
- `[System]` - Pesan sistem
- `[INFO]` - Informasi bot
- `[ERROR]` - Error messages

#### Quick Info Card
- **Icon**: 💡
- **Content**: Tips penggunaan bot
- **Style**: Card dengan border

---

### 4. **Control Buttons**

#### Row 1: Main Controls

##### Button Start (▶ Start)
- **Warna Active**: Hijau (#16A34A)
- **Warna Disabled**: Abu-abu (#6B7280)
- **Fungsi**: Memulai bot WhatsApp
- **Disabled When**: Bot sudah running

##### Button Stop (■ Stop)
- **Warna Active**: Merah (#DC2626)
- **Warna Disabled**: Abu-abu (#6B7280)
- **Fungsi**: Menghentikan bot
- **Konfirmasi**: Ya
- **Disabled When**: Bot tidak running

##### Button Restart (🔄 Restart)
- **Warna Active**: Orange (#F59E0B)
- **Warna Disabled**: Abu-abu (#6B7280)
- **Fungsi**: Restart bot otomatis
- **Konfirmasi**: Ya
- **Disabled When**: Bot tidak running

#### Row 2: Additional Features

##### Button Konfigurasi (⚙️ Konfigurasi)
- **Warna**: Ungu (#6366F1)
- **Fungsi**: Buka menu konfigurasi
- **Menu Options**:
  1. 📝 Edit .env File
  2. 📂 Buka Folder OPENCLAW
  3. 🔧 Install Dependencies
  4. 📋 Lihat Setup Guide

##### Button Logs (📄 Logs)
- **Warna**: Purple (#8B5CF6)
- **Fungsi**: Buka menu logs
- **Menu Options**:
  1. 🗑️ Clear Console
  2. 💾 Save Logs to File (coming soon)
  3. 📤 Share Logs (coming soon)

---

## 🎬 User Flow

### Flow 1: Start Bot Pertama Kali
```
1. User klik "▶ Start"
   ↓
2. System cek Node.js & dependencies
   ↓
3. Console log: "Memulai bot..."
   ↓
4. Bot start, QR code muncul di console
   ↓
5. User scan QR dengan WhatsApp
   ↓
6. Status berubah: ✅ Online
   ↓
7. Uptime timer mulai: 00:00:01, 00:00:02...
   ↓
8. Bot siap menerima pesan
```

### Flow 2: Stop Bot
```
1. User klik "■ Stop"
   ↓
2. Konfirmasi: "Stop bot WhatsApp?"
   ↓
3. User klik "Ya"
   ↓
4. Console log: "Menghentikan bot..."
   ↓
5. Bot process terminated
   ↓
6. Status berubah: ⭕ Offline
   ↓
7. Uptime timer stop & reset
```

### Flow 3: Restart Bot
```
1. User klik "🔄 Restart"
   ↓
2. Konfirmasi: "Restart bot WhatsApp?"
   ↓
3. User klik "Ya"
   ↓
4. Console log: "Restarting bot..."
   ↓
5. Bot stop (delay 2 detik)
   ↓
6. Console log: "Memulai ulang bot..."
   ↓
7. Bot start ulang
   ↓
8. Message counter reset ke 0
```

### Flow 4: Konfigurasi Bot
```
1. User klik "⚙️ Konfigurasi"
   ↓
2. Action sheet muncul dengan 4 opsi
   ↓
3. User pilih opsi (misal: "📋 Lihat Setup Guide")
   ↓
4. Dialog muncul dengan panduan lengkap
   ↓
5. User baca panduan
   ↓
6. User klik "OK"
```

### Flow 5: Clear Console
```
1. User klik "🗑️ Clear" di console header
   ↓
2. Semua log dihapus
   ↓
3. Console menampilkan: "[System] Console cleared"
```

---

## 📊 Status Indicators

### Visual Status Indicators

#### Bot Offline
```
Status Card:  ⭕ Offline (abu-abu)
Uptime:       00:00:00
Messages:     0
Start Button: HIJAU (enabled)
Stop Button:  ABU-ABU (disabled)
Restart:      ABU-ABU (disabled)
```

#### Bot Online
```
Status Card:  ✅ Online (hijau)
Uptime:       00:15:42 (counting)
Messages:     23 (counting)
Start Button: ABU-ABU (disabled)
Stop Button:  MERAH (enabled)
Restart:      ORANGE (enabled)
```

---

## 🎨 Color Reference

### Status Colors
| Status | Color Code | RGB |
|--------|-----------|-----|
| Online | #10B981 | rgb(16, 185, 129) |
| Offline | #6B7280 | rgb(107, 114, 128) |

### Button Colors
| Button | Active | Disabled |
|--------|--------|----------|
| Start | #16A34A (Green) | #6B7280 (Gray) |
| Stop | #DC2626 (Red) | #6B7280 (Gray) |
| Restart | #F59E0B (Orange) | #6B7280 (Gray) |
| Config | #6366F1 (Indigo) | - |
| Logs | #8B5CF6 (Purple) | - |

### Console Colors
| Element | Color Code | Description |
|---------|-----------|-------------|
| Background | #0F172A | Dark blue-black |
| Border | #1E293B | Darker blue |
| Text | #94A3B8 | Light gray |

---

## 💡 Tips & Best Practices

### Untuk User

1. **Pertama Kali Setup**
   - Pastikan Node.js sudah terinstall
   - Copy .env.example ke .env
   - Isi semua konfigurasi di .env
   - Klik Start dan scan QR code

2. **Monitoring Bot**
   - Cek Status card untuk status online/offline
   - Cek Uptime untuk durasi bot berjalan
   - Cek Messages untuk aktivitas bot

3. **Troubleshooting**
   - Jika bot tidak start, cek console log
   - Klik "❓ Help" untuk panduan
   - Klik "⚙️ Konfigurasi" → "📋 Setup Guide"

4. **Maintenance**
   - Clear console secara berkala
   - Restart bot jika ada masalah
   - Cek .env jika bot tidak connect

### Untuk Developer

1. **Testing**
   - Test semua button states
   - Verify uptime timer accuracy
   - Check message counter logic
   - Test all menu options

2. **Debugging**
   - Monitor console log output
   - Check bot service events
   - Verify timer disposal on page close

3. **Future Enhancements**
   - Implement save logs to file
   - Add share logs functionality
   - Display QR code in app
   - Add push notifications

---

## 🐛 Known Issues & Limitations

### Current Limitations
1. **Save Logs** - Belum diimplementasi
2. **Share Logs** - Belum diimplementasi
3. **QR Code Display** - Hanya di console, belum di UI
4. **Message Counter** - Heuristic based (kata "message"/"pesan")

### Workarounds
1. **Save Logs**: Screenshot console untuk sementara
2. **Share Logs**: Copy-paste dari console
3. **QR Code**: Lihat di console log
4. **Message Counter**: Tidak 100% akurat, hanya estimasi

---

## 📞 Support & Help

### In-App Help
- Klik tombol "❓ Help" di console header
- Klik "⚙️ Konfigurasi" → "📋 Lihat Setup Guide"

### Console Log Messages
- `[System]` - Pesan sistem aplikasi
- `[INFO]` - Output normal dari bot
- `[ERROR]` - Error yang perlu perhatian

### Status Messages
- "⭕ Bot tidak aktif" - Bot offline
- "✅ Bot aktif dan terhubung" - Bot online dan siap

---

## 🎉 Summary

Halaman OPENCLAW Bot sekarang memiliki UI yang **professional** dan **user-friendly** dengan:

✅ Real-time statistics (Status, Uptime, Messages)
✅ Dark theme console untuk readability
✅ 6 control buttons (Start, Stop, Restart, Config, Logs, Help)
✅ Configuration menu dengan 4 opsi
✅ Logs management menu
✅ Comprehensive help system
✅ Dynamic button states
✅ Auto-updating uptime timer
✅ Message counter

**Selamat menggunakan OPENCLAW Bot! 🚀**

---

**Version History:**
- v2.0 (2026-05-09): Major UI overhaul dengan stats & menus
- v1.0 (2026-05-09): Initial release dengan basic controls
