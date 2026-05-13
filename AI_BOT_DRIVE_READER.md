# 🤖 AI Bot + Google Drive Reader (Read-Only)

Integrasi AI Bot "Claw" di aplikasi StokBarangMAUI dengan Google Drive. User bisa tanya ke bot untuk baca file Excel/Google Sheets di Drive tanpa buka browser.

## Arsitektur

```
HP (MAUI app)
   └─ AiChatService (chat "Claw")
        └─ GDriveCommandHandler (detect keyword "drive")
             └─ GDriveReaderService (HTTP client, read-only)
                  └─── HTTP ───→ Laptop: http_server.py (FastAPI)
                                   └─ mcp_gdrive_filter/server.py (MCP core)
                                        └─ Google Drive API (scope .readonly)
```

**Jaminan read-only 3-lapis:**
1. OAuth scope Python hanya `.readonly` (Google-side)
2. Runtime HTTP guard Python blok method selain GET
3. C# `GDriveReaderService` **tidak punya** method Create/Update/Delete

## Cara Jalanin

### 1. Di Laptop (server)

Double-click `mcp_gdrive_filter\start_server.bat`. Atau manual:
```
set GOOGLE_APPLICATION_CREDENTIALS=D:\!FTTH\Program\UPLOAD DOKUMEN\StokBarangMAUI\credentials\gdrive-credentials.json
python -m mcp_gdrive_filter.http_server
```

Default port: `20129`. Docs: `http://localhost:20129/docs`.

### 2. Share Google Drive

Share folder/file ke service account:
```
dimmi-145@drive-477514.iam.gserviceaccount.com
```
Permission: **Viewer**.

### 3. Di HP / Aplikasi MAUI

1. Buka **AI Settings** (menu ⚙️ di chat popup)
2. Scroll ke **📂 Google Drive Reader (Read-Only)**
3. Toggle **ON**
4. Isi **Server URL**:
   - Laptop sama: `http://localhost:20129`
   - HP → laptop di WiFi sama: `http://<IP-laptop>:20129` (cek IP via `ipconfig`)
   - Dari mana saja: pakai Cloudflare tunnel (lihat section bawah)
5. Klik **🔌 Test** — pastikan ✅ Connected
6. Simpan

### 4. Pakai di Chat

Buka chat bot, ketik command berikut (natural language Indonesia / Inggris OK):

| Command | Fungsi |
|---|---|
| `drive status` | Cek koneksi & service account |
| `drive list` | List semua file ter-share |
| `drive list BOQ` | Cari file by nama |
| `isi folder Projects` | Browse isi folder |
| `drive sheet BOQ FWA` | List tab di file |
| `drive header BOQ FWA` | Lihat kolom + tipe data |
| `drive summary BOQ FWA` | Statistik kolom |
| `drive filter BOQ FWA {"segment":"FWA","limit":20}` | Filter data server-side |
| `drive help` | Cheatsheet lengkap |

**Multi-sheet:** tambah `#nama-sheet`. Contoh: `drive header BOQ FWA#Data`

## Kenapa Hemat Token?

Filter dilakukan di Python (laptop), bukan di AI. AI hanya terima hasil yang sudah difilter.

| Scenario | Tanpa Drive Reader | Dengan Drive Reader |
|---|---|---|
| File 10,000 baris, query segment=FWA | AI harus baca semua → ~500k token | Python filter → AI terima 50 baris → ~2.5k token |
| Lihat kolom file | Upload file → AI parse → ~ribuan token | `drive header` → ~100 token |

Hemat **~99%** untuk query data besar.

## Remote Access (HP dari luar rumah)

Kalau HP tidak di WiFi sama dengan laptop, bisa tunnel via Cloudflare (sudah ada pattern serupa untuk OpenClaw):

```
cloudflared tunnel --url http://localhost:20129
```

Dapat URL seperti `https://xxx.trycloudflare.com`. Paste URL itu di **AI Settings → Server URL**.

**Saran security:** set bearer token supaya tidak public. Di `start_server.bat`:
```bat
set GDRIVE_API_TOKEN=your-secret-token-here
```
Lalu paste token yang sama di **AI Settings → Bearer Token**.

## Files yang Ditambahkan

**Python (laptop):**
- `mcp_gdrive_filter/server.py` — MCP core (read-only)
- `mcp_gdrive_filter/http_server.py` — FastAPI wrapper
- `mcp_gdrive_filter/start_server.bat` — launcher
- `mcp_gdrive_filter/requirements.txt` — deps
- `credentials/gdrive-credentials.json` — service account (gitignored)

**C# (MAUI):**
- `Services/GDriveReaderService.cs` — HTTP client read-only
- `Services/GDriveCommandHandler.cs` — natural language command parser
- `Services/AiChatService.cs` — integrate handler di `SendMessageAsync`
- `Pages/AiSettingsPage.xaml` + `.cs` — UI toggle & config
- `MauiProgram.cs` — DI registration

**Dokumentasi:**
- `GOOGLE_DRIVE_SETUP_STEP_BY_STEP.md` — setup Google Cloud
- `MCP_GDRIVE_CAPABILITIES.md` — daftar kemampuan
- `MCP_GDRIVE_EXAMPLES.md` — contoh penggunaan

## Troubleshooting

**"Drive Reader belum aktif"** di chat
→ Buka AI Settings → toggle Drive ON.

**"Tidak bisa connect"** saat test
→ Cek `http_server.py` jalan di laptop, URL benar, firewall tidak blok port 20129.

**"Tidak ada file di Drive"**
→ Belum share file/folder ke service account. Ketik `drive status` di chat untuk lihat email-nya.

**"File 'X' tidak ketemu"**
→ Nama harus unik atau ketik ID. Coba `drive list X` dulu untuk cari.

**Server Python crash / freeze**
→ Cek `credentials/gdrive-credentials.json` valid (field `type: service_account` ada).
