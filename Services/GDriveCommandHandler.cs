using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace StokBarangMAUI.Services
{
    /// <summary>
    /// Natural-language command handler untuk akses Google Drive read-only.
    ///
    /// Pola: mirip SearchProgressAsync di AiChatService  detect keyword di message,
    /// call GDriveReaderService, format hasilnya jadi teks rapi buat ditampilin langsung
    /// (skip LLM, hemat token maksimal).
    ///
    /// Command yang di-support (semua bahasa Indonesia natural):
    ///    "drive list" / "list drive" / "file drive"
    ///    "drive folder [nama/id]"
    ///    "drive sheet [nama/id]"  list tab
    ///    "drive header [nama/id]" / "kolom drive [nama/id]"
    ///    "drive summary [nama/id]"
    ///    "drive filter [nama/id] [json atau kriteria]"
    ///    "drive status" / "cek drive"
    ///    "drive help" / "bantuan drive"
    /// </summary>
    public class GDriveCommandHandler
    {
        private readonly GDriveReaderService _drive;

        // Cache pemetaan "nama file"  file_id biar user bisa refer by name
        private readonly Dictionary<string, string> _nameToIdCache = new(StringComparer.OrdinalIgnoreCase);
        private DateTime _cacheExpiry = DateTime.MinValue;

        public GDriveCommandHandler(GDriveReaderService drive)
        {
            _drive = drive;
        }

        /// <summary>
        /// Cek apakah message user adalah command drive. Return true + isi response kalau ya.
        /// Return false kalau bukan command drive (lanjut ke LLM flow biasa).
        // Max chars untuk response (biar hemat token & ga spam layar)
        private const int MAX_RESPONSE_CHARS = 1000;

        /// <summary>Cap response string dan tambah "... (trimmed)" kalau overflow.</summary>
        private static string CapResponse(string? s)
        {
            if (string.IsNullOrEmpty(s)) return s ?? "";
            if (s.Length <= MAX_RESPONSE_CHARS) return s;
            return s.Substring(0, MAX_RESPONSE_CHARS - 20) + "\n...(trimmed)";
        }

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

            // === PENDING CLARIFICATION: user tadi ditanya, sekarang jawab ===
            // State format JSON: {"flow":"stok","step":"gudang"} atau {"flow":"stok","step":"barang","gudang":"BREBES"}
            var pendingJson = Preferences.Get("gdrive_pending_clarif", "");
            if (!string.IsNullOrEmpty(pendingJson))
            {
                Console.WriteLine($"[GDriveCmd] Has pending: {pendingJson}");
                var resolved = await TryResolveClarificationAsync(pendingJson, msg, lower);
                if (resolved.handled)
                {
                    return (true, CapResponse(resolved.response));
                }
                // Cancel flow kalau user ketik sesuatu yang ga match
                Preferences.Remove("gdrive_pending_clarif");
            }

            // === Detect multi-step queries (site belum / stok / dll) ===
            if (_drive.IsEnabled)
            {
                // Direct city query: "cek pekerjaan di [kota]" / "yang belum di [kota]"
                var cityResult = await TryDirectCityWorkQueryAsync(lower);
                if (cityResult.handled) return (true, CapResponse(cityResult.response));

                var multiStepResult = TryMultiStepQuery(lower);
                if (multiStepResult.handled) return (true, CapResponse(multiStepResult.response));
            }

            // === Alias detection (dari remote config) ===
            if (_drive.IsEnabled && _drive.Aliases.Count > 0)
            {
                var aliasResult = await TryHandleAliasAsync(msg, lower);
                if (aliasResult.handled) return (true, CapResponse(aliasResult.response));
            }

            // Quick rejection - harus ada kata "drive" atau prefix khusus
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

                // Keyword "drive" tapi tidak ada pattern yang match  kasih help
                if (lower.Contains("drive"))
                {
                    return (true, BuildHelp());
                }
            }
            catch (Exception ex)
            {
                return (true, $" Error akses Drive: {ex.Message}\n\n Cek apakah server Python (http_server.py) sedang jalan di laptop, dan URL di Settings sudah benar.");
            }

            return (false, null);
        }

        //  Command builders 

        private async Task<string> BuildStatusAsync()
        {
            var (ok, message, email) = await _drive.CheckHealthAsync();
            var sb = new StringBuilder();
            sb.AppendLine("\uD83D\uDD0C **Status Google Drive Reader**");
            sb.AppendLine();
            sb.AppendLine($"Server: {_drive.BaseUrl}");
            sb.AppendLine($"Koneksi: {(ok ? " OK" : " " + message)}");
            if (!string.IsNullOrEmpty(email))
            {
                sb.AppendLine($"Service account: `{email}`");
                sb.AppendLine();
                sb.AppendLine(" Share folder/file Google Drive-mu ke email di atas (permission: Viewer) supaya bisa dibaca.");
            }
            if (!ok)
            {
                sb.AppendLine();
                sb.AppendLine(" **Troubleshooting:**");
                sb.AppendLine("1. Pastikan `start_server.bat` di laptop sedang jalan");
                sb.AppendLine("2. Cek URL di AI Settings  Drive Reader URL");
                sb.AppendLine("3. Kalau HP  laptop, pakai IP laptop atau Cloudflare tunnel (bukan localhost)");
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
            sb.AppendLine($" **File di Google Drive** ({result.Total} file)");
            if (!string.IsNullOrWhiteSpace(nameQuery))
                sb.AppendLine($" Filter nama: *{nameQuery}*");
            sb.AppendLine();
            sb.AppendLine("");

            int i = 1;
            foreach (var f in result.Files)
            {
                var icon = TypeIcon(f.Type ?? f.MimeType ?? "");
                sb.AppendLine($"{i}. {icon} **{f.Name}**");
                sb.AppendLine($"    `{f.Id}` ({f.Type ?? "file"})");
                i++;
            }
            sb.AppendLine();
            sb.AppendLine(" Tip: `drive header <nama file>` untuk lihat kolom.");
            return sb.ToString().TrimEnd();
        }

        private async Task<string> BuildFolderContentsAsync(string folderRef)
        {
            var folderId = await ResolveFileIdAsync(folderRef);
            // folderId boleh null/empty  list root (semua yg ter-share)

            var result = await _drive.ListFolderContentsAsync(folderId, recursive: false);
            if (result == null) return " Gagal ambil isi folder.";

            var sb = new StringBuilder();
            sb.AppendLine($" **Isi Folder** {(string.IsNullOrWhiteSpace(folderRef) ? "(root  file ter-share)" : $"*{folderRef}*")}");
            sb.AppendLine();
            sb.AppendLine($"Summary: {result.Summary?.Folders ?? 0} folder, {result.Summary?.Spreadsheets ?? 0} spreadsheet, {result.Summary?.OtherFiles ?? 0} file lain");
            sb.AppendLine("");

            if (result.Folders != null && result.Folders.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine(" **Folder:**");
                foreach (var f in result.Folders)
                {
                    sb.AppendLine($"   {f.Name}  `{f.Id}`");
                }
            }

            if (result.Spreadsheets != null && result.Spreadsheets.Count > 0)
            {
                CacheNameToId(result.Spreadsheets);
                sb.AppendLine();
                sb.AppendLine(" **Spreadsheet:**");
                foreach (var f in result.Spreadsheets)
                {
                    var icon = TypeIcon(f.Type ?? "");
                    sb.AppendLine($"   {icon} {f.Name}  `{f.Id}`");
                }
            }

            if (result.OtherFiles != null && result.OtherFiles.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine(" **Lainnya:**");
                foreach (var f in result.OtherFiles)
                {
                    sb.AppendLine($"   {f.Name}  `{f.Id}`");
                }
            }

            return sb.ToString().TrimEnd();
        }

        private async Task<string> BuildSheetTabsAsync(string fileRef)
        {
            if (string.IsNullOrWhiteSpace(fileRef))
                return " Kasih nama atau ID file-nya ya. Contoh: `drive sheet BOQ FWA`";

            var fileId = await ResolveFileIdAsync(fileRef);
            if (string.IsNullOrEmpty(fileId))
                return $" File '{fileRef}' tidak ketemu di Drive. Coba `drive list {fileRef}` dulu.";

            var result = await _drive.ListSheetTabsAsync(fileId);
            if (result == null) return " Gagal ambil tab sheet.";

            var sb = new StringBuilder();
            sb.AppendLine($" **Tab di *{result.FileName}***");
            sb.AppendLine($"Total: {result.TotalSheets} tab");
            sb.AppendLine("");
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
                return " Kasih nama atau ID file-nya ya. Contoh: `drive header BOQ FWA`";

            var (fileId, sheetName, headerRows, headerRowStart) = ParseFileSheetHeader(fileRef);
            var resolved = await ResolveFileIdAsync(fileId);
            if (string.IsNullOrEmpty(resolved))
                return $" File '{fileId}' tidak ketemu di Drive.";

            var result = await _drive.GetSheetHeadersAsync(resolved, sheetName, headerRows, headerRowStart);
            if (result == null) return " Gagal ambil header.";

            var sb = new StringBuilder();
            sb.AppendLine($" **{result.FileName}**");
            if (!string.IsNullOrEmpty(sheetName))
                sb.AppendLine($"Sheet: *{sheetName}*");
            if (headerRows > 1)
                sb.AppendLine($"Header rows: {headerRows} (multi-row header aktif)");
            if (headerRowStart > 1)
                sb.AppendLine($"Header row start: baris {headerRowStart}");
            sb.AppendLine($" {result.TotalRows:N0} baris  {result.TotalColumns} kolom");
            sb.AppendLine("");
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
            sb.AppendLine($" Untuk filter data, coba: `drive filter {result.FileName} {{\"{result.Columns?.FirstOrDefault() ?? "kolom"}\":\"nilai\"}}`");
            if (headerRows == 1 && headerRowStart == 1)
                sb.AppendLine(" Kalau kolom terlihat aneh, coba `@h2` (header 2 baris) atau `@s3` (header dimulai baris 3).");
            return sb.ToString().TrimEnd();
        }

        private async Task<string> BuildSummaryAsync(string fileRef)
        {
            if (string.IsNullOrWhiteSpace(fileRef))
                return " Kasih nama atau ID file-nya ya. Contoh: `drive summary BOQ FWA`";

            var (fileId, sheetName, headerRows, headerRowStart) = ParseFileSheetHeader(fileRef);
            var resolved = await ResolveFileIdAsync(fileId);
            if (string.IsNullOrEmpty(resolved))
                return $" File '{fileId}' tidak ketemu di Drive.";

            var result = await _drive.GetSheetSummaryAsync(resolved, sheetName, null, headerRows, headerRowStart);
            if (result == null) return " Gagal ambil summary.";

            var sb = new StringBuilder();
            sb.AppendLine($" **Summary Data**");
            if (!string.IsNullOrEmpty(sheetName))
                sb.AppendLine($"Sheet: *{sheetName}*");
            sb.AppendLine("");

            try
            {
                var root = result.Value;
                if (root.TryGetProperty("shape", out var shape))
                {
                    var rows = shape.GetProperty("rows").GetInt32();
                    var cols = shape.GetProperty("columns").GetInt32();
                    sb.AppendLine($" {rows:N0} baris  {cols} kolom");
                    sb.AppendLine();
                }

                if (root.TryGetProperty("columns", out var cols_el))
                {
                    foreach (var prop in cols_el.EnumerateObject())
                    {
                        sb.AppendLine($" **{prop.Name}**");
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
                sb.AppendLine($" Parsing error: {ex.Message}");
            }
            return sb.ToString().TrimEnd();
        }

        private async Task<string> BuildFilterAsync(string args)
        {
            if (string.IsNullOrWhiteSpace(args))
                return " Format: `drive filter <nama file> {\"kolom\":\"nilai\"}`\n\nContoh:\n   `drive filter BOQ FWA {\"segment\":\"FWA\",\"limit\":20}`\n   `drive filter Monthly Report {\"tanggal\":{\"min\":\"2024-01-01\"}}`";

            // Parse: "<nama file>[#sheet][@h2] <json>"
            var jsonStart = args.IndexOf('{');
            if (jsonStart < 0)
                return " Kriteria filter harus berupa JSON. Contoh: `drive filter BOQ FWA {\"segment\":\"FWA\"}`";

            var fileRef = args.Substring(0, jsonStart).Trim();
            var jsonStr = args.Substring(jsonStart).Trim();
            if (string.IsNullOrEmpty(fileRef))
                return " Kasih nama file-nya dulu. Contoh: `drive filter BOQ FWA {\"segment\":\"FWA\"}`";

            var (fileId, sheetName, headerRows, headerRowStart) = ParseFileSheetHeader(fileRef);
            var resolved = await ResolveFileIdAsync(fileId);
            if (string.IsNullOrEmpty(resolved))
                return $" File '{fileId}' tidak ketemu di Drive.";

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
                return $" JSON tidak valid: {ex.Message}\n\nContoh yang benar: `drive filter BOQ FWA {{\"segment\":\"FWA\",\"limit\":20}}`";
            }

            var result = await _drive.FilterSheetAsync(resolved, filters, columns, sheetName, limit, headerRows, headerRowStart);
            if (result == null) return " Gagal filter data.";

            var sb = new StringBuilder();
            sb.AppendLine($" **Hasil Filter**");
            sb.AppendLine($"Total: {result.OriginalRows:N0} baris  filter {result.RowsAfterFilter:N0}  tampil {result.RowsReturned}");
            sb.AppendLine("");

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
                            sb.AppendLine($"   {col}: {s}");
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

        //  Alias handling (natural language  sheet query) 

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
                return (true, $" Alias '{matchedTrigger}' tidak punya sheet_name. Cek config GitHub.");
            }

            try
            {
                // Extract sisa kata setelah trigger  jadi search keyword
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
                    // Use server-side smart filter (cached + efficient)
                    Console.WriteLine($"[GDriveCmd] SmartFilter intent={postFilter.Mode} keyword={keyword.value}");
                    result = await _drive.SmartFilterAsync(
                        matched.FileId!,
                        matched.SheetName,
                        keyword.value,
                        postFilter.Mode,
                        matched.SearchColumns,
                        limit,
                        header_rows,
                        header_row_start);
                }
                else if (keyword.column == "__any__" || (!isListAll && keyword.column == null))
                {
                    // Use smart filter for keyword search too (cached)
                    Console.WriteLine($"[GDriveCmd] SmartFilter keyword='{keyword.value}' limit={limit}");
                    result = await _drive.SmartFilterAsync(
                        matched.FileId!,
                        matched.SheetName,
                        keyword.value,
                        null,
                        matched.SearchColumns,
                        limit,
                        header_rows,
                        header_row_start);
                }
                else
                {
                    Console.WriteLine($"[GDriveCmd] FilterSheet with filters={filters.Count} keys, limit={limit}");
                    result = await _drive.FilterSheetAsync(
                        matched.FileId!, filters, null, matched.SheetName, limit, header_rows, header_row_start);
                }

                Console.WriteLine($"[GDriveCmd] Result: rows_after={result?.RowsAfterFilter} returned={result?.RowsReturned}");
                return (true, FormatAliasResult(matched, keyword.value, result));
            }
            catch (Exception ex)
            {
                return (true, $" Gagal ambil data {matched.Name ?? matchedTrigger}: {ex.Message}\n\n Cek server Python sedang jalan dan tunnel aktif.");
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
                sb.AppendLine($" **{title}**  cari: *{keyword}*");
            else
                sb.AppendLine($" **{title}**");

            if (!string.IsNullOrEmpty(alias.Description))
                sb.AppendLine($"_{alias.Description}_");

            sb.AppendLine("");

            if (result == null || result.Data == null || result.Data.Count == 0)
            {
                sb.AppendLine();
                if (!string.IsNullOrWhiteSpace(keyword))
                    sb.AppendLine($" Tidak ada data yang cocok dengan '{keyword}' di sheet {alias.SheetName}.");
                else
                    sb.AppendLine($" Sheet {alias.SheetName} kosong.");
                return sb.ToString().TrimEnd();
            }

            sb.AppendLine($"Total: {result.OriginalRows:N0} baris  filter {result.RowsAfterFilter:N0}  tampil {result.RowsReturned}");

            int i = 1;
            int shown = 0;
            foreach (var row in result.Data.Take(20))
            {
                // Early break kalau approaching cap
                if (sb.Length > 850) break;

                sb.AppendLine();
                sb.AppendLine($"#{i}");
                foreach (var kv in row)
                {
                    if (kv.Value == null) continue;
                    var s = kv.Value.ToString()?.Trim();
                    if (string.IsNullOrWhiteSpace(s) || s == "null" || s.Equals("nan", StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (kv.Key.StartsWith("_col_") || kv.Key == "_unnamed") continue;
                    if (System.Text.RegularExpressions.Regex.IsMatch(kv.Key, @"_\d+$")) continue;
                    s = System.Text.RegularExpressions.Regex.Replace(s, @"\s+", " ").Trim();
                    if (s.Length > 80) s = s.Substring(0, 77) + "...";
                    sb.AppendLine($"  {kv.Key}: {s}");
                    if (sb.Length > 900) break;
                }
                i++; shown++;
                if (sb.Length > 900) break;
            }

            if (shown < result.Data.Count)
            {
                sb.AppendLine();
                sb.AppendLine($"...(+{result.Data.Count - shown} baris lagi, ketik pertanyaan lebih spesifik)");
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
                            progressPct = pct; // worst-case (min)  kalau salah satu kategori 0%, dianggap belum
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
            sb.AppendLine($" **{title}**");
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
            sb.AppendLine(" Tinggal ketik salah satu di atas, atau kombinasikan sendiri (mis. `cek progres site 0244 kemarin`).");
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

            // Format tanggal di sheet Progress biasanya "Senin, 13 April"  kita tidak tahu pasti tahunnya.
            // Pakai string matching ke format Indonesia. Kalau user bilang "kemarin"  target text = "Minggu, 10 Mei"
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
            // Sheet biasa pakai "Senin, 13 April"  nama hari full + tgl + bulan
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

            // Return sebagai list  filter pakai OR "isin" + partial match
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
        /// Parse "site 0244"  column=SITE ID, value=0244.
        /// Parse "rute brebes"  column=RUTE, value=brebes.
        /// Parse "kabel" (no qualifier)  column=__any__, value=kabel.
        /// Parse ""  no keyword.
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

        //  End alias handling 

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
        /// @hN  header_rows. @sN  header_row_start. Default 1, 1.
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
            if (t.Contains("folder")) return "";
            if (t.Contains("sheet") || t.Contains("spreadsheet")) return "";
            if (t.Contains("excel")) return "";
            if (t.Contains("csv")) return "";
            return "";
        }

        private string BuildNoFilesMessage(string nameQuery)
        {
            var sb = new StringBuilder();
            sb.AppendLine(" Tidak ada file di Drive yang ter-share ke service account.");
            if (!string.IsNullOrWhiteSpace(nameQuery))
                sb.AppendLine($"   (dengan nama mengandung: *{nameQuery}*)");
            sb.AppendLine();
            sb.AppendLine(" **Cara pakai:**");
            sb.AppendLine("1. Buka Google Drive kamu");
            sb.AppendLine("2. Klik kanan folder/file  **Share**");
            sb.AppendLine("3. Paste email service account (ketik `drive status` untuk lihat)");
            sb.AppendLine("4. Permission: **Viewer**");
            sb.AppendLine("5. Tunggu ~1 menit, coba lagi");
            return sb.ToString().TrimEnd();
        }

        private static string BuildDisabledHint()
        {
            return " **Google Drive Reader belum aktif.**\n\n" +
                   "Buka **AI Settings**  **Google Drive Reader**  toggle ON & isi URL server.\n\n" +
                   "Default URL: `http://localhost:20129` (untuk laptop yang sama).\n\n" +
                   "Kalau dari HP, pakai IP laptop atau Cloudflare tunnel.";
        }

        // ── Multi-step clarification (site yang belum dikerjakan, dll) ──

        /// <summary>
        /// Direct city work query: "cek pekerjaan di brebes", "yang belum di sragen", "pekerjaan belum di klaten"
        /// Langsung execute tanpa multi-step kalau kota bisa di-resolve.
        /// </summary>
        private async Task<(bool handled, string? response)> TryDirectCityWorkQueryAsync(string lower)
        {
            // Pattern: "cek pekerjaan di [kota]" / "pekerjaan belum di [kota]" / "yang belum di [kota]"
            var m = Regex.Match(lower,
                @"\b(cek\s+)?(pekerjaan|kerja(an)?|rute|site)\s*(yang\s+)?(belum\s*)?(di|daerah|kota|kab)\s+(?<city>[a-z\s]{3,25})",
                RegexOptions.IgnoreCase);
            if (!m.Success)
            {
                m = Regex.Match(lower,
                    @"\b(yang\s+)?belum(\s+dikerjakan|\s+selesai)?\s+(di|daerah|kota)\s+(?<city>[a-z\s]{3,25})",
                    RegexOptions.IgnoreCase);
            }
            if (!m.Success)
            {
                // "pekerjaan [kota]" tanpa "di"
                m = Regex.Match(lower,
                    @"\b(cek\s+)?(pekerjaan|kerja(an)?)\s+(belum\s+)?(di\s+)?(?<city>brebes|tasik(malaya)?|purwokerto|sukoharjo|klaten|surakarta|solo|sragen|grobogan|blora|tegal|banyumas|cilacap)",
                    RegexOptions.IgnoreCase);
            }
            if (!m.Success) return (false, null);

            var city = m.Groups["city"].Value.Trim().TrimEnd('?', '!', '.', ',').Trim();
            var segment = MapCityToSegment(city);
            if (segment == null) return (false, null);

            // Determine intent: default "outstanding" (belum), unless "selesai/done" in message
            var intent = Regex.IsMatch(lower, @"\b(selesai|done|sudah|100)\b") ? "done" : "outstanding";

            return await ExecuteSiteQueryAsync(segment, intent);
        }

        /// <summary>Map city name (lowercase) to segment name used in sheet.</summary>
        private static string? MapCityToSegment(string city)
        {
            var c = city.ToLowerInvariant().Trim();
            if (Regex.IsMatch(c, @"\bbrebes\b|\btegal\b|\bpekalongan\b|\bcirebon\b|\bindramayu\b")) return "BREBES";
            if (Regex.IsMatch(c, @"\btasik(malaya)?\b|\bbanjar\b")) return "TASIKMALAYA";
            if (Regex.IsMatch(c, @"\bpurwokerto\b|\bbanyumas\b|\bcilacap\b|\bkebumen\b|\bpurworejo\b")) return "PURWOKERTO";
            if (Regex.IsMatch(c, @"\bsukoharjo\b|\bklaten\b|\bsurakarta\b|\bsolo\b|\bwonogiri\b")) return "SUKOHARJO";
            if (Regex.IsMatch(c, @"\bsragen\b|\bkarang\s*anyar\b")) return "SRAGEN";
            if (Regex.IsMatch(c, @"\bgrobogan\b|\bblora\b")) return "GROBOGAN";
            if (Regex.IsMatch(c, @"\b(semua|all)\b")) return "ALL";
            // Fallback: coba exact match uppercase
            var upper = c.ToUpperInvariant();
            var known = new[] { "BREBES", "TASIKMALAYA", "PURWOKERTO", "SUKOHARJO", "SRAGEN", "GROBOGAN" };
            foreach (var k in known)
                if (upper.Contains(k)) return k;
            return null;
        }

        /// <summary>
        /// Detect queries yang butuh clarification (stok 2-step, site yang belum, dll).
        /// State JSON: {"flow":"stok","step":"gudang"} atau {"flow":"site_outstanding","step":"segment"}
        /// </summary>
        private (bool handled, string? response) TryMultiStepQuery(string lower)
        {
            // === STOK flow (2-step: gudang -> barang) ===
            // Trigger harus spesifik biar tidak bentrok dengan alias "cek stok" yang single-step
            if (Regex.IsMatch(lower, @"^(cek\s+)?stok\s*\?*$")
                || Regex.IsMatch(lower, @"\bstok\s+(mana|apa|detail|detil)\b")
                || Regex.IsMatch(lower, @"^stok$"))
            {
                Preferences.Set("gdrive_pending_clarif", "{\"flow\":\"stok\",\"step\":\"gudang\"}");
                return (true, BuildStokGudangMenu());
            }

            // === SITE flow (belum / done) ===
            if (Regex.IsMatch(lower, @"\bsite\s+(mana|yang|apa)?\s*(belum|outstanding|blm)\b")
                || Regex.IsMatch(lower, @"\b(site|span|rute)\s+.{0,20}\s*(belum\s+dikerjakan|belum\s+selesai|outstanding)\b")
                || Regex.IsMatch(lower, @"^(belum\s+dikerjakan|outstanding|yang\s+belum)$"))
            {
                Preferences.Set("gdrive_pending_clarif", "{\"flow\":\"site\",\"step\":\"segment\",\"intent\":\"outstanding\"}");
                return (true, BuildSegmentMenu("site yang belum dikerjakan"));
            }

            if (Regex.IsMatch(lower, @"\bsite\s+(mana|yang)?\s*(selesai|done|100|komplit)\b")
                || Regex.IsMatch(lower, @"^(selesai|done)$"))
            {
                Preferences.Set("gdrive_pending_clarif", "{\"flow\":\"site\",\"step\":\"segment\",\"intent\":\"done\"}");
                return (true, BuildSegmentMenu("site yang sudah selesai"));
            }

            if (Regex.IsMatch(lower, @"\bprogres(s)?\s+(mana|yang)?\s*(belum|outstanding)\b"))
            {
                Preferences.Set("gdrive_pending_clarif", "{\"flow\":\"site\",\"step\":\"segment\",\"intent\":\"outstanding\"}");
                return (true, BuildSegmentMenu("progres yang belum selesai"));
            }

            return (false, null);
        }

        /// <summary>Menu untuk pilih gudang/kota (step 1 stok flow).</summary>
        private string BuildStokGudangMenu()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Stok di gudang mana?");
            sb.AppendLine();
            sb.AppendLine("1. BREBES");
            sb.AppendLine("2. TASIKMALAYA");
            sb.AppendLine("3. PURWOKERTO");
            sb.AppendLine("4. SUKOHARJO");
            sb.AppendLine("5. SRAGEN");
            sb.AppendLine("6. GROBOGAN");
            sb.AppendLine("7. SEMUA");
            sb.AppendLine();
            sb.AppendLine("Ketik nama atau angka.");
            return sb.ToString().TrimEnd();
        }

        /// <summary>Menu untuk pilih barang (step 2 stok flow).</summary>
        private string BuildStokBarangMenu(string gudang)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Mau cek barang apa di {gudang}?");
            sb.AppendLine();
            sb.AppendLine("1. Cable 24C (kabel 24 core)");
            sb.AppendLine("2. Tiang 7M");
            sb.AppendLine("3. Tiang 9M");
            sb.AppendLine("4. Closure 24C");
            sb.AppendLine("5. ODP 8 Port");
            sb.AppendLine("6. Strength Clamp 25/50");
            sb.AppendLine("7. X Frame 80x80");
            sb.AppendLine("8. SEMUA barang di gudang ini");
            sb.AppendLine();
            sb.AppendLine("Ketik nama atau angka.");
            return sb.ToString().TrimEnd();
        }

        /// <summary>Build menu 7 opsi segment untuk site flow.</summary>
        private string BuildSegmentMenu(string topic)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Cek {topic} di segment mana?");
            sb.AppendLine();
            sb.AppendLine("1. BREBES (Cirebon - Brebes - Tegal - Pekalongan)");
            sb.AppendLine("2. TASIKMALAYA (Tasikmalaya - Banjar)");
            sb.AppendLine("3. PURWOKERTO (Banyumas - Cilacap - Kebumen)");
            sb.AppendLine("4. SUKOHARJO (Klaten - Surakarta - Wonogiri)");
            sb.AppendLine("5. SRAGEN (Sragen - Karang Anyar)");
            sb.AppendLine("6. GROBOGAN (Blora)");
            sb.AppendLine("7. SEMUA (overview)");
            sb.AppendLine();
            sb.AppendLine("Ketik nama atau angka.");
            return sb.ToString().TrimEnd();
        }

        /// <summary>Parse user's answer to segment/gudang number or name. Return normalized segment or null.</summary>
        private string? ParseSegmentAnswer(string lower)
        {
            int? num = null;
            if (int.TryParse(lower.Trim(), out var n)) num = n;

            if (num == 1 || Regex.IsMatch(lower, @"\bbrebes\b|\btegal\b|\bpekalongan\b|\bcirebon\b|\bindramayu\b")) return "BREBES";
            if (num == 2 || Regex.IsMatch(lower, @"\btasik(malaya)?\b|\bbanjar\b")) return "TASIKMALAYA";
            if (num == 3 || Regex.IsMatch(lower, @"\bpurwokerto\b|\bbanyumas\b|\bcilacap\b|\bkebumen\b|\bpurworejo\b")) return "PURWOKERTO";
            if (num == 4 || Regex.IsMatch(lower, @"\bsukoharjo\b|\bklaten\b|\bsurakarta\b|\bsolo\b|\bwonogiri\b")) return "SUKOHARJO";
            if (num == 5 || Regex.IsMatch(lower, @"\bsragen\b|\bkarang\s*anyar\b")) return "SRAGEN";
            if (num == 6 || Regex.IsMatch(lower, @"\bgrobogan\b|\bblora\b")) return "GROBOGAN";
            if (num == 7 || Regex.IsMatch(lower, @"\b(semua|all|overview|total|semua\s*segment)\b")) return "ALL";
            return null;
        }

        /// <summary>Parse user's answer to barang number or name. Return keyword for filter, or null.</summary>
        private string? ParseBarangAnswer(string lower)
        {
            int? num = null;
            if (int.TryParse(lower.Trim(), out var n)) num = n;

            if (num == 1 || Regex.IsMatch(lower, @"\bcable\s*24|\bkabel\s*24|\b24c\b|\b24\s*core\b")) return "Cable 24C";
            if (num == 2 || Regex.IsMatch(lower, @"\btiang\s*7|\b7\s*m(eter)?\b")) return "Tiang 7M";
            if (num == 3 || Regex.IsMatch(lower, @"\btiang\s*9|\b9\s*m(eter)?\b")) return "Tiang 9M";
            if (num == 4 || Regex.IsMatch(lower, @"\bclosure\b")) return "Closure";
            if (num == 5 || Regex.IsMatch(lower, @"\bodp\b|\b8\s*port\b")) return "ODP";
            if (num == 6 || Regex.IsMatch(lower, @"\bstrength\s*clamp\b|\bclamp\b")) return "Strength Clamp";
            if (num == 7 || Regex.IsMatch(lower, @"\bx\s*frame\b|\bxframe\b|\b80\s*x\s*80\b")) return "X Frame";
            if (num == 8 || Regex.IsMatch(lower, @"\b(semua|all|total)\b")) return "ALL";
            return null;
        }

        /// <summary>Handler routing berdasarkan pending state JSON.</summary>
        private async Task<(bool handled, string? response)> TryResolveClarificationAsync(string pendingJson, string msg, string lower)
        {
            // Parse pending state
            Dictionary<string, string> state;
            try
            {
                using var doc = JsonDocument.Parse(pendingJson);
                state = new Dictionary<string, string>();
                foreach (var p in doc.RootElement.EnumerateObject())
                    state[p.Name] = p.Value.GetString() ?? "";
            }
            catch
            {
                return (false, null);
            }

            var flow = state.GetValueOrDefault("flow", "");
            var step = state.GetValueOrDefault("step", "");

            // === SITE flow ===
            if (flow == "site" && step == "segment")
            {
                var segment = ParseSegmentAnswer(lower);
                if (segment == null) return (false, null);
                Preferences.Remove("gdrive_pending_clarif");
                return await ExecuteSiteQueryAsync(segment, state.GetValueOrDefault("intent", "outstanding"));
            }

            // === STOK flow step 1: gudang ===
            if (flow == "stok" && step == "gudang")
            {
                var gudang = ParseSegmentAnswer(lower);
                if (gudang == null) return (false, null);

                // Next step: tanya barang
                var nextState = $"{{\"flow\":\"stok\",\"step\":\"barang\",\"gudang\":\"{gudang}\"}}";
                Preferences.Set("gdrive_pending_clarif", nextState);
                return (true, BuildStokBarangMenu(gudang));
            }

            // === STOK flow step 2: barang ===
            if (flow == "stok" && step == "barang")
            {
                var gudang = state.GetValueOrDefault("gudang", "ALL");
                var barang = ParseBarangAnswer(lower);
                if (barang == null) return (false, null);
                Preferences.Remove("gdrive_pending_clarif");
                return await ExecuteStokQueryAsync(gudang, barang);
            }

            return (false, null);
        }

        /// <summary>Execute site outstanding/done query di sheet RESUME BY SITE ID.</summary>
        private async Task<(bool handled, string? response)> ExecuteSiteQueryAsync(string segment, string intent)
        {
            var sheetFileId = _drive.Aliases
                .FirstOrDefault(a => a.SheetName == "RESUME BY SITE ID" || a.SheetName == "BREBES")?.FileId;
            if (string.IsNullOrEmpty(sheetFileId))
                return (true, "⚠️ Config RESUME Progres FWA tidak ketemu.");

            try
            {
                // Fetch dari RESUME BY SITE ID dengan kolom yang relevan
                var columns = new[]
                {
                    "Rute", "KAB/KOTA",
                    "Penarikan Kabel 24 core adss - Plan",
                    "Penarikan Kabel 24 core adss - Progress",
                    "Penanaman Tiang 7m 24Core - Plan",
                    "Penanaman Tiang 7m 24Core - Progress",
                    "Penanaman Tiang 9m 24Core - Plan",
                    "Penanaman Tiang 9m 24Core - Progress"
                };

                // Filter by KAB/KOTA kalau segment bukan ALL
                IDictionary<string, object>? filters = null;
                if (segment != "ALL")
                {
                    // Segment name = kota (BREBES, SRAGEN, dll)
                    filters = new Dictionary<string, object> { { "KAB/KOTA", segment } };
                }

                var result = await _drive.FilterSheetAsync(
                    sheetFileId,
                    filters: filters,
                    columns: columns,
                    sheetName: "RESUME BY SITE ID",
                    limit: 200,
                    headerRows: 2,
                    headerRowStart: 1);

                if (result?.Data == null || result.Data.Count == 0)
                    return (true, $"📭 Tidak ada data di RESUME BY SITE ID{(segment != "ALL" ? $" untuk {segment}" : "")}.");

                // Client-side filter: belum dikerjakan = semua Progress == 0
                var filtered = new List<Dictionary<string, object>>();
                foreach (var row in result.Data)
                {
                    var progKabel = GetNumericValue(row, "Penarikan Kabel 24 core adss - Progress");
                    var progT7 = GetNumericValue(row, "Penanaman Tiang 7m 24Core - Progress");
                    var progT9 = GetNumericValue(row, "Penanaman Tiang 9m 24Core - Progress");

                    bool belumDikerjakan = progKabel <= 0 && progT7 <= 0 && progT9 <= 0;
                    bool sudahDikerjakan = progKabel > 0 || progT7 > 0 || progT9 > 0;

                    if (intent == "done" && sudahDikerjakan)
                        filtered.Add(row);
                    else if (intent != "done" && belumDikerjakan)
                        filtered.Add(row);
                }

                // Format output rapi
                return (true, FormatSiteWorkResult(segment, intent, filtered, result.Data.Count));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GDriveCmd] ExecuteSite error: {ex.Message}");
                return (true, $"❌ Gagal query: {ex.Message}");
            }
        }

        /// <summary>Get numeric value from row dict, return 0 if null/empty/NaN.</summary>
        private static double GetNumericValue(Dictionary<string, object> row, string key)
        {
            if (!row.TryGetValue(key, out var val) || val == null) return 0;
            var s = val.ToString()?.Trim();
            if (string.IsNullOrWhiteSpace(s) || s == "null" || s.Equals("nan", StringComparison.OrdinalIgnoreCase))
                return 0;
            if (double.TryParse(s.Replace(",", "."),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var d))
                return d;
            return 0;
        }

        /// <summary>Format hasil query pekerjaan belum/sudah dikerjakan — rapi, ringkas.</summary>
        private static string FormatSiteWorkResult(string segment, string intent, List<Dictionary<string, object>> rows, int totalRows)
        {
            var sb = new StringBuilder();
            var statusLabel = intent == "done" ? "sudah dikerjakan ✅" : "belum dikerjakan 🔴";
            var title = segment == "ALL"
                ? $"Pekerjaan {statusLabel}"
                : $"Pekerjaan di {segment} — {statusLabel}";

            sb.AppendLine($"📋 **{title}**");
            sb.AppendLine($"📊 {rows.Count} dari {totalRows} rute");
            sb.AppendLine();

            if (rows.Count == 0)
            {
                if (intent == "done")
                    sb.AppendLine("😅 Belum ada rute yang dikerjakan.");
                else
                    sb.AppendLine("🎉 Semua rute sudah dikerjakan! Mantap!");
                return sb.ToString().TrimEnd();
            }

            // Header tabel
            sb.AppendLine("```");
            sb.AppendLine($"{"Rute",-30} {"Kota",-12} {"Kabel(m)",-9} {"T7",-4} {"T9",-4}");
            sb.AppendLine(new string('─', 62));

            int shown = 0;
            foreach (var row in rows.Take(25))
            {
                var rute = GetStringValue(row, "Rute");
                var kota = GetStringValue(row, "KAB/KOTA");
                var planKabel = GetNumericValue(row, "Penarikan Kabel 24 core adss - Plan");
                var planT7 = GetNumericValue(row, "Penanaman Tiang 7m 24Core - Plan");
                var planT9 = GetNumericValue(row, "Penanaman Tiang 9m 24Core - Plan");

                // Truncate rute name kalau kepanjangan
                if (rute.Length > 28) rute = rute.Substring(0, 25) + "...";
                if (kota.Length > 11) kota = kota.Substring(0, 8) + "...";

                sb.AppendLine($"{rute,-30} {kota,-12} {planKabel,8:N0} {planT7,4:N0} {planT9,4:N0}");
                shown++;
            }

            sb.AppendLine("```");

            if (rows.Count > shown)
                sb.AppendLine($"📄 ...+{rows.Count - shown} rute lagi");

            sb.AppendLine();
            sb.AppendLine("💡 Kabel(m) = Plan penarikan kabel, T7/T9 = Plan tiang 7m/9m");
            sb.AppendLine("🔎 Ketik `site [ID]` untuk detail spesifik");

            return sb.ToString().TrimEnd();
        }

        /// <summary>Get string value from row dict safely.</summary>
        private static string GetStringValue(Dictionary<string, object> row, string key)
        {
            if (!row.TryGetValue(key, out var val) || val == null) return "-";
            var s = val.ToString()?.Trim();
            return string.IsNullOrWhiteSpace(s) || s == "null" ? "-" : s;
        }

        /// <summary>Execute stok query di sheet Aktual Stok (crosstab).</summary>
        private async Task<(bool handled, string? response)> ExecuteStokQueryAsync(string gudang, string barang)
        {
            var stokAlias = _drive.Aliases.FirstOrDefault(a => a.SheetName == "Aktual Stok");
            if (stokAlias == null || string.IsNullOrEmpty(stokAlias.FileId))
                return (true, "⚠️ Config Aktual Stok tidak ketemu.");

            try
            {
                // Fetch ALL data from Aktual Stok (crosstab, 3-row header)
                var result = await _drive.FilterSheetAsync(
                    stokAlias.FileId,
                    filters: null,
                    columns: null, // all columns
                    sheetName: "Aktual Stok",
                    limit: 30,
                    headerRows: 3,
                    headerRowStart: 1);

                return (true, FormatStokCrosstab(gudang, barang, result));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GDriveCmd] ExecuteStok error: {ex.Message}");
                return (true, $"❌ Gagal query stok: {ex.Message}");
            }
        }

        /// <summary>Format crosstab stok dengan emoji yang menarik.</summary>
        private string FormatStokCrosstab(string gudang, string barang, SheetFilterResult? result)
        {
            if (result == null || result.Data == null || result.Data.Count == 0)
                return "📭 Data Aktual Stok kosong.";

            var sb = new StringBuilder();

            // Determine which columns to show based on gudang
            // Crosstab structure (from raw preview):
            // Col 0: Material name (key varies: "STOK GUDANG..." or first col)
            // Col 1-6: Stok Diterima per gudang (BREBES, TASIK, PWT, SKH, SRAGEN, GROBOGAN)
            // Col 7: Grand Total Diterima
            // Col 8: TOTAL SISA GUDANG
            // Col 9: Grand Total Keluar
            // Col 10-15: Stok Keluar per gudang

            // Find column name patterns
            string? colMaterial = null;
            string? colGrandDiterima = null;
            string? colSisaGudang = null;
            string? colGrandKeluar = null;
            string? colGudangDiterima = null;
            string? colGudangKeluar = null;

            if (result.Columns != null && result.Columns.Count > 0)
            {
                colMaterial = result.Columns[0]; // first col = material name
                foreach (var c in result.Columns)
                {
                    var cu = c.ToUpperInvariant();
                    if (cu.Contains("GRAND TOTAL") && cu.Contains(">>")) colGrandDiterima ??= c;
                    if (cu.Contains("SISA GUDANG") || cu.Contains("TOTAL SISA")) colSisaGudang ??= c;
                    if (cu.Contains("GRAND TOTAL TERPAKAI") && cu.Contains("<<")) colGrandKeluar ??= c;
                }
            }

            // Gudang-specific column matching
            string gudangPattern = gudang switch
            {
                "BREBES" => "CIREBON|BREBES|TEGAL|PEKALONGAN|INDRAMAYU|SEMARANG",
                "TASIKMALAYA" => "TASIKMALAYA|BANJAR",
                "PURWOKERTO" => "BANYUMAS|CILACAP|KEBUMEN|PURWOREJO",
                "SUKOHARJO" => "SUKOHARJO|KLATEN|SURAKARTA|WONOGIRI",
                "SRAGEN" => "SRAGEN|KARANG",
                "GROBOGAN" => "GROBOGAN|BLORA",
                _ => ""
            };

            // Title
            if (gudang == "ALL")
            {
                sb.AppendLine("📦 **STOK MATERIAL — Ringkasan Semua Gudang**");
            }
            else
            {
                sb.AppendLine($"📦 **STOK MATERIAL — Gudang {gudang}**");
            }
            sb.AppendLine();

            // Filter rows by barang if specified
            var rows = result.Data;
            if (barang != "ALL" && !string.IsNullOrEmpty(barang))
            {
                rows = rows.Where(r =>
                {
                    var name = r.Values.FirstOrDefault()?.ToString() ?? "";
                    return name.ToUpperInvariant().Contains(barang.ToUpperInvariant());
                }).ToList();
            }

            if (rows.Count == 0)
            {
                sb.AppendLine($"🔍 Material '{barang}' tidak ditemukan di sheet Aktual Stok.");
                return sb.ToString().TrimEnd();
            }

            // === ALL gudang: show summary table ===
            if (gudang == "ALL")
            {
                sb.AppendLine("```");
                sb.AppendLine(string.Format("{0,-22} {1,9} {2,9} {3,9}", "Material", "Diterima", "Keluar", "Sisa"));
                sb.AppendLine(new string('─', 52));

                foreach (var row in rows.Take(15))
                {
                    var material = GetFirstColValue(row);
                    if (string.IsNullOrWhiteSpace(material) || material.Length < 3) continue;

                    var diterima = FindColValue(row, result.Columns, "GRAND TOTAL", ">>"); 
                    var keluar = FindColValue(row, result.Columns, "GRAND TOTAL TERPAKAI", "<<");
                    var sisa = FindColValue(row, result.Columns, "SISA GUDANG", "SISA");

                    // Shorten material name
                    var matShort = ShortenMaterial(material);
                    sb.AppendLine(string.Format("{0,-22} {1,9} {2,9} {3,9}", matShort, FormatNum(diterima), FormatNum(keluar), FormatNum(sisa)));
                }
                sb.AppendLine("```");
                sb.AppendLine();

                // Highlight items with low/zero sisa
                var lowStock = new List<string>();
                foreach (var row in rows.Take(15))
                {
                    var material = GetFirstColValue(row);
                    if (string.IsNullOrWhiteSpace(material) || material.Length < 3) continue;
                    var sisa = FindColValue(row, result.Columns, "SISA GUDANG", "SISA");
                    if (sisa <= 0)
                        lowStock.Add(ShortenMaterial(material));
                }
                if (lowStock.Count > 0)
                {
                    sb.AppendLine($"🚨 **Stok habis:** {string.Join(", ", lowStock)}");
                    sb.AppendLine();
                }

                sb.AppendLine("💡 Ketik `stok di brebes` atau `stok kabel 24c` untuk detail per gudang/material");
            }
            else
            {
                // === Per gudang: show diterima + keluar for that gudang ===
                sb.AppendLine("```");
                sb.AppendLine(string.Format("{0,-22} {1,8} {2,8} {3,8}", "Material", "Masuk", "Keluar", "Sisa"));
                sb.AppendLine(new string('─', 49));

                foreach (var row in rows.Take(15))
                {
                    var material = GetFirstColValue(row);
                    if (string.IsNullOrWhiteSpace(material) || material.Length < 3) continue;

                    var masuk = FindGudangColValue(row, result.Columns, gudangPattern, isKeluar: false);
                    var keluar = FindGudangColValue(row, result.Columns, gudangPattern, isKeluar: true);
                    var sisa = masuk - keluar;

                    var matShort = ShortenMaterial(material);
                    sb.AppendLine(string.Format("{0,-22} {1,8} {2,8} {3,8}", matShort, FormatNum(masuk), FormatNum(keluar), FormatNum(sisa)));
                }
                sb.AppendLine("```");
                sb.AppendLine();

                // Emoji indicators
                sb.AppendLine("📊 Masuk = stok diterima, Keluar = stok terpakai");
            }

            return sb.ToString().TrimEnd();
        }

        /// <summary>Get first column value (material name) from row.</summary>
        private static string GetFirstColValue(Dictionary<string, object> row)
        {
            return row.Values.FirstOrDefault()?.ToString()?.Trim() ?? "";
        }

        /// <summary>Find column value by partial name match (for Grand Total, Sisa, etc).</summary>
        private static double FindColValue(Dictionary<string, object> row, List<string>? columns, string pattern1, string pattern2)
        {
            foreach (var kv in row)
            {
                var cu = kv.Key.ToUpperInvariant();
                if (cu.Contains(pattern1.ToUpperInvariant()) && cu.Contains(pattern2.ToUpperInvariant()))
                {
                    return ParseStokNumber(kv.Value?.ToString());
                }
            }
            // Fallback: try pattern1 only
            foreach (var kv in row)
            {
                var cu = kv.Key.ToUpperInvariant();
                if (cu.Contains(pattern1.ToUpperInvariant()))
                    return ParseStokNumber(kv.Value?.ToString());
            }
            return 0;
        }

        /// <summary>Find gudang-specific column value (diterima or keluar section).</summary>
        private static double FindGudangColValue(Dictionary<string, object> row, List<string>? columns, string gudangPattern, bool isKeluar)
        {
            if (string.IsNullOrEmpty(gudangPattern)) return 0;
            var regex = new Regex(gudangPattern, RegexOptions.IgnoreCase);

            // Columns are ordered: first half = diterima, second half (after Grand Total) = keluar
            // We need to find the right section
            bool passedGrandTotal = false;
            foreach (var kv in row)
            {
                var cu = kv.Key.ToUpperInvariant();
                if (cu.Contains("GRAND TOTAL") && cu.Contains(">>"))
                {
                    passedGrandTotal = true;
                    continue;
                }
                if (cu.Contains("SISA GUDANG") || cu.Contains("GRAND TOTAL TERPAKAI"))
                {
                    passedGrandTotal = true;
                    continue;
                }

                if (regex.IsMatch(kv.Key))
                {
                    // Before Grand Total = diterima section, after = keluar section
                    if (!isKeluar && !passedGrandTotal)
                        return ParseStokNumber(kv.Value?.ToString());
                    if (isKeluar && passedGrandTotal)
                        return ParseStokNumber(kv.Value?.ToString());
                }
            }
            return 0;
        }

        /// <summary>Parse stok number: "44.000" or "  44.000 " or "(6.522)" → double.</summary>
        private static double ParseStokNumber(string? s)
        {
            if (string.IsNullOrWhiteSpace(s) || s == "null" || s == "-" || s.Equals("nan", StringComparison.OrdinalIgnoreCase))
                return 0;
            s = s.Trim();
            bool negative = s.StartsWith("(") && s.EndsWith(")");
            if (negative) s = s.Trim('(', ')').Trim();
            // Indonesian number format: "44.000" = 44000, "14.272" = 14272
            // Remove dots (thousand separator), replace comma with dot for decimal
            s = s.Replace(".", "").Replace(",", ".").Trim();
            if (double.TryParse(s, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var d))
                return negative ? -d : d;
            return 0;
        }

        /// <summary>Format number with thousand separator (Indonesian style).</summary>
        private static string FormatNum(double val)
        {
            if (val == 0) return "0";
            if (val < 0) return $"({Math.Abs(val):N0})".Replace(",", ".");
            return val.ToString("N0").Replace(",", ".");
        }

        /// <summary>Shorten material name for table display.</summary>
        private static string ShortenMaterial(string name)
        {
            if (name.Length <= 22) return name;
            // Common shortenings
            var s = name;
            s = Regex.Replace(s, @"Penarikan Kabel 24 core adss", "Cable 24C", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"Penanaman Tiang 7m.*", "Tiang 7M", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"Penanaman Tiang 9m.*", "Tiang 9M", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"ODP 8 Port.*Fisher", "ODP 8 Port", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"Connector.*Feeder", "Connector SC UPC", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"Patchcord.*Outdoor", "Patchcord 5M", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"Closure 24C \(Komplit\)", "Closure 24C", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"Strength Clamp25/50", "Strength Clamp", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"X Frame 80 X 80", "X Frame 80x80", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"Pigtail SC/UPC 1 meter", "Pigtail SC/UPC", RegexOptions.IgnoreCase);
            if (s.Length > 22) s = s.Substring(0, 19) + "...";
            return s;
        }

        private static string BuildHelp()
        {
            return " **Commands Google Drive** (read-only)\n" +
                   "\n\n" +
                   " **Explore:**\n" +
                   " `drive status`  cek koneksi & service account\n" +
                   " `drive list`  list semua file yang ter-share\n" +
                   " `drive list BOQ`  cari file berdasarkan nama\n" +
                   " `isi folder <nama/id>`  lihat isi folder\n\n" +
                   " **Baca Spreadsheet:**\n" +
                   " `drive sheet <nama>`  list tab di file\n" +
                   " `drive header <nama>`  lihat kolom + tipe data\n" +
                   " `drive summary <nama>`  statistik kolom\n\n" +
                   " **Filter (hemat token!):**\n" +
                   " `drive filter BOQ FWA {\"segment\":\"FWA\",\"limit\":20}`\n" +
                   " `drive filter Report {\"tanggal\":{\"min\":\"2024-01-01\"},\"columns\":[\"Name\",\"Qty\"]}`\n\n" +
                   " **Tips:**\n" +
                   " Multi-sheet? Tambah `#nama-sheet`. Contoh: `drive header BOQ#Data FWA`\n" +
                   " Header 2 baris (group+sub)? Tambah `@h2`. Contoh: `drive header RESUME Progres FWA#GROBOGAN @h2`\n" +
                   " Contoh lengkap: `drive filter RESUME Progres FWA#GROBOGAN @h2 {\"RUTE\":\"0244\"}`\n" +
                   " Nama file fuzzy  partial match otomatis\n" +
                   " Kolom di filter fuzzy-match (tgl = tanggal)";
        }
    }
}

