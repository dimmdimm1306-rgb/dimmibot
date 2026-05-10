# OpenClaw API Server

OpenClaw sekarang bisa jadi **API Server** yang bisa diakses aplikasi StokBarangMAUI!

## 🚀 Cara Pakai

### 1. Jalankan API Server

```bash
cd OPENCLAW
npm run api
```

Server akan running di: **`http://localhost:20128`**

### 2. Setting di Aplikasi StokBarangMAUI

Login sebagai **Admin**, buka AI Settings (⚙️), lalu isi:

- **Base URL**: `http://10.0.2.2:20128/v1` (untuk emulator) atau `http://192.168.x.x:20128/v1` (untuk HP fisik)
- **API Key**: (kosong atau isi apa aja, tidak dipakai)
- **Model**: `meta-llama/llama-3.2-3b-instruct:free`

### 3. Test Koneksi

Tap tombol **Test** di AI Settings → Kalau berhasil, AI akan jawab!

---

## 📡 Endpoint

### `POST /v1/chat/completions`

OpenAI-compatible endpoint.

**Request:**
```json
{
  "model": "meta-llama/llama-3.2-3b-instruct:free",
  "messages": [
    {"role": "system", "content": "You are a helpful assistant"},
    {"role": "user", "content": "Hello!"}
  ],
  "temperature": 0.7,
  "max_tokens": 1000
}
```

**Response:**
```json
{
  "id": "chatcmpl-xxx",
  "object": "chat.completion",
  "created": 1234567890,
  "model": "meta-llama/llama-3.2-3b-instruct:free",
  "choices": [
    {
      "index": 0,
      "message": {
        "role": "assistant",
        "content": "Hello! How can I help you?"
      },
      "finish_reason": "stop"
    }
  ]
}
```

### `GET /health`

Health check endpoint.

**Response:**
```json
{
  "status": "ok",
  "service": "OpenClaw API Server"
}
```

---

## 🔧 Konfigurasi

Edit `.env`:

```env
API_PORT=20128                    # Port server (default: 20128)
OPENROUTER_API_KEY=sk-or-v1-...  # API key OpenRouter
```

---

## 📱 IP Address untuk HP Fisik

Kalau pakai HP fisik (bukan emulator), cari IP komputer:

**Windows:**
```bash
ipconfig
```

Cari **IPv4 Address** (contoh: `192.168.1.100`)

Lalu di aplikasi, set Base URL: `http://192.168.1.100:20128/v1`

---

## 🎯 Keuntungan

1. **Satu API Key** - OpenClaw dan aplikasi pakai API key yang sama
2. **Kontrol Penuh** - Admin bisa monitor semua request di terminal
3. **Offline-Ready** - Bisa ganti ke local LLM (Ollama, LM Studio) nanti
4. **Debug Mudah** - Lihat log real-time di terminal

---

## 🔄 Mode Running

### Mode 1: API Server Saja
```bash
npm run api
```
Hanya API server, tanpa WhatsApp bot.

### Mode 2: WhatsApp Bot Saja
```bash
npm start
```
Hanya WhatsApp bot, tanpa API server.

### Mode 3: Keduanya (Parallel)
Buka 2 terminal:
```bash
# Terminal 1
npm start

# Terminal 2
npm run api
```

---

## 🐛 Troubleshooting

### Error: Port already in use
Port 20128 sudah dipakai. Ganti di `.env`:
```env
API_PORT=20129
```

### Error: Cannot connect from app
- Pastikan firewall tidak block port 20128
- Cek IP address benar (ipconfig)
- Kalau pakai emulator, pakai `10.0.2.2` bukan `localhost`

### Error: OPENROUTER_API_KEY not found
Pastikan `.env` ada dan berisi API key yang valid.

---

**Selamat mencoba! 🚀**
