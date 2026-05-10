# AI Chat Bot - Troubleshooting Guide

## Masalah yang Diperbaiki

### 1. Bot Tidak Muncul / Tidak Bisa Diklik

**Penyebab:**
- `InputTransparent="True"` pada AbsoluteLayout memblokir semua input
- FloatingAiButton belum diinjeksi ke halaman
- Z-index rendah sehingga tertutup elemen lain

**Solusi yang Diterapkan:**
- ✅ Ubah `InputTransparent="False"` pada ContentView dan AbsoluteLayout
- ✅ Tambahkan `ZIndex="1000"` pada Border button
- ✅ Injection otomatis di RootTabbedPage untuk semua tab
- ✅ Tambahkan debug logging untuk tracking

### 2. Bot Tidak Merespon Tap

**Penyebab:**
- Gesture recognizer tidak terpicu
- Konflik antara PanGestureRecognizer dan TapGestureRecognizer
- Error saat membuka AiChatPopup

**Solusi yang Diterapkan:**
- ✅ Tambahkan threshold drag (10px) untuk membedakan tap vs drag
- ✅ Tambahkan extensive error handling dan logging
- ✅ Perbaiki navigation logic untuk berbagai tipe page

## Cara Testing

### 1. Cek Apakah Bot Muncul

Setelah build dan run aplikasi:

1. Buka aplikasi dan login
2. Masuk ke halaman utama (RootTabbedPage dengan tabs)
3. **Cari tombol bulat biru dengan emoji 🐱 di pojok kanan bawah**
4. Tombol harus terlihat di semua tab: Surat Jalan, Progress, Stok Diterima, Stok Gudang, Input

**Jika tidak muncul:**
- Cek debug log untuk pesan `[AI] Injecting FloatingAiButton`
- Pastikan AiChatService terdaftar di MauiProgram.cs (sudah ✅)

### 2. Test Tap/Click

1. **Tap sekali** pada tombol bot
2. Harus ada animasi scale (mengecil lalu kembali normal)
3. Popup chat harus muncul dengan:
   - Header "AI Assistant" 
   - Welcome message
   - Quick action chips
   - Input field di bawah

**Jika tidak merespon:**
- Cek debug log untuk pesan `[FloatingAiButton] Tapped!`
- Jika tidak ada log, berarti gesture recognizer tidak terpicu
- Coba tap di tengah-tengah tombol, bukan di tepi

### 3. Test Drag

1. **Tekan dan tahan** tombol bot
2. **Geser** ke posisi lain
3. Lepas - tombol harus snap ke tepi kiri atau kanan terdekat

**Expected behavior:**
- Saat drag > 10px, `_isDragging` menjadi true
- Saat lepas setelah drag, chat popup TIDAK boleh terbuka
- Debug log: `[FloatingAiButton] Pan Started`, `Pan Completed, isDragging=true`

### 4. Test Chat Functionality

1. Buka chat popup
2. Ketik pesan: "Halo"
3. Tekan tombol kirim (➤)
4. Harus muncul loading indicator
5. Setelah beberapa detik, AI harus merespon

**Jika error:**
- Cek koneksi internet
- Cek API key di AiChatService (sudah ada default key)
- Cek debug log untuk error message

### 5. Test Quick Actions

1. Buka chat popup
2. Tap salah satu chip quick action (misal: "📅 Progress hari ini")
3. Pertanyaan harus otomatis terisi di input field
4. Chat harus langsung terkirim

## Debug Logs yang Harus Muncul

Saat aplikasi berjalan normal, Anda harus melihat log berikut:

```
[AI] Injecting FloatingAiButton to SuratJalanPage
[AI] FloatingAiButton injected successfully to SuratJalanPage
[AI] Injecting FloatingAiButton to ProgressPage
[AI] FloatingAiButton injected successfully to ProgressPage
... (untuk setiap tab)
[FloatingAiButton] Initialized
```

Saat tap tombol:
```
[FloatingAiButton] Tapped! isDragging=False
[FloatingAiButton] Opening chat...
[FloatingAiButton] AI Service found, creating popup...
[FloatingAiButton] Current page type: NavigationPage
[FloatingAiButton] Pushing modal via TabbedPage.CurrentPage
[FloatingAiButton] Chat popup opened successfully
```

## Troubleshooting Lanjutan

### Bot Muncul Tapi Tidak Bisa Diklik

**Kemungkinan penyebab:**
1. Ada elemen lain dengan Z-index lebih tinggi yang menutupi
2. Parent container memiliki InputTransparent=True
3. Platform-specific issue (Android/iOS)

**Solusi:**
```xml
<!-- Pastikan di FloatingAiButton.xaml -->
<ContentView InputTransparent="False">
    <AbsoluteLayout InputTransparent="False">
        <Border InputTransparent="False" ZIndex="1000">
```

### Chat Popup Tidak Muncul

**Kemungkinan penyebab:**
1. AiChatService tidak terdaftar di DI
2. Navigation stack error
3. Modal push gagal

**Solusi:**
- Cek MauiProgram.cs: `builder.Services.AddSingleton<AiChatService>();` ✅
- Cek debug log untuk error message
- Coba restart aplikasi

### AI Tidak Merespon

**Kemungkinan penyebab:**
1. Tidak ada koneksi internet
2. API key invalid
3. OpenRouter API down
4. Data belum dimuat (GoogleSheetsService)

**Solusi:**
- Pastikan ada koneksi internet
- Cek API key di `AiChatService.cs` (ada default key)
- Buka halaman utama dulu untuk load data
- Coba model AI lain via settings (⚙)

## Perubahan yang Dilakukan

### FloatingAiButton.xaml
- ✅ Ubah `InputTransparent="True"` → `"False"` pada AbsoluteLayout
- ✅ Tambahkan `InputTransparent="False"` pada ContentView
- ✅ Tambahkan `ZIndex="1000"` pada Border

### FloatingAiButton.xaml.cs
- ✅ Tambahkan `DRAG_THRESHOLD = 10` untuk membedakan tap vs drag
- ✅ Tambahkan extensive debug logging di semua method
- ✅ Tambahkan try-catch dan error handling
- ✅ Perbaiki OpenChatAsync dengan better error messages
- ✅ Ubah animation dari `false` → `true` untuk smooth transition

### RootTabbedPage.xaml.cs
- ✅ Tambahkan debug logging di InjectFloatingButton
- ✅ Tambahkan ZIndex=1000 pada FAB saat injection
- ✅ Perbaiki Grid setup dengan explicit Row/Column

## Testing Checklist

- [ ] Bot muncul di semua tab
- [ ] Bot bisa di-tap (muncul animasi)
- [ ] Chat popup terbuka saat tap
- [ ] Bot bisa di-drag ke posisi lain
- [ ] Bot snap ke tepi saat lepas drag
- [ ] Chat popup TIDAK terbuka saat drag
- [ ] Bisa ketik dan kirim pesan
- [ ] AI merespon dengan benar
- [ ] Quick action chips berfungsi
- [ ] Settings button buka model picker
- [ ] Clear chat berfungsi
- [ ] Close button tutup popup
- [ ] Tap backdrop tutup popup

## Kontak & Support

Jika masih ada masalah setelah mengikuti panduan ini:
1. Cek debug log di Output window
2. Screenshot error message
3. Catat langkah-langkah untuk reproduce masalah
4. Hubungi developer dengan informasi di atas
