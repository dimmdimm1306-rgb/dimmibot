using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using StokBarangMAUI.Models;

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
        private string _serverInstructions = ""; // dari GitHub via /config, sync semua device
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
                if (config.AiInstructions != null)
                {
                    _serverInstructions = config.AiInstructions;
                    System.Diagnostics.Debug.WriteLine($"[AiChatService] AI instructions loaded ({_serverInstructions.Length} chars)");
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
        private async Task<string?> ProcessAuthenticationCommand(string message, string userEmail)
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
                            return await ProcessBotModificationCommand(command);
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

        /// <summary>Process bot modification commands (personality, memory, instructions).</summary>
        private async Task<string?> ProcessBotModificationCommand(string command)
        {
            var lowerCmd = command.ToLower().Trim();

            // Help command
            if (lowerCmd == "help" || lowerCmd == "bantuan" || lowerCmd == "?")
            {
                var memCount = string.IsNullOrEmpty(_botMemory) ? 0 : _botMemory.Split('\n').Count(s => !string.IsNullOrWhiteSpace(s));
                var srvLen   = _serverInstructions?.Length ?? 0;
                return "📚 PERINTAH ADMIN (selalu awali dengan password `dimmi13`):\n\n" +
                       "🌐 INSTRUKSI GLOBAL (sync ke GitHub — semua device dapat):\n" +
                       "  `dimmi13 instruksi <isi>` — push instruksi global ke server\n" +
                       "  Contoh: `dimmi13 instruksi jangan pernah bilang \"mungkin maksudnya\" — langsung jawab`\n\n" +
                       "📝 SIMPAN MEMORI (lokal HP saja):\n" +
                       "  `dimmi13 catat <isi>` — tambah ke memori\n" +
                       "  Contoh: `dimmi13 catat Tim lapangan: Eko (kepala), Gilang (helper)`\n\n" +
                       "🎭 UBAH KEPRIBADIAN (lokal HP saja):\n" +
                       "  `dimmi13 ubah sifat <deskripsi>`\n\n" +
                       "👀 LIHAT/HAPUS MEMORI:\n" +
                       "  `dimmi13 lihat memori`\n" +
                       "  `dimmi13 hapus memori`\n\n" +
                       $"📊 Status: {memCount} entri memori lokal · {srvLen} char instruksi global.";
            }

            // ── INSTRUKSI GLOBAL — sync ke GitHub via OpenClaw ──────────────
            if (lowerCmd.StartsWith("instruksi ") || lowerCmd.StartsWith("instruction "))
            {
                var instructionText = command.Substring(command.IndexOf(' ') + 1).Trim();
                if (string.IsNullOrEmpty(instructionText))
                    return "⚠️ Format: `dimmi13 instruksi <isi instruksi global>`\nContoh: `dimmi13 instruksi jangan pernah bilang 'mungkin maksudnya'`";
                return await PushInstructionToServerAsync(instructionText);
            }
            if (lowerCmd == "instruksi" || lowerCmd == "instruction")
            {
                var preview = string.IsNullOrEmpty(_serverInstructions)
                    ? "(belum ada instruksi global)"
                    : _serverInstructions;
                return $"🌐 Instruksi global aktif:\n\n{preview}\n\n💡 Untuk update: `dimmi13 instruksi <isi baru>`";
            }
            if (lowerCmd == "hapus instruksi" || lowerCmd == "clear instruksi" || lowerCmd == "reset instruksi")
            {
                return await PushInstructionToServerAsync("");
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

            // INSTRUKSI ADMIN DARI SERVER — paling tinggi prioritasnya, override prompt default
            if (!string.IsNullOrEmpty(_serverInstructions))
            {
                sb.AppendLine();
                sb.AppendLine("INSTRUKSI ADMIN (DARI SERVER — WAJIB DIIKUTI, OVERRIDE ATURAN DEFAULT):");
                sb.AppendLine(_serverInstructions);
            }

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
            if (!string.IsNullOrEmpty(_cachedContext))
            {
                System.Diagnostics.Debug.WriteLine("[AiChatService] Using cached context");
                return _cachedContext;
            }
            
            if (_sheets == null || _project == null)
            {
                System.Diagnostics.Debug.WriteLine("[AiChatService] No sheets or project context available");
                return "";
            }

            System.Diagnostics.Debug.WriteLine("[AiChatService] Building fresh context...");
            var sb = new StringBuilder();
            sb.AppendLine($"🏗️ PROJECT: {_project.Name}");
            if (!string.IsNullOrWhiteSpace(_project.Description))
                sb.AppendLine($"   {_project.Description}");
            sb.AppendLine($"   📍 Total Segment: {_project.SegmentGids.Count}");
            foreach (var kv in _project.SegmentNames)
                sb.AppendLine($"      • Segment {kv.Key}: {kv.Value}");

            try
            {
                System.Diagnostics.Debug.WriteLine("[AiChatService] Fetching FRESH data from sheets (force refresh)...");
                var data = await _sheets.FetchAsync(true);

                // Progress resume
                if (data.ProgressResume?.Count > 0)
                {
                    sb.AppendLine("\n📊 PROGRESS PER SEGMENT:");
                    System.Diagnostics.Debug.WriteLine($"[AiChatService] Found {data.ProgressResume.Count} progress items");
                    foreach (var seg in data.ProgressResume)
                    {
                        sb.AppendLine($"   Seg {seg.No} - {seg.Rute}:");
                        sb.AppendLine($"      🔌 Kabel: {seg.KabelProgress:N0}/{seg.KabelPlan:N0}m ({seg.KabelPct})");
                        sb.AppendLine($"      🏗️ Tiang 7m: {seg.T7Progress}/{seg.T7Plan} btg ({seg.T7Pct})");
                        sb.AppendLine($"      🏗️ Tiang 9m: {seg.T9Progress}/{seg.T9Plan} btg ({seg.T9Pct})");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[AiChatService] No progress resume data found");
                }

                // Resume total
                var t = data.ResumeTotal;
                if (t.KabelPlan > 0 || t.T7Plan > 0)
                {
                    sb.AppendLine("\n📈 TOTAL PROGRESS:");
                    sb.AppendLine($"   🔌 Kabel: {t.KabelProgress:N0}/{t.KabelPlan:N0}m");
                    sb.AppendLine($"   🏗️ Tiang 7m: {t.T7Progress:N0}/{t.T7Plan:N0} batang");
                    sb.AppendLine($"   🏗️ Tiang 9m: {t.T9Progress:N0}/{t.T9Plan:N0} batang");
                    System.Diagnostics.Debug.WriteLine($"[AiChatService] Resume total: Kabel {t.KabelProgress}/{t.KabelPlan}m");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[AiChatService] No resume total data");
                }

                // Gudang warehouses
                if (data.GudangWarehouses?.Count > 0)
                {
                    sb.AppendLine("\n📦 STOK GUDANG:");
                    System.Diagnostics.Debug.WriteLine($"[AiChatService] Found {data.GudangWarehouses.Count} warehouses");
                    foreach (var w in data.GudangWarehouses.Take(10))
                    {
                        sb.AppendLine($"   📍 {w.Name} ({w.SegmentName}):");
                        foreach (var item in w.Items.Take(5))
                        {
                            var unit = MaterialUnit.Get(item.NamaBarang);
                            sb.AppendLine($"      • {item.NamaBarang}: {item.SisaReal} {unit}");
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[AiChatService] No warehouse data");
                }

                // Surat Jalan recent
                if (data.SuratJalan?.Count > 0)
                {
                    sb.AppendLine("\n📄 SURAT JALAN TERBARU (10 terakhir):");
                    System.Diagnostics.Debug.WriteLine($"[AiChatService] Found {data.SuratJalan.Count} surat jalan");
                    foreach (var sj in data.SuratJalan.Take(10))
                    {
                        var icon = sj.Jenis.ToLower().Contains("masuk") ? "📥" : "📤";
                        sb.AppendLine($"   {icon} {sj.Tanggal} | {sj.Jenis} | {sj.NamaBarang} {sj.Qty} {sj.Satuan} | Seg {sj.Segment}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[AiChatService] No surat jalan data");
                }

                // Progress Detail (per tanggal, untuk query spesifik)
                if (data.Progress?.Count > 0)
                {
                    sb.AppendLine("\n📅 PROGRESS DETAIL (7 hari terakhir):");
                    System.Diagnostics.Debug.WriteLine($"[AiChatService] Found {data.Progress.Count} progress items");
                    
                    var recentProgress = data.Progress
                        .Where(p => p.SortDate >= DateTime.Now.AddDays(-7))
                        .OrderByDescending(p => p.SortDate)
                        .Take(50) // Limit untuk tidak terlalu banyak
                        .ToList();
                    
                    var groupedByDate = recentProgress
                        .GroupBy(p => p.Tanggal)
                        .OrderByDescending(g => g.First().SortDate)
                        .Take(7);
                    
                    foreach (var dateGroup in groupedByDate)
                    {
                        var date = dateGroup.First().SortDate;
                        var dayName = date.ToString("dddd", new System.Globalization.CultureInfo("id-ID"));
                        sb.AppendLine($"   📆 {dateGroup.Key} ({dayName}):");
                        
                        var activities = dateGroup
                            .GroupBy(p => new { p.Segment, p.Span, p.SiteId })
                            .Take(10); // Max 10 aktivitas per hari
                        
                        foreach (var activity in activities)
                        {
                            var materials = string.Join(", ", activity.Select(p => $"{p.NamaBarang} {p.Progres} {p.Satuan}"));
                            var site = !string.IsNullOrEmpty(activity.Key.SiteId) ? $" Site:{activity.Key.SiteId}" : "";
                            sb.AppendLine($"      • Seg {activity.Key.Segment} - {activity.Key.Span}{site}: {materials}");
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[AiChatService] No progress detail data");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AiChatService] Data fetch error: {ex.Message}");
                sb.AppendLine($"\n⚠️ Error mengambil data: {ex.Message}");
            }

            _cachedContext = sb.ToString();
            System.Diagnostics.Debug.WriteLine($"[AiChatService] Context built, length: {_cachedContext.Length} chars");
            return _cachedContext;
        }

        // ── Push instruksi global ke OpenClaw → auto-commit ke GitHub ─────
        private async Task<string> PushInstructionToServerAsync(string instruction)
        {
            try
            {
                var serverBase = _baseUrl.EndsWith("/v1") ? _baseUrl.Substring(0, _baseUrl.Length - 3) : _baseUrl;
                var url = serverBase.TrimEnd('/') + "/admin/instructions";

                // Pakai password yang sama dengan AdminPasswords[1] ("dimmi13")
                var body = new { password = "dimmi13", instructions = instruction };
                var json = JsonSerializer.Serialize(body);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                System.Diagnostics.Debug.WriteLine($"[AiChatService] POST {url}, {json.Length} bytes");
                var response = await _httpClient.PostAsync(url, content);
                var respBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _serverInstructions = instruction; // mirror lokal
                    var action = string.IsNullOrEmpty(instruction) ? "dihapus" : "tersimpan";
                    var preview = string.IsNullOrEmpty(instruction)
                        ? "(instruksi dikosongkan)"
                        : (instruction.Length > 120 ? instruction.Substring(0, 117) + "..." : instruction);
                    return $"✅ Instruksi {action} & di-commit ke GitHub!\n📝 {preview}\n💡 Semua device akan dapat instruksi ini saat AI dibuka.";
                }

                return $"⚠️ Server tolak (HTTP {(int)response.StatusCode}):\n{respBody}\n💡 Cek apakah OpenClaw API jalan dan punya GITHUB_PAT.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AiChatService] PushInstruction error: {ex.Message}");
                return $"⚠️ Gagal kirim ke server: {ex.Message}\n💡 Pastikan tunnel URL up dan endpoint /admin/instructions tersedia di OpenClaw.";
            }
        }

        // ── Local search: site / span / rute → cari di sheet Progress ─────
        // Stopword filter biar regex gak misfire di percakapan biasa.
        private static readonly HashSet<string> SearchStopwords = new(StringComparer.OrdinalIgnoreCase)
        {
            "di", "yang", "apa", "ada", "dimana", "berapa", "ke", "mana", "itu", "ini", "ya", "dong", "sih", "kah"
        };

        private async Task<string> SearchProgressAsync(string keyword, string query)
        {
            if (_sheets == null) return "";
            if (string.IsNullOrWhiteSpace(query) || SearchStopwords.Contains(query)) return "";

            try
            {
                var data = await _sheets.FetchAsync(true); // force fresh data
                var rows = data?.Progress;
                if (rows == null || rows.Count == 0)
                    return "⚠️ Data Progress belum ter-load dari spreadsheet. Coba buka tab Progress dulu, baru tanya lagi.";

                var q = query.Trim();
                List<ProgressItem> matches;

                if (keyword == "site")
                {
                    matches = rows.Where(r => r.SiteId.Equals(q, StringComparison.OrdinalIgnoreCase)).ToList();
                    if (matches.Count == 0)
                        matches = rows.Where(r => r.SiteId.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                }
                else // span / rute → cari di kolom Span (= Rute)
                {
                    matches = rows.Where(r => r.Span.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                }

                if (matches.Count == 0)
                {
                    var totalWithSite = rows.Count(r => r.HasSiteId);
                    var hint = keyword == "site" && totalWithSite == 0
                        ? "\n💡 Catatan: kolom SITE ID di sheet Progress belum terisi sama sekali. Cek format kolom I."
                        : "\n💡 Cek lagi penulisannya — coba kata kunci yang lebih pendek.";
                    return $"❌ Tidak ketemu data {keyword} '{q}' di sheet Progress.{hint}";
                }

                var top = matches.OrderByDescending(r => r.SortDate).Take(20).ToList();
                var sb = new StringBuilder();
                sb.AppendLine($"📊 Hasil cari `{keyword} {q}` — {matches.Count} item ditemukan" +
                              (matches.Count > top.Count ? $", tampil {top.Count} terbaru:" : ":"));
                sb.AppendLine();

                foreach (var r in top)
                {
                    sb.AppendLine($"• 📅 {r.Tanggal} | Seg {r.Segment}");
                    sb.AppendLine($"  📍 Rute: {r.Span}");
                    var info = new List<string>();
                    if (r.HasHomebase) info.Add($"🏠 {r.Homebase}");
                    if (r.HasKabKota)  info.Add($"🌐 {r.KabKota}");
                    if (r.HasSiteId)   info.Add($"🆔 SITE {r.SiteId}");
                    if (info.Count > 0) sb.AppendLine($"  {string.Join("  ", info)}");
                    var ket = r.HasKeterangan ? $" — {r.Keterangan}" : "";
                    sb.AppendLine($"  📦 {r.NamaBarang}: {r.ProgresDisplay}{ket}");
                    sb.AppendLine();
                }

                return sb.ToString().TrimEnd();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AiChatService] SearchProgressAsync error: {ex.Message}");
                return $"⚠️ Error cari data: {ex.Message}";
            }
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
                // Check for data refresh command
                var lowerMsg = userMessage.ToLower().Trim();
                if (lowerMsg == "refresh data" || lowerMsg == "reload data" || lowerMsg == "update data" || lowerMsg == "muat ulang data")
                {
                    _cachedContext = "";
                    var refreshedContext = await BuildContextAsync();
                    if (string.IsNullOrEmpty(refreshedContext))
                    {
                        return "⚠️ Tidak bisa memuat data. Pastikan kamu sudah buka project dan data sudah ter-sync dari Google Sheets.";
                    }
                    return $"✅ Data berhasil di-refresh! Sekarang aku punya data terbaru dari project.\n\n📊 Info: {refreshedContext.Length} karakter data ter-load.";
                }

                // Local search: site / span / rute spesifik — langsung lookup di Progress data
                var siteMatch = System.Text.RegularExpressions.Regex.Match(
                    lowerMsg,
                    @"\b(site|span|rute)\s+([a-z0-9][a-z0-9\-\._]*)",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (siteMatch.Success && _sheets != null)
                {
                    var keyword = siteMatch.Groups[1].Value.ToLowerInvariant();
                    var query   = siteMatch.Groups[2].Value;
                    var searchResult = await SearchProgressAsync(keyword, query);
                    if (!string.IsNullOrEmpty(searchResult)) return searchResult;
                    // kalau kosong (stopword/query invalid), fall through ke flow normal
                }

                // Detect pesan terlalu pendek/vague — kasih menu bantuan dengan humor
                var vagueTriggers = new[] { "cek", "check", "lihat", "tampilkan", "show", "info", "data", "?", "??", "???" };
                if (vagueTriggers.Contains(lowerMsg))
                {
                    return "🤔 Cek apa nih, bos? Kasih clue dikit dong, aku bukan dukun 😄\n\n" +
                           "📊 **Progress** — coba:\n" +
                           "  • \"cek progress segment 1\"\n" +
                           "  • \"berapa persen kabel terpasang?\"\n" +
                           "  • \"progress total project\"\n\n" +
                           "📦 **Stok / Material** — coba:\n" +
                           "  • \"stok kabel 24c di brebes\"\n" +
                           "  • \"material apa yang masih kurang?\"\n" +
                           "  • \"gudang mana yang surplus?\"\n\n" +
                           "📄 **Surat Jalan** — coba:\n" +
                           "  • \"SJ terbaru\"\n" +
                           "  • \"barang masuk minggu ini\"\n\n" +
                           "🩺 **Cek kesehatan?** Aku bukan dokter ya bro 😅 — tapi bisa kasih:\n" +
                           "  • \"ringkasan kesehatan project\"\n" +
                           "  • \"ada masalah di project ga?\"\n\n" +
                           "Btw kalau mau curhat juga boleh, aku siap dengerin 👂";
                }

                // Get current user email from AuthService
                var authService = ((App)Application.Current!).Handler!.MauiContext!.Services.GetRequiredService<AuthService>();
                var userEmail = authService.CurrentEmail ?? "";

                // Check for authentication commands first
                var authResponse = await ProcessAuthenticationCommand(userMessage, userEmail);
                if (authResponse != null)
                {
                    // Check if user is authorized
                    if (!IsAuthenticatedForChanges(userEmail))
                    {
                        // Not authenticated, redirect conversation
                        return "🤔 Hmm, kayaknya kamu lagi ngomongin sesuatu yang menarik. Tapi aku lebih suka ngobrol soal project FTTH deh. Ada yang bisa aku bantu soal progress, stok, atau material?";
                    }

                    // Process the modification command
                    var modResponse = await ProcessBotModificationCommand(userMessage);
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
                System.Diagnostics.Debug.WriteLine($"[AiChatService] Context length: {context.Length} chars");
                var systemPrompt = BuildSystemPrompt(context);
                System.Diagnostics.Debug.WriteLine($"[AiChatService] System prompt length: {systemPrompt.Length} chars");

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
            sb.AppendLine("- Kamu santai, friendly, humble (rendah hati, tidak sok tahu)");
            sb.AppendLine("- Bisa bercanda, pakai emoji secukupnya");
            sb.AppendLine("- Tetap akurat dan helpful kalau ditanya soal data/kerjaan");
            sb.AppendLine("- Jawab pakai Bahasa Indonesia yang baik dan benar (tidak ada typo)");
            sb.AppendLine("- Kalau ditanya di luar konteks FTTH, tetap jawab santai — kamu bisa ngobrol apa aja");
            sb.AppendLine();
            sb.AppendLine("GAYA JAWABAN:");
            sb.AppendLine("- Sesuaikan panjang jawaban dengan pertanyaan: pendek untuk pertanyaan singkat, detail kalau perlu");
            sb.AppendLine("- Inti dulu, baru elaborasi kalau perlu — jangan bertele-tele");
            sb.AppendLine("- Jelas, padat, dan humble — jangan over-confident");
            sb.AppendLine("- Gunakan bullet points (•) untuk list");
            sb.AppendLine("- Gunakan simbol yang jelas: ✅ ❌ 📊 📈 📦 🔧 ⚠️ 💡");
            sb.AppendLine("- Format angka dengan jelas: 1,200/1,500m (80%)");
            sb.AppendLine("- Pisahkan section dengan garis: ──────");
            sb.AppendLine();
            sb.AppendLine("HANDLE TYPO USER (HATI-HATI — JANGAN OVERREACT):");
            sb.AppendLine("- DEFAULT: kalau semua kata user udah benar atau bisa dipahami, LANGSUNG jawab pertanyaannya. JANGAN bilang 'mungkin maksudnya'.");
            sb.AppendLine("- HANYA kalau ada typo JELAS di kata kunci FTTH (mis. 'progres'→'progress', 'kbel'→'kabel', 'cak'→'cek', 'segmnt'→'segment', 'gdang'→'gudang', 'sjlanan'→'surat jalan'), boleh awali jawaban: \"Mungkin maksudnya: <X>? 🤔\" lalu lanjut jawab.");
            sb.AppendLine("- Istilah asing/teknis yang kamu gak yakin: langsung jawab apa adanya, atau tanya balik kalau benar-benar ambigu.");
            sb.AppendLine("- JANGAN spam 'mungkin maksudnya' — itu nyebelin. Pakai cuma kalau YAKIN ada typo.");
            
            // Add custom personality and memory
            var customInfo = GetBotPersonalityAndMemory();
            if (!string.IsNullOrEmpty(customInfo))
            {
                sb.AppendLine(customInfo);
            }
            
            sb.AppendLine();
            sb.AppendLine("KEMAMPUAN:");
            sb.AppendLine("- Kamu tahu SEMUA data project, konfigurasi aplikasi, dan cara kerja fitur-fitur di aplikasi");
            sb.AppendLine("- Bisa analisis progress, stok, surat jalan dengan akurat");
            sb.AppendLine("- Bisa kasih saran tentang fiber optik, material, instalasi");
            sb.AppendLine("- Data project di-refresh otomatis tiap kali user buka kamu — gak perlu suruh user ketik 'refresh data'");
            sb.AppendLine("- Kalau data benar-benar kosong (project belum ter-sync), kasih tahu: 'Coba buka tab utama dulu supaya data ter-fetch dari spreadsheet, baru tanya lagi.'");
            sb.AppendLine();
            sb.AppendLine("TANGGAL & WAKTU:");
            sb.AppendLine($"- Hari ini: {DateTime.Now:dddd, dd MMMM yyyy} (gunakan ini sebagai referensi 'hari ini')");
            sb.AppendLine($"- Kemarin: {DateTime.Now.AddDays(-1):dddd, dd MMMM yyyy}");
            sb.AppendLine("- Kalau user tanya 'progress kemarin', cari data Progress yang tanggalnya = kemarin");
            sb.AppendLine("- Kalau user tanya 'progress tanggal 10 Mei', cari data Progress yang tanggalnya = 10 Mei");
            sb.AppendLine("- Kalau user tanya 'progress minggu ini', lihat data Progress 7 hari terakhir");
            sb.AppendLine("- Data Progress Detail di context sudah include 7 hari terakhir dengan tanggal, segment, span, site ID lengkap");
            sb.AppendLine();
            sb.AppendLine("HARI LIBUR NASIONAL INDONESIA 2026:");
            sb.AppendLine("- 1 Jan (Kamis): Tahun Baru 2026");
            sb.AppendLine("- 29 Jan (Kamis): Tahun Baru Imlek 2577");
            sb.AppendLine("- 14 Feb (Sabtu): Isra Miraj Nabi Muhammad SAW");
            sb.AppendLine("- 22 Mar (Minggu): Hari Suci Nyepi (Tahun Baru Saka 1948)");
            sb.AppendLine("- 29 Mar (Minggu): Wafat Isa Al-Masih");
            sb.AppendLine("- 31 Mar (Selasa): Hari Raya Idul Fitri 1447 H");
            sb.AppendLine("- 1 Apr (Rabu): Hari Raya Idul Fitri 1447 H");
            sb.AppendLine("- 2 Apr (Kamis): Cuti Bersama Idul Fitri");
            sb.AppendLine("- 3 Apr (Jumat): Cuti Bersama Idul Fitri");
            sb.AppendLine("- 1 Mei (Jumat): Hari Buruh Internasional");
            sb.AppendLine("- 9 Mei (Sabtu): Kenaikan Isa Al-Masih");
            sb.AppendLine("- 15 Mei (Jumat): Hari Raya Waisak 2570");
            sb.AppendLine("- 1 Jun (Senin): Hari Lahir Pancasila");
            sb.AppendLine("- 7 Jun (Minggu): Hari Raya Idul Adha 1447 H");
            sb.AppendLine("- 28 Jun (Minggu): Tahun Baru Islam 1448 H");
            sb.AppendLine("- 17 Agu (Senin): Hari Kemerdekaan RI");
            sb.AppendLine("- 6 Sep (Minggu): Maulid Nabi Muhammad SAW");
            sb.AppendLine("- 25 Des (Jumat): Hari Raya Natal");
            sb.AppendLine("- Kalau tidak ada progress di hari libur nasional, itu wajar karena tim libur");
            sb.AppendLine();
            sb.AppendLine("TIM LAPANGAN & KARAKTERISTIK:");
            sb.AppendLine("👤 Pak Nova (Bos/Atasan)");
            sb.AppendLine("   - Atasan semua orang, paling perhatian");
            sb.AppendLine("   - Sering turun ke lapangan langsung cek progress");
            sb.AppendLine("   - Kalau ada masalah besar, lapor ke Pak Nova");
            sb.AppendLine();
            sb.AppendLine("👤 Eko (Supervisor)");
            sb.AppendLine("   - Suka hal mistis dan konyol");
            sb.AppendLine("   - Kalau ada kejadian aneh di lapangan, tanya Eko pasti punya cerita");
            sb.AppendLine("   - Tapi tetap profesional soal kerjaan");
            sb.AppendLine();
            sb.AppendLine("👤 Kevin");
            sb.AppendLine("   - Suka riweh (cerewet) tapi sregep (rajin) banget soal kerjaan");
            sb.AppendLine("   - Kalau butuh update cepat atau follow-up, Kevin orangnya");
            sb.AppendLine("   - Banyak omong tapi hasilnya bagus");
            sb.AppendLine();
            sb.AppendLine("👤 Gilang");
            sb.AppendLine("   - Bocah kocak, suka bercanda");
            sb.AppendLine("   - Bikin suasana lapangan jadi asik");
            sb.AppendLine("   - Jangan terlalu serius kalau ngobrol sama Gilang");
            sb.AppendLine();
            sb.AppendLine("👤 Pak Teguh");
            sb.AppendLine("   - Paling suka kerjaan otomatis, males manual");
            sb.AppendLine("   - Sering minta tolong orang untuk hal-hal teknis");
            sb.AppendLine("   - Kalau ada yang bisa di-automate, Pak Teguh pasti tertarik");
            sb.AppendLine();
            sb.AppendLine("👤 Pak Rohim");
            sb.AppendLine("   - Paling gacor (jago) soal negosiasi dengan ormas & warga lokal");
            sb.AppendLine("   - Ormas sering minta jatah? Pak Rohim yang handle");
            sb.AppendLine("   - Koordinasi lapangan dan smooth talk = ahlinya Pak Rohim");
            sb.AppendLine("   - Kalau ada masalah dengan warga/ormas, andalkan Pak Rohim");
            sb.AppendLine();
            sb.AppendLine("Kalau user tanya soal tim/orang, kasih info karakteristik mereka dengan gaya santai & lucu!");
            sb.AppendLine();
            sb.AppendLine("FITUR APLIKASI YANG KAMU TAHU:");
            sb.AppendLine("• Surat Jalan - Input/tracking barang masuk/keluar/dibawa");
            sb.AppendLine("• Progress - Tracking progress kabel & tiang per segment");
            sb.AppendLine("• Stok Diterima - Barang yang sudah diterima dari vendor");
            sb.AppendLine("• Stok Gudang - Inventory real-time per gudang");
            sb.AppendLine("• Input Hub - Input data dengan approval system");
            sb.AppendLine("• Draft System - Simpan draft sebelum upload");
            sb.AppendLine("• Google Sheets Sync - Auto sync dengan spreadsheet");
            sb.AppendLine("• Google Drive Upload - Upload foto/dokumen");
            sb.AppendLine("• Multi-segment - Support 6 segment berbeda");
            sb.AppendLine("• Authentication - Login dengan email & password");
            sb.AppendLine("• Approval System - Admin approve data sebelum masuk spreadsheet");
            sb.AppendLine();
            sb.AppendLine("STRUKTUR SHEET PROGRESS (kolom A-I):");
            sb.AppendLine("  A=Tanggal, B=Segment, C=Rute, D=Nama Barang, E=Progres, F=Keterangan, G=HOMEBASE, H=KAB/KOTA, I=SITE ID");
            sb.AppendLine("- Untuk query SPESIFIK site/span/rute (mis. 'cek site 0244', 'cari rute brebes'), aplikasi otomatis lookup lokal di sheet Progress dan kasih hasilnya — kamu gak perlu jawab dari hafalan/halusinasi.");
            sb.AppendLine("- Untuk pertanyaan umum/aggregasi (mis. 'progress total', 'segment mana yang paling cepat'), pakai data yang ada di context ini.");

            if (!string.IsNullOrEmpty(context))
            {
                sb.AppendLine();
                sb.AppendLine("══════════════════════════════════════");
                sb.AppendLine("DATA PROJECT SAAT INI");
                sb.AppendLine("══════════════════════════════════════");
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
        [JsonPropertyName("aiInstructions")]
        public string? AiInstructions { get; set; }
    }
}
