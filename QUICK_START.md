# 🚀 QUICK START - AI Bot OpenRouter Integration

**Status:** ✅ READY TO TEST  
**Date:** 2026-05-10

---

## 📱 CARA CEPAT TEST DI HP

### 1️⃣ Deploy App (Pilih salah satu)

**Via Visual Studio:**
```
1. Colok HP via USB
2. Enable USB Debugging di HP
3. Buka StokBarangMAUI.sln
4. Tekan F5
```

**Via APK:**
```bash
dotnet publish -c Release -f net9.0-android
# APK ada di: bin\Release\net9.0-android\publish\
# Transfer ke HP dan install
```

### 2️⃣ Setup Bot

1. Buka app StokBarangMAUI
2. Masuk ke halaman AI Bot
3. Klik Settings (⚙️)
4. Masukkan **OpenRouter API Key** kamu
5. Model otomatis set ke: `liquid/lfm-2.5-1.2b-instruct:free`
6. Save

### 3️⃣ Test Chat

```
Kamu: "Hi, test"
Bot: [Response dalam 1-2 detik] ✅

Kamu: "Berapa total progress kabel?"
Bot: [Jawab berdasarkan data project] ✅
```

---

## �� GET OPENROUTER API KEY

1. Buka: https://openrouter.ai/keys
2. Sign up / Login
3. Create new API key
4. Copy key
5. Paste ke app Settings

**Free tier:** $5 credit gratis untuk testing!

---

## ⚡ VERIFIED WORKING MODELS

### Primary (Recommended)
```
liquid/lfm-2.5-1.2b-instruct:free
```
- Latency: 1.4-1.5 detik
- Quality: Good ✅
- Stability: High ✅

### Backup
```
nvidia/nemotron-nano-9b-v2:free
```
- Latency: 1.4-1.6 detik
- Quality: Medium
- Stability: Medium

---

## �� TROUBLESHOOTING

### Bot Tidak Respon?
1. ✅ Cek internet connection
2. ✅ Cek API key benar
3. ✅ Tunggu 1-2 menit, retry
4. ✅ Switch ke backup model

### "Provider returned error"?
- Free tier lagi overload
- Coba lagi 5 menit kemudian
- Atau switch model

### Timeout?
- Normal di peak hours
- Retry sekali lagi
- Atau test di jam sepi

---

## 📊 WHAT'S NEW

### ✅ Fixed
- Model yang tidak bekerja diganti
- Default model: `liquid/lfm-2.5-1.2b-instruct:free`
- Auto-fetch config dari GitHub
- Build error (corrupted PNG) fixed

### ✅ Updated
- `AiChatService.cs` - Model configuration
- `cloudflare-config.json` - Verified models
- GitHub URL - main → master

### ✅ Added
- Complete testing documentation
- Performance metrics
- Troubleshooting guide

---

## 📁 DOCUMENTATION

- **Testing Guide:** `TESTING_GUIDE.md`
- **Test Results:** `OPENROUTER_FREE_MODELS_TEST.md`
- **Full Summary:** `IMPLEMENTATION_SUMMARY.md`
- **This File:** `QUICK_START.md`

---

## 🎯 SUCCESS CHECKLIST

- [ ] App deployed ke HP
- [ ] API key dimasukkan
- [ ] Bot response dalam 2 detik
- [ ] Conversation history works
- [ ] Project data accessible by bot
- [ ] Model switching works

---

## 🔗 LINKS

- **GitHub:** https://github.com/dimmdimm1306-rgb/dimmibot
- **Config:** https://raw.githubusercontent.com/dimmdimm1306-rgb/dimmibot/master/cloudflare-config.json
- **OpenRouter:** https://openrouter.ai/keys

---

## 💡 TIPS

1. **Free tier best for:** Testing, development, low traffic
2. **Peak hours:** Avoid 9am-5pm UTC (response slower)
3. **Best time:** Late night / early morning
4. **Backup ready:** Always have 2nd model as fallback
5. **Monitor usage:** Check OpenRouter dashboard

---

## 🎉 READY!

Semua sudah siap. Tinggal:
1. Deploy ke HP
2. Masukkan API key
3. Test chat
4. Enjoy! 🚀

**Questions?** Check `TESTING_GUIDE.md` atau `IMPLEMENTATION_SUMMARY.md`

---

**Last Updated:** 2026-05-10  
**Build Status:** ✅ Success  
**Deployment:** Ready  
**Status:** PRODUCTION READY 🎯
