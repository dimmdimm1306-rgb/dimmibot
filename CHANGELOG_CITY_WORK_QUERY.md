# Changelog - City-Based Work Query Feature

**Date**: 11 Mei 2026, 02:11 WIB  
**Build**: Release APK  
**File**: `bin\Release\net9.0-android\publish\APP MONITOR.apk`

## 🎯 Fitur Baru: Query Pekerjaan Per Kota

### Masalah Sebelumnya
- User bertanya "cek pekerjaan di Surakarta" → Bot jabarkan semua segment
- Tidak fokus ke site/rute/span yang **belum dikerjakan** (progress = 0)
- Jawaban terlalu panjang dan tidak detail

### Solusi Implementasi
Bot sekarang bisa menjawab pertanyaan tentang pekerjaan yang belum dikerjakan di kota tertentu dengan detail spesifik.

## 📝 Perubahan Kode

### File: `Services/AiChatService.cs`

**Lokasi**: Method `GetSystemPrompt()` - bagian instruksi struktur sheet

**Instruksi Baru Ditambahkan**:
```
⚠️ ATURAN KHUSUS - CEK PEKERJAAN PER KOTA:
Kalau user tanya 'cek pekerjaan di [nama kota]' atau 'site mana yang belum di [kota]':
1. Cari di kolom H (KAB/KOTA) yang sesuai dengan nama kota yang ditanya
2. Filter hanya yang kolom E (Progres) = 0 atau kosong (belum dikerjakan)
3. JANGAN jabarkan per segment — langsung kasih detail:
   • SITE ID (kolom I)
   • RUTE (kolom C)
   • NAMA BARANG/SPAN (kolom D)
   • HOMEBASE (kolom G) kalau ada
4. Format jawaban:
   📍 Pekerjaan yang belum dikerjakan di [KOTA]:
   
   🔴 Site [ID] - Rute [nama]
      • Span: [nama barang]
      • Homebase: [lokasi]
   
   Total: [X] site belum dikerjakan
5. Kalau SEMUA site di kota itu sudah ada progress (tidak ada yang 0), jawab:
   ✅ Semua pekerjaan di [KOTA] sudah dimulai/selesai
6. Cek di SEMUA sheet/segment yang ada data progress-nya
```

## 🎨 Contoh Penggunaan

### Query User:
```
"cek pekerjaan di Surakarta"
"site mana yang belum di Brebes"
"pekerjaan yang belum dikerjakan di Tegal"
```

### Respons Bot (Format Baru):
```
📍 Pekerjaan yang belum dikerjakan di Surakarta:

🔴 Site 0244 - Rute Surakarta Utara
   • Span: Kabel FO 144 Core
   • Homebase: Gudang Solo

🔴 Site 0245 - Rute Surakarta Selatan
   • Span: Tiang Beton 9m
   • Homebase: Gudang Solo

Total: 2 site belum dikerjakan
```

### Jika Semua Sudah Dikerjakan:
```
✅ Semua pekerjaan di Surakarta sudah dimulai/selesai
```

## 🔍 Cara Kerja

1. **Input**: User bertanya tentang pekerjaan di kota tertentu
2. **Filter**: Bot cari di kolom H (KAB/KOTA) yang match
3. **Check Progress**: Filter hanya yang kolom E (Progres) = 0 atau kosong
4. **Output**: Tampilkan detail SITE ID, RUTE, SPAN, HOMEBASE
5. **Cross-Sheet**: Cek di semua sheet/segment yang ada

## 📊 Data yang Digunakan

**Struktur Sheet Progress**:
- Kolom A: Tanggal
- Kolom B: Segment
- Kolom C: Rute
- Kolom D: Nama Barang/Span
- Kolom E: **Progres** (filter = 0 atau kosong)
- Kolom F: Keterangan
- Kolom G: **HOMEBASE**
- Kolom H: **KAB/KOTA** (filter by city name)
- Kolom I: **SITE ID**

## ✅ Testing

### Test Case 1: Ada Pekerjaan Belum Dikerjakan
- **Input**: "cek pekerjaan di Surakarta"
- **Expected**: List detail site yang progress = 0
- **Format**: Site ID, Rute, Span, Homebase

### Test Case 2: Semua Sudah Dikerjakan
- **Input**: "site mana yang belum di Brebes"
- **Expected**: "✅ Semua pekerjaan di Brebes sudah dimulai/selesai"

### Test Case 3: Multi-Segment
- **Input**: "pekerjaan yang belum di Tegal"
- **Expected**: Cek di semua segment, gabungkan hasilnya

## 🚀 Deployment

1. ✅ Build Release berhasil (121.7s)
2. ✅ Publish APK berhasil (9.1s)
3. ✅ APK Location: `bin\Release\net9.0-android\publish\APP MONITOR.apk`
4. ✅ File Size: 40.8 MB
5. ✅ Build Time: 11 Mei 2026, 02:11 WIB

## 📱 Cara Install

1. Copy APK ke HP Android
2. Install (allow unknown sources jika perlu)
3. Login dengan akun yang ada
4. Test dengan bertanya: "cek pekerjaan di [nama kota]"

## 🔧 Perintah Admin Terkait

Jika perlu adjust instruksi ini via bot:
```
dimmi13 instruksi kalau ditanya pekerjaan di kota, fokus ke site yang progress 0 saja
```

## 📌 Notes

- Instruksi ini **hardcoded** di system prompt
- Tidak perlu sync ke GitHub (sudah di dalam APK)
- Untuk update instruksi tambahan, pakai command `dimmi13 instruksi`
- Bot akan otomatis cek di semua sheet yang ada data progress

---

**Developer**: Dimas  
**AI Assistant**: Kiro (Claude Sonnet 4.5)  
**Build Tool**: .NET 9.0 MAUI for Android
