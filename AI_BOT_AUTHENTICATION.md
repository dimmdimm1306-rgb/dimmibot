# 🔐 Sistem Autentikasi Bot AI

## Overview
Sistem autentikasi untuk melindungi kepribadian dan memori bot AI agar hanya bisa diubah oleh admin atau pengguna yang terautentikasi.

## 🎯 Fitur Utama

### 1. **Autentikasi Admin**
- **Email Admin**: `dimmdimm1306@gmail.com`
- Admin otomatis memiliki akses penuh untuk mengubah bot
- Tidak perlu password jika login sebagai admin

### 2. **Autentikasi Password**
Pengguna lain bisa mengubah bot dengan password:
- **Password 1**: `kevingoblok`
- **Password 2**: `dimmi13`
- Autentikasi berlaku selama **1 jam** setelah berhasil

### 3. **Proteksi Memori Bot**
- Hanya admin atau pengguna terautentikasi yang bisa mengubah memori
- Pengguna lain akan dialihkan ke percakapan biasa

## �� Cara Menggunakan

### Autentikasi dengan Password

**Format 1: Password + Perintah**
```
dimmi13 catat tim saya ada Andi, Eko, dan Bagas
```

**Format 2: Password Saja (untuk autentikasi)**
```
kevingoblok
```
Bot akan membalas:
```
✅ Autentikasi berhasil! Sekarang kamu bisa mengubah kepribadian dan memori bot selama 1 jam ke depan.
```

### Menambah Memori Bot

Setelah terautentikasi, gunakan perintah:

**Catat informasi:**
```
dimmi13 catat tim saya ada Andi, Eko, dan Bagas
```

**Ingat sesuatu:**
```
kevingoblok ingat lokasi project di Jakarta Selatan
```

**Tambah data:**
```
dimmi13 tambah vendor kabel adalah PT Fiber Indonesia
```

Bot akan membalas:
```
✅ Oke, aku catat ya: "tim saya ada Andi, Eko, dan Bagas"
```

### Mengubah Kepribadian Bot

**Format:**
```
dimmi13 ubah sifat jadi lebih formal dan profesional
```

atau

```
kevingoblok ganti sifat jadi lebih santai dan humoris
```

Bot akan membalas:
```
✅ Kepribadian bot diubah menjadi: "jadi lebih formal dan profesional"
```

### Melihat Memori Bot

**Format:**
```
dimmi13 lihat memori
```

atau

```
kevingoblok cek memori
```

Bot akan menampilkan semua memori yang tersimpan:
```
📝 Memori Bot:
[2026-05-10 16:30] tim saya ada Andi, Eko, dan Bagas
[2026-05-10 16:35] lokasi project di Jakarta Selatan
[2026-05-10 16:40] vendor kabel adalah PT Fiber Indonesia
```

### Menghapus Memori Bot

**Format:**
```
dimmi13 hapus memori
```

atau

```
kevingoblok reset memori
```

Bot akan membalas:
```
🗑️ Memori bot berhasil dihapus.
```

## 🚫 Proteksi untuk Pengguna Tidak Terautentikasi

Jika pengguna yang **bukan admin** dan **tidak terautentikasi** mencoba mengubah bot:

**Contoh:**
```
User (viewer): catat tim saya ada Andi
```

Bot akan membalas:
```
🤔 Hmm, kayaknya kamu lagi ngomongin sesuatu yang menarik. Tapi aku lebih suka ngobrol soal project FTTH deh. Ada yang bisa aku bantu soal progress, stok, atau material?
```

Bot akan **mengalihkan percakapan** dan **tidak akan** menyimpan perubahan.

## �� Perintah yang Dilindungi

Perintah berikut **hanya bisa digunakan** oleh admin atau pengguna terautentikasi:

1. **Catat / Ingat / Tambah** - Menambah memori bot
2. **Ubah sifat / Ganti sifat / Ubah kepribadian** - Mengubah kepribadian bot
3. **Lihat memori / Cek memori** - Melihat memori bot
4. **Hapus memori / Reset memori** - Menghapus memori bot

## 💡 Tips

1. **Autentikasi berlaku 1 jam** - Setelah autentikasi dengan password, Anda bisa mengubah bot selama 1 jam tanpa perlu password lagi
2. **Admin tidak perlu password** - Jika login sebagai `dimmdimm1306@gmail.com`, langsung bisa mengubah bot
3. **Password case-sensitive** - Pastikan menulis password dengan benar
4. **Memori tersimpan permanen** - Memori bot disimpan di Preferences dan akan tetap ada setelah restart aplikasi
5. **Kepribadian mempengaruhi semua percakapan** - Setelah kepribadian diubah, bot akan menggunakan kepribadian baru untuk semua user

## 🎨 Perubahan Logo

Logo bot telah diubah dari 🤖 (robot) menjadi 🐾 (cakar kucing) untuk mencerminkan nama "OPENCLAW" yang lebih friendly dan unik.

## 🔧 Technical Details

### Penyimpanan Data
- **Kepribadian**: Disimpan di `Preferences` dengan key `bot_personality`
- **Memori**: Disimpan di `Preferences` dengan key `bot_memory`
- **Autentikasi**: Disimpan di memory (tidak persisten, expire setelah 1 jam)

### Keamanan
- Password di-hardcode di `AiChatService.cs`
- Email admin di-hardcode di `AiChatService.cs`
- Autentikasi expire otomatis setelah 1 jam
- Tidak ada logging password untuk keamanan

### Integrasi dengan System Prompt
Kepribadian dan memori bot diinjeksi ke system prompt saat setiap percakapan:
```csharp
private string GetBotPersonalityAndMemory()
{
    // Menambahkan kepribadian khusus
    // Menambahkan memori bot
    // Return sebagai bagian dari system prompt
}
```

## 📱 Contoh Skenario Penggunaan

### Skenario 1: Admin Menambah Tim
```
Admin (dimmdimm1306@gmail.com): catat tim saya ada Andi, Eko, dan Bagas
Bot: ✅ Oke, aku catat ya: "tim saya ada Andi, Eko, dan Bagas"

User lain: siapa aja tim kita?
Bot: Tim kita ada Andi, Eko, dan Bagas. Ada yang mau kamu tanyakan tentang mereka?
```

### Skenario 2: Viewer Mencoba Mengubah
```
Viewer: catat vendor baru PT ABC
Bot: 🤔 Hmm, kayaknya kamu lagi ngomongin sesuatu yang menarik. Tapi aku lebih suka ngobrol soal project FTTH deh. Ada yang bisa aku bantu soal progress, stok, atau material?
```

### Skenario 3: Autentikasi dengan Password
```
User: dimmi13
Bot: ✅ Autentikasi berhasil! Sekarang kamu bisa mengubah kepribadian dan memori bot selama 1 jam ke depan.

User: catat lokasi project di Bandung
Bot: ✅ Oke, aku catat ya: "lokasi project di Bandung"

User: ubah sifat jadi lebih profesional
Bot: ✅ Kepribadian bot diubah menjadi: "jadi lebih profesional"
```

## 🎯 Kesimpulan

Sistem autentikasi ini memastikan bahwa:
- ✅ Hanya admin atau pengguna terautentikasi yang bisa mengubah bot
- ✅ Memori dan kepribadian bot terlindungi dari perubahan tidak sah
- ✅ Pengguna lain tetap bisa menggunakan bot untuk percakapan biasa
- ✅ Pengalaman pengguna tetap smooth dengan redirect otomatis
- ✅ Logo bot lebih menarik dan sesuai dengan nama OPENCLAW
