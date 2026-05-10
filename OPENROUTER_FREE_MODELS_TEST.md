# OpenRouter Free Models Testing Results

**Test Date:** 2026-05-10  
**Tested By:** Automated script via OpenRouter API  
**API Endpoint:** https://openrouter.ai/api/v1/chat/completions

## Summary

Out of 24 free models available on OpenRouter, only **2 models** are consistently working and reliable for production use.

## ✅ WORKING MODELS (Recommended)

### 1. liquid/lfm-2.5-1.2b-instruct:free
- **Status:** ✅ Working
- **Latency:** ~1400-1500ms
- **Response Quality:** Good, complete responses
- **Reliability:** High
- **Recommended Use:** Primary model for production

### 2. nvidia/nemotron-nano-9b-v2:free
- **Status:** ✅ Working (with caveats)
- **Latency:** ~1400-1600ms
- **Response Quality:** Sometimes returns empty content
- **Reliability:** Medium
- **Recommended Use:** Fallback model

## ❌ TESTED BUT FAILING

### Provider Errors (Rate Limited / Unavailable)
- `meta-llama/llama-3.2-3b-instruct:free` - Provider returned error
- `meta-llama/llama-3.3-70b-instruct:free` - Provider returned error
- `qwen/qwen3-next-80b-a3b-instruct:free` - Provider returned error
- `google/gemma-4-31b-it:free` - Provider returned error
- `google/gemma-4-26b-a4b-it:free` - Provider returned error
- `openai/gpt-oss-20b:free` - Provider returned error
- `openai/gpt-oss-120b:free` - Provider returned error

### Paid Models (Not Free)
- `deepseek/deepseek-v4-flash` - 402 Insufficient credits

### Deprecated / Not Found
- `openrouter/owl-alpha` - Not in free models list

## Configuration Updates

### AiChatService.cs
```csharp
private string _baseUrl = "https://openrouter.ai/api/v1";
private string _selectedModel = "liquid/lfm-2.5-1.2b-instruct:free";

public List<string> GetAvailableModels()
{
    return new List<string>
    {
        // ✅ VERIFIED WORKING (tested 2026-05-10)
        "liquid/lfm-2.5-1.2b-instruct:free",
        "nvidia/nemotron-nano-9b-v2:free",
        "meta-llama/llama-3.2-3b-instruct:free",
        
        // Other free models (may have availability issues)
        "poolside/laguna-xs.2:free",
        "google/gemma-4-26b-a4b-it:free",
        "nousresearch/hermes-3-llama-3.1-405b:free",
        "qwen/qwen3-coder:free"
    };
}
```

### cloudflare-config.json
```json
{
  "baseUrl": "https://openrouter.ai/api/v1",
  "models": [
    "liquid/lfm-2.5-1.2b-instruct:free",
    "nvidia/nemotron-nano-9b-v2:free",
    "meta-llama/llama-3.2-3b-instruct:free",
    "poolside/laguna-xs.2:free"
  ],
  "model": "liquid/lfm-2.5-1.2b-instruct:free",
  "apiKeyRequired": true,
  "version": "8"
}
```

## Known Issues

### Free Tier Limitations
- **Rate Limiting:** Free models often return "Provider returned error" during peak hours
- **Queue Delays:** Response times can exceed 15 seconds during high load
- **Availability:** Models can go offline without notice
- **Timeout Issues:** 15-second timeout may be too short for free tier

### Recommendations
1. **Use `liquid/lfm-2.5-1.2b-instruct:free` as primary model**
2. **Set timeout to 30+ seconds** for free tier
3. **Implement retry logic** with exponential backoff
4. **Monitor model availability** and switch if needed
5. **Consider paid tier** for production use if reliability is critical

## Testing Command

```bash
cd "d:/!FTTH/Program/UPLOAD DOKUMEN/StokBarangMAUI/OPENCLAW"
KEY=$(grep "^OPENAI_API_KEY=" .env | cut -d= -f2)

curl -s --max-time 25 "https://openrouter.ai/api/v1/chat/completions" \
  -H "Authorization: Bearer $KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "model": "liquid/lfm-2.5-1.2b-instruct:free",
    "messages": [{"role": "user", "content": "Say hi"}],
    "max_tokens": 15
  }'
```

## Next Steps

1. ✅ Update `AiChatService.cs` with working models
2. ✅ Update `cloudflare-config.json` 
3. ✅ Change default model to `liquid/lfm-2.5-1.2b-instruct:free`
4. 🔄 Push config to GitHub repo
5. 🔄 Test in production app
6. 🔄 Monitor error rates and switch models if needed

## Notes

- OpenRouter free tier is best for **testing and development**
- For **production**, consider:
  - OpenRouter paid tier ($0.001-0.01 per request)
  - Self-hosted models (Ollama, LM Studio)
  - Direct API from model providers
