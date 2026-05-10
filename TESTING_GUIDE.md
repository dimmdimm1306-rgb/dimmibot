# Testing Guide - OpenRouter Free Models Integration

**Date:** 2026-05-10  
**Build Status:** ✅ Success (211 warnings, 0 errors)

## 🎯 What Was Fixed

### 1. Model Configuration
- ✅ Replaced non-working models with verified free models
- ✅ Updated default model to `liquid/lfm-2.5-1.2b-instruct:free`
- ✅ Changed base URL to `https://openrouter.ai/api/v1`
- ✅ Fixed GitHub config URL (main → master branch)

### 2. Files Updated
- `Services/AiChatService.cs` - Updated model list and defaults
- `cloudflare-config.json` - New verified models config
- `OPENROUTER_FREE_MODELS_TEST.md` - Complete testing documentation

### 3. Build Issues Fixed
- ✅ Removed corrupted `claw_cat.png` (was actually JPEG with wrong extension)
- ✅ Build completed successfully

## 📱 How to Test on Your Phone

### Option 1: Deploy from Visual Studio
1. Connect your Android phone via USB
2. Enable Developer Mode & USB Debugging on phone
3. Open `StokBarangMAUI.sln` in Visual Studio
4. Select your device from dropdown
5. Click Run (F5)

### Option 2: Build APK and Install
```bash
# Build release APK
dotnet publish StokBarangMAUI.csproj -c Release -f net9.0-android

# APK will be in:
# bin\Release\net9.0-android\publish\
```

Then transfer APK to phone and install.

## 🧪 Testing Checklist

### 1. Bot Configuration Auto-Fetch
- [ ] Open app
- [ ] Check if bot auto-fetches config from GitHub
- [ ] Verify model is set to `liquid/lfm-2.5-1.2b-instruct:free`
- [ ] Verify base URL is `https://openrouter.ai/api/v1`

### 2. Manual Bot Settings
- [ ] Open Bot Settings (⚙️)
- [ ] Check available models list shows:
  - `liquid/lfm-2.5-1.2b-instruct:free` ✅
  - `nvidia/nemotron-nano-9b-v2:free` ✅
  - `meta-llama/llama-3.2-3b-instruct:free`
  - Others...
- [ ] Enter your OpenRouter API key
- [ ] Save settings

### 3. Bot Functionality Test
- [ ] Open AI Chat Bot page
- [ ] Send test message: "Hi, test"
- [ ] Expected: Response in 1-2 seconds
- [ ] Check response quality
- [ ] Try asking about project data
- [ ] Test conversation history (multiple messages)

### 4. Error Handling Test
- [ ] Test without API key → Should show error message
- [ ] Test with wrong API key → Should show auth error
- [ ] Test with slow connection → Should show timeout message

### 5. Model Switching Test
- [ ] Switch to `nvidia/nemotron-nano-9b-v2:free`
- [ ] Send test message
- [ ] Verify it works
- [ ] Switch back to `liquid/lfm-2.5-1.2b-instruct:free`

## 🔧 Troubleshooting

### Bot Not Responding
1. Check internet connection
2. Verify API key is correct
3. Check model name is exact (case-sensitive)
4. Try switching to backup model: `nvidia/nemotron-nano-9b-v2:free`

### "Provider returned error"
- Free tier is overloaded
- Wait 1-2 minutes and retry
- Or switch to different model

### Timeout Errors
- Normal for free tier during peak hours
- Increase timeout in code if needed (currently 60s)
- Try again later

### Config Not Auto-Fetching
1. Check GitHub URL in code: 
   `https://raw.githubusercontent.com/dimmdimm1306-rgb/dimmibot/master/cloudflare-config.json`
2. Verify internet connection
3. Check GitHub repo is public
4. Manually set model in Settings as fallback

## 📊 Expected Performance

### Response Times (Free Tier)
- **liquid/lfm-2.5-1.2b-instruct:free**: 1.4-1.5 seconds
- **nvidia/nemotron-nano-9b-v2:free**: 1.4-1.6 seconds
- **Peak hours**: May exceed 3-5 seconds

### Response Quality
- **liquid/lfm**: Good, complete responses
- **nvidia/nemotron**: Sometimes empty, but no errors

## 🚀 Next Steps After Testing

### If Everything Works:
1. ✅ Mark this as production-ready
2. ✅ Monitor error rates
3. ✅ Consider paid tier if reliability is critical

### If Issues Found:
1. Check logs in Visual Studio Output window
2. Test with different models
3. Verify API key has credits
4. Check OpenRouter status page

## 📝 Notes

- **Free tier limitations**: Rate limiting, queue delays, availability issues
- **Recommended for**: Testing, development, low-traffic use
- **Not recommended for**: Production with high traffic
- **Alternative**: Self-hosted models (Ollama, LM Studio) for offline use

## 🔗 Useful Links

- OpenRouter Dashboard: https://openrouter.ai/keys
- GitHub Config: https://github.com/dimmdimm1306-rgb/dimmibot/blob/master/cloudflare-config.json
- Testing Results: See `OPENROUTER_FREE_MODELS_TEST.md`

---

**Build Info:**
- Platform: .NET 9.0 Android
- Build Time: ~60 seconds
- Warnings: 211 (mostly XAML binding optimizations - safe to ignore)
- Errors: 0 ✅
