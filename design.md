# Design Specification — StokBarangMAUI (FTTH Monitor)

Dokumentasi UI/UX hasil ekstraksi dari 5 screenshot aplikasi: **Surat Jalan**, **Progress (Per Hari & Per Segment)**, **Stok Diterima**, **Stok Gudang**.

---

## 1. Struktur Global

### 1.1 Top Tab Bar (persisten di semua halaman utama)
Urutan kiri → kanan, latar gelap (navy `#1a1f3d`-ish), teks putih, indikator aktif berwarna ungu (`#7C3AED`-ish) dengan underline.

```
SURAT JALAN | PROGRESS | STOK DITERIMA | STOK GUDANG | INPUT
```

- Label uppercase, ukuran kecil, bold pada tab aktif.
- Tab tidak aktif: putih buram (~70% opacity).
- Tab aktif: ungu solid + underline 2px.

### 1.2 Header Block (di bawah tab bar, latar biru `#3D5DDC`-ish)
Pola umum:
- Kiri: ikon hamburger (`☰`) untuk drawer, kemudian ikon kontekstual + judul + subtitle.
- Kanan: deretan tombol bulat (theme toggle 🌙, palette warna, refresh ↻).

### 1.3 Drawer (Hamburger)
Sesuai konteks proyek (4 tab + drawer) — drawer berisi navigasi tambahan; tidak ditampilkan di screenshot.

---

## 2. Halaman: SURAT JALAN

### 2.1 Layout
```
┌─ Top Tab Bar (SURAT JALAN aktif) ─────────────────────┐
├─ Header: ☰  📋 Surat Jalan Material                   │
│            Riwayat keluar-masuk material   🌙 🟣 ↻    │
├─ Stats Row (3 kartu) ─────────────────────────────────┤
│  [48 Total SJ]   [42 Masuk]   [6 Keluar]              │
├─ Search Bar: "Cari barang, segment, penerima..."      │
├─ Filter Pills: [Semua] MASUK  KELUAR  DIBAWA   ↓Terbaru│
├─ List Card (per surat jalan) ─────────────────────────┤
│  ▎ HOMEBASE                          [STATUS PILL]    │
│  ▎ Nama Barang                       [Qty + satuan]   │
│  ▎ [Lihat Surat Jalan]                                │
│  ▎ Tanggal • No: <nomor SJ>                           │
└────────────────────────────────────────────────────────┘
```

### 2.2 Komponen
- **Stat card**:
  - Total SJ → angka biru tua, latar putih
  - Masuk → angka hijau, latar hijau muda
  - Keluar → angka merah, latar merah muda
  - Sudut membulat 12–16px, padding generous.
- **Filter pills**: pill horizontal, aktif = navy solid, inaktif = outline tipis. "↓ Terbaru" = sort toggle terpisah di kanan.
- **List card**:
  - Border kiri 4px, warna mengikuti status (hijau = MASUK, merah = KELUAR, kuning = DIBAWA).
  - Status pill pojok kanan atas.
  - Tombol "Lihat Surat Jalan" = ghost button kecil dengan ikon attachment.
  - Tanggal pakai format pendek hari + tanggal (`Rabu, 06 Mei`).

---

## 3. Halaman: PROGRESS — Per Hari (Default)

### 3.1 Layout
```
┌─ Top Tab Bar (PROGRESS aktif) ────────────────────────┐
├─ Header navy: ←  Rabu                       🌙        │
│                  6 Mei 2026                            │
├─ Stats Row (3 kartu) ─────────────────────────────────┤
│  [4 Aktivitas] [3 Segment] [0 Done]                   │
├─ Search Bar: "Cari segment / span / nama barang..."   │
├─ Activity Card ───────────────────────────────────────┤
│  ▎ <KODE-SEGMENT;ID-SPAN>          [Proses ⏳]        │
│  ▎ 📍 RUTE KABUPATEN1 - KABUPATEN2 - ...              │
│  ▎ ─────────────────────────                          │
│  ▎ Cable 24C                       [1.750 m]         │
│  ▎ Tiang 7M                        [27 btg]          │
│  ▎ 🏠 HOMEBASE                                        │
└────────────────────────────────────────────────────────┘
```

### 3.2 Detail
- **Date navigator**: panah `<` untuk hari sebelumnya; tap tanggal = date picker (asumsi); icon bulan = theme toggle.
- **Stat card**:
  - Aktivitas (hitam), Segment (biru), Done (hijau) — angka besar, label kecil di bawah.
- **Activity card**:
  - Border kiri 4–5px oranye = status "Proses".
  - Header: kode segment + ID span (mono-style).
  - Rute: ikon pin merah + teks biru kecil, support multi-line.
  - Material rows: nama di kiri, qty pill di kanan (latar biru tua, teks putih).
  - Tag homebase di bawah dengan ikon rumah, latar abu muda, pill rounded.

---

## 4. Halaman: PROGRESS — Per Segment

### 4.1 Layout
```
┌─ Header: ☰  🦌 PROGRESS SEGMENT     🌙 🟣 ↻          │
│            FWA IJE                                     │
├─ Search Bar: "Cari segment, rute, span, kabupaten..." │
├─ Toggle: [📋 Per Segment] [📅 Per Hari]               │
├─ Card "Progress Total per Segment (%)" ───────────────┤
│  ▎ ↑ Tap judul untuk layar penuh                      │
│  ▎ [Semua] Kabel  Tiang 7m  Tiang 9m                  │
│  ▎                              ⚙ Atur Tampilan (6/6) │
│  ▎ <Line chart 15 Apr → 8 Mei, multi-segment>         │
├─ Card "Pilih Segment untuk Detail" [6 segment] ──────┤
│  ▎ <ID> SEGMENT N                       [✗ NOK]      │
│  ▎      RUTE KABUPATEN-KABUPATEN                      │
│  ▎      Kbl ▓▓▓▓▓▓▓▓▓░ 96%                           │
│  ▎      T7  ▓▓▓▓▓░░░░░ 49%                           │
│  ▎      T9  ▓░░░░░░░░░ 2%       [📊]                 │
└────────────────────────────────────────────────────────┘
```

### 4.2 Detail
- **Toggle Per Segment / Per Hari**: lebar penuh, pill aktif biru solid + ikon putih, inaktif outline.
- **Filter material**: pill horizontal scroll-able, aktif = biru solid.
- **"Atur Tampilan (6/6)"**: tombol kanan, label menunjukkan segment terlihat / total.
- **Line chart**:
  - Sumbu Y: 0% – 120% step 20%.
  - Sumbu X: tanggal mulai → terkini, label tanggal (`Mulai`, `15 Apr`, `18 Apr`, …).
  - Multi-line warna berbeda per segment, marker bulat di tiap titik data.
- **Segment card**:
  - Nomor besar di lingkaran kiri.
  - Status pill: `✗ NOK` (merah muda) / `✓ OK` (hijau) — di pojok kanan atas.
  - Tiga progress bar bertumpuk: `Kbl` (biru), `T7` (hijau), `T9` (ungu) — masing-masing dengan persentase di ujung kanan.
  - Ikon chart kecil di kanan bawah → drill-down detail.

---

## 5. Halaman: STOK DITERIMA

### 5.1 Layout
```
┌─ Header: ☰  📦 Stok Material per Homebase  🌙 🟣 ↻   │
│             Kebutuhan · Diterima · Kekurangan          │
├─ Legend Chips ────────────────────────────────────────┤
│  ▪ Material Inti  ▪ Additional  + Surplus  − Kurang  │
├─ Card per Homebase ───────────────────────────────────┤
│  HOMEBASE                          [⚠ Kurang]         │
│  RUTE KABUPATEN - ...                                  │
│  ┌──────────────────────────────────────────────┐     │
│  │ Nama Barang | Kebutuhan | Diterima | Kekurangan│   │
│  ├──────────────────────────────────────────────┤     │
│  │ Kabel 24C   |   40.000  |  40.000  |    -    │     │
│  │ Tiang 7m    |     617   |    617   |    -    │     │
│  │ ...         |           |          |  -60 🔴 │     │
│  └──────────────────────────────────────────────┘     │
│  ▶ ADDITIONAL                                         │
│  ┌──────────────────────────────────────────────┐     │
│  │ Nama Barang                       Diterima   │     │
│  │ Kabel 12C (m)                       600      │     │
│  │ Stok langsung datang...                       │     │
│  └──────────────────────────────────────────────┘     │
└────────────────────────────────────────────────────────┘
```

### 5.2 Detail
- **Legend chip**: kotak warna kecil + label, layout horizontal.
  - Material Inti: biru
  - Additional: oranye
  - Surplus: hijau (prefix `+`)
  - Kurang: merah (prefix `−`)
- **Card homebase**:
  - Header: nama homebase besar bold + status pill di kanan (`⚠ Kurang` merah / `✓ Cukup` hijau / `+ Surplus` oranye).
  - Subtitle: rute kabupaten kecil abu-abu.
  - Tabel material dengan header biru solid teks putih:
    - Angka rata kanan, font tabular/mono.
    - Sel kekurangan ditampilkan `-` saat 0; angka merah saat < 0 (mis. `-60`).
- **Section ADDITIONAL**:
  - Header oranye solid, hanya kolom Diterima.
  - Card item additional latar oranye sangat muda dengan note kecil.

---

## 6. Halaman: STOK GUDANG

### 6.1 Layout
```
┌─ Header: ☰  📦 STOK GUDANG          🌙 🟣 ↻          │
│            Pilih Gudang / Wilayah                      │
├─ Toggle: [📋 Per Gudang] [🔀 Combine] ────────────────┤
├─ Warehouse Card ──────────────────────────────────────┤
│  ▎ Nama Gudang                       [▼ 1.494]       │
│  ▎ RUTE KABUPATEN - ...                                │
│  ▎ Kabel  ▓▓▓▓▓▓▓▓▓▓ 100%                            │
│  ▎ Total  ▓▓▓▓▓▓▓▓▓▓ 100%   ❯                       │
└────────────────────────────────────────────────────────┘
```

### 6.2 Detail
- **Toggle Per Gudang / Combine**: sama dengan toggle progress (biru solid aktif).
- **Warehouse card**:
  - Border kiri tebal (5–6px), warna by status:
    - Merah → ada kekurangan signifikan (Brebes, Purwokerto, Sragen)
    - Oranye → ada surplus (Tasikmalaya)
    - Kuning → mixed/marginal (Sukoharjo, Grobogan)
  - Nama gudang besar bold (Title Case) — beda dari halaman Stok Diterima yang UPPER.
  - Subtitle rute kecil abu-abu UPPERCASE.
  - **Selisih badge** kanan atas, latar kontekstual:
    - `▼ 1.494` (merah/coklat gelap) = kekurangan
    - `▲ 3.350` (oranye/coklat) = surplus
  - Dua progress bar: `Kabel` (biru), `Total` (hijau), persentase di ujung kanan bar.
  - Chevron `❯` di kanan = drill-down ke detail per material.

---

## 7. Sistem Warna Status

| Status      | Warna                  | Contoh penggunaan                       |
|-------------|------------------------|------------------------------------------|
| Proses      | Oranye `#F59E0B`       | Pill aktivitas progress                  |
| Done/OK     | Hijau `#10B981`        | Pill OK, MASUK, surplus                  |
| NOK/Kurang  | Merah `#EF4444`        | Pill NOK, KELUAR, kekurangan             |
| Dibawa      | Kuning `#FACC15`       | Pill DIBAWA                              |
| Surplus     | Oranye/coklat hangat   | Badge ▲ + delta                          |
| Inti        | Biru `#3B82F6`         | Material utama, progress kabel           |
| Tiang 9m    | Ungu `#8B5CF6`         | Progress T9                              |

Border kiri kartu **selalu** mencerminkan status utama supaya dapat dipindai cepat di lapangan.

---

## 8. Tipografi & Spacing (ringkas)

- **Judul halaman**: 18–20px bold (`Surat Jalan Material`, `FWA IJE`, dll.).
- **Angka stat besar**: 28–32px bold.
- **Label stat**: 11–12px regular abu-abu.
- **Body card**: 14–15px regular.
- **Mono / data tabular**: 13–14px medium untuk angka di tabel & quantity pill.
- **Padding card**: 16px horizontal, 12–14px vertikal.
- **Gap antar card list**: 12px.
- **Radius card**: 12px (besar), 8px (pill quantity), pill status fully rounded.

---

## 9. Pola Interaksi Umum

1. **Search di tiap halaman**: bar lebar penuh, ikon kaca pembesar, placeholder spesifik konteks.
2. **Stat card di top**: ringkasan numerik 2–3 angka kunci, tidak interaktif (read-only).
3. **Toggle dual-mode** (Per Segment/Per Hari, Per Gudang/Combine): segmented control biru solid.
4. **Filter pill row**: horizontal, satu aktif eksklusif (Semua/MASUK/KELUAR/...).
5. **List card drill-down**: tap card → halaman detail; ikon chart/chevron di kanan sebagai afford.
6. **Theme toggle persisten** di header kanan (🌙 = light/dark, 🟣 = palette swap, ↻ = refresh data).

---

## 10. Catatan Implementasi MAUI

- Top tab bar = `Shell` `Tab` items dengan style override (warna navy, indikator ungu).
- Stat row & filter pill = `HorizontalStackLayout` di dalam `Border` dengan `StrokeShape="RoundRectangle 16"`.
- Progress bar bertumpuk = `Grid` 3 baris × 3 kolom (label | bar | percent), atau custom drawable.
- Chart line = `Microcharts` / `LiveCharts` sesuai library yang dipilih; sumbu Y fixed 0–120%.
- Border kiri berwarna = `Border` dengan padding kiri 4–6px diisi `BoxView` warna status.
- Read-only data dari Google Sheets → semua tabel/kartu hanya display; aksi `↻` me-refetch sheet.
