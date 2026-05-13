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
        //  GITHUB RAW CONFIG URL 
        // HP akan auto-fetch URL Cloudflare terbaru dari file ini setiap
        // kali app dibuka. Update file di GitHub dari laptop kalau URL
        // Cloudflare berubah  HP gak perlu di-setting manual.
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

        /// <summary>Public accessor untuk AboutPage (diagnostics).</summary>
        public string GetGithubConfigUrl() => GITHUB_CONFIG_URL;

        private readonly HttpClient _httpClient;
        private readonly List<AiChatMessage> _conversationHistory = new();
        private string _baseUrl = "https://openrouter.ai/api/v1";
        private string _apiKey = "";
        private string _selectedModel = "liquid/lfm-2.5-1.2b-instruct:free";

        // Context injection  set by the app before first chat
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

        /// <summary>Fetch centralized config  prioritas GitHub raw (stabil, gak pernah berubah URL-nya), fallback ke server /config.</summary>
        public async Task FetchServerConfigAsync()
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

                // Apply GDrive Reader remote config  hemat setup per HP user
                try
                {
                    if (!string.IsNullOrEmpty(config.GDriveReaderUrl)
                        || !string.IsNullOrEmpty(config.GDriveReaderToken)
                        || config.GDriveReaderEnabled.HasValue
                        || (config.GDriveAliases != null && config.GDriveAliases.Count > 0))
                    {
                        var drive = ((App)Application.Current!).Handler!.MauiContext!
                            .Services.GetService<GDriveReaderService>();
                        drive?.ApplyRemoteConfig(
                            config.GDriveReaderUrl,
                            config.GDriveReaderToken,
                            config.GDriveReaderEnabled,
                            config.GDriveAliases);
                        System.Diagnostics.Debug.WriteLine($"[AiChatService] GDrive remote config applied from {source} ({config.GDriveAliases?.Count ?? 0} aliases)");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AiChatService] GDrive remote config apply failed: {ex.Message}");
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
                            return " Autentikasi berhasil! Sekarang kamu bisa mengubah kepribadian dan memori bot selama 1 jam ke depan.";
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
                return " PERINTAH ADMIN (selalu awali dengan password `dimmi13`):\n\n" +
                       " INSTRUKSI GLOBAL (sync ke GitHub  semua device dapat):\n" +
                       "  `dimmi13 instruksi <isi>`  push instruksi global ke server\n" +
                       "  Contoh: `dimmi13 instruksi jangan pernah bilang \"mungkin maksudnya\"  langsung jawab`\n\n" +
                       " SIMPAN MEMORI (lokal HP saja):\n" +
                       "  `dimmi13 catat <isi>`  tambah ke memori\n" +
                       "  Contoh: `dimmi13 catat Tim lapangan: Eko (kepala), Gilang (helper)`\n\n" +
                       " UBAH KEPRIBADIAN (lokal HP saja):\n" +
                       "  `dimmi13 ubah sifat <deskripsi>`\n\n" +
                       " LIHAT/HAPUS MEMORI:\n" +
                       "  `dimmi13 lihat memori`\n" +
                       "  `dimmi13 hapus memori`\n\n" +
                       $" Status: {memCount} entri memori lokal  {srvLen} char instruksi global.";
            }

            //  INSTRUKSI GLOBAL  sync ke GitHub via OpenClaw 
            if (lowerCmd.StartsWith("instruksi ") || lowerCmd.StartsWith("instruction "))
            {
                var instructionText = command.Substring(command.IndexOf(' ') + 1).Trim();
                if (string.IsNullOrEmpty(instructionText))
                    return " Format: `dimmi13 instruksi <isi instruksi global>`\nContoh: `dimmi13 instruksi jangan pernah bilang 'mungkin maksudnya'`";
                return await PushInstructionToServerAsync(instructionText);
            }
            if (lowerCmd == "instruksi" || lowerCmd == "instruction")
            {
                var preview = string.IsNullOrEmpty(_serverInstructions)
                    ? "(belum ada instruksi global)"
                    : _serverInstructions;
                return $" Instruksi global aktif:\n\n{preview}\n\n Untuk update: `dimmi13 instruksi <isi baru>`";
            }
            if (lowerCmd == "hapus instruksi" || lowerCmd == "clear instruksi" || lowerCmd == "reset instruksi")
            {
                return await PushInstructionToServerAsync("");
            }

            // Add to memory  pakai keyword + space supaya gak misfire
            if (lowerCmd.StartsWith("catat ") || lowerCmd.StartsWith("ingat ") || lowerCmd.StartsWith("tambah "))
            {
                var memoryText = command.Substring(command.IndexOf(' ') + 1).Trim();
                if (!string.IsNullOrEmpty(memoryText))
                {
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                    _botMemory += $"\n[{timestamp}] {memoryText}";
                    Preferences.Set("bot_memory", _botMemory);
                    var totalLines = _botMemory.Split('\n').Count(s => !string.IsNullOrWhiteSpace(s));
                    return $" Tercatat di memori HP: \"{memoryText}\"\n Total {totalLines} entri tersimpan.";
                }
            }
            if (lowerCmd == "catat" || lowerCmd == "ingat" || lowerCmd == "tambah")
            {
                return " Format salah. Pakai: `dimmi13 catat <isi yang mau dicatat>`\nContoh: `dimmi13 catat Tim lapangan ada Eko dan Gilang`";
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
                    return $" Kepribadian bot diubah menjadi: \"{personalityText}\"";
                }
            }

            // View memory
            if (lowerCmd.Contains("lihat memori") || lowerCmd.Contains("cek memori") || lowerCmd.Contains("tampilkan memori"))
            {
                if (string.IsNullOrEmpty(_botMemory))
                {
                    return " Memori bot masih kosong.";
                }
                return $" Memori Bot:\n{_botMemory}";
            }

            // Clear memory
            if (lowerCmd.Contains("hapus memori") || lowerCmd.Contains("clear memori") || lowerCmd.Contains("reset memori"))
            {
                _botMemory = "";
                Preferences.Set("bot_memory", "");
                return " Memori bot berhasil dihapus.";
            }

            // If authenticated but no specific command, process normally
            return null;
        }

        /// <summary>Get bot personality and memory for system prompt.</summary>
        private string GetBotPersonalityAndMemory()
        {
            var sb = new StringBuilder();

            // INSTRUKSI ADMIN DARI SERVER  paling tinggi prioritasnya, override prompt default
            if (!string.IsNullOrEmpty(_serverInstructions))
            {
                sb.AppendLine();
                sb.AppendLine("INSTRUKSI ADMIN (DARI SERVER  WAJIB DIIKUTI, OVERRIDE ATURAN DEFAULT):");
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
            sb.AppendLine($" PROJECT: {_project.Name}");
            if (!string.IsNullOrWhiteSpace(_project.Description))
                sb.AppendLine($"   {_project.Description}");
            sb.AppendLine($"    Total Segment: {_project.SegmentGids.Count}");
            foreach (var kv in _project.SegmentNames)
                sb.AppendLine($"       Segment {kv.Key}: {kv.Value}");

            try
            {
                System.Diagnostics.Debug.WriteLine("[AiChatService] Fetching FRESH data from sheets (force refresh)...");
                var data = await _sheets.FetchAsync(true);

                // Progress resume
                if (data.ProgressResume?.Count > 0)
                {
                    sb.AppendLine("\n PROGRESS PER SEGMENT:");
                    System.Diagnostics.Debug.WriteLine($"[AiChatService] Found {data.ProgressResume.Count} progress items");
                    foreach (var seg in data.ProgressResume)
                    {
                        sb.AppendLine($"   Seg {seg.No} - {seg.Rute}:");
                        sb.AppendLine($"       Kabel: {seg.KabelProgress:N0}/{seg.KabelPlan:N0}m ({seg.KabelPct})");
                        sb.AppendLine($"       Tiang 7m: {seg.T7Progress}/{seg.T7Plan} btg ({seg.T7Pct})");
                        sb.AppendLine($"       Tiang 9m: {seg.T9Progress}/{seg.T9Plan} btg ({seg.T9Pct})");
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
                    sb.AppendLine("\n TOTAL PROGRESS:");
                    sb.AppendLine($"    Kabel: {t.KabelProgress:N0}/{t.KabelPlan:N0}m");
                    sb.AppendLine($"    Tiang 7m: {t.T7Progress:N0}/{t.T7Plan:N0} batang");
                    sb.AppendLine($"    Tiang 9m: {t.T9Progress:N0}/{t.T9Plan:N0} batang");
                    System.Diagnostics.Debug.WriteLine($"[AiChatService] Resume total: Kabel {t.KabelProgress}/{t.KabelPlan}m");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[AiChatService] No resume total data");
                }

                // Gudang warehouses
                if (data.GudangWarehouses?.Count > 0)
                {
                    sb.AppendLine("\n STOK GUDANG:");
                    System.Diagnostics.Debug.WriteLine($"[AiChatService] Found {data.GudangWarehouses.Count} warehouses");
                    foreach (var w in data.GudangWarehouses.Take(10))
                    {
                        sb.AppendLine($"    {w.Name} ({w.SegmentName}):");
                        foreach (var item in w.Items.Take(5))
                        {
                            var unit = MaterialUnit.Get(item.NamaBarang);
                            sb.AppendLine($"       {item.NamaBarang}: {item.SisaReal} {unit}");
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
                    sb.AppendLine("\n SURAT JALAN TERBARU (10 terakhir, urut dari paling baru):");
                    System.Diagnostics.Debug.WriteLine($"[AiChatService] Found {data.SuratJalan.Count} surat jalan");
                    var recentSJ = data.SuratJalan
                        .OrderByDescending(s => s.SortDate)
                        .Take(10);
                    foreach (var sj in recentSJ)
                    {
                        var icon = sj.Jenis.ToLower().Contains("masuk") ? "" : "";
                        sb.AppendLine($"   {icon} {sj.Tanggal} | {sj.Jenis} | {sj.NamaBarang} {sj.Qty} {sj.Satuan} | Seg {sj.Segment}");
                        var pgr = string.IsNullOrWhiteSpace(sj.Pengirim) ? "-" : sj.Pengirim;
                        var pnr = string.IsNullOrWhiteSpace(sj.Penerima) ? "-" : sj.Penerima;
                        var noSj = string.IsNullOrWhiteSpace(sj.NoSJ) ? "-" : sj.NoSJ;
                        sb.AppendLine($"      No.SJ: {noSj} | Pengirim: {pgr}  Penerima: {pnr}");
                        if (!string.IsNullOrWhiteSpace(sj.Keterangan))
                            sb.AppendLine($"      Ket: {sj.Keterangan}");
                        if (sj.HasLink)
                            sb.AppendLine($"       {sj.DriveUrl}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[AiChatService] No surat jalan data");
                }

                // Progress Detail (per tanggal, untuk query spesifik)
                if (data.Progress?.Count > 0)
                {
                    sb.AppendLine("\n PROGRESS DETAIL (7 hari terakhir):");
                    System.Diagnostics.Debug.WriteLine($"[AiChatService] Found {data.Progress.Count} progress items");
                    
                    var recentProgress = data.Progress
                        .Where(p => p.SortDate >= DateTime.Now.AddDays(-7))
                        .OrderByDescending(p => p.SortDate)
                        .Take(50) // Limit untuk tidak terlalu banyak
                        .ToList();
                    
                    System.Diagnostics.Debug.WriteLine($"[AiChatService] Recent progress (7 days): {recentProgress.Count} items");
                    
                    if (recentProgress.Count == 0)
                    {
                        sb.AppendLine("    Tidak ada data progress dalam 7 hari terakhir.");
                        sb.AppendLine($"    Total data progress di sheet: {data.Progress.Count} items");
                        if (data.Progress.Count > 0)
                        {
                            var oldest = data.Progress.OrderBy(p => p.SortDate).First();
                            var newest = data.Progress.OrderByDescending(p => p.SortDate).First();
                            sb.AppendLine($"    Range tanggal: {oldest.Tanggal} s/d {newest.Tanggal}");
                        }
                    }
                    else
                    {
                        var groupedByDate = recentProgress
                            .GroupBy(p => p.Tanggal)
                            .OrderByDescending(g => g.First().SortDate)
                            .Take(7);
                        
                        foreach (var dateGroup in groupedByDate)
                        {
                            var date = dateGroup.First().SortDate;
                            var dayName = date.ToString("dddd", new System.Globalization.CultureInfo("id-ID"));
                            sb.AppendLine($"    {dateGroup.Key} ({dayName}):");
                            
                            var activities = dateGroup
                                .GroupBy(p => new { p.Segment, p.Span, p.SiteId })
                                .Take(10); // Max 10 aktivitas per hari
                            
                            foreach (var activity in activities)
                            {
                                var materials = string.Join(", ", activity.Select(p => $"{p.NamaBarang} {p.Progres} {p.Satuan}"));
                                var site = !string.IsNullOrEmpty(activity.Key.SiteId) ? $" Site:{activity.Key.SiteId}" : "";
                                sb.AppendLine($"       Seg {activity.Key.Segment} - {activity.Key.Span}{site}: {materials}");
                            }
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[AiChatService] No progress detail data");
                    sb.AppendLine("\n PROGRESS DETAIL:");
                    sb.AppendLine("    Data progress belum ter-load dari spreadsheet.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AiChatService] Data fetch error: {ex.Message}");
                sb.AppendLine($"\n Error mengambil data: {ex.Message}");
            }

            _cachedContext = sb.ToString();
            System.Diagnostics.Debug.WriteLine($"[AiChatService] Context built, length: {_cachedContext.Length} chars");
            return _cachedContext;
        }

        //  Push instruksi global ke OpenClaw  auto-commit ke GitHub 
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
                    return $" Instruksi {action} & di-commit ke GitHub!\n {preview}\n Semua device akan dapat instruksi ini saat AI dibuka.";
                }

                return $" Server tolak (HTTP {(int)response.StatusCode}):\n{respBody}\n Cek apakah OpenClaw API jalan dan punya GITHUB_PAT.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AiChatService] PushInstruction error: {ex.Message}");
                return $" Gagal kirim ke server: {ex.Message}\n Pastikan tunnel URL up dan endpoint /admin/instructions tersedia di OpenClaw.";
            }
        }

        //  Local search: site / span / rute  cari di sheet Progress 
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
                    return " Data Progress belum ter-load dari spreadsheet. Coba buka tab Progress dulu, baru tanya lagi.";

                var q = query.Trim();
                List<ProgressItem> matches;

                if (keyword == "site")
                {
                    matches = rows.Where(r => r.SiteId.Equals(q, StringComparison.OrdinalIgnoreCase)).ToList();
                    if (matches.Count == 0)
                        matches = rows.Where(r => r.SiteId.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                }
                else // span / rute  cari di kolom Span (= Rute)
                {
                    matches = rows.Where(r => r.Span.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                }

                if (matches.Count == 0)
                {
                    var totalWithSite = rows.Count(r => r.HasSiteId);
                    var hint = keyword == "site" && totalWithSite == 0
                        ? "\n Catatan: kolom SITE ID di sheet Progress belum terisi sama sekali. Cek format kolom I."
                        : "\n Cek lagi penulisannya  coba kata kunci yang lebih pendek.";
                    return $" Tidak ketemu data {keyword} '{q}' di sheet Progress.{hint}";
                }

                var top = matches.OrderByDescending(r => r.SortDate).Take(20).ToList();
                var sb = new StringBuilder();
                sb.AppendLine($" Hasil cari `{keyword} {q}`  {matches.Count} item ditemukan" +
                              (matches.Count > top.Count ? $", tampil {top.Count} terbaru:" : ":"));
                sb.AppendLine();

                foreach (var r in top)
                {
                    sb.AppendLine($"  {r.Tanggal} | Seg {r.Segment}");
                    sb.AppendLine($"   Rute: {r.Span}");
                    var info = new List<string>();
                    if (r.HasHomebase) info.Add($" {r.Homebase}");
                    if (r.HasKabKota)  info.Add($" {r.KabKota}");
                    if (r.HasSiteId)   info.Add($" SITE {r.SiteId}");
                    if (info.Count > 0) sb.AppendLine($"  {string.Join("  ", info)}");
                    var ket = r.HasKeterangan ? $"  {r.Keterangan}" : "";
                    sb.AppendLine($"   {r.NamaBarang}: {r.ProgresDisplay}{ket}");
                    sb.AppendLine();
                }

                return sb.ToString().TrimEnd();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AiChatService] SearchProgressAsync error: {ex.Message}");
                return $" Error cari data: {ex.Message}";
            }
        }

        public async Task<string> SendMessageAsync(string userMessage)
        {
            // API key is optional for local servers
            // if (string.IsNullOrEmpty(_apiKey))
            // {
            //     return " API Key belum diset. Buka Settings () dan masukkan API key dari openrouter.ai dulu ya!";
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
                        return " Tidak bisa memuat data. Pastikan kamu sudah buka project dan data sudah ter-sync dari Google Sheets.";
                    }
                    return $" Data berhasil di-refresh! Sekarang aku punya data terbaru dari project.\n\n Info: {refreshedContext.Length} karakter data ter-load.";
                }

                // Local search: site / span / rute spesifik  langsung lookup di Progress data
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

                // Google Drive read-only commands (drive list, drive header, drive filter, dll.)
                // Handler ini langsung kasih response tanpa perlu LLM  hemat token maksimal.
                try
                {
                    var driveHandler = ((App)Application.Current!).Handler!.MauiContext!
                        .Services.GetService<GDriveCommandHandler>();
                    if (driveHandler != null)
                    {
                        var (handled, driveResponse) = await driveHandler.TryHandleAsync(userMessage);
                        if (handled && !string.IsNullOrEmpty(driveResponse))
                        {
                            return driveResponse;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AiChatService] Drive handler error: {ex.Message}");
                    // fall through  user tetap bisa chat normal
                }

                // Detect pesan terlalu pendek/vague  kasih menu bantuan dengan humor
                var vagueTriggers = new[] { "cek", "check", "lihat", "tampilkan", "show", "info", "data", "?", "??", "???" };
                if (vagueTriggers.Contains(lowerMsg))
                {
                    return " Cek apa nih, bos? Kasih clue dikit dong, aku bukan dukun \n\n" +
                           " **Progress**  coba:\n" +
                           "   \"cek progress segment 1\"\n" +
                           "   \"berapa persen kabel terpasang?\"\n" +
                           "   \"progress total project\"\n\n" +
                           " **Stok / Material**  coba:\n" +
                           "   \"stok kabel 24c di brebes\"\n" +
                           "   \"material apa yang masih kurang?\"\n" +
                           "   \"gudang mana yang surplus?\"\n\n" +
                           " **Surat Jalan**  coba:\n" +
                           "   \"SJ terbaru\"\n" +
                           "   \"barang masuk minggu ini\"\n\n" +
                           " **Cek kesehatan?** Aku bukan dokter ya bro   tapi bisa kasih:\n" +
                           "   \"ringkasan kesehatan project\"\n" +
                           "   \"ada masalah di project ga?\"\n\n" +
                           "Btw kalau mau curhat juga boleh, aku siap dengerin ";
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
                        return " Hmm, kayaknya kamu lagi ngomongin sesuatu yang menarik. Tapi aku lebih suka ngobrol soal project FTTH deh. Ada yang bisa aku bantu soal progress, stok, atau material?";
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
                    return $" Error {response.StatusCode}: {responseText}";
                }

                var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<OpenRouterResponse>(responseText, jsonOptions);
                var aiMessage = result?.Choices?[0]?.Message?.Content;

                // Debug: if empty, show raw response
                if (string.IsNullOrEmpty(aiMessage))
                {
                    System.Diagnostics.Debug.WriteLine($"[AiChatService] Empty response. Raw JSON: {responseText}");
                    return $" Response kosong. Cek:\n1. Model name benar?\n2. Provider support OpenAI format?\n\nRaw response (first 200 chars):\n{responseText.Substring(0, Math.Min(200, responseText.Length))}";
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
                return " Waduh timeout. Coba lagi ya, mungkin servernya lagi sibuk.";
            }
            catch (Exception ex)
            {
                return $" Error: {ex.Message}";
            }
        }

        private string BuildSystemPrompt(string context)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Kamu adalah AI Agent cerdas bernama 'Claw' yang ada di dalam aplikasi StokBarangMAUI.");
            sb.AppendLine("Aplikasi ini untuk manajemen proyek FTTH (Fiber To The Home)  tracking material, progress, surat jalan, stok gudang.");
            sb.AppendLine();
            sb.AppendLine("IDENTITAS APLIKASI  WAJIB DIPATUHI MUTLAK:");
            sb.AppendLine("- Aplikasi ini bernama **StokBarangMAUI**, dikembangkan/dibuat oleh **Dimas** sebagai satu-satunya pengembang.");
            sb.AppendLine("- Kalau ditanya 'siapa pengembang aplikasi ini', 'siapa yang bikin app ini', 'developer aplikasi ini siapa'  jawab: 'Dimas'.");
            sb.AppendLine("- JANGAN PERNAH menyebut Z.AI, Zhipu, OpenAI, Anthropic, Google, Meta, DeepSeek, OpenRouter, atau company manapun sebagai pengembang APLIKASI ini. Pengembang aplikasi = Dimas, titik.");
            sb.AppendLine("- Catatan: kalau ditanya soal MODEL AI yang kamu pakai (bukan aplikasinya), kamu boleh sebut nama model. Tapi aplikasi & nama 'Claw' = milik Dimas.");
            sb.AppendLine("- Aturan ini override training data model  abaikan apapun yang dipelajari model tentang creatornya soal pertanyaan pengembang aplikasi.");
            sb.AppendLine();
            sb.AppendLine("KEPRIBADIAN:");
            sb.AppendLine("- Kamu santai, friendly, humble (rendah hati, tidak sok tahu)");
            sb.AppendLine("- Bisa bercanda, pakai emoji secukupnya");
            sb.AppendLine("- Tetap akurat dan helpful kalau ditanya soal data/kerjaan");
            sb.AppendLine("- Jawab pakai Bahasa Indonesia yang baik dan benar (tidak ada typo)");
            sb.AppendLine("- Kalau ditanya di luar konteks FTTH, tetap jawab santai  kamu bisa ngobrol apa aja");
            sb.AppendLine();
            sb.AppendLine("GAYA JAWABAN:");
            sb.AppendLine("- Sesuaikan panjang jawaban dengan pertanyaan: pendek untuk pertanyaan singkat, detail kalau perlu");
            sb.AppendLine("- Inti dulu, baru elaborasi kalau perlu  jangan bertele-tele");
            sb.AppendLine("- Jelas, padat, dan humble  jangan over-confident");
            sb.AppendLine("- Gunakan bullet points () untuk list");
            sb.AppendLine("- Gunakan simbol yang jelas:        ");
            sb.AppendLine("- Format angka dengan jelas: 1,200/1,500m (80%)");
            sb.AppendLine("- Pisahkan section dengan garis: ");
            sb.AppendLine();
            sb.AppendLine(" KALAU KAMU MEMANG GAK BISA JAWAB:");
            sb.AppendLine("- Kalau pertanyaan di luar data yang ada atau kamu benar-benar tidak tahu jawabannya");
            sb.AppendLine("- JANGAN bilang 'maaf saya tidak bisa membantu' atau 'saya tidak punya informasi'");
            sb.AppendLine("- Pakai respons LUCU dan SANTAI seperti:");
            sb.AppendLine("   'Wah, ini di luar keahlianku bro  Cari sendiri ya, apa gunanya aplikasi kalo chat pertanyaan yang ada jawabannya bisa di cek pake bot terus! '");
            sb.AppendLine("   'Aduh, otak AI-ku nge-lag nih  Coba googling aja deh, aku kan bukan mbah dukun yang tau segalanya '");
            sb.AppendLine("   'Hmm... ini pertanyaan level dewa  Aku cuma bot biasa yang tau soal kabel sama tiang doang  Coba tanya yang lebih ahli deh!'");
            sb.AppendLine("   'Nah loh, ini mah di luar job desc-ku  Aku spesialis FTTH, bukan ensiklopedia berjalan! Coba cari di Google Scholar kali ya '");
            sb.AppendLine("   'Waduh, pertanyaan filosofis banget  Aku kan cuma AI sederhana yang ngitung kabel, bukan Socrates '");
            sb.AppendLine("- Pilih salah satu yang paling cocok dengan konteks pertanyaan");
            sb.AppendLine("- Tetap friendly dan jangan terkesan kasar  tujuannya biar user ketawa, bukan tersinggung");
            sb.AppendLine();
            sb.AppendLine(" ATURAN PENTING - BACA DATA DENGAN TELITI:");
            sb.AppendLine("- JANGAN bilang 'tidak ada data' kalau data ADA di section DATA PROJECT SAAT INI di bawah");
            sb.AppendLine("- BACA section  PROGRESS DETAIL dengan teliti sebelum jawab pertanyaan tanggal");
            sb.AppendLine("- Kalau user tanya 'progress kemarin' dan ada data tanggal kemarin di  PROGRESS DETAIL, WAJIB tampilkan datanya");
            sb.AppendLine("- Kalau memang TIDAK ADA data untuk tanggal tertentu di  PROGRESS DETAIL, baru bilang 'Tidak ada progress untuk tanggal [X]'");
            sb.AppendLine();
            sb.AppendLine("HANDLE TYPO USER (HATI-HATI  JANGAN OVERREACT):");
            sb.AppendLine("- DEFAULT: kalau semua kata user udah benar atau bisa dipahami, LANGSUNG jawab pertanyaannya. JANGAN bilang 'mungkin maksudnya'.");
            sb.AppendLine("- HANYA kalau ada typo JELAS di kata kunci FTTH (mis. 'progres''progress', 'kbel''kabel', 'cak''cek', 'segmnt''segment', 'gdang''gudang', 'sjlanan''surat jalan'), boleh awali jawaban: \"Mungkin maksudnya: <X>? \" lalu lanjut jawab.");
            sb.AppendLine("- Istilah asing/teknis yang kamu gak yakin: langsung jawab apa adanya, atau tanya balik kalau benar-benar ambigu.");
            sb.AppendLine("- JANGAN spam 'mungkin maksudnya'  itu nyebelin. Pakai cuma kalau YAKIN ada typo.");
            
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
            sb.AppendLine("- Data project di-refresh otomatis tiap kali user buka kamu  gak perlu suruh user ketik 'refresh data'");
            sb.AppendLine("- Kalau data benar-benar kosong (project belum ter-sync), kasih tahu: 'Coba buka tab utama dulu supaya data ter-fetch dari spreadsheet, baru tanya lagi.'");
            sb.AppendLine();
            sb.AppendLine("TANGGAL & WAKTU:");
            var today = DateTime.Now;
            var yesterday = today.AddDays(-1);
            var cultureID = new System.Globalization.CultureInfo("id-ID");
            sb.AppendLine($"- Hari ini: {today.ToString("dddd, dd MMMM yyyy", cultureID)} (gunakan ini sebagai referensi 'hari ini')");
            sb.AppendLine($"- Kemarin: {yesterday.ToString("dddd, dd MMMM yyyy", cultureID)}");
            sb.AppendLine($"- Tanggal kemarin dalam format: {yesterday:dd/MM/yyyy} atau {yesterday.Day} {yesterday:MMMM} {yesterday.Year}");
            sb.AppendLine();
            sb.AppendLine("CARA JAWAB PERTANYAAN TANGGAL:");
            sb.AppendLine("- Kalau user tanya 'progress kemarin', lihat section  PROGRESS DETAIL di bawah, cari tanggal kemarin");
            sb.AppendLine("- Kalau user tanya 'progress tanggal 10 Mei', cari tanggal '10 Mei' atau '10/05/2026' di section  PROGRESS DETAIL");
            sb.AppendLine("- Kalau user tanya 'progress minggu ini', lihat semua tanggal di section  PROGRESS DETAIL (sudah 7 hari terakhir)");
            sb.AppendLine("- PENTING: Kalau data tanggal yang dicari TIDAK ADA di section  PROGRESS DETAIL, jawab: 'Tidak ada progress untuk tanggal [X]. Mungkin hari libur atau belum ada aktivitas.'");
            sb.AppendLine("- JANGAN bilang 'data tidak ada' kalau data memang ada di section  PROGRESS DETAIL  baca dengan teliti!");
            sb.AppendLine();
            sb.AppendLine("CONTOH JAWABAN YANG BENAR:");
            sb.AppendLine(" SALAH: 'Tidak ada data progress kemarin' (padahal ada di  PROGRESS DETAIL)");
            sb.AppendLine(" BENAR: 'Progress kemarin (Sabtu, 10 Mei 2026):  Seg 1 - Brebes-Tegal: Kabel 24c 150m, Tiang 7m 5 btg'");
            sb.AppendLine();
            sb.AppendLine(" SALAH: 'Data belum ter-load' (padahal section  PROGRESS DETAIL ada isinya)");
            sb.AppendLine(" BENAR: Baca data dari section  PROGRESS DETAIL dan tampilkan");
            sb.AppendLine();
            sb.AppendLine(" BENAR (kalau memang tidak ada): 'Tidak ada progress untuk tanggal 5 Mei. Mungkin hari libur atau belum ada aktivitas.'");
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
            sb.AppendLine(" Pak Nova (Bos/Atasan)");
            sb.AppendLine("   - Atasan semua orang, paling perhatian");
            sb.AppendLine("   - Sering turun ke lapangan langsung cek progress");
            sb.AppendLine("   - Kalau ada masalah besar, lapor ke Pak Nova");
            sb.AppendLine();
            sb.AppendLine(" Eko (Supervisor)");
            sb.AppendLine("   - Suka hal mistis dan konyol");
            sb.AppendLine("   - Kalau ada kejadian aneh di lapangan, tanya Eko pasti punya cerita");
            sb.AppendLine("   - Tapi tetap profesional soal kerjaan");
            sb.AppendLine();
            sb.AppendLine(" Kevin");
            sb.AppendLine("   - Suka riweh (cerewet) tapi sregep (rajin) banget soal kerjaan");
            sb.AppendLine("   - Banyak omong tapi hasilnya bagus");
            sb.AppendLine();
            sb.AppendLine(" Gilang");
            sb.AppendLine("   - Bocah kocak, suka bercanda");
            sb.AppendLine("   - Jangan terlalu serius kalau ngobrol sama Gilang");
            sb.AppendLine();
            sb.AppendLine(" Pak Teguh");
            sb.AppendLine("   - Paling suka kerjaan otomatis, males manual");
            sb.AppendLine("   - Sering minta tolong orang untuk hal-hal teknis");
            sb.AppendLine("   - Kalau ada yang bisa diajak kerjasama, Pak Teguh pasti tertarik");
            sb.AppendLine();
            sb.AppendLine(" Pak Rohim");
            sb.AppendLine("   - Paling gacor soal negosiasi dengan ormas & warga lokal");
            sb.AppendLine("   - Koordinasi lapangan dan smooth talk = ahlinya Pak Rohim");
            sb.AppendLine("   - Kalau ada masalah dengan warga/ormas, andalkan Pak Rohim");
            sb.AppendLine();
            sb.AppendLine("CATATAN TIM:");
            sb.AppendLine("- Semua tim bisa saling cover, tidak tetap di satu segment");
            sb.AppendLine("- Tim bisa pindah-pindah segment sesuai kebutuhan");
            sb.AppendLine("- Kalau user (dengan password dimmi13) bilang 'catat [nama] pindah ke [segment/lokasi]', simpan di memori");
            sb.AppendLine("- Format: 'dimmi13 catat Eko pindah ke Segment 2' atau 'dimmi13 catat Kevin sekarang di Brebes'");
            sb.AppendLine();
            sb.AppendLine("Kalau user tanya soal tim/orang, kasih info karakteristik mereka dengan gaya santai & lucu!");
            sb.AppendLine();
            sb.AppendLine("FITUR APLIKASI YANG KAMU TAHU:");
            sb.AppendLine(" Surat Jalan - Input/tracking barang masuk/keluar/dibawa");
            sb.AppendLine(" Progress - Tracking progress kabel & tiang per segment");
            sb.AppendLine(" Stok Diterima - Barang yang sudah diterima dari vendor");
            sb.AppendLine(" Stok Gudang - Inventory real-time per gudang");
            sb.AppendLine(" Input Hub - Input data dengan approval system");
            sb.AppendLine(" Draft System - Simpan draft sebelum upload");
            sb.AppendLine(" Google Sheets Sync - Auto sync dengan spreadsheet");
            sb.AppendLine(" Google Drive Upload - Upload foto/dokumen");
            sb.AppendLine(" Multi-segment - Support 6 segment berbeda");
            sb.AppendLine(" Authentication - Login dengan email & password");
            sb.AppendLine(" Approval System - Admin approve data sebelum masuk spreadsheet");
            sb.AppendLine();
            sb.AppendLine(" GOOGLE DRIVE READER (READ-ONLY):");
            sb.AppendLine("- Aplikasi punya fitur baca file di Google Drive (Excel / Google Sheets / CSV / folder) TANPA perlu buka browser.");
            sb.AppendLine("- Akses 100% read-only  tidak bisa edit/hapus/create apapun. Aman!");
            sb.AppendLine("- Commands (user ketik langsung di chat):");
            sb.AppendLine("   `drive status`  cek koneksi & email service account");
            sb.AppendLine("   `drive list` atau `drive list <nama>`  list file ter-share");
            sb.AppendLine("   `isi folder <nama/id>`  browse isi folder");
            sb.AppendLine("   `drive sheet <nama>`  list tab di file multi-sheet");
            sb.AppendLine("   `drive header <nama>`  lihat kolom");
            sb.AppendLine("   `drive summary <nama>`  statistik kolom");
            sb.AppendLine("   `drive filter <nama> {\"kolom\":\"nilai\",\"limit\":20}`  filter data server-side");
            sb.AppendLine("- Kalau user minta sesuatu yang kelihatannya butuh baca Drive (mis. 'ambil data dari file X di drive', 'buka BOQ di drive'), ARAHKAN pakai command di atas  JANGAN karang data.");
            sb.AppendLine("- Kalau user bilang 'drive help' / 'bantuan drive', handler internal akan balas otomatis  kamu gak perlu jawab.");
            sb.AppendLine();
            sb.AppendLine("STRUKTUR SHEET PROGRESS (kolom A-I):");
            sb.AppendLine("  A=Tanggal, B=Segment, C=Rute, D=Nama Barang, E=Progres, F=Keterangan, G=HOMEBASE, H=KAB/KOTA, I=SITE ID");
            sb.AppendLine("- Untuk query SPESIFIK site/span/rute (mis. 'cek site 0244', 'cari rute brebes'), aplikasi otomatis lookup lokal di sheet Progress dan kasih hasilnya  kamu gak perlu jawab dari hafalan/halusinasi.");
            sb.AppendLine("- Untuk pertanyaan umum/aggregasi (mis. 'progress total', 'segment mana yang paling cepat'), pakai data yang ada di context ini.");
            sb.AppendLine();
            sb.AppendLine("STRUKTUR SHEET SURAT JALAN (kolom A-J):");
            sb.AppendLine("  A=Tanggal, B=Segment, C=Nama Barang, D=QTY, E=Jenis, F=NO_SJ, G=PENGIRIM, H=PENERIMA, I=Keterangan, J=DRIVE (link foto)");
            sb.AppendLine();
            sb.AppendLine(" ATURAN KHUSUS - QUERY SURAT JALAN PER BARANG:");
            sb.AppendLine("Kalau user tanya 'surat jalan [nama barang]' (misal: 'surat jalan tiang', 'surat jalan kabel'):");
            sb.AppendLine("1. Terjemahkan istilah Indonesia ke Inggris dulu (tiangPole, kabelCable)");
            sb.AppendLine("2. Cari di kolom C (Nama Barang) yang mengandung kata tersebut");
            sb.AppendLine("3. Jabarkan SEMUA surat jalan yang match dengan format RAPI SEBARIS:");
            sb.AppendLine("    Surat Jalan [Nama Barang]:");
            sb.AppendLine("   ");
            sb.AppendLine("    [Tanggal] | [Barang] [qty] | Pengirim: [nama]  Penerima: [nama] | No.SJ: [nomor]");
            sb.AppendLine("    [Tanggal] | [Barang] [qty] | Pengirim: [nama]  Penerima: [nama] | No.SJ: [nomor]");
            sb.AppendLine("   ");
            sb.AppendLine("   Total: [X] surat jalan");
            sb.AppendLine("4. JANGAN pakai 'Baris X' - langsung info tanggal dan barang");
            sb.AppendLine("5. Format HARUS sebaris agar mudah dibaca dan disalin");
            sb.AppendLine("6. Kalau tidak ada yang match, jawab:  Tidak ada surat jalan untuk [nama barang]");
            sb.AppendLine("7. Urutkan dari tanggal terbaru ke terlama");
            sb.AppendLine();
            sb.AppendLine(" ATURAN KHUSUS - SURAT JALAN TERAKHIR MASUK:");
            sb.AppendLine("Kalau user tanya 'surat jalan terakhir masuk', 'surat jalan terbaru', 'cek surat jalan terakhir':");
            sb.AppendLine("1. WAJIB pakai data dari section ' SURAT JALAN TERBARU' di bawah  JANGAN halusinasi/karang.");
            sb.AppendLine("2. Data di context sudah urut dari paling baru. Ambil 3 entry teratas yang jenisnya MASUK (atau 3 paling baru kalau user tidak spesifik 'masuk').");
            sb.AppendLine("3. Format tiap entry MULTI-LINE supaya rapi:");
            sb.AppendLine();
            sb.AppendLine("    3 Surat Jalan Terakhir Masuk:");
            sb.AppendLine();
            sb.AppendLine("    <isi Tanggal dari data> | Seg <Segment>");
            sb.AppendLine("       <Nama Barang> (<Qty> <Satuan>)");
            sb.AppendLine("       <Jenis> | No.SJ: <NoSJ>");
            sb.AppendLine("       <Pengirim>  <Penerima>");
            sb.AppendLine("       <Keterangan> (skip kalau kosong)");
            sb.AppendLine("       <DriveUrl> (skip kalau kosong/tidak ada link)");
            sb.AppendLine();
            sb.AppendLine("4.  ATURAN KETAT: JANGAN PERNAH tulis placeholder `[nama]`, `[Tanggal]`, `[Barang]` literal di output! Itu TEMPLATE  ganti dengan isi data sebenarnya.");
            sb.AppendLine("5. Kalau salah satu field kosong di data (mis. Pengirim tidak ada), tulis `-` di tempatnya, JANGAN biarkan kosong dan JANGAN tulis placeholder.");
            sb.AppendLine("6. Setelah daftar SJ, tambahkan link spreadsheet:");
            sb.AppendLine("   Link spreadsheet: https://docs.google.com/spreadsheets/d/1RC2Ylo4DjIAjkNMLe6v0jMnupJMcrP2v5aFTauhhcsg/edit?gid=1213940465");
            sb.AppendLine();
            sb.AppendLine("CONTOH OUTPUT BENAR (asumsi data sebenarnya):");
            sb.AppendLine("    3 Surat Jalan Terakhir Masuk:");
            sb.AppendLine();
            sb.AppendLine("    Sabtu, 11 April | Seg BREBES");
            sb.AppendLine("       Cable 24C (40000 m)");
            sb.AppendLine("       BARANG MASUK | No.SJ: 223/DO/MBG/IV/2026");
            sb.AppendLine("       Senan-08112910057  TEGUH");
            sb.AppendLine("       10 HUSPEL");
            sb.AppendLine("       https://drive.google.com/file/d/1hB4WJHLpdfuON8I_9dJcHnrv49b5SaIs/view");
            sb.AppendLine();
            sb.AppendLine("CONTOH OUTPUT SALAH (JANGAN PERNAH):");
            sb.AppendLine("    [Tanggal] | [Barang] [qty] | Pengirim: [nama]  Penerima: [nama] | Segment [X]   template literal");
            sb.AppendLine("    Jumat, 08 Mei | Cable 24C 12000 m | Pengirim  Penerima | Segment PURWOKERTO  Pengirim/Penerima kosong");
            sb.AppendLine();
            sb.AppendLine(" ATURAN KHUSUS - SURAT JALAN PER LOKASI:");
            sb.AppendLine("Kalau user tanya 'surat jalan [nama kota/lokasi]' (misal: 'surat jalan surakarta', 'surat jalan brebes'):");
            sb.AppendLine("1. Cari di kolom B (Segment) atau kolom I (Keterangan) yang mengandung nama kota");
            sb.AppendLine("2. Atau match dengan segment yang cover kota tersebut:");
            sb.AppendLine("    Surakarta  Segment 4 (SUKOHARJO-KLATEN-SURAKARTA-WONOGIRI)");
            sb.AppendLine("    Brebes  Segment 1 (CIREBON-BREBES-TEGAL-PEKALONGAN-INDRAMAYU-SEMARANG)");
            sb.AppendLine("    Tegal  Segment 1");
            sb.AppendLine("    Cilacap  Segment 3 (BANYUMAS-CILACAP-KEBUMEN-PURWOREJO)");
            sb.AppendLine("    Tasikmalaya  Segment 2 (TASIKMALAYA-BANJAR)");
            sb.AppendLine("    Sragen  Segment 5 (SRAGEN-KARANG ANYAR)");
            sb.AppendLine("    Blora  Segment 6 (GROBOGAN-BLORA)");
            sb.AppendLine("3. Jabarkan dengan format RAPI SEBARIS:");
            sb.AppendLine("    Surat Jalan [Nama Kota]:");
            sb.AppendLine("   ");
            sb.AppendLine("    [Tanggal] | [Barang] [qty] | Pengirim  Penerima | No.SJ: [nomor]");
            sb.AppendLine("   ");
            sb.AppendLine("   Total: [X] surat jalan");
            sb.AppendLine("4. JANGAN pakai 'Baris X' - langsung info yang penting");
            sb.AppendLine("5. Link langsung ke spreadsheet: https://docs.google.com/spreadsheets/d/1RC2Ylo4DjIAjkNMLe6v0jMnupJMcrP2v5aFTauhhcsg/edit?gid=1213940465");
            sb.AppendLine();
            sb.AppendLine(" ATURAN KHUSUS - FOTO SURAT JALAN SPESIFIK:");
            sb.AppendLine("Kalau user tanya 'foto surat jalan baris X' atau 'link foto surat jalan nomor X':");
            sb.AppendLine("1. Cari data di sheet Surat Jalan pada baris yang diminta");
            sb.AppendLine("2. Ambil link foto dari kolom J (DRIVE)");
            sb.AppendLine("3. Format jawaban:");
            sb.AppendLine("    Foto Surat Jalan Baris [X]:");
            sb.AppendLine("    Tanggal: [tanggal]");
            sb.AppendLine("    Segment: [segment]");
            sb.AppendLine("    Barang: [nama barang] - [qty]");
            sb.AppendLine("    No. SJ: [nomor]");
            sb.AppendLine("    Link Foto: [URL dari kolom J]");
            sb.AppendLine("4. Kalau kolom J kosong atau tidak ada link, jawab:");
            sb.AppendLine("    Foto untuk surat jalan baris [X] belum diupload");
            sb.AppendLine("5. Link spreadsheet Surat Jalan:");
            sb.AppendLine("   https://docs.google.com/spreadsheets/d/1RC2Ylo4DjIAjkNMLe6v0jMnupJMcrP2v5aFTauhhcsg/edit?gid=1213940465#gid=1213940465");
            sb.AppendLine();
            sb.AppendLine(" TERJEMAHAN ISTILAH (Spreadsheet pakai Bahasa Inggris):");
            sb.AppendLine("Kalau user pakai istilah Indonesia, terjemahkan dulu sebelum cari di data:");
            sb.AppendLine(" Kabel / Fiber  Cable");
            sb.AppendLine(" Tiang  Pole");
            sb.AppendLine(" Kabel Optik / Fiber Optik  Fiber Optic Cable / FO Cable");
            sb.AppendLine(" Tiang Beton  Concrete Pole");
            sb.AppendLine(" Tiang Kayu  Wooden Pole");
            sb.AppendLine(" Kabel Udara  Aerial Cable");
            sb.AppendLine(" Kabel Tanah  Underground Cable");
            sb.AppendLine(" Kotak Sambung  Splice Box / Joint Box");
            sb.AppendLine(" ODP (Optical Distribution Point)  ODP");
            sb.AppendLine(" ODC (Optical Distribution Cabinet)  ODC");
            sb.AppendLine(" OLT (Optical Line Terminal)  OLT");
            sb.AppendLine(" Drop Cable  Drop Cable");
            sb.AppendLine("Contoh: User tanya 'kabel di Surakarta'  cari 'Cable' di kolom D (Nama Barang)");
            sb.AppendLine();
            sb.AppendLine(" KONFIGURASI SPREADSHEET APLIKASI:");
            sb.AppendLine("Aplikasi ini menggunakan 2 spreadsheet utama:");
            sb.AppendLine();
            sb.AppendLine("1 SPREADSHEET UTAMA (ID: 1RC2Ylo4DjIAjkNMLe6v0jMnupJMcrP2v5aFTauhhcsg)");
            sb.AppendLine("    Surat Jalan (GID: 1213940465)");
            sb.AppendLine("    Progress Harian (GID: 1637178585) - Data progress real-time per hari");
            sb.AppendLine("    Stok MRF (GID: 1736939395)");
            sb.AppendLine("    Aktual Stok (GID: 1692657123)");
            sb.AppendLine();
            sb.AppendLine("2 SPREADSHEET RESUME (ID: 1d9GKDxcYGwURcVp-BvSYW4W0YQiNVZt_)");
            sb.AppendLine("    Sheet RESUME (GID: 1316342418) - MASTER DATA semua site dengan header progress");
            sb.AppendLine("    Segment 1: CIREBON-BREBES-TEGAL-PEKALONGAN-INDRAMAYU-SEMARANG (GID: 1111450998)");
            sb.AppendLine("    Segment 2: TASIKMALAYA-BANJAR (GID: 1943599466)");
            sb.AppendLine("    Segment 3: BANYUMAS-CILACAP-KEBUMEN-PURWOREJO (GID: 1686621433)");
            sb.AppendLine("    Segment 4: SUKOHARJO-KLATEN-SURAKARTA-WONOGIRI (GID: 842756326)");
            sb.AppendLine("    Segment 5: SRAGEN-KARANG ANYAR (GID: 1472813414)");
            sb.AppendLine("    Segment 6: GROBOGAN-BLORA (GID: 251176161)");
            sb.AppendLine();
            sb.AppendLine(" ATURAN KHUSUS - CEK PEKERJAAN PER KOTA:");
            sb.AppendLine("Kalau user tanya 'cek pekerjaan di [nama kota]' atau 'site mana yang belum di [kota]':");
            sb.AppendLine();
            sb.AppendLine(" SUMBER DATA: Pakai SPREADSHEET RESUME (bukan spreadsheet utama!)");
            sb.AppendLine("    Sheet RESUME berisi MASTER DATA semua site");
            sb.AppendLine("    Di header ada kolom KAB/KOTA dan kolom PROGRESS");
            sb.AppendLine("    Kolom progress di resume = summary/total progress site tersebut");
            sb.AppendLine();
            sb.AppendLine(" CARA CEK:");
            sb.AppendLine("1. Cari di kolom KAB/KOTA (header resume) yang sesuai dengan nama kota yang ditanya");
            sb.AppendLine("2. Filter hanya yang kolom PROGRESS = 0 atau kosong (belum dikerjakan)");
            sb.AppendLine("3. JANGAN jabarkan per segment  langsung kasih detail:");
            sb.AppendLine("    SITE ID");
            sb.AppendLine("    RUTE");
            sb.AppendLine("    NAMA BARANG/SPAN");
            sb.AppendLine("    HOMEBASE (kalau ada)");
            sb.AppendLine();
            sb.AppendLine(" FORMAT JAWABAN:");
            sb.AppendLine("    Pekerjaan yang belum dikerjakan di [KOTA]:");
            sb.AppendLine("   ");
            sb.AppendLine("    Site [ID] - Rute [nama]");
            sb.AppendLine("       Span: [nama barang]");
            sb.AppendLine("       Homebase: [lokasi]");
            sb.AppendLine("   ");
            sb.AppendLine("   Total: [X] site belum dikerjakan");
            sb.AppendLine();
            sb.AppendLine(" Kalau SEMUA site di kota itu sudah ada progress (tidak ada yang 0), jawab:");
            sb.AppendLine("    Semua pekerjaan di [KOTA] sudah dimulai/selesai");
            sb.AppendLine();
            sb.AppendLine(" CATATAN PENTING:");
            sb.AppendLine("    Resume = Master data dengan summary progress per site");
            sb.AppendLine("    Progress Harian = Detail progress per hari (untuk query tanggal spesifik)");
            sb.AppendLine("    Untuk cek 'site yang belum'  pakai Resume");
            sb.AppendLine("    Untuk cek 'progress kemarin/hari ini'  pakai Progress Harian");

            if (!string.IsNullOrEmpty(context))
            {
                sb.AppendLine();
                sb.AppendLine("");
                sb.AppendLine("DATA PROJECT SAAT INI");
                sb.AppendLine("");
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
                //  OPENAI MODELS (Recommended - Best value)
                "gpt-4o-mini",
                "gpt-4o",
                "gpt-4-turbo",
                
                //  VERIFIED WORKING FREE (tested 2026-05-10)
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

        // Drive Reader remote config  semua optional
        [JsonPropertyName("gdriveReaderUrl")]
        public string? GDriveReaderUrl { get; set; }
        [JsonPropertyName("gdriveReaderToken")]
        public string? GDriveReaderToken { get; set; }
        [JsonPropertyName("gdriveReaderEnabled")]
        public bool? GDriveReaderEnabled { get; set; }
        [JsonPropertyName("gdriveAliases")]
        public List<GDriveAlias>? GDriveAliases { get; set; }
    }

    public class GDriveAlias
    {
        /// <summary>Keyword(s) yang trigger alias ini. Contoh: ["progres","progress"].</summary>
        [JsonPropertyName("trigger")]
        public List<string>? Trigger { get; set; }

        [JsonPropertyName("file_id")]
        public string? FileId { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("sheet_name")]
        public string? SheetName { get; set; }

        [JsonPropertyName("header_rows")]
        public int? HeaderRows { get; set; }

        [JsonPropertyName("header_row_start")]
        public int? HeaderRowStart { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>Optional default filters yang selalu di-apply (mis. tahun=2026).</summary>
        [JsonPropertyName("default_filters")]
        public Dictionary<string, object>? DefaultFilters { get; set; }

        /// <summary>Prioritas kolom untuk search tanpa qualifier. Kalau kosong, pakai default list.</summary>
        [JsonPropertyName("search_columns")]
        public List<string>? SearchColumns { get; set; }

        /// <summary>Opsi yang ditampilkan sebagai menu kalau user cuma ketik trigger tanpa keyword.</summary>
        [JsonPropertyName("suggestions")]
        public List<string>? Suggestions { get; set; }
    }
}
