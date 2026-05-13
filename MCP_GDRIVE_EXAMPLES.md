# 📖 MCP Google Drive - Contoh Penggunaan

## 🎯 Skenario Real: Analisis BOQ FWA

### Skenario 1: Eksplorasi Data Baru

**Situasi:** Anda dapat file BOQ FWA baru, belum tahu strukturnya.

#### Step 1: List Files
```
User: AI, list files di Drive yang namanya mengandung "BOQ"

AI akan panggil:
- Tool: list_drive_files
- Query: name contains "BOQ"

Response:
- BOQ FWA.xlsx (ID: 1abc...xyz)
- BOQ FTTH.xlsx (ID: 1def...uvw)
```

#### Step 2: Lihat Header
```
User: AI, ambil header dari BOQ FWA.xlsx
File ID: 1abc...xyz

AI akan panggil:
- Tool: get_sheet_headers
- File ID: 1abc...xyz

Response:
Columns: [Tanggal, Segment, Rute, Material, Qty, Satuan, Harga, Total]
Total rows: 8,547
```

#### Step 3: Summary Statistik
```
User: AI, berikan summary dari kolom Segment dan Rute

AI akan panggil:
- Tool: get_sheet_summary
- Columns: [Segment, Rute]

Response:
Segment:
  - Unique: FWA, FTTH, FTTX
  - Count: FWA (3,200), FTTH (4,500), FTTX (847)

Rute:
  - Unique: A, B, C, D
  - Count: A (2,100), B (3,400), C (2,000), D (1,047)
```

**Token Usage:** ~600 tokens (vs 500,000 jika load semua!)

---

### Skenario 2: Filter Data Spesifik

**Situasi:** Anda perlu data FWA untuk Rute A di bulan Januari 2024.

```
User: AI, dari BOQ FWA ambil data:
- Segment: FWA
- Rute: A
- Tanggal: Januari 2024
- Limit: 100 baris

AI akan panggil:
- Tool: read_and_filter_sheet
- Filters:
  {
    "segment": "FWA",
    "rute": "A",
    "tanggal": {
      "min": "2024-01-01",
      "max": "2024-01-31"
    }
  }
- Limit: 100

Response:
Total rows after filter: 87
Data: [... 87 rows ...]
```

**Token Usage:** ~4,000 tokens (vs 500,000!)

---

### Skenario 3: Analisis Multi-Kriteria

**Situasi:** Cari material tertentu di beberapa rute.

```
User: AI, dari BOQ FWA cari:
- Material yang mengandung "Fiber"
- Rute: A, B, atau C
- Harga > 100000
- Ambil kolom: Material, Qty, Harga, Total

AI akan panggil:
- Tool: read_and_filter_sheet
- Filters:
  {
    "material": "Fiber",
    "rute": ["A", "B", "C"],
    "harga": {"min": 100000}
  }
- Columns: [Material, Qty, Harga, Total]
- Limit: 100

Response:
Total rows after filter: 45
Columns: [Material, Qty, Harga, Total]
Data: [... 45 rows ...]
```

**Token Usage:** ~2,000 tokens

---

### Skenario 4: Workflow Efisien

**Situasi:** Analisis lengkap dengan minimal token.

#### Workflow:
```
1. User: AI, list files BOQ

2. User: AI, ambil header dari BOQ FWA
   → Lihat struktur

3. User: AI, summary kolom Segment, Rute, Material
   → Lihat distribusi data

4. User: AI, filter data:
   - Segment: FWA
   - Rute: A
   - Limit: 50
   → Ambil sample data

5. User: AI, analisis data ini dan buat summary
   → AI analisis 50 baris saja
```

**Total Token:** ~5,000 tokens (vs 500,000!)

---

## 🔍 Fuzzy Column Matching

MCP server otomatis match kolom dengan nama mirip:

### Contoh 1: Case Insensitive
```
Filter: "segment": "FWA"
Match: "Segment", "SEGMENT", "segment"
```

### Contoh 2: Partial Match
```
Filter: "tgl": "2024-01-01"
Match: "Tanggal", "TGL", "tgl_input", "tanggal_entry"
```

### Contoh 3: Typo Tolerant
```
Filter: "rute": "A"
Match: "Rute", "Route", "RUTE"
```

---

## 📊 Filter Types

### 1. Exact Match (String)
```json
{
  "segment": "FWA"
}
```
Match: "FWA" (case-insensitive, partial)

### 2. Multiple Values (OR)
```json
{
  "rute": ["A", "B", "C"]
}
```
Match: Rute A OR B OR C

### 3. Range (Numeric/Date)
```json
{
  "harga": {
    "min": 100000,
    "max": 500000
  }
}
```
Match: 100,000 ≤ Harga ≤ 500,000

### 4. Min Only
```json
{
  "tanggal": {
    "min": "2024-01-01"
  }
}
```
Match: Tanggal ≥ 2024-01-01

### 5. Max Only
```json
{
  "qty": {
    "max": 100
  }
}
```
Match: Qty ≤ 100

### 6. Kombinasi
```json
{
  "segment": "FWA",
  "rute": ["A", "B"],
  "tanggal": {"min": "2024-01-01"},
  "harga": {"min": 50000, "max": 200000}
}
```

---

## 💡 Best Practices

### ✅ DO:
1. **Selalu ambil header dulu**
   ```
   AI, ambil header dari file X
   ```

2. **Gunakan summary untuk eksplorasi**
   ```
   AI, summary kolom Segment dan Rute
   ```

3. **Filter di server, bukan di AI**
   ```
   AI, filter data: segment=FWA, limit=50
   ```

4. **Pilih kolom yang diperlukan**
   ```
   AI, ambil kolom: Material, Qty, Harga
   ```

5. **Gunakan limit**
   ```
   AI, limit 100 baris
   ```

### ❌ DON'T:
1. **Jangan load semua data**
   ```
   ❌ AI, baca semua data dari BOQ FWA
   ```

2. **Jangan filter di AI**
   ```
   ❌ AI, baca semua data lalu filter segment=FWA
   ```

3. **Jangan ambil kolom tidak perlu**
   ```
   ❌ AI, ambil semua kolom (padang cuma perlu 3)
   ```

---

## 🎯 Cheat Sheet

### Quick Commands:
```bash
# List files
AI, list files di Drive

# Get headers
AI, header dari [file name]

# Summary
AI, summary kolom [col1, col2]

# Filter
AI, filter: [criteria], limit [n]

# Get File ID
AI, list files yang namanya [keyword]
```

### Filter Syntax:
```json
{
  "string_col": "value",           // Partial match
  "multi_col": ["val1", "val2"],   // OR condition
  "num_col": {"min": 10, "max": 100},  // Range
  "date_col": {"min": "2024-01-01"}    // Min only
}
```

---

## 📈 Token Comparison

| Scenario | Without MCP | With MCP | Savings |
|----------|-------------|----------|---------|
| Load all (10k rows) | 500,000 | - | - |
| Headers only | 500,000 | 100 | 99.98% |
| Summary stats | 500,000 | 500 | 99.90% |
| Filter 50 rows | 500,000 | 2,500 | 99.50% |
| Filter 100 rows | 500,000 | 4,000 | 99.20% |

---

## 🚀 Advanced Examples

### Example 1: Aggregasi Data
```
User: AI, dari BOQ FWA:
1. Filter segment=FWA, rute=A
2. Hitung total qty per material
3. Urutkan dari terbesar

AI akan:
1. Filter data (2,500 tokens)
2. Agregasi di memory
3. Return hasil
```

### Example 2: Komparasi
```
User: AI, bandingkan:
- BOQ FWA segment FWA vs FTTH
- Kolom: Material, Qty, Total
- Limit masing-masing 50

AI akan:
1. Filter FWA (2,500 tokens)
2. Filter FTTH (2,500 tokens)
3. Bandingkan
Total: ~5,000 tokens
```

### Example 3: Time Series
```
User: AI, dari BOQ FWA:
- Segment: FWA
- Tanggal: Jan-Mar 2024
- Group by bulan
- Hitung total per bulan

AI akan:
1. Filter Jan-Mar (3,000 tokens)
2. Group by bulan
3. Agregasi
```

---

## 🎉 Kesimpulan

Dengan MCP Google Drive Filter:
- ✅ Hemat token hingga 99%
- ✅ Fuzzy column matching
- ✅ Smart filtering
- ✅ Fast exploration
- ✅ Scalable untuk file besar

**Happy coding!** 🚀
