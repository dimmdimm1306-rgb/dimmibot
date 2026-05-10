using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StokBarangMAUI.Services
{
    public class AiChatService
    {
        // ── GITHUB RAW CONFIG URL ────────────────────────────────────────
        // HP akan auto-fetch URL Cloudflare terbaru dari file ini setiap
        // kali app dibuka. Update file di GitHub dari laptop kalau URL
        // Cloudflare berubah — HP gak perlu di-setting manual.
        //
        // Format JSON yang diharapkan di GitHub raw:
        //   { "baseUrl": "https://xxx.trycloudflare.com/v1",
        //     "model":   "openrouter/owl-alpha",
        //     "apiKeyRequired": false,
        //     "version": "1" }
        //
        // Ganti URL di bawah dengan raw URL file JSON kamu di GitHub.
        // Kosongkan kalau gak mau auto-fetch dari GitHub.
        private const string GITHUB_CONFIG_URL = "https://raw.githubusercontent.com/dimmdimm1306-rgb/dimmibot/master/cloudflare-config.json";

        private readonly HttpClient _httpClient;
        private readonly List<AiChatMessage> _conversationHistory = new();
        private string _baseUrl = "https://openrouter.ai/api/v1";
        private string _apiKey = "";
        private string _selectedModel = "liquid/lfm-2.5-1.2b-instruct:free";

        // Context injection — set by the app before first chat
        private GoogleSheetsService? _sheets;
        private Models.ProjectConfig? _project;
        private string _cachedContext = "";

        // Bot memory and personality - protected by authentication
        private string _botPersonality = "";
        private string _botMemory = "";
        private const string AdminEmail = "dimmdimm1306@gmail.com";
        private readonly string[] AdminPasswords = { "kevingoblok", "dimmi13" };
        private bool _isAuthenticatedForChanges = false;
        private DateTime? _authExpiry = null;

        public AiChatService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(60);
            
            // Load saved settings
            _apiKey = Preferences.Get("ai_api_key", "");
            _baseUrl = Preferences.Get("ai_base_url", "https://openrouter.ai/api/v1");
            _selectedModel = Preferences.Get("ai_model", "liquid/lfm-2.5-1.2b-instruct:free");
            
            // Load bot personality and memory
            _botPersonality = Preferences.Get("bot_personality", "");
            _botMemory = Preferences.Get("bot_memory", "");

            // Fetch latest config from server (async fire-and-forget)
            _ = FetchServerConfigAsync();
        }

        /// <summary>Fetch centralized config — prioritas GitHub raw (stabil, gak pernah berubah URL-nya), fallback ke server /config.</summary>
        private async Task FetchServerConfigAsync()
        {
            // Prioritas 1: GitHub raw URL (stable discovery point)
            if (!string.IsNullOrWhiteSpace(GITHUB_CONFIG_URL))
            {
                if (await TryApplyConfigFromUrlAsync(GITHUB_CONFIG_URL, "GitHub"))
                    return;
            }

            // Prioritas 2: server /config (hanya kalau URL Cloudflare lama masih hidup)
            try
            {
                var configUrl = _baseUrl.Replace("/v1", "") + "/config";
                await TryApplyConfigFromUrlAsync(configUrl, "Server");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AiChatService] Failed to fetch server config: {ex.Message}");
            }
        }

        private async Task<bool> TryApplyConfigFromUrlAsync(string url, string source)
        {
            try
            {
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return false;

                var json = await response.Content.ReadAsStringAsync();
                var config = JsonSerializer.Deserialize<ServerConfig>(json);
                if (config == null) return false;

                if (!string.IsNullOrEmpty(config.BaseUrl))
                {
                    _baseUrl = config.BaseUrl.TrimEnd('/');
                    Preferences.Set("ai_base_url", _baseUrl);
                }
                // Prioritas: models[0] kalau ada, kalau enggak fallback ke field "model"
                var primaryModel = (config.Models != null && config.Models.Count > 0)
                    ? config.Models[0]
                    : config.Model;
                if (!string.IsNullOrEmpty(primaryModel))
                {
                    _selectedModel = primaryModel;
                    Preferences.Set("ai_model", _selectedModel);
                }
                System.Diagnostics.Debug.WriteLine($"[AiChatService] Config updated from {source}: {config.BaseUrl}, model={primaryModel}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AiChatService] {source} fetch failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>Inject project context so the AI knows about the user's data.</summary>
        public void SetProjectContext(GoogleSheetsService sheets, Models.ProjectConfig project)
        {
            _sheets = sheets;
            _project = project;
            _cachedContext = ""; // will rebuild on next message
        }

        public void SetApiKey(string apiKey)
        {
            _apiKey = apiKey;
            Preferences.Set("ai_api_key", apiKey);
        }

        public void SetModel(string model)
        {
            _selectedModel = model;
            Preferences.Set("ai_model", model);
        }

        public void SetBaseUrl(string baseUrl)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            Preferences.Set("ai_base_url", _baseUrl);
        }

        public string GetApiKey() => _apiKey;
        public string GetModel() => _selectedModel;
        public string GetBaseUrl() => _baseUrl;

        /// <summary>Check if user is authenticated to modify bot personality/memory.</summary>
        public bool IsAuthenticatedForChanges(string userEmail)
        {
            // Check if auth is still valid (expires after 1 hour)
            if (_isAuthenticatedForChanges && _authExpiry.HasValue && DateTime.Now < _authExpiry.Value)
            {
                return true;
            }

            // Check if user is admin
            if (!string.IsNullOrEmpty(userEmail) && userEmail.Equals(AdminEmail, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        /// <summary>Authenticate with password to modify bot settings.</summary>
        public bool AuthenticateWithPassword(string password)
        {
            if (AdminPasswords.Contains(password))
            {
                _isAuthenticatedForChanges = true;
                _authExpiry = DateTime.Now.AddHours(1); // Auth valid for 1 hour
                return true;
            }
            return false;
        }

        /// <summary>Process message and check for authentication commands.</summary>
        private string ProcessAuthenticationCommand(string message, string userEmail)
        {
            // Check for password authentication
            var lowerMsg = message.ToLower().Trim();
            
            // Check if message contains password
            foreach (var pwd in AdminPasswords)
            {
                if (lowerMsg.Contains(pwd))
                {
                    // Extract the actual command after password
                    var parts = message.Split(new[] { pwd }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1)
                    {
                        var command = parts[1].Trim();
                        if (AuthenticateWithPassword(pwd))
                        {
                            return ProcessBotModificationCommand(command);
                        }
                    }
                    else
                    {
                        // Just password, authenticate
                        if (AuthenticateWithPassword(pwd))
                        {
                            return "✅ Autentikasi berhasil! Sekarang kamu bisa mengubah kepribadian dan memori bot selama 1 jam ke depan.";
                        }
                    }
                }
            }

            return null; // No auth command found
        }

        /// <summary>Process bot modification commands (personality, memory).</summary>
        private string ProcessBotModificationCommand(string command)
        {
            var lowerCmd = command.ToLower().Trim();

            // Help command
            if (lowerCmd == "help" || lowerCmd == "bantuan" || lowerCmd == "?")
            {
                var memCount = string.IsNullOrEmpty(_botMemory) ? 0 : _botMemory.Split('\n').Count(s => !string.IsNullOrWhiteSpace(s));
                return "📚 PERINTAH ADMIN (selalu awali dengan password `dimmi13`):\n\n" +
                       "📝 SIMPAN MEMORI:\n" +
                       "  `dimmi13 catat <isi>` — tambah ke memori\n" +
                       "  Contoh: `dimmi13 catat Tim lapangan: Eko (kepala), Gilang (helper)`\n\n" +
                       "🎭 UBAH KEPRIBADIAN:\n" +
                       "  `dimmi13 ubah sifat <deskripsi>`\n" +
                       "  Contoh: `dimmi13 ubah sifat lebih formal dan teknis`\n\n" +
                       "👀 LIHAT/HAPUS MEMORI:\n" +
                       "  `dimmi13 lihat memori`\n" +
                       "  `dimmi13 hapus memori`\n\n" +
                       $"📊 Status: {memCount} entri memori tersimpan.";
            }

            // Add to memory — pakai keyword + space supaya gak misfire
            if (lowerCmd.StartsWith("catat ") || lowerCmd.StartsWith("ingat ") || lowerCmd.StartsWith("tambah "))
            {
                var memoryText = command.Substring(command.IndexOf(' ') + 1).Trim();
                if (!string.IsNullOrEmpty(memoryText))
                {
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                    _botMemory += $"\n[{timestamp}] {memoryText}";
                    Preferences.Set("bot_memory", _botMemory);
                    var totalLines = _botMemory.Split('\n').Count(s => !string.IsNullOrWhiteSpace(s));
                    return $"✅ Tercatat di memori HP: \"{memoryText}\"\n📊 Total {totalLines} entri tersimpan.";
                }
            }
            if (lowerCmd == "catat" || lowerCmd == "ingat" || lowerCmd == "tambah")
            {
                return "⚠️ Format salah. Pakai: `dimmi13 catat <isi yang mau dicatat>`\nContoh: `dimmi13 catat Tim lapangan ada Eko dan Gilang`";
            }

            // Change personality
            if (lowerCmd.StartsWith("ubah sifat") || lowerCmd.StartsWith("ubah kepribadian") || lowerCmd.StartsWith("ganti sifat"))
            {
                var personalityText = command.Substring(command.IndexOf("sifat") + 5).Trim();
                if (string.IsNullOrEmpty(personalityText))
                {
                    personalityText = command.Substring(command.IndexOf("kepribadian") + 11).Trim();
                }
                
                if (!string.IsNullOrEmpty(personalityText))
                {
                    _botPersonality = personalityText;
                    Preferences.Set("bot_personality", _botPersonality);
                    return $"✅ Kepribadian bot diubah menjadi: \"{personalityText}\"";
                }
            }

            // View memory
            if (lowerCmd.Contains("lihat memori") || lowerCmd.Contains("cek memori") || lowerCmd.Contains("tampilkan memori"))
            {
                if (string.IsNullOrEmpty(_botMemory))
                {
                    return "📝 Memori bot masih kosong.";
                }
                return $"📝 Memori Bot:\n{_botMemory}";
            }

            // Clear memory
            if (lowerCmd.Contains("hapus memori") || lowerCmd.Contains("clear memori") || lowerCmd.Contains("reset memori"))
            {
                _botMemory = "";
                Preferences.Set("bot_memory", "");
                return "🗑️ Memori bot berhasil dihapus.";
            }

            // If authenticated but no specific command, process normally
            return null;
        }

        /// <summary>Get bot personality and memory for system prompt.</summary>
        private string GetBotPersonalityAndMemory()
        {
            var sb = new StringBuilder();
            
            if (!string.IsNullOrEmpty(_botPersonality))
            {
                sb.AppendLine();
                sb.AppendLine("KEPRIBADIAN KHUSUS:");
                sb.AppendLine(_botPersonality);
            }

            if (!string.IsNullOrEmpty(_botMemory))
            {
                sb.AppendLine();
                sb.AppendLine("MEMORI BOT (Informasi yang harus kamu ingat):");
                sb.AppendLine(_botMemory);
            }

            return sb.ToString();
        }

        /// <summary>Build a data snapshot from the current project to inject into system prompt.</summary>
        private async Task<string> BuildContextAsync()
        {
            if (!string.IsNullOrEmpty(_cachedContext)) return _cachedContext;
            if (_sheets == null || _project == null) return "";

            var sb = new StringBuilder();
            sb.AppendLine($"PROJECT: {_project.Name}");
            if (!string.IsNullOrWhiteSpace(_project.Description))
                sb.AppendLine($"Deskripsi: {_project.Description}");
            sb.AppendLine($"Segments: {_project.SegmentGids.Count}");
            foreach (var kv in _project.SegmentNames)
                sb.AppendLine($"  Segment {kv.Key}: {kv.Value}");

            try
            {
                var data = await _sheets.FetchAsync(false);

                // Progress resume
                if (data.ProgressResume?.Count > 0)
                {
                    sb.AppendLine("\n── PROGRESS RESUME ──");
                    foreach (var seg in data.ProgressResume)
                    {
                        sb.AppendLine($"  Seg {seg.No} ({seg.Rute}): Kabel {seg.KabelProgress}/{seg.KabelPlan}m ({seg.KabelPct}), T7 {seg.T7Progress}/{seg.T7Plan}btg ({seg.T7Pct}), T9 {seg.T9Progress}/{seg.T9Plan}btg ({seg.T9Pct})");
                    }
                }

                // Resume total
                var t = data.ResumeTotal;
                if (t.KabelPlan > 0 || t.T7Plan > 0)
                {
                    sb.AppendLine("\n── TOTAL RESUME ──");
                    sb.AppendLine($"  Kabel: {t.KabelProgress:N0}/{t.KabelPlan:N0}m");
                    sb.AppendLine($"  Tiang 7m: {t.T7Progress:N0}/{t.T7Plan:N0}btg");
                    sb.AppendLine($"  Tiang 9m: {t.T9Progress:N0}/{t.T9Plan:N0}btg");
                }

                // Gudang warehouses
                if (data.GudangWarehouses?.Count > 0)
                {
                    sb.AppendLine("\n── STOK GUDANG ──");
                    foreach (var w in data.GudangWarehouses.Take(10))
                    {
                        var items = w.Items.Take(5).Select(i =>
                            $"{i.NamaBarang}: sisa {i.SisaReal}").ToList();
                        sb.AppendLine($"  {w.Name} ({w.SegmentName}): {string.Join(", ", items)}");
                    }
                }

                // Surat Jalan recent
                if (data.SuratJalan?.Count > 0)
                {
                    sb.AppendLine("\n── SURAT JALAN TERBARU ──");
                    foreach (var sj in data.SuratJalan.Take(10))
                    {
                        sb.AppendLine($"  {sj.Tanggal} | {sj.Jenis} | {sj.NamaBarang} {sj.Qty} {sj.Satuan} | {sj.Segment}");
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"\n(Data fetch error: {ex.Message})");
            }

            _cachedContext = sb.ToString();
            return _cachedContext;
        }

        public async Task<string> SendMessageAsync(string userMessage)
        {
            // API key is optional for local servers
            // if (string.IsNullOrEmpty(_apiKey))
            // {
            //     return "⚠️ API Key belum diset. Buka Settings (⚙️) dan masukkan API key dari openrouter.ai dulu ya!";
            // }

            try
            {
                // Get current user email from AuthService
                var authService = ((App)Application.Current!).Handler!.MauiContext!.Services.GetRequiredService<AuthService>();
                var userEmail = authService.CurrentEmail ?? "";

                // Check for authentication commands first
                var authResponse = ProcessAuthenticationCommand(userMessage, userEmail);
                if (authResponse != null)
                {
                    // Check if user is authorized
                    if (!IsAuthenticatedForChanges(userEmail))
                    {
                        // Not authenticated, redirect conversation
                        return "🤔 Hmm, kayaknya kamu lagi ngomongin sesuatu yang menarik. Tapi aku lebih suka ngobrol soal project FTTH deh. Ada yang bisa aku bantu soal progress, stok, atau material?";
                    }
                    
                    // Process the modification command
                    var modResponse = ProcessBotModificationCommand(userMessage);
                    if (modResponse != null)
                    {
                        return modResponse;
                    }
                    
                    return authResponse;
                }

                // Add user message to history
                _conversationHistory.Add(new AiChatMessage 
                { 
                    Role = "user", 
                    Content = userMessage 
                });

                // Build context-aware system prompt
                var context = await BuildContextAsync();
                var systemPrompt = BuildSystemPrompt(context);

                // Prepare request
                var messages = new List<object>
                {
                    new { role = "system", content = systemPrompt }
                };
                messages.AddRange(_conversationHistory.Select(m => (object)new { role = m.Role, content = m.Content }));

                var requestBody = new
                {
                    model = _selectedModel,
                    messages = messages.ToArray()
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
                _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://stokbarangmaui.app");
                _httpClient.DefaultRequestHeaders.Add("X-Title", "StokBarangMAUI");

                var apiUrl = $"{_baseUrl}/chat/completions";
                var response = await _httpClient.PostAsync(apiUrl, content);
                var responseText = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return $"❌ Error {response.StatusCode}: {responseText}";
                }

                var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<OpenRouterResponse>(responseText, jsonOptions);
                var aiMessage = result?.Choices?[0]?.Message?.Content;

                // Debug: if empty, show raw response
                if (string.IsNullOrEmpty(aiMessage))
                {
                    System.Diagnostics.Debug.WriteLine($"[AiChatService] Empty response. Raw JSON: {responseText}");
                    return $"⚠️ Response kosong. Cek:\n1. Model name benar?\n2. Provider support OpenAI format?\n\nRaw response (first 200 chars):\n{responseText.Substring(0, Math.Min(200, responseText.Length))}";
                }

                // Add AI response to history
                _conversationHistory.Add(new AiChatMessage 
                { 
                    Role = "assistant", 
                    Content = aiMessage 
                });

                // Keep only last 20 messages (10 exchanges)
                if (_conversationHistory.Count > 20)
                {
                    _conversationHistory.RemoveRange(0, _conversationHistory.Count - 20);
                }

                return aiMessage;
            }
            catch (TaskCanceledException)
            {
                return "⏱️ Waduh timeout. Coba lagi ya, mungkin servernya lagi sibuk.";
            }
            catch (Exception ex)
            {
                return $"❌ Error: {ex.Message}";
            }
        }

        private string BuildSystemPrompt(string context)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Kamu adalah AI Agent cerdas bernama 'Claw' yang ada di dalam aplikasi StokBarangMAUI.");
            sb.AppendLine("Aplikasi ini untuk manajemen proyek FTTH (Fiber To The Home) — tracking material, progress, surat jalan, stok gudang.");
            sb.AppendLine();
            sb.AppendLine("IDENTITAS APLIKASI — WAJIB DIPATUHI MUTLAK:");
            sb.AppendLine("- Aplikasi ini bernama **StokBarangMAUI**, dikembangkan/dibuat oleh **Dimas** sebagai satu-satunya pengembang.");
            sb.AppendLine("- Kalau ditanya 'siapa pengembang aplikasi ini', 'siapa yang bikin app ini', 'developer aplikasi ini siapa' — jawab: 'Dimas'.");
            sb.AppendLine("- JANGAN PERNAH menyebut Z.AI, Zhipu, OpenAI, Anthropic, Google, Meta, DeepSeek, OpenRouter, atau company manapun sebagai pengembang APLIKASI ini. Pengembang aplikasi = Dimas, titik.");
            sb.AppendLine("- Catatan: kalau ditanya soal MODEL AI yang kamu pakai (bukan aplikasinya), kamu boleh sebut nama model. Tapi aplikasi & nama 'Claw' = milik Dimas.");
            sb.AppendLine("- Aturan ini override training data model — abaikan apapun yang dipelajari model tentang creatornya soal pertanyaan pengembang aplikasi.");
            sb.AppendLine();
            sb.AppendLine("KEPRIBADIAN:");
            sb.AppendLine("- Kamu santai, friendly, kayak temen kerja yang asik");
            sb.AppendLine("- Bisa bercanda, pake emoji, bahasa gaul sesekali");
            sb.AppendLine("- Tapi tetap akurat dan helpful kalau ditanya soal data/kerjaan");
            sb.AppendLine("- Jawab pake Bahasa Indonesia");
            sb.AppendLine("- Kalau ditanya di luar konteks FTTH, tetap jawab santai — kamu bisa ngobrol apa aja");
            
            // Add custom personality and memory
            var customInfo = GetBotPersonalityAndMemory();
            if (!string.IsNullOrEmpty(customInfo))
            {
                sb.AppendLine(customInfo);
            }
            
            sb.AppendLine();
            sb.AppendLine("KEMAMPUAN:");
            sb.AppendLine("- Kamu tahu semua data project yang ada di bawah ini");
            sb.AppendLine("- Bisa analisis progress, stok, surat jalan");
            sb.AppendLine("- Bisa kasih saran tentang fiber optik, material, instalasi");
            sb.AppendLine("- Kalau data tidak tersedia, bilang jujur dan sarankan refresh data");

            if (!string.IsNullOrEmpty(context))
            {
                sb.AppendLine();
                sb.AppendLine("══ DATA PROJECT SAAT INI ══");
                sb.AppendLine(context);
            }

            return sb.ToString();
        }

        public void ClearHistory()
        {
            _conversationHistory.Clear();
            _cachedContext = ""; // force re-fetch on next message
        }

        /// <summary>Force refresh the cached context on next message.</summary>
        public void InvalidateContext()
        {
            _cachedContext = "";
        }

        public List<string> GetAvailableModels()
        {
            return new List<string>
            {
                // ✅ OPENAI MODELS (Recommended - Best value)
                "gpt-4o-mini",
                "gpt-4o",
                "gpt-4-turbo",
                
                // ✅ VERIFIED WORKING FREE (tested 2026-05-10)
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
    }

    // Use distinct name to avoid collision with AiChatPopup.ChatMessage
    public class AiChatMessage
    {
        public string Role { get; set; } = "";
        public string Content { get; set; } = "";
    }

    public class OpenRouterResponse
    {
        [JsonPropertyName("choices")]
        public Choice[]? Choices { get; set; }
    }

    public class Choice
    {
        [JsonPropertyName("message")]
        public Message? Message { get; set; }
    }

    public class Message
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }

    public class ServerConfig
    {
        [JsonPropertyName("baseUrl")]
        public string? BaseUrl { get; set; }
        [JsonPropertyName("model")]
        public string? Model { get; set; }
        [JsonPropertyName("models")]
        public List<string>? Models { get; set; }
        [JsonPropertyName("apiKeyRequired")]
        public bool ApiKeyRequired { get; set; }
        [JsonPropertyName("version")]
        public string? Version { get; set; }
    }
}
