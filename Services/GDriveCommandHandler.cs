using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace StokBarangMAUI.Services
{
    /// <summary>
    /// Natural-language command handler untuk akses Google Drive read-only.
    ///
    /// Pola: mirip SearchProgressAsync di AiChatService â€” detect keyword di message,
    /// call GDriveReaderService, format hasilnya jadi teks rapi buat ditampilin langsung
    /// (skip LLM, hemat token maksimal).
    ///
    /// Command yang di-support (semua bahasa Indonesia natural):
    ///   â€¢ "drive list" / "list drive" / "file drive"
    ///   â€¢ "drive folder [nama/id]"
    ///   â€¢ "drive sheet [nama/id]" â€” list tab
    ///   â€¢ "drive header [nama/id]" / "kolom drive [nama/id]"
    ///   â€¢ "drive summary [nama/id]"
    ///   â€¢ "drive filter [nama/id] [json atau kriteria]"
    ///   â€¢ "drive status" / "cek drive"
    ///   â€¢ "drive help" / "bantuan drive"
    /// </summary>
    public class GDriveCommandHandler
    {
        private readonly GDriveReaderService _drive;

        // Cache pemetaan "nama file" â†’ file_id biar user bisa refer by name
        private readonly Dictionary<string, string> _nameToIdCache = new(StringComparer.OrdinalIgnoreCase);
        private DateTime _cacheExpiry = DateTime.MinValue;

        public GDriveCommandHandler(GDriveReaderService drive)
        {
            _drive = drive;
        }

        /// <summary>
        /// Cek apakah message user adalah command drive. Return true + isi response kalau ya.
        /// Return false kalau bukan command drive (lanjut ke LLM flow biasa).
        /// </summary>
        public async Task<(bool handled, string? response)> TryHandleAsync(string userMessage)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
                return (false, null);

            var msg = userMessage.Trim();
            var lower = msg.ToLowerInvariant();

            Console.WriteLine($"[GDriveCmd] TryHandle: '{msg}' (enabled={_drive.IsEnabled}, aliases={_drive.Aliases.Count})");

            // === Alias detection (dari remote config) â€” PRIORITAS TINGGI ===
            if (_drive.IsEnabled && _drive.Aliases.Count > 0)
            {
                var aliasResult = await TryHandleAliasAsync(msg, lower);
                if (aliasResult.handled) return aliasResult;
            }

            // Quick rejection â€” harus ada kata "drive" atau prefix khusus
            if (!ContainsDriveKeyword(lower))
                return (false, null);

            // Enabled check
            if (!_drive.IsEnabled)
            {
                return (true, BuildDisabledHint());
            }

            try
            {
                // Status check
                if (Regex.IsMatch(lower, @"\bdrive\s+(status|cek|check|test|health|kesehatan)\b") ||
                    Regex.IsMatch(lower, @"\b(cek|check|test)\s+drive\b"))
                {
                    return (true, await BuildStatusAsync());
                }

                // Help
                if (Regex.IsMatch(lower, @"\bdrive\s+(help|bantuan|cara)\b") ||
                    Regex.IsMatch(lower, @"\b(help|bantuan|cara)\s+drive\b"))
                {
                    return (true, BuildHelp());
                }

                // Folder contents
                var folderMatch = Regex.Match(lower, @"\b(isi\s+folder|folder\s+drive|drive\s+folder|list\s+folder)\b\s*(.*)");
                if (folderMatch.Success)
                {
                    var rest = folderMatch.Groups[2].Value.Trim();
                    return (true, await BuildFolderContentsAsync(rest));
                }

                // List files (generic)
                if (Regex.IsMatch(lower, @"\b(drive\s+list|list\s+drive|file\s+drive|drive\s+file|drive\s+files)\b"))
                {
                    var queryMatch = Regex.Match(msg, @"(?:drive\s+list|list\s+drive|file\s+drive|drive\s+file|drive\s+files)\s*(.*)",
                        RegexOptions.IgnoreCase);
                    var nameQuery = queryMatch.Groups[1].Value.Trim();
                    return (true, await BuildListFilesAsync(nameQuery));
                }

                // Sheet tabs
                var tabsMatch = Regex.Match(msg, @"\b(drive\s+sheet|drive\s+tab|tab\s+drive|sheet\s+drive)\b\s*(.*)",
                    RegexOptions.IgnoreCase);
                if (tabsMatch.Success)
                {
                    var rest = tabsMatch.Groups[2].Value.Trim();
                    return (true, await BuildSheetTabsAsync(rest));
                }

                // Headers
                var headerMatch = Regex.Match(msg, @"\b(drive\s+header|header\s+drive|kolom\s+drive|drive\s+kolom)\b\s*(.*)",
                    RegexOptions.IgnoreCase);
                if (headerMatch.Success)
                {
                    var rest = headerMatch.Groups[2].Value.Trim();
                    return (true, await BuildHeadersAsync(rest));
                }

                // Summary
                var summaryMatch = Regex.Match(msg, @"\b(drive\s+summary|summary\s+drive|ringkasan\s+drive|drive\s+ringkas)\b\s*(.*)",
                    RegexOptions.IgnoreCase);
                if (summaryMatch.Success)
                {
                    var rest = summaryMatch.Groups[2].Value.Trim();
                    return (true, await BuildSummaryAsync(rest));
                }

                // Filter
                var filterMatch = Regex.Match(msg, @"\b(drive\s+filter|filter\s+drive|drive\s+cari|cari\s+drive)\b\s*(.*)",
                    RegexOptions.IgnoreCase);
                if (filterMatch.Success)
                {
                    var rest = filterMatch.Groups[2].Value.Trim();
                    return (true, await BuildFilterAsync(rest));
                }

                // Keyword "drive" tapi tidak ada pattern yang match â†’ kasih help
                if (lower.Contains("drive"))
                {
                    return (true, BuildHelp());
                }
            }
            catch (Exception ex)
            {
                return (true, $"âŒ Error akses Drive: {ex.Message}\n\nðŸ’¡ Cek apakah server Python (http_server.py) sedang jalan di laptop, dan URL di Settings sudah benar.");
            }

            return (false, null);
        }

        // â”€â”€ Command builders â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private async Task<string> BuildStatusAsync()
        {
            var (ok, message, email) = await _drive.CheckHealthAsync();
            var sb = new StringBuilder();
            sb.AppendLine("ðŸ”Œ **Status Google Drive Reader**");
            sb.AppendLine();
            sb.AppendLine($"Server: {_drive.BaseUrl}");
            sb.AppendLine($"Koneksi: {(ok ? "âœ… OK" : "âŒ " + message)}");
            if (!string.IsNullOrEmpty(email))
            {
                sb.AppendLine($"Service account: `{email}`");
                sb.AppendLine();
                sb.AppendLine("ðŸ’¡ Share folder/file Google Drive-mu ke email di atas (permission: Viewer) supaya bisa dibaca.");
            }
            if (!ok)
            {
                sb.AppendLine();
                sb.AppendLine("ðŸ› ï¸ **Troubleshooting:**");
                sb.AppendLine("1. Pastikan `start_server.bat` di laptop sedang jalan");
                sb.AppendLine("2. Cek URL di AI Settings â†’ Drive Reader URL");
                sb.AppendLine("3. Kalau HP â†’ laptop, pakai IP laptop atau Cloudflare tunnel (bukan localhost)");
            }
            return sb.ToString().TrimEnd();
        }

        private async Task<string> BuildListFilesAsync(string nameQuery)
        {
            var query = string.IsNullOrWhiteSpace(nameQuery)
                ? null
                : $"name contains \"{EscapeDriveQuery(nameQuery)}\"";

            var result = await _drive.ListFilesAsync(query: query, fileType: "all", pageSize: 30);
            if (result == null || result.Files == null || result.Files.Count == 0)
            {
                return BuildNoFilesMessage(nameQuery);
            }

            CacheNameToId(result.Files);

            var sb = new StringBuilder();
            sb.AppendLine($"ðŸ“‚ **File di Google Drive** ({result.Total} file)");
            if (!string.IsNullOrWhiteSpace(nameQuery))
                sb.AppendLine($"ðŸ”Ž Filter nama: *{nameQuery}*");
            sb.AppendLine();
            sb.AppendLine("â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€");

            int i = 1;
            foreach (var f in result.Files)
            {
                var icon = TypeIcon(f.Type ?? f.MimeType ?? "");
                sb.AppendLine($"{i}. {icon} **{f.Name}**");
                sb.AppendLine($"   ðŸ“‹ `{f.Id}` ({f.Type ?? "file"})");
                i++;
            }
            sb.AppendLine();
            sb.AppendLine("ðŸ’¡ Tip: `drive header <nama file>` untuk lihat kolom.");
            return sb.ToString().TrimEnd();
        }

        private async Task<string> BuildFolderContentsAsync(string folderRef)
        {
            var folderId = await ResolveFileIdAsync(folderRef);
            // folderId boleh null/empty â†’ list root (semua yg ter-share)

            var result = await _drive.ListFolderContentsAsync(folderId, recursive: false);
            if (result == null) return "âŒ Gagal ambil isi folder.";

            var sb = new StringBuilder();
            sb.AppendLine($"ðŸ“ **Isi Folder** {(string.IsNullOrWhiteSpace(folderRef) ? "(root â€” file ter-share)" : $"*{folderRef}*")}");
            sb.AppendLine();
            sb.AppendLine($"Summary: {result.Summary?.Folders ?? 0} folder, {result.Summary?.Spreadsheets ?? 0} spreadsheet, {result.Summary?.OtherFiles ?? 0} file lain");
            sb.AppendLine("â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€");

            if (result.Folders != null && result.Folders.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("ðŸ“ **Folder:**");
                foreach (var f in result.Folders)
                {
                    sb.AppendLine($"  â€¢ {f.Name}  `{f.Id}`");
                }
            }

            if (result.Spreadsheets != null && result.Spreadsheets.Count > 0)
            {
                CacheNameToId(result.Spreadsheets);
                sb.AppendLine();
                sb.AppendLine("ðŸ“Š **Spreadsheet:**");
                foreach (var f in result.Spreadsheets)
                {
                    var icon = TypeIcon(f.Type ?? "");
                    sb.AppendLine($"  â€¢ {icon} {f.Name}  `{f.Id}`");
                }
            }

            if (result.OtherFiles != null && result.OtherFiles.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("ðŸ“„ **Lainnya:**");
                foreach (var f in result.OtherFiles)
                {
                    sb.AppendLine($"  â€¢ {f.Name}  `{f.Id}`");
                }
            }

            return sb.ToString().TrimEnd();
        }

        private async Task<string> BuildSheetTabsAsync(string fileRef)
        {
            if (string.IsNullOrWhiteSpace(fileRef))
                return "âš ï¸ Kasih nama atau ID file-nya ya. Contoh: `drive sheet BOQ FWA`";

            var fileId = await ResolveFileIdAsync(fileRef);
            if (string.IsNullOrEmpty(fileId))
                return $"âŒ File '{fileRef}' tidak ketemu di Drive. Coba `drive list {fileRef}` dulu.";

            var result = await _drive.ListSheetTabsAsync(fileId);
            if (result == null) return "âŒ Gagal ambil tab sheet.";

            var sb = new StringBuilder();
            sb.AppendLine($"ðŸ“‘ **Tab di *{result.FileName}***");
            sb.AppendLine($"Total: {result.TotalSheets} tab");
            sb.AppendLine("â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€");
            if (result.Sheets != null)
            {
                int i = 1;
                foreach (var s in result.Sheets)
                {
                    sb.AppendLine($"{i}. {s}");
                    i++;
                }
            }
            return sb.ToString().TrimEnd();
        }

        private async Task<string> BuildHeadersAsync(string fileRef)
        {
            if (string.IsNullOrWhiteSpace(fileRef))
                return "âš ï¸ Kasih nama atau ID file-nya ya. Contoh: `drive header BOQ FWA`";

            var (fileId, sheetName, headerRows, headerRowStart) = ParseFileSheetHeader(fileRef);
            var resolved = await ResolveFileIdAsync(fileId);
            if (string.IsNullOrEmpty(resolved))
                return $"âŒ File '{fileId}' tidak ketemu di Drive.";

            var result = await _drive.GetSheetHeadersAsync(resolved, sheetName, headerRows, headerRowStart);
            if (result == null) return "âŒ Gagal ambil header.";

            var sb = new StringBuilder();
            sb.AppendLine($"ðŸ“Š **{result.FileName}**");
            if (!string.IsNullOrEmpty(sheetName))
                sb.AppendLine($"Sheet: *{sheetName}*");
            if (headerRows > 1)
                sb.AppendLine($"Header rows: {headerRows} (multi-row header aktif)");
            if (headerRowStart > 1)
                sb.AppendLine($"Header row start: baris {headerRowStart}");
            sb.AppendLine($"ðŸ“ {result.TotalRows:N0} baris Ã— {result.TotalColumns} kolom");
            sb.AppendLine("â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€");
            sb.AppendLine("**Kolom:**");
            if (result.Columns != null)
            {
                int i = 1;
                foreach (var col in result.Columns)
                {
                    var dtype = result.Dtypes != null && result.Dtypes.TryGetValue(col, out var d) ? $" ({d})" : "";
                    sb.AppendLine($"  {i}. {col}{dtype}");
                    i++;
                }
            }
            sb.AppendLine();
            sb.AppendLine($"ðŸ’¡ Untuk filter data, coba: `drive filter {result.FileName} {{\"{result.Columns?.FirstOrDefault() ?? "kolom"}\":\"nilai\"}}`");
            if (headerRows == 1 && headerRowStart == 1)
                sb.AppendLine("ðŸ’¡ Kalau kolom terlihat aneh, coba `@h2` (header 2 baris) atau `@s3` (header dimulai baris 3).");
            return sb.ToString().TrimEnd();
        }

        private async Task<string> BuildSummaryAsync(string fileRef)
        {
            if (string.IsNullOrWhiteSpace(fileRef))
                return "âš ï¸ Kasih nama atau ID file-nya ya. Contoh: `drive summary BOQ FWA`";

            var (fileId, sheetName, headerRows, headerRowStart) = ParseFileSheetHeader(fileRef);
            var resolved = await ResolveFileIdAsync(fileId);
            if (string.IsNullOrEmpty(resolved))
                return $"âŒ File '{fileId}' tidak ketemu di Drive.";

            var result = await _drive.GetSheetSummaryAsync(resolved, sheetName, null, headerRows, headerRowStart);
            if (result == null) return "âŒ Gagal ambil summary.";

            var sb = new StringBuilder();
            sb.AppendLine($"ðŸ“ˆ **Summary Data**");
            if (!string.IsNullOrEmpty(sheetName))
                sb.AppendLine($"Sheet: *{sheetName}*");
            sb.AppendLine("â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€");

            try
            {
                var root = result.Value;
                if (root.TryGetProperty("shape", out var shape))
                {
                    var rows = shape.GetProperty("rows").GetInt32();
                    var cols = shape.GetProperty("columns").GetInt32();
                    sb.AppendLine($"ðŸ“ {rows:N0} baris Ã— {cols} kolom");
                    sb.AppendLine();
                }

                if (root.TryGetProperty("columns", out var cols_el))
                {
                    foreach (var prop in cols_el.EnumerateObject())
                    {
                        sb.AppendLine($"ðŸ“Œ **{prop.Name}**");
                        var v = prop.Value;
                        if (v.TryGetProperty("dtype", out var dt)) sb.AppendLine($"   Type: {dt.GetString()}");
                        if (v.TryGetProperty("non_null", out var nn)) sb.AppendLine($"   Non-null: {nn.GetInt32():N0}");
                        if (v.TryGetProperty("min", out var mn) && v.TryGetProperty("max", out var mx))
                            sb.AppendLine($"   Range: {mn} .. {mx}");
                        if (v.TryGetProperty("unique_count", out var uc))
                            sb.AppendLine($"   Unique: {uc.GetInt32():N0}");
                        if (v.TryGetProperty("unique_values", out var uv) && uv.ValueKind == JsonValueKind.Array)
                        {
                            var preview = string.Join(", ", uv.EnumerateArray().Take(10).Select(x => x.ToString()));
                            sb.AppendLine($"   Values: {preview}");
                        }
                        sb.AppendLine();
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"âš ï¸ Parsing error: {ex.Message}");
            }
            return sb.ToString().TrimEnd();
        }

        private async Task<string> BuildFilterAsync(string args)
        {
            if (string.IsNullOrWhiteSpace(args))
                return "âš ï¸ Format: `drive filter <nama file> {\"kolom\":\"nilai\"}`\n\nContoh:\n  â€¢ `drive filter BOQ FWA {\"segment\":\"FWA\",\"limit\":20}`\n  â€¢ `drive filter Monthly Report {\"tanggal\":{\"min\":\"2024-01-01\"}}`";

            // Parse: "<nama file>[#sheet][@h2] <json>"
            var jsonStart = args.IndexOf('{');
            if (jsonStart < 0)
                return "âš ï¸ Kriteria filter harus berupa JSON. Contoh: `drive filter BOQ FWA {\"segment\":\"FWA\"}`";

            var fileRef = args.Substring(0, jsonStart).Trim();
            var jsonStr = args.Substring(jsonStart).Trim();
            if (string.IsNullOrEmpty(fileRef))
                return "âš ï¸ Kasih nama file-nya dulu. Contoh: `drive filter BOQ FWA {\"segment\":\"FWA\"}`";

            var (fileId, sheetName, headerRows, headerRowStart) = ParseFileSheetHeader(fileRef);
            var resolved = await ResolveFileIdAsync(fileId);
            if (string.IsNullOrEmpty(resolved))
                return $"âŒ File '{fileId}' tidak ketemu di Drive.";

            Dictionary<string, object>? filters = null;
            int limit = 50;
            List<string>? columns = null;

            try
            {
                using var doc = JsonDocument.Parse(jsonStr);
                var root = doc.RootElement;
                filters = new Dictionary<string, object>();
                foreach (var prop in root.EnumerateObject())
                {
                    // Extract meta fields
                    if (prop.NameEquals("limit") && prop.Value.ValueKind == JsonValueKind.Number)
                    {
                        limit = prop.Value.GetInt32();
                    }
                    else if (prop.NameEquals("header_rows") && prop.Value.ValueKind == JsonValueKind.Number)
                    {
                        headerRows = prop.Value.GetInt32();
                    }
                    else if (prop.NameEquals("header_row_start") && prop.Value.ValueKind == JsonValueKind.Number)
                    {
                        headerRowStart = prop.Value.GetInt32();
                    }
                    else if (prop.NameEquals("columns") && prop.Value.ValueKind == JsonValueKind.Array)
                    {
                        columns = prop.Value.EnumerateArray().Select(e => e.GetString() ?? "").ToList();
                    }
                    else
                    {
                        filters[prop.Name] = JsonElementToObject(prop.Value);
                    }
                }
            }
            catch (JsonException ex)
            {
                return $"âŒ JSON tidak valid: {ex.Message}\n\nContoh yang benar: `drive filter BOQ FWA {{\"segment\":\"FWA\",\"limit\":20}}`";
            }

            var result = await _drive.FilterSheetAsync(resolved, filters, columns, sheetName, limit, headerRows, headerRowStart);
            if (result == null) return "âŒ Gagal filter data.";

            var sb = new StringBuilder();
            sb.AppendLine($"ðŸ”Ž **Hasil Filter**");
            sb.AppendLine($"Total: {result.OriginalRows:N0} baris â†’ filter {result.RowsAfterFilter:N0} â†’ tampil {result.RowsReturned}");
            sb.AppendLine("â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€");

            if (result.Data == null || result.Data.Count == 0)
            {
                sb.AppendLine();
                sb.AppendLine("Tidak ada data yang cocok dengan filter.");
                return sb.ToString().TrimEnd();
            }

            // Compact table rendering
            var cols = result.Columns ?? result.Data[0].Keys.ToList();
            int i = 1;
            foreach (var row in result.Data.Take(limit))
            {
                sb.AppendLine();
                sb.AppendLine($"**#{i}**");
                foreach (var col in cols)
                {
                    if (row.TryGetValue(col, out var val) && val != null)
                    {
                        var s = val.ToString();
                        if (!string.IsNullOrWhiteSpace(s) && s != "null")
                            sb.AppendLine($"  â€¢ {col}: {s}");
                    }
                }
                i++;
                if (i > 20)
                {
                    sb.AppendLine();
                    sb.AppendLine($"... dan {result.RowsReturned - 20} baris lainnya (tambah `\"limit\":N` untuk lebih banyak)");
                    break;
                }
            }

            return sb.ToString().TrimEnd();
        }

        // â”€â”€ Alias handling (natural language â†’ sheet query) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        /// <summary>
        /// Match message ke alias config dari GitHub. Kalau ada trigger yang match,
        /// extract sisa kata sebagai search keyword, dan query sheet yg sesuai.
        /// </summary>
        private async Task<(bool handled, string? response)> TryHandleAliasAsync(string msg, string lower)
        {
            // Cari alias yang trigger-nya match
            GDriveAlias? matched = null;
            string? matchedTrigger = null;
            int matchedPos = int.MaxValue;

            foreach (var alias in _drive.Aliases)
            {
                if (alias.Trigger == null) continue;
                foreach (var trg in alias.Trigger)
                {
                    if (string.IsNullOrWhiteSpace(trg)) continue;
                    var trgLower = trg.ToLowerInvariant().Trim();
                    // Match sebagai phrase (boleh di tengah kalimat)
                    var idx = IndexOfPhrase(lower, trgLower);
                    if (idx >= 0 && idx < matchedPos)
                    {
                        matched = alias;
                        matchedTrigger = trgLower;
                        matchedPos = idx;
                    }
                }
            }

            if (matched == null || string.IsNullOrEmpty(matched.FileId))
            {
                Console.WriteLine($"[GDriveCmd] No alias matched for '{msg}'");
                return (false, null);
            }

            Console.WriteLine($"[GDriveCmd] Matched alias: trigger='{matchedTrigger}' sheet='{matched.SheetName}' hrows={matched.HeaderRows} hstart={matched.HeaderRowStart}");

            if (string.IsNullOrEmpty(matched.SheetName))
            {
                return (true, $"âš ï¸ Alias '{matchedTrigger}' tidak punya sheet_name. Cek config GitHub.");
            }

            try
            {
                // Extract sisa kata setelah trigger â†’ jadi search keyword
                var afterTrigger = ExtractAfterTrigger(msg, lower, matchedTrigger!);
                var header_rows = matched.HeaderRows ?? 1;
                var header_row_start = matched.HeaderRowStart ?? 1;

                // Build filters
                var filters = new Dictionary<string, object>();

                // Apply default filters dari config (kalau ada)
                if (matched.DefaultFilters != null)
                {
                    foreach (var kv in matched.DefaultFilters)
                    {
                        // Convert JsonElement kalau perlu (deserializer dari text/json ngasih JsonElement)
                        filters[kv.Key] = kv.Value is JsonElement je ? JsonElementToObject(je) : kv.Value;
                    }
                }

                // Auto-extract search keyword dari user message
                var keyword = ParseSearchKeyword(afterTrigger);
                bool isListAll = string.IsNullOrWhiteSpace(keyword.value);

                Console.WriteLine($"[GDriveCmd] afterTrigger='{afterTrigger}' keyword=(col='{keyword.column}', val='{keyword.value}') isListAll={isListAll}");

                // Date keyword detection: kemarin / hari ini / minggu ini / 7 hari
                var dateFilterObj = TryParseDateKeyword(afterTrigger);
                if (dateFilterObj != null)
                {
                    filters["Tanggal"] = dateFilterObj;
                    isListAll = false;
                    keyword = (null, "rentang tanggal");
                    Console.WriteLine($"[GDriveCmd] Date filter applied: {(dateFilterObj as List<string>)?.Count} patterns");
                }

                // Status intent: "belum / outstanding / 0% / kosong / progress 0" -> post-filter rows where progress < 100
                var postFilter = ParseStatusIntent(afterTrigger);
                if (postFilter != null)
                {
                    isListAll = false;
                    keyword = (null, postFilter.Description);
                    Console.WriteLine($"[GDriveCmd] Status intent: {postFilter.Description}");
                }

                // Empty query + alias has suggestions -> show menu
                if (isListAll && matched.Suggestions != null && matched.Suggestions.Count > 0)
                {
                    Console.WriteLine($"[GDriveCmd] Showing menu ({matched.Suggestions.Count} suggestions)");
                    return (true, BuildAliasMenu(matched));
                }

                if (!isListAll && keyword.column != null && dateFilterObj == null)
                {
                    filters[keyword.column] = keyword.value!;
                }

                var limit = isListAll ? 20 : 50;
                SheetFilterResult? result;

                if (postFilter != null)
                {
                    // Fetch all then filter client-side (for outstanding/progress intent)
                    Console.WriteLine($"[GDriveCmd] Fetching all rows then client post-filter");
                    result = await _drive.FilterSheetAsync(
                        matched.FileId!, new Dictionary<string, object>(filters), null,
                        matched.SheetName, 200, header_rows, header_row_start);
                    result = ApplyPostFilter(result, postFilter);
                }
                else if (keyword.column == "__any__" || (!isListAll && keyword.column == null))
                {
                    Console.WriteLine($"[GDriveCmd] Calling FilterAcrossColumns with keyword='{keyword.value}' limit={limit}");
                    result = await FilterAcrossColumnsAsync(
                        matched.FileId!, matched.SheetName, header_rows, header_row_start,
                        keyword.value!, filters, limit, matched.SearchColumns);
                }
                else
                {
                    Console.WriteLine($"[GDriveCmd] Calling FilterSheet with filters={filters.Count} keys, limit={limit}");
                    result = await _drive.FilterSheetAsync(
                        matched.FileId!, filters, null, matched.SheetName, limit, header_rows, header_row_start);
                }

                Console.WriteLine($"[GDriveCmd] Result: rows_after={result?.RowsAfterFilter} returned={result?.RowsReturned}");
                return (true, FormatAliasResult(matched, keyword.value, result));
            }
            catch (Exception ex)
            {
                return (true, $"âŒ Gagal ambil data {matched.Name ?? matchedTrigger}: {ex.Message}\n\nðŸ’¡ Cek server Python sedang jalan dan tunnel aktif.");
            }
        }

        /// <summary>
        /// Fallback filter search: coba beberapa kolom umum (site id, rute, no_sj, nama barang, kab/kota).
        /// Pakai search_columns dari alias kalau ada, fallback ke default list.
        /// </summary>
        private async Task<SheetFilterResult?> FilterAcrossColumnsAsync(
            string fileId, string? sheetName, int headerRows, int headerRowStart,
            string keyword, Dictionary<string, object> baseFilters, int limit,
            List<string>? configuredColumns = null)
        {
            var columns = (configuredColumns != null && configuredColumns.Count > 0)
                ? configuredColumns.ToArray()
                : new[] { "site id", "rute", "no_sj", "no sj", "nama barang", "kab/kota", "kab", "segment", "homebase", "pengirim", "penerima" };

            foreach (var c in columns)
            {
                var f = new Dictionary<string, object>(baseFilters) { [c] = keyword };
                var r = await _drive.FilterSheetAsync(fileId, f, null, sheetName, limit, headerRows, headerRowStart);
                if (r != null && r.RowsAfterFilter > 0)
                {
                    return r;
                }
            }
            return await _drive.FilterSheetAsync(fileId, new Dictionary<string, object>(baseFilters), null, sheetName, limit, headerRows, headerRowStart);
        }

        private string FormatAliasResult(GDriveAlias alias, string? keyword, SheetFilterResult? result)
        {
            var sb = new StringBuilder();
            var title = alias.Name ?? alias.SheetName ?? "Data";
            if (!string.IsNullOrWhiteSpace(keyword))
                sb.AppendLine($"ðŸ” **{title}** â€” cari: *{keyword}*");
            else
                sb.AppendLine($"ðŸ“Š **{title}**");

            if (!string.IsNullOrEmpty(alias.Description))
                sb.AppendLine($"_{alias.Description}_");

            sb.AppendLine("â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€");

            if (result == null || result.Data == null || result.Data.Count == 0)
            {
                sb.AppendLine();
                if (!string.IsNullOrWhiteSpace(keyword))
                    sb.AppendLine($"âš ï¸ Tidak ada data yang cocok dengan '{keyword}' di sheet {alias.SheetName}.");
                else
                    sb.AppendLine($"âš ï¸ Sheet {alias.SheetName} kosong.");
                return sb.ToString().TrimEnd();
            }

            sb.AppendLine($"Total: {result.OriginalRows:N0} baris â†’ filter {result.RowsAfterFilter:N0} â†’ tampil {result.RowsReturned}");

            int i = 1;
            foreach (var row in result.Data.Take(20))
            {
                sb.AppendLine();
                sb.AppendLine($"**#{i}**");
                foreach (var kv in row)
                {
                    if (kv.Value == null) continue;
                    var s = kv.Value.ToString()?.Trim();
                    if (string.IsNullOrWhiteSpace(s) || s == "null" || s.Equals("nan", StringComparison.OrdinalIgnoreCase))
                        continue;
                    // Skip kolom dummy dari dedup / unnamed
                    if (kv.Key.StartsWith("_col_") || kv.Key == "_unnamed") continue;
                    // Skip kolom duplikat hasil dedup (ending _2, _3, dll.)
                    if (System.Text.RegularExpressions.Regex.IsMatch(kv.Key, @"_\d+$")) continue;
                    // Clean excessive whitespace in value
                    s = System.Text.RegularExpressions.Regex.Replace(s, @"\s+", " ").Trim();
                    sb.AppendLine($"  â€¢ {kv.Key}: {s}");
                }
                i++;
            }

            if (result.RowsReturned > 20)
            {
                sb.AppendLine();
                sb.AppendLine($"_...(+{result.RowsReturned - 20} baris lainnya)_");
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Client-side post filter (setelah data balik dari server).
        /// Untuk kasus yang kompleks seperti "progress &lt; 100%" atau "status outstanding".
        /// </summary>
        private class ClientPostFilter
        {
            public string Description { get; set; } = "";
            public string Mode { get; set; } = ""; // "progress_lt_100" | "progress_0" | "status_ne_ok"
        }

        /// <summary>
        /// Parse intent: "belum", "yang belum", "outstanding", "progress 0", "kosong", "0%"
        /// </summary>
        private static ClientPostFilter? ParseStatusIntent(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            var t = text.ToLowerInvariant();

            // "0%" atau "progress 0" atau "belum ada progress"
            if (Regex.IsMatch(t, @"\b(progres(s)?\s*0|progres(s)?\s*kosong|belum\s+ada\s+progres|0\s*%|nol\s*persen)\b"))
                return new ClientPostFilter { Mode = "progress_0", Description = "yang progres 0%" };

            // "belum" / "yang belum" / "outstanding" / "belum selesai" / "belum done"
            if (Regex.IsMatch(t, @"\b(belum(\s+selesai|\s+done|\s+100)?|outstanding|kurang|blm)\b"))
                return new ClientPostFilter { Mode = "progress_lt_100", Description = "yang belum selesai (progres < 100%)" };

            // "done" / "selesai" / "100%"
            if (Regex.IsMatch(t, @"\b(selesai|done|100\s*%|completed|komplit)\b"))
                return new ClientPostFilter { Mode = "progress_gte_100", Description = "yang sudah selesai" };

            return null;
        }

        /// <summary>Apply post-filter after server returned data.</summary>
        private static SheetFilterResult? ApplyPostFilter(SheetFilterResult? input, ClientPostFilter pf)
        {
            if (input == null || input.Data == null) return input;

            var kept = new List<Dictionary<string, object>>();
            foreach (var row in input.Data)
            {
                if (MatchPostFilter(row, pf)) kept.Add(row);
            }

            return new SheetFilterResult
            {
                OriginalRows = input.OriginalRows,
                RowsAfterFilter = kept.Count,
                RowsReturned = Math.Min(kept.Count, 30),
                Columns = input.Columns,
                Data = kept.Take(30).ToList()
            };
        }

        private static bool MatchPostFilter(Dictionary<string, object> row, ClientPostFilter pf)
        {
            // Cari kolom progres (%). Sheet RESUME pakai "... - %" atau "... - Progress"
            double? progressPct = null;
            bool anyProgress = false;

            foreach (var kv in row)
            {
                var key = kv.Key.ToLowerInvariant();
                if (kv.Value == null) continue;
                var val = kv.Value.ToString()?.Trim();
                if (string.IsNullOrWhiteSpace(val) || val == "null") continue;

                // Kolom persentase: "- %" atau mengandung "%"
                if (key.EndsWith(" - %") || key == "%" || key.Contains("persen"))
                {
                    anyProgress = true;
                    if (double.TryParse(val.Replace("%", "").Replace(",", ".").Trim(),
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out var d))
                    {
                        // Value bisa 0.5 (=50%) atau 50 atau 1 (=100%)
                        var pct = d <= 1.5 ? d * 100 : d;
                        if (progressPct == null || pct < progressPct.Value)
                            progressPct = pct; // worst-case (min) — kalau salah satu kategori 0%, dianggap belum
                    }
                }
            }

            if (!anyProgress) return false; // baris ga relevan

            switch (pf.Mode)
            {
                case "progress_0":
                    return progressPct.HasValue && progressPct.Value < 1;
                case "progress_lt_100":
                    return progressPct.HasValue && progressPct.Value < 100;
                case "progress_gte_100":
                    return progressPct.HasValue && progressPct.Value >= 100;
                default: return true;
            }
        }

        /// <summary>
        /// Build menu dari alias suggestions. User tinggal tap/ketik salah satu.
        /// </summary>
        private string BuildAliasMenu(GDriveAlias alias)
        {
            var sb = new StringBuilder();
            var title = alias.Name ?? alias.SheetName ?? "Data";
            sb.AppendLine($"ðŸ“Š **{title}**");
            if (!string.IsNullOrEmpty(alias.Description))
                sb.AppendLine($"_{alias.Description}_");
            sb.AppendLine();
            sb.AppendLine("Mau cek apa? Pilih salah satu:");
            sb.AppendLine();

            int i = 1;
            foreach (var sug in alias.Suggestions!)
            {
                sb.AppendLine($"{i}. {sug}");
                i++;
            }

            sb.AppendLine();
            sb.AppendLine("ðŸ’¡ Tinggal ketik salah satu di atas, atau kombinasikan sendiri (mis. `cek progres site 0244 kemarin`).");
            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Parse date keywords (kemarin/hari ini/minggu ini/7 hari terakhir/hari X) 
        /// jadi dict {min,max} untuk filter. Return null kalau tidak match.
        /// </summary>
        private static object? TryParseDateKeyword(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            var t = text.ToLowerInvariant().Trim();

            var today = DateTime.Today;
            var cultureID = new System.Globalization.CultureInfo("id-ID");

            // Format tanggal di sheet Progress biasanya "Senin, 13 April" â€” kita tidak tahu pasti tahunnya.
            // Pakai string matching ke format Indonesia. Kalau user bilang "kemarin" â†’ target text = "Minggu, 10 Mei"
            // Untuk 7 hari terakhir, kita pakai OR list (beberapa format).

            DateTime[]? dates = null;

            if (Regex.IsMatch(t, @"\bkemarin\b"))
                dates = new[] { today.AddDays(-1) };
            else if (Regex.IsMatch(t, @"\bhari\s*ini\b|^ini$"))
                dates = new[] { today };
            else if (Regex.IsMatch(t, @"\bminggu\s*ini\b|\b7\s*hari\b|\bseminggu\b"))
                dates = Enumerable.Range(0, 7).Select(i => today.AddDays(-i)).ToArray();
            else if (Regex.IsMatch(t, @"\bkemarin\s*lusa\b|\b2\s*hari\s*lalu\b"))
                dates = new[] { today.AddDays(-2) };

            if (dates == null) return null;

            // Build list of string patterns to match (day+date format)
            // Sheet biasa pakai "Senin, 13 April" â€” nama hari full + tgl + bulan
            var patterns = new List<string>();
            foreach (var d in dates)
            {
                // "Senin, 13 April" (no year)
                patterns.Add(d.ToString("dddd, dd MMMM", cultureID));
                patterns.Add(d.ToString("dddd, d MMMM", cultureID));
                // "13/05/2026"
                patterns.Add(d.ToString("dd/MM/yyyy"));
                // "2026-05-13"
                patterns.Add(d.ToString("yyyy-MM-dd"));
            }

            // Return sebagai list â†’ filter pakai OR "isin" + partial match
            return patterns;
        }

        /// <summary>Find phrase as whole word-ish match. Return index or -1.</summary>
        private static int IndexOfPhrase(string haystack, string needle)
        {
            if (string.IsNullOrEmpty(needle)) return -1;
            int idx = 0;
            while (true)
            {
                var i = haystack.IndexOf(needle, idx, StringComparison.OrdinalIgnoreCase);
                if (i < 0) return -1;
                var beforeOk = i == 0 || !char.IsLetterOrDigit(haystack[i - 1]);
                var after = i + needle.Length;
                var afterOk = after >= haystack.Length || !char.IsLetterOrDigit(haystack[after]);
                if (beforeOk && afterOk) return i;
                idx = i + 1;
            }
        }

        /// <summary>Text sisa setelah trigger (buat di-parse jadi search keyword).</summary>
        private static string ExtractAfterTrigger(string msg, string lower, string trigger)
        {
            var idx = IndexOfPhrase(lower, trigger);
            if (idx < 0) return "";
            return msg.Substring(idx + trigger.Length).Trim().TrimStart(':', '-', ',').Trim();
        }

        /// <summary>
        /// Parse "site 0244" â†’ column=SITE ID, value=0244.
        /// Parse "rute brebes" â†’ column=RUTE, value=brebes.
        /// Parse "kabel" (no qualifier) â†’ column=__any__, value=kabel.
        /// Parse "" â†’ no keyword.
        /// </summary>
        private static (string? column, string? value) ParseSearchKeyword(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return (null, null);

            // Strip common connecting words
            var t = text.Trim();
            foreach (var w in new[] { "yang ", "untuk ", "di ", "dari ", "ada " })
            {
                if (t.StartsWith(w, StringComparison.OrdinalIgnoreCase))
                    t = t.Substring(w.Length).Trim();
            }

            // Strip surrounding question marks/punctuation
            t = t.TrimEnd('?', '!', '.', ',').Trim();
            if (string.IsNullOrWhiteSpace(t)) return (null, null);

            // Pattern: "<qualifier> <value>"
            var m = Regex.Match(t, @"^(?<q>site|span|rute|kab|kota|segment|homebase|nama|barang|material|sj|no\s*sj|pengirim|penerima)\s+(?<v>.+)$",
                RegexOptions.IgnoreCase);
            if (m.Success)
            {
                var q = m.Groups["q"].Value.ToLowerInvariant().Replace(" ", "");
                var v = m.Groups["v"].Value.Trim();
                var col = q switch
                {
                    "site" => "SITE ID",
                    "span" => "LIST SPAN",
                    "rute" => "RUTE",
                    "kab" or "kota" => "KAB/KOTA",
                    "segment" => "Segment",
                    "homebase" => "HOMEBASE",
                    "nama" or "barang" or "material" => "Nama Barang",
                    "sj" or "nosj" => "NO_SJ",
                    "pengirim" => "PENGIRIM",
                    "penerima" => "PENERIMA",
                    _ => null
                };
                return (col, v);
            }

            // No qualifier: keyword akan dicoba di beberapa kolom
            return ("__any__", t);
        }

        // â”€â”€ End alias handling â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private static bool ContainsDriveKeyword(string lower)
        {
            // Match "drive" sebagai kata utuh, bukan substring dari "driver" dll
            return Regex.IsMatch(lower, @"\bdrive\b") ||
                   Regex.IsMatch(lower, @"\bgdrive\b") ||
                   Regex.IsMatch(lower, @"\bgoogle\s+drive\b");
        }

        /// <summary>
        /// Resolve file reference (bisa ID, URL, atau nama file) ke file_id.
        /// Pakai cache hasil list sebelumnya, kalau belum ada coba search.
        /// </summary>
        private async Task<string?> ResolveFileIdAsync(string reference)
        {
            if (string.IsNullOrWhiteSpace(reference)) return null;
            reference = reference.Trim();

            // 1. URL Drive/Sheets
            var urlMatch = Regex.Match(reference, @"[-\w]{25,}");
            if (urlMatch.Success && (reference.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                                     || IsLikelyFileId(urlMatch.Value)))
            {
                return urlMatch.Value;
            }

            // 2. Raw file ID (25+ chars alphanumeric/dash/underscore, no space)
            if (IsLikelyFileId(reference)) return reference;

            // 3. Cache lookup (case-insensitive)
            if (_nameToIdCache.TryGetValue(reference, out var cached) && DateTime.UtcNow < _cacheExpiry)
                return cached;

            // 4. Partial cache match
            foreach (var kv in _nameToIdCache)
            {
                if (kv.Key.Contains(reference, StringComparison.OrdinalIgnoreCase))
                    return kv.Value;
            }

            // 5. Search Drive by name
            try
            {
                var result = await _drive.ListFilesAsync(
                    query: $"name contains \"{EscapeDriveQuery(reference)}\"",
                    fileType: "all",
                    pageSize: 10);

                if (result?.Files != null && result.Files.Count > 0)
                {
                    CacheNameToId(result.Files);
                    // Prefer exact match, else first
                    var exact = result.Files.FirstOrDefault(f =>
                        string.Equals(f.Name, reference, StringComparison.OrdinalIgnoreCase));
                    return (exact ?? result.Files[0]).Id;
                }
            }
            catch { }

            return null;
        }

        private static bool IsLikelyFileId(string s)
        {
            return s.Length >= 25 && Regex.IsMatch(s, @"^[A-Za-z0-9_\-]+$");
        }

        private static string EscapeDriveQuery(string q)
        {
            return q.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private void CacheNameToId(IEnumerable<DriveFileDto> files)
        {
            _cacheExpiry = DateTime.UtcNow.AddMinutes(10);
            foreach (var f in files)
            {
                if (!string.IsNullOrEmpty(f.Name) && !string.IsNullOrEmpty(f.Id))
                {
                    _nameToIdCache[f.Name] = f.Id;
                }
            }
        }

        /// <summary>
        /// Split "file name[#sheet tab][@h2][@s3]" into components.
        /// @hN â†’ header_rows. @sN â†’ header_row_start. Default 1, 1.
        /// </summary>
        private static (string file, string? sheet, int headerRows, int headerRowStart) ParseFileSheetHeader(string input)
        {
            input = input.Trim();
            int headerRows = 1;
            int headerRowStart = 1;

            // Extract @hN
            var hMatch = Regex.Match(input, @"@h(\d+)\b", RegexOptions.IgnoreCase);
            if (hMatch.Success)
            {
                int.TryParse(hMatch.Groups[1].Value, out headerRows);
                if (headerRows < 1) headerRows = 1;
                input = (input.Substring(0, hMatch.Index) + input.Substring(hMatch.Index + hMatch.Length)).Trim();
            }

            // Extract @sN
            var sMatch = Regex.Match(input, @"@s(\d+)\b", RegexOptions.IgnoreCase);
            if (sMatch.Success)
            {
                int.TryParse(sMatch.Groups[1].Value, out headerRowStart);
                if (headerRowStart < 1) headerRowStart = 1;
                input = (input.Substring(0, sMatch.Index) + input.Substring(sMatch.Index + sMatch.Length)).Trim();
            }

            var (file, sheet) = ParseFileAndSheet(input);
            return (file, sheet, headerRows, headerRowStart);
        }

        /// <summary>Split "file name#sheet tab" or "file name:sheet tab".</summary>
        private static (string file, string? sheet) ParseFileAndSheet(string input)
        {
            input = input.Trim();
            var idx = input.IndexOf('#');
            if (idx < 0) idx = input.LastIndexOf(':');
            // guard against URLs (https://) using ':'
            if (idx > 0 && input.Length > idx + 1 && input.Substring(Math.Max(0, idx - 4), Math.Min(5, input.Length - idx)).StartsWith("http"))
                idx = -1;
            if (idx > 0)
            {
                return (input.Substring(0, idx).Trim(), input.Substring(idx + 1).Trim());
            }
            return (input, null);
        }

        private static object JsonElementToObject(JsonElement el)
        {
            switch (el.ValueKind)
            {
                case JsonValueKind.String: return el.GetString() ?? "";
                case JsonValueKind.Number:
                    return el.TryGetInt64(out var i) ? i : (object)el.GetDouble();
                case JsonValueKind.True: return true;
                case JsonValueKind.False: return false;
                case JsonValueKind.Null: return "";
                case JsonValueKind.Array:
                    return el.EnumerateArray().Select(JsonElementToObject).ToList();
                case JsonValueKind.Object:
                    var d = new Dictionary<string, object>();
                    foreach (var p in el.EnumerateObject())
                        d[p.Name] = JsonElementToObject(p.Value);
                    return d;
                default: return el.ToString();
            }
        }

        private static string TypeIcon(string type)
        {
            var t = type.ToLowerInvariant();
            if (t.Contains("folder")) return "ðŸ“";
            if (t.Contains("sheet") || t.Contains("spreadsheet")) return "ðŸ“Š";
            if (t.Contains("excel")) return "ðŸ“—";
            if (t.Contains("csv")) return "ðŸ“„";
            return "ðŸ“Ž";
        }

        private string BuildNoFilesMessage(string nameQuery)
        {
            var sb = new StringBuilder();
            sb.AppendLine("ðŸ“­ Tidak ada file di Drive yang ter-share ke service account.");
            if (!string.IsNullOrWhiteSpace(nameQuery))
                sb.AppendLine($"   (dengan nama mengandung: *{nameQuery}*)");
            sb.AppendLine();
            sb.AppendLine("ðŸ’¡ **Cara pakai:**");
            sb.AppendLine("1. Buka Google Drive kamu");
            sb.AppendLine("2. Klik kanan folder/file â†’ **Share**");
            sb.AppendLine("3. Paste email service account (ketik `drive status` untuk lihat)");
            sb.AppendLine("4. Permission: **Viewer**");
            sb.AppendLine("5. Tunggu ~1 menit, coba lagi");
            return sb.ToString().TrimEnd();
        }

        private static string BuildDisabledHint()
        {
            return "ðŸ”Œ **Google Drive Reader belum aktif.**\n\n" +
                   "Buka **AI Settings** â†’ **Google Drive Reader** â†’ toggle ON & isi URL server.\n\n" +
                   "Default URL: `http://localhost:20129` (untuk laptop yang sama).\n\n" +
                   "Kalau dari HP, pakai IP laptop atau Cloudflare tunnel.";
        }

        private static string BuildHelp()
        {
            return "ðŸ“š **Commands Google Drive** (read-only)\n" +
                   "â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€\n\n" +
                   "ðŸ” **Explore:**\n" +
                   "â€¢ `drive status` â€” cek koneksi & service account\n" +
                   "â€¢ `drive list` â€” list semua file yang ter-share\n" +
                   "â€¢ `drive list BOQ` â€” cari file berdasarkan nama\n" +
                   "â€¢ `isi folder <nama/id>` â€” lihat isi folder\n\n" +
                   "ðŸ“Š **Baca Spreadsheet:**\n" +
                   "â€¢ `drive sheet <nama>` â€” list tab di file\n" +
                   "â€¢ `drive header <nama>` â€” lihat kolom + tipe data\n" +
                   "â€¢ `drive summary <nama>` â€” statistik kolom\n\n" +
                   "ðŸŽ¯ **Filter (hemat token!):**\n" +
                   "â€¢ `drive filter BOQ FWA {\"segment\":\"FWA\",\"limit\":20}`\n" +
                   "â€¢ `drive filter Report {\"tanggal\":{\"min\":\"2024-01-01\"},\"columns\":[\"Name\",\"Qty\"]}`\n\n" +
                   "ðŸ’¡ **Tips:**\n" +
                   "â€¢ Multi-sheet? Tambah `#nama-sheet`. Contoh: `drive header BOQ#Data FWA`\n" +
                   "â€¢ Header 2 baris (group+sub)? Tambah `@h2`. Contoh: `drive header RESUME Progres FWA#GROBOGAN @h2`\n" +
                   "â€¢ Contoh lengkap: `drive filter RESUME Progres FWA#GROBOGAN @h2 {\"RUTE\":\"0244\"}`\n" +
                   "â€¢ Nama file fuzzy â€” partial match otomatis\n" +
                   "â€¢ Kolom di filter fuzzy-match (tgl = tanggal)";
        }
    }
}

