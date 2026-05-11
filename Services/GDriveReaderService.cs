using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StokBarangMAUI.Services
{
    /// <summary>
    /// Read-only client untuk GDrive Reader HTTP API (Python FastAPI di mcp_gdrive_filter/http_server.py).
    ///
    /// JAMINAN READ-ONLY:
    ///   - Service ini TIDAK punya method Create/Update/Delete/Write apapun
    ///   - Server Python scope-nya .readonly (Google server tolak write)
    ///   - Runtime guard Python block method selain GET
    ///
    /// Server config disimpan di Preferences:
    ///   - gdrive_api_url   (default: http://localhost:20129)
    ///   - gdrive_api_token (optional bearer token)
    ///   - gdrive_enabled   (toggle on/off)
    ///
    /// Remote config (optional): kalau env GITHUB_CONFIG_URL di-set di AiChatService,
    /// field gdriveReaderUrl + gdriveReaderToken dari JSON itu akan di-apply
    /// via ApplyRemoteConfig() â€” HP gak perlu manual setup.
    /// </summary>
    public class GDriveReaderService
    {
        private readonly HttpClient _http;

        private const string DEFAULT_URL = "http://localhost:20129";
        public const string PREF_URL = "gdrive_api_url";
        public const string PREF_TOKEN = "gdrive_api_token";
        public const string PREF_ENABLED = "gdrive_enabled";

        public GDriveReaderService()
        {
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        }

        // â”€â”€ Configuration â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        public string BaseUrl => Preferences.Get(PREF_URL, DEFAULT_URL).TrimEnd('/');
        public string Token => Preferences.Get(PREF_TOKEN, "");
        public bool IsEnabled => Preferences.Get(PREF_ENABLED, false);

        public void SetBaseUrl(string url) => Preferences.Set(PREF_URL, (url ?? "").TrimEnd('/'));
        public void SetToken(string token) => Preferences.Set(PREF_TOKEN, token ?? "");
        public void SetEnabled(bool enabled) => Preferences.Set(PREF_ENABLED, enabled);

        /// <summary>
        /// Apply remote config (dari GitHub raw JSON via AiChatService).
        /// JSON fields yang dikenali:
        ///   gdriveReaderUrl     â€” URL server (tunnel). Kosong = skip.
        ///   gdriveReaderToken   â€” bearer token. Kosong = skip.
        ///   gdriveReaderEnabled â€” bool auto-enable.
        ///   gdriveAliases       â€” list alias untuk natural-language command (cek progres, stok, dll).
        ///
        /// Logic:
        ///   - Kalau user TIDAK set "lock" manual (gdrive_manual_override=false/missing):
        ///     remote URL/token SELALU override local. Ini supaya HP auto-update saat
        ///     tunnel Cloudflare ganti URL.
        ///   - Kalau user set "lock" manual (toggle di AI Settings): remote cuma di-cache
        ///     sebagai fallback, tidak nimpa setting manual.
        /// </summary>
        public void ApplyRemoteConfig(string? url, string? token, bool? autoEnable, List<GDriveAlias>? aliases = null)
        {
            var manualOverride = Preferences.Get("gdrive_manual_override", false);

            // URL: selalu override dari remote kecuali user pakai manual lock
            if (!string.IsNullOrWhiteSpace(url))
            {
                if (!manualOverride)
                {
                    SetBaseUrl(url);
                }
                Preferences.Set(PREF_URL + "_remote", url.TrimEnd('/'));
            }
            if (!string.IsNullOrWhiteSpace(token))
            {
                if (!manualOverride)
                {
                    SetToken(token);
                }
                Preferences.Set(PREF_TOKEN + "_remote", token);
            }
            if (autoEnable == true && !Preferences.ContainsKey(PREF_ENABLED))
            {
                SetEnabled(true);
            }

            // Aliases â€” always overwrite with remote (single source of truth)
            _aliases = aliases ?? new List<GDriveAlias>();
        }

        /// <summary>Get current aliases list (from remote config).</summary>
        public IReadOnlyList<GDriveAlias> Aliases => _aliases;
        private List<GDriveAlias> _aliases = new();

        /// <summary>Reset ke config dari remote (abaikan manual override).</summary>
        public void ResetToRemoteConfig()
        {
            var remoteUrl = Preferences.Get(PREF_URL + "_remote", "");
            var remoteToken = Preferences.Get(PREF_TOKEN + "_remote", "");
            if (!string.IsNullOrEmpty(remoteUrl)) SetBaseUrl(remoteUrl);
            if (!string.IsNullOrEmpty(remoteToken)) SetToken(remoteToken);
        }

        // â”€â”€ Health / service account â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        public async Task<(bool ok, string message, string? serviceAccountEmail)> CheckHealthAsync()
        {
            try
            {
                var resp = await SendAsync(HttpMethod.Get, "/health", null);
                if (!resp.IsSuccessStatusCode)
                    return (false, $"HTTP {(int)resp.StatusCode}", null);

                var json = await resp.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);
                var status = doc.RootElement.TryGetProperty("status", out var s) ? s.GetString() : null;
                var email = doc.RootElement.TryGetProperty("service_account", out var e) ? e.GetString() : null;
                return (status == "ok", status ?? "unknown", email);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, null);
            }
        }

        public async Task<string?> GetServiceAccountEmailAsync()
        {
            try
            {
                var resp = await SendAsync(HttpMethod.Get, "/service-account", null);
                if (!resp.IsSuccessStatusCode) return null;
                var json = await resp.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);
                return doc.RootElement.TryGetProperty("email", out var e) ? e.GetString() : null;
            }
            catch { return null; }
        }

        // â”€â”€ Read operations (all GET-equivalent) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        /// <summary>List files at Drive root / search. Read-only.</summary>
        public Task<DriveListResult?> ListFilesAsync(
            string? query = null,
            string? folderId = null,
            string fileType = "all",
            int pageSize = 30)
            => PostAsync<DriveListResult>("/files/list", new
            {
                query,
                folder_id = folderId,
                file_type = fileType,
                page_size = pageSize
            });

        /// <summary>List contents of a specific folder. Read-only.</summary>
        public Task<DriveFolderContents?> ListFolderContentsAsync(
            string? folderId = null,
            bool recursive = false)
            => PostAsync<DriveFolderContents>("/folder/contents", new
            {
                folder_id = folderId,
                recursive
            });

        /// <summary>List sheet/tab names inside a spreadsheet. Read-only.</summary>
        public Task<SheetTabsResult?> ListSheetTabsAsync(string fileId)
            => PostAsync<SheetTabsResult>("/sheet/tabs", new { file_id = fileId });

        /// <summary>Get headers only (super hemat token). Read-only.</summary>
        public Task<SheetHeadersResult?> GetSheetHeadersAsync(string fileId, string? sheetName = null, int headerRows = 1, int headerRowStart = 1)
            => PostAsync<SheetHeadersResult>("/sheet/headers", new
            {
                file_id = fileId,
                sheet_name = sheetName,
                header_rows = headerRows,
                header_row_start = headerRowStart
            });

        /// <summary>Get column statistics summary. Read-only.</summary>
        public async Task<JsonElement?> GetSheetSummaryAsync(string fileId, string? sheetName = null, IEnumerable<string>? columns = null, int headerRows = 1, int headerRowStart = 1)
        {
            var el = await PostAsync<JsonElement>("/sheet/summary", new
            {
                file_id = fileId,
                sheet_name = sheetName,
                columns,
                header_rows = headerRows,
                header_row_start = headerRowStart
            });
            return el.ValueKind == JsonValueKind.Undefined ? (JsonElement?)null : el;
        }

        /// <summary>Filter sheet data server-side. Hemat token. Read-only.</summary>
        public Task<SheetFilterResult?> FilterSheetAsync(
            string fileId,
            IDictionary<string, object>? filters = null,
            IEnumerable<string>? columns = null,
            string? sheetName = null,
            int limit = 100,
            int headerRows = 1,
            int headerRowStart = 1)
            => PostAsync<SheetFilterResult>("/sheet/filter", new
            {
                file_id = fileId,
                sheet_name = sheetName,
                filters,
                columns,
                limit,
                header_rows = headerRows,
                header_row_start = headerRowStart
            });

        // â”€â”€ Internal HTTP helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private async Task<T?> PostAsync<T>(string path, object body)
        {
            try
            {
                var resp = await SendAsync(HttpMethod.Post, path, body);
                if (!resp.IsSuccessStatusCode)
                {
                    var err = await resp.Content.ReadAsStringAsync();
                    Console.WriteLine($"[GDriveReader] {path} failed: {(int)resp.StatusCode} {err}");
                    throw new HttpRequestException($"{(int)resp.StatusCode}: {err}");
                }

                if (typeof(T) == typeof(JsonElement))
                {
                    var raw = await resp.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(raw);
                    return (T)(object)doc.RootElement.Clone();
                }

                return await resp.Content.ReadFromJsonAsync<T>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GDriveReader] {path} error: {ex.Message}");
                throw;
            }
        }

        private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object? body)
        {
            // SAFETY: only allow GET and POST (matching read-only backend)
            if (method != HttpMethod.Get && method != HttpMethod.Post)
                throw new InvalidOperationException($"Method {method} not allowed. This service is read-only.");

            var url = BaseUrl + path;
            var req = new HttpRequestMessage(method, url);

            if (!string.IsNullOrWhiteSpace(Token))
                req.Headers.Add("Authorization", $"Bearer {Token}");

            if (body != null)
            {
                var json = JsonSerializer.Serialize(body, new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });
                req.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            }

            return await _http.SendAsync(req);
        }
    }

    // â”€â”€ DTOs matching HTTP server JSON responses â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public class DriveFileDto
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("mimeType")] public string? MimeType { get; set; }
        [JsonPropertyName("modifiedTime")] public string? ModifiedTime { get; set; }
        [JsonPropertyName("size")] public string? Size { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("parents")] public List<string>? Parents { get; set; }
    }

    public class DriveListResult
    {
        [JsonPropertyName("total")] public int Total { get; set; }
        [JsonPropertyName("files")] public List<DriveFileDto>? Files { get; set; }
    }

    public class DriveFolderContents
    {
        [JsonPropertyName("folder_id")] public string? FolderId { get; set; }
        [JsonPropertyName("summary")] public FolderSummary? Summary { get; set; }
        [JsonPropertyName("folders")] public List<DriveFileDto>? Folders { get; set; }
        [JsonPropertyName("spreadsheets")] public List<DriveFileDto>? Spreadsheets { get; set; }
        [JsonPropertyName("other_files")] public List<DriveFileDto>? OtherFiles { get; set; }
    }

    public class FolderSummary
    {
        [JsonPropertyName("folders")] public int Folders { get; set; }
        [JsonPropertyName("spreadsheets")] public int Spreadsheets { get; set; }
        [JsonPropertyName("other_files")] public int OtherFiles { get; set; }
    }

    public class SheetTabsResult
    {
        [JsonPropertyName("file_name")] public string? FileName { get; set; }
        [JsonPropertyName("file_type")] public string? FileType { get; set; }
        [JsonPropertyName("sheets")] public List<string>? Sheets { get; set; }
        [JsonPropertyName("total_sheets")] public int TotalSheets { get; set; }
    }

    public class SheetHeadersResult
    {
        [JsonPropertyName("file_name")] public string? FileName { get; set; }
        [JsonPropertyName("sheet_name")] public string? SheetName { get; set; }
        [JsonPropertyName("columns")] public List<string>? Columns { get; set; }
        [JsonPropertyName("total_rows")] public int TotalRows { get; set; }
        [JsonPropertyName("total_columns")] public int TotalColumns { get; set; }
        [JsonPropertyName("dtypes")] public Dictionary<string, string>? Dtypes { get; set; }
    }

    public class SheetFilterResult
    {
        [JsonPropertyName("original_rows")] public int OriginalRows { get; set; }
        [JsonPropertyName("rows_after_filter")] public int RowsAfterFilter { get; set; }
        [JsonPropertyName("rows_returned")] public int RowsReturned { get; set; }
        [JsonPropertyName("columns")] public List<string>? Columns { get; set; }
        [JsonPropertyName("data")] public List<Dictionary<string, object>>? Data { get; set; }
    }
}

