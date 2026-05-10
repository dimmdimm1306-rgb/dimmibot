# Cara Pakai Bot WhatsApp

## 🤖 Fitur Bot

Bot ini memiliki 2 fungsi utama:

### 1. Input Arsip Barang (ke Spreadsheet ARSIP)
Untuk mencatat barang masuk/keluar dengan foto

### 2. Query Data Progress (dari Spreadsheet Progress)
Untuk mencari informasi site, rute, progress, dll

---

## 📝 Cara 1: Input Arsip Barang

### Format Lengkap:
```
@bot jenis: keluar
sj: SJ-001
dari: Gudang Dimas
ke: Brebes
barang: Kabel Dropcore
jumlah: 24000 meter
ket: kurang 2 roll
```

### Format Cepat:
```
@bot kabel 24000 ke brebes
```

### Dengan Foto:
Kirim foto dengan caption:
```
@bot kabel 24000 ke brebes
```

**Hasil:** Data tersimpan di spreadsheet ARSIP + foto di Google Drive

---

## 🔍 Cara 2: Query Data Progress

Bot akan mencari data dari spreadsheet Progress yang berisi:
- 329 baris data
- Kolom: Tanggal, Segment, Rute, Nama Barang, Progress, Keterangan, Homebase, Kab/Kota, Site ID

### Contoh Query:

#### Cari berdasarkan Kab/Kota:
```
@bot cek brebes
@bot cari semarang
@bot info tegal
```

#### Cari berdasarkan Site ID:
```
@bot cari site 1437221023
@bot cek 142377126
```

#### Cari berdasarkan Rute:
```
@bot info JC5_002
@bot cari JAW-CJV-0160-M-P
```

#### Cari berdasarkan Barang:
```
@bot cek cable 24c
@bot info kabel
```

#### Cari berdasarkan Segment:
```
@bot cari cirebon
@bot info segment brebes
```

### Contoh Respon Bot:

```
Ditemukan 3 hasil untuk "brebes":

1. Senin, 13 April | CIREBON - BREBES - TEGAL | JC5_002
   Barang: Cable 24C
   Progress: 1490
   Kab/Kota: KAB. BREBES | Site: 1437221023

2. Senin, 13 April | CIREBON - BREBES - TEGAL | JC2_001
   Barang: Cable 24C
   Progress: 730
   Kab/Kota: Brebes | Site: JAW-CJV-0160-M-P

3. ...
```

---

## ⚙️ Konfigurasi Penting

### Di file `.env`:

```env
# Spreadsheet untuk ARSIP (input barang)
GOOGLE_SHEET_ID=1Qemx6SgHVQ6Q9rY7K_wnnT40BWhFZ2GjbrLVjzkmryY
SHEET_NAME=ARSIP

# Spreadsheet untuk DATA (query progress)
DATA_SHEET_ID=1RC2Ylo4DjIAJkNMLe6v0jMnupJMcrP2v5aFTauhhcsg
DATA_SHEET_NAME=Progress

# Update data setiap 24 jam
DATA_UPDATE_INTERVAL=86400000

# Bot hanya respon jika di-mention/tag
REQUIRE_MENTION=true

# Bot kirim balasan ke WhatsApp
SEND_REPLY=true

# Grup yang diizinkan
ALLOWED_GROUP_OR_NUMBER=120363421575804012@g.us
```

---

## 🚀 Menjalankan Bot

### 1. Install dependencies:
```bash
npm install
```

### 2. Test koneksi ke spreadsheet data:
```bash
npm run test-data
```

### 3. Jalankan bot:
```bash
npm start
```

### 4. Scan QR Code dengan WhatsApp

---

## 📊 Update Data

- Bot otomatis update data dari spreadsheet Progress setiap **24 jam**
- Data juga di-refresh otomatis saat ada query baru
- Total data saat ini: **329 baris**

---

## ⚠️ Troubleshooting

### Bot tidak respon saat di-tag:
1. Pastikan `REQUIRE_MENTION=true`
2. Tag bot dengan format: `@bot [pesan]`
3. Cek terminal untuk error

### Query tidak menemukan data:
1. Pastikan keyword yang dicari ada di spreadsheet
2. Coba keyword lebih spesifik (site ID, nama kota)
3. Cek apakah data sudah ter-update (lihat log terminal)

### Bot tidak bisa baca spreadsheet:
1. Pastikan spreadsheet di-share ke OAuth account
2. Cek `DATA_SHEET_ID` dan `DATA_SHEET_NAME` sudah benar
3. Jalankan `npm run test-data` untuk test koneksi

---

## 💡 Tips

1. **Untuk input arsip**: Gunakan format lengkap agar data lebih akurat
2. **Untuk query data**: Gunakan keyword spesifik (site ID, kota) untuk hasil lebih cepat
3. **Mention bot**: Selalu tag `@bot` di awal pesan
4. **Foto**: Bisa kirim foto dengan caption untuk arsip barang

---

## 📞 Kata Kunci Bot

Bot akan otomatis deteksi jenis pesan berdasarkan kata kunci:

### Untuk Query Data:
- `cek`, `cari`, `data`, `info`, `status`, `progress`, `site`, `rute`, `segment`

### Untuk Input Arsip:
- Jika ada angka (jumlah) + kata seperti `ke`, `dari`, `barang`, `kabel`, dll
- Format field: `jenis:`, `sj:`, `dari:`, `ke:`, `barang:`, `jumlah:`, `ket:`

Bot pintar membedakan apakah pesan untuk query atau input arsip! 🎯
