# 🐱 AI Chat Bot - Dokumentasi Lengkap

## Overview
AI Chat Bot terintegrasi dalam aplikasi monitoring FTTH yang dapat menjawab pertanyaan tentang progress pekerjaan, stok material, dan surat jalan menggunakan OpenRouter API.

---

## ✨ Fitur Utama

### 1. **Floating Button 🐱**
- Tombol kucing mengambang di pojok kanan bawah
- Muncul di semua halaman tab (Surat Jalan, Progress, Stok, Input)
- Bisa digeser ke mana saja, otomatis snap ke tepi layar
- Tap untuk membuka chat popup

### 2. **Chat Interface**
- Popup chat yang bisa digeser
- Chat history (ingat 20 percakapan terakhir)
- Quick action chips untuk pertanyaan cepat:
  - 📅 Progress hari ini
  - 📡 Resume segment
  - 📦 Cek stok
  - 📋 Surat jalan

### 3. **Kemampuan Bot**
Bot mengetahui semua data aplikasi secara real-time:

#### Progress Pekerjaan
- Progress harian hari ini (per segment, per span, per material)
- Progress kemarin (fallback jika hari ini kosong)
- Ringkasan 7 hari terakhir
- Progress kumulatif per segment (Kabel 24C, Tiang 7m, Tiang 9m)
- Persentase completion per segment

#### Stok Material
- Stok gudang (diterima, keluar, sisa)
- Stok per homebase (kebutuhan, diterima, kekurangan)
- Status kecukupan material per homebase

#### Surat Jalan
- Surat jalan hari ini
- 10 surat jalan terbaru
- Filter berdasarkan jenis (masuk/keluar/dibawa mandor)

### 4. **Visualisasi ASCII Chart**
Bot dapat membuat grafik bar chart dalam format teks:
```
Seg 1  ████████████████░░░░  80% (1200/1500 m)
Seg 2  ██████████░░░░░░░░░░  50% (750/1500 m)
Seg 3  ████████████████████ 100% (1500/1500 m)
```

Contoh pertanyaan:
- "Buatkan grafik progress semua segment"
- "Tampilkan chart perbandingan stok gudang"

---

## 🔧 Konfigurasi

### API Key
API key OpenRouter sudah **hardcoded** di aplikasi:
```
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
```

User tidak perlu setup apapun, langsung bisa pakai.

### Model AI (Gratis)
Default: **Llama 3.3 70B** (paling pintar)

Model lain yang tersedia:
- Qwen3 235B
- Gemma 3 27B
- Hermes 3 405B
- Laguna M.1
- OpenRouter Auto

Ganti model via Settings (⚙️) di header chat.

---

## 💬 Contoh Pertanyaan

### Progress
- "Cek progress hari ini"
- "Berapa total kabel yang sudah ditarik hari ini?"
- "Segment mana yang paling lambat?"
- "Tampilkan progress 7 hari terakhir"
- "Buatkan laporan lengkap progress hari ini"

### Stok
- "Stok kabel 24C masih cukup?"
- "Material apa yang hampir habis?"
- "Berapa sisa stok tiang 7m di gudang?"
- "Homebase mana yang kekurangan material?"

### Surat Jalan
- "Surat jalan hari ini ada berapa?"
- "Tampilkan surat jalan 3 hari terakhir"
- "Barang apa yang paling banyak keluar minggu ini?"

### Analisis
- "Segment mana yang paling tinggi progress-nya?"
- "Berapa persen total progress keseluruhan?"
- "Buatkan grafik perbandingan progress per segment"

---

## 🎨 UI/UX

### Warna & Tema
- Background: Dark mode (#0F172A, #1E293B)
- Accent: Purple (#6366F1)
- User message: Purple bubble
- Bot message: Dark gray bubble
- System message: Gray text (center)

### Animasi
- Floating button: Pulse saat tap
- Drag & drop: Smooth snap ke tepi
- Loading: Spinner saat bot berpikir

### Responsif
- Chat popup: 340x480px
- Bisa digeser ke mana saja
- Auto-scroll ke pesan terbaru

---

## 🛠️ Implementasi Teknis

### File Structure
```
Services/
  └── AiChatService.cs          # OpenRouter API integration
Pages/
  ├── AiChatPopup.xaml          # Chat UI
  ├── AiChatPopup.xaml.cs       # Chat logic
  ├── AiSettingsPage.xaml       # Settings UI
  └── AiSettingsPage.xaml.cs    # Settings logic
Controls/
  ├── FloatingAiButton.xaml     # Floating button UI
  └── FloatingAiButton.xaml.cs  # Button logic
```

### Dependency Injection
Registered di `MauiProgram.cs`:
```csharp
builder.Services.AddSingleton<AiChatService>();
```

### Context Building
Bot mendapat context dari:
- `ProjectService.GetAllAsync()` → Info project & segment
- `GoogleSheetsService.FetchAsync()` → Data sheet (progress, stok, SJ)
- `DateTime.Today` → Tanggal hari ini

Context di-refresh setiap kali user kirim pesan.

### API Request
```csharp
POST https://openrouter.ai/api/v1/chat/completions
{
  "model": "meta-llama/llama-3.3-70b-instruct:free",
  "messages": [
    { "role": "system", "content": "..." },
    { "role": "user", "content": "..." }
  ],
  "max_tokens": 1500,
  "temperature": 0.4
}
```

---

## 🐛 Troubleshooting

### Bot tidak merespon
- Cek koneksi internet
- Pastikan data sudah dimuat (buka halaman utama dulu)
- Coba clear chat (🗑️) dan mulai lagi

### Floating button tidak muncul
- Pastikan sudah di halaman tab (bukan halaman login/projects)
- Restart aplikasi

### Data tidak akurat
- Refresh data di halaman utama (↻)
- Bot menggunakan cache, bukan real-time dari Google Sheets

### API Key error
- Buka Settings (⚙️) → masukkan API key baru
- Atau gunakan default key yang sudah hardcoded

---

## 📊 Performa

- **Response time**: 2-5 detik (tergantung model & kompleksitas)
- **Context size**: ~3000-5000 tokens (data lengkap)
- **Max tokens**: 1500 (cukup untuk laporan detail)
- **Temperature**: 0.4 (lebih faktual, kurang kreatif)

---

## 🔐 Keamanan

- API key disimpan di `Preferences` (encrypted by OS)
- Tidak ada data sensitif dikirim ke OpenRouter
- Hanya data agregat (progress, stok) yang digunakan sebagai context
- Tidak ada PII (Personally Identifiable Information)

---

## 🚀 Future Improvements

- [ ] Voice input (speech-to-text)
- [ ] Export chat history ke PDF
- [ ] Notifikasi proaktif (bot kasih alert jika ada anomali)
- [ ] Multi-language support (English, Javanese)
- [ ] Integration dengan WhatsApp bot

---

## 📝 Changelog

### v1.0.0 (9 Mei 2026)
- ✅ Initial release
- ✅ OpenRouter integration (Llama 3.3 70B)
- ✅ Floating button dengan drag & drop
- ✅ Chat popup dengan history
- ✅ Quick action chips
- ✅ ASCII chart visualization
- ✅ Context dari semua data aplikasi
- ✅ Settings page untuk ganti model/API key
- ✅ Admin login auto-recognition fix
- ✅ Logo kucing 🐱 (bukan robot 🤖)

---

## 👨‍💻 Developer Notes

### Menambah Model Baru
Edit `AiChatService.cs`:
```csharp
public static readonly List<AiModel> AvailableModels = new()
{
    new AiModel { Id = "model-id:free", Name = "Model Name" },
    // tambah di sini
};
```

### Menambah Quick Action
Edit `AiChatPopup.xaml`:
```xml
<Frame>
    <Frame.GestureRecognizers>
        <TapGestureRecognizer Tapped="OnQuickAction" 
                              CommandParameter="Pertanyaan kamu"/>
    </Frame.GestureRecognizers>
    <Label Text="📌 Label" />
</Frame>
```

### Mengubah System Prompt
Edit `AiChatService.BuildSystemPromptAsync()`:
```csharp
sb.AppendLine("Instruksi tambahan untuk bot...");
```

---

**Dibuat dengan ❤️ untuk monitoring FTTH Jawa Tengah & DIY**
