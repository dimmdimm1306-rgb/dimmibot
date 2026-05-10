using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using StokBarangMAUI.Models;

namespace StokBarangMAUI.Services
{
    public class SheetData
    {
        // Bump this when parser logic changes (e.g., new material categories).
        // Cache files with lower version are discarded → forces a fresh fetch.
        public const int CurrentSchemaVersion = 3;
        public int SchemaVersion { get; set; } = CurrentSchemaVersion;

        public List<SuratJalanItem>      SuratJalan        { get; set; } = new();
        public List<ProgressItem>        Progress          { get; set; } = new();
        public List<StokHomebaseItem>    StokAktual        { get; set; } = new();
        public List<StokGudangItem>      StokGudang        { get; set; } = new();
        public List<GudangWarehouse>     GudangWarehouses  { get; set; } = new();
        public List<ProgressResumeItem>  ProgressResume    { get; set; } = new();
        public ProgressResumeTotal       ResumeTotal       { get; set; } = new();

        // Cache metadata
        public DateTime SavedAt        { get; set; } = DateTime.MinValue;
        public string   RawProgressCsv { get; set; } = "";

        // Nama project dari sheet Config (sinkron antar HP)
        public string? ProjectNameFromSheet { get; set; }
        public string? ProjectDescFromSheet { get; set; }

        // Populated only on fresh network fetch — not persisted to cache
        [JsonIgnore]
        public string? LoadWarning { get; set; }

        [JsonIgnore]
        public string SavedAtText => SavedAt == DateTime.MinValue ? "–"
            : SavedAt.Date == DateTime.Today ? $"Hari ini {SavedAt:HH:mm}"
            : $"{SavedAt:d MMM yyyy, HH:mm}";

        [JsonIgnore]
        public bool IsFromCache => SavedAt != DateTime.MinValue;
    }

    public class GoogleSheetsService
    {
        private static string Csv(string id, string gid) =>
            $"https://docs.google.com/spreadsheets/d/{id}/export?format=csv&gid={gid}";
        // gviz/tq preserves HYPERLINK formulas as <a href> in HTML output for publicly-shared sheets
        private static string GvizHtml(string id, string gid) =>
            $"https://docs.google.com/spreadsheets/d/{id}/gviz/tq?tqx=out:html&gid={gid}";
        // gviz CSV with explicit sheet name + range. Used to read project name (A1) &
        // description (A2) from a sheet bernama "Alamat" di setiap spreadsheet utama.
        private static string GvizCsvSheetRange(string id, string sheet, string range) =>
            $"https://docs.google.com/spreadsheets/d/{id}/gviz/tq?tqx=out:csv&sheet={Uri.EscapeDataString(sheet)}&range={range}";

        private readonly HttpClient _http = new(new HttpClientHandler { AllowAutoRedirect = true });
        private SheetData?     _cache;
        private string?        _csvProgressRaw;
        private ProjectConfig? _project;

        public ProjectConfig? CurrentProject => _project;

// ── Disk Cache ───────────────────────────────────────────────────
private static readonly JsonSerializerOptions _jsonOpts = new()
{
    WriteIndented            = false,
    PropertyNameCaseInsensitive = true,
};

private static string GetSafeHash(string input)
{
    using var sha = System.Security.Cryptography.SHA256.Create();
    var bytes = System.Text.Encoding.UTF8.GetBytes(input);
    var hash = sha.ComputeHash(bytes);
    return Convert.ToHexString(hash)[..16]; // Take first 16 chars for reasonable filename length
}

private string CacheFilePath =>
    Path.Combine(FileSystem.AppDataDirectory,
        $"cache_{GetSafeHash(P.SpreadsheetId)}.json");

        private async Task<SheetData?> LoadFromDiskAsync()
        {
            try
            {
                var path = CacheFilePath;
                if (!File.Exists(path)) return null;
                var json = await File.ReadAllTextAsync(path);
                var data = JsonSerializer.Deserialize<SheetData>(json, _jsonOpts);
                // Discard old caches so parser fixes (e.g., Tiang support) take effect.
                if (data == null || data.SchemaVersion < SheetData.CurrentSchemaVersion)
                {
                    try { File.Delete(path); } catch { }
                    return null;
                }
                return data;
            }
            catch { return null; }
        }

        private async Task SaveToDiskAsync(SheetData data)
        {
            try
            {
                var json = JsonSerializer.Serialize(data, _jsonOpts);
                await File.WriteAllTextAsync(CacheFilePath, json);
            }
            catch { /* ignore — disk full or permission error */ }
        }

        public void SetProject(ProjectConfig project)
        {
            if (_project?.Id != project.Id)
            {
                _cache          = null;
                _csvProgressRaw = null;
            }
            _project = project;
        }

        private ProjectConfig P =>
            _project ?? throw new InvalidOperationException("Project belum dipilih. Pilih project terlebih dahulu.");

        // Never throws — returns empty string on any HTTP or network error
        private async Task<string> TryGetStringAsync(string url)
        {
            try { return await _http.GetStringAsync(url); }
            catch { return string.Empty; }
        }

        // Fetch nama project (A1) & deskripsi (A2) dari sheet "Alamat" secara independen
        // (tidak tergantung cache main FetchAsync). Selalu hit network — payload sangat kecil.
        // Coba baca daftar nama barang dari sheet master dengan beberapa nama umum.
        // Kalau ketemu (kolom A bukan kosong), pakai itu. Kalau tidak, return list kosong
        // → caller bisa fallback ke extract dari data existing.
        private static readonly string[] MasterBarangSheetCandidates =
            { "Master", "MasterBarang", "Master Barang", "MASTER BARANG", "MASTER",
              "Validasi", "Validation", "DropDown", "Dropdown", "DROPDOWN",
              "List", "Daftar Barang", "Daftar", "Barang", "BARANG",
              "Items", "Item", "Material", "Materials", "MATERIAL", "Nama Barang" };

        public async Task<List<string>> FetchMasterBarangAsync()
        {
            try
            {
                var p = P;

                // 1. Override eksplisit via GidMasterBarang
                if (!string.IsNullOrWhiteSpace(p.GidMasterBarang))
                {
                    var csv = await TryGetStringAsync(Csv(p.SpreadsheetId, p.GidMasterBarang));
                    var items = ExtractColumnA(csv);
                    if (items.Count > 0) return items;
                }

                // 2. Sheet Aktual Stok (kolom A = NamaBarang) — sumber paling reliable
                if (!string.IsNullOrWhiteSpace(p.GidAktualStok))
                {
                    var csv = await TryGetStringAsync(Csv(p.SpreadsheetId, p.GidAktualStok));
                    var items = ExtractColumnA(csv);
                    if (items.Count > 0) return items;
                }

                // 3. Auto-detect via nama sheet umum
                foreach (var sheet in MasterBarangSheetCandidates)
                {
                    var csv = await TryGetStringAsync(GvizCsvSheetRange(p.SpreadsheetId, sheet, "A1:A1000"));
                    var items = ExtractColumnA(csv);
                    if (items.Count > 0) return items;
                }
            }
            catch { }
            return new();
        }

        // Skip header/total/junk rows. Only keep entries yang kemungkinan nama barang.
        private static readonly string[] _excludeKeywords =
        {
            "total", "grand", "kebutuhan", "diterima", "kekurangan",
            "nama barang", "namabarang", "material", "homebase", "wilayah",
            "no.", "no ", "fwa", "fiberisasi", "project", "header"
        };

        private static List<string> ExtractColumnA(string csv)
        {
            if (string.IsNullOrWhiteSpace(csv) || csv.TrimStart().StartsWith("<")) return new();
            var rows = ParseAllRows(csv);
            return rows
                .Select(r => C(r, 0).Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s)
                         && s.Length >= 3 && s.Length < 100
                         && !double.TryParse(s, out _) // bukan angka murni
                         && !_excludeKeywords.Any(k => s.Equals(k, StringComparison.OrdinalIgnoreCase)
                                                    || s.StartsWith(k + " ", StringComparison.OrdinalIgnoreCase)
                                                    || s.EndsWith(" " + k, StringComparison.OrdinalIgnoreCase)))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        // ── Remote Project Config Sync via URL ───────────────────────────
        // Fetch JSON ProjectConfig dari URL apapun (Google Drive, GitHub raw, dll).
        // Google Drive share link (https://drive.google.com/file/d/ID/view)
        // otomatis dikonversi ke direct download URL.
        public async Task<ProjectConfig?> FetchRemoteProjectConfigFromUrlAsync(string url)
        {
            try
            {
                var direct = ToDirectDownloadUrl(url);
                var json   = await TryGetStringAsync(direct);
                if (string.IsNullOrWhiteSpace(json) || !json.TrimStart().StartsWith("{")) return null;
                return System.Text.Json.JsonSerializer.Deserialize<ProjectConfig>(json,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch { return null; }
        }

        // Konversi berbagai format URL ke direct download
        private static string ToDirectDownloadUrl(string url)
        {
            // Google Drive: https://drive.google.com/file/d/FILE_ID/view?...
            var m = System.Text.RegularExpressions.Regex.Match(url,
                @"drive\.google\.com/file/d/([^/\?]+)");
            if (m.Success)
                return $"https://drive.google.com/uc?export=download&id={m.Groups[1].Value}";

            // Google Drive open: https://drive.google.com/open?id=FILE_ID
            m = System.Text.RegularExpressions.Regex.Match(url,
                @"drive\.google\.com/open\?id=([^&]+)");
            if (m.Success)
                return $"https://drive.google.com/uc?export=download&id={m.Groups[1].Value}";

            return url; // sudah direct atau format lain
        }

        // ── Remote Project Config Sync via Spreadsheet (legacy) ──────────
        public async Task<ProjectConfig?> FetchRemoteProjectConfigAsync(string spreadsheetId, string gidConfig)
        {
            try
            {
                string csv;
                if (!string.IsNullOrWhiteSpace(gidConfig))
                    csv = await TryGetStringAsync(Csv(spreadsheetId, gidConfig));
                else
                    csv = await TryGetStringAsync(GvizCsvSheetRange(spreadsheetId, "Config", "A1"));

                if (string.IsNullOrWhiteSpace(csv) || csv.TrimStart().StartsWith("<")) return null;

                var rows = ParseAllRows(csv);
                if (rows.Count == 0) return null;
                var json = C(rows[0], 0).Trim();
                if (string.IsNullOrWhiteSpace(json) || !json.StartsWith("{")) return null;

                return System.Text.Json.JsonSerializer.Deserialize<ProjectConfig>(json,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch { return null; }
        }

        public async Task<(string? Name, string? Desc)> FetchProjectAlamatAsync()
        {
            try
            {
                var p = P;
                string csv = "";

                if (!string.IsNullOrWhiteSpace(p.GidConfig))
                {
                    csv = await TryGetStringAsync(Csv(p.SpreadsheetId, p.GidConfig));
                }
                else
                {
                    // Coba variasi nama sheet umum, lalu fallback ke first-sheet
                    foreach (var sheetName in new[] { "Alamat", "ALAMAT", "alamat" })
                    {
                        csv = await TryGetStringAsync(GvizCsvSheetRange(p.SpreadsheetId, sheetName, "A1:A2"));
                        if (!string.IsNullOrWhiteSpace(csv) && !csv.TrimStart().StartsWith("<")) break;
                        csv = "";
                    }
                }
                if (string.IsNullOrWhiteSpace(csv)) return (null, null);
                if (csv.TrimStart().StartsWith("<")) return (null, null); // HTML error page

                var rows = ParseAllRows(csv);
                string? name = null, desc = null;
                if (rows.Count > 0) { var v = C(rows[0], 0); if (!string.IsNullOrWhiteSpace(v)) name = v.Trim(); }
                if (rows.Count > 1) { var v = C(rows[1], 0); if (!string.IsNullOrWhiteSpace(v)) desc = v.Trim(); }
                if (_cache != null)
                {
                    if (!string.IsNullOrWhiteSpace(name)) _cache.ProjectNameFromSheet = name;
                    if (!string.IsNullOrWhiteSpace(desc)) _cache.ProjectDescFromSheet = desc;
                }
                return (name, desc);
            }
            catch { return (null, null); }
        }

        public async Task<SheetData> FetchAsync(bool forceRefresh = false)
        {
            // 1. Return in-memory cache if available and no force refresh
            if (_cache != null && !forceRefresh) return _cache;

            // 2. Try disk cache if not forcing (offline-first)
            if (!forceRefresh)
            {
                var disk = await LoadFromDiskAsync();
                if (disk != null)
                {
                    _cache = disk;
                    if (!string.IsNullOrEmpty(disk.RawProgressCsv))
                        _csvProgressRaw = disk.RawProgressCsv;
                    return _cache;
                }
            }

            // 3. Download fresh from Google Sheets (TryGetStringAsync never throws)
            var p = P;
            var tStok    = TryGetStringAsync(Csv(p.SpreadsheetId, p.GidStok));
            var tSjCsv   = TryGetStringAsync(Csv(p.SpreadsheetId, p.GidSuratJalan));
            var tSjHtml  = TryGetStringAsync(GvizHtml(p.SpreadsheetId, p.GidSuratJalan));
            var tProg    = TryGetStringAsync(Csv(p.SpreadsheetId, p.GidProgress));
            var tResume  = TryGetStringAsync(Csv(p.ResumeSpreadsheetId, p.GidResume));
            var tAktual  = string.IsNullOrWhiteSpace(p.GidAktualStok)
                ? Task.FromResult(string.Empty)
                : TryGetStringAsync(Csv(p.SpreadsheetId, p.GidAktualStok));
            // Nama project (A1) & deskripsi (A2) dibaca dari sheet "Alamat".
            // GidConfig tetap di-honor sebagai override eksplisit.
            var tConfig  = !string.IsNullOrWhiteSpace(p.GidConfig)
                ? TryGetStringAsync(Csv(p.SpreadsheetId, p.GidConfig))
                : TryGetStringAsync(GvizCsvSheetRange(p.SpreadsheetId, "Alamat", "A1:A2"));

            await Task.WhenAll(tStok, tSjCsv, tSjHtml, tProg, tResume, tAktual, tConfig);

            var csvStok   = tStok.Result;
            var csvSJ     = tSjCsv.Result;
            var htmlSJ    = tSjHtml.Result;
            var csvProg   = tProg.Result;
            var csvResume = tResume.Result;
            var csvAktual = tAktual.Result;
            var csvConfig = tConfig.Result;

            // Offline: all empty → fall back to disk cache
            if (string.IsNullOrWhiteSpace(csvStok) && string.IsNullOrWhiteSpace(csvSJ) &&
                string.IsNullOrWhiteSpace(csvProg) && string.IsNullOrWhiteSpace(csvResume))
            {
                var disk = await LoadFromDiskAsync();
                if (disk != null) { _cache = disk; return _cache; }
                throw new Exception("Tidak ada koneksi internet dan belum ada data tersimpan.");
            }

            _csvProgressRaw = csvProg;

            foreach (var c in new[] { csvStok, csvSJ, csvProg, csvResume })
                if (!string.IsNullOrWhiteSpace(c) && c.TrimStart().StartsWith("<"))
                    throw new Exception("Sheet tidak dapat diakses. Pastikan sheet sudah public.");

            var (resumeList, resumeTotal) = ParseProgressResume(ParseAllRows(csvResume));
            var stokRows = ParseAllRows(csvStok);
            var aktualRows = string.IsNullOrWhiteSpace(csvAktual) ? new List<List<string>>() : ParseAllRows(csvAktual);
            List<StokGudangItem> gudangItems;
            List<GudangWarehouse> gudangWarehouses;
            if (aktualRows.Count > 0)
                (gudangItems, gudangWarehouses) = ParseStokGudangFull(aktualRows);
            else
                (gudangItems, gudangWarehouses) = (new List<StokGudangItem>(), new List<GudangWarehouse>());
            // Parse config sheet untuk sinkronisasi nama project
            string? projName = null, projDesc = null;
            if (!string.IsNullOrWhiteSpace(csvConfig))
            {
                var cfgRows = ParseAllRows(csvConfig);
                if (cfgRows.Count > 0) { var v = C(cfgRows[0], 0); if (!string.IsNullOrWhiteSpace(v)) projName = v.Trim(); }
                if (cfgRows.Count > 1) { var v = C(cfgRows[1], 0); if (!string.IsNullOrWhiteSpace(v)) projDesc = v.Trim(); }
            }

            _cache = new SheetData
            {
                SuratJalan          = ParseSuratJalan(ParseAllRows(csvSJ)),
                Progress            = ParseProgress(ParseAllRows(csvProg)),
                StokAktual          = ParseStokMRF(stokRows),
                StokGudang          = gudangItems,
                GudangWarehouses    = gudangWarehouses,
                ProgressResume      = resumeList,
                ResumeTotal         = resumeTotal,
                RawProgressCsv      = csvProg,
                SavedAt             = DateTime.Now,
                ProjectNameFromSheet = projName,
                ProjectDescFromSheet = projDesc,
            };

            // Warn user if some sheets returned empty (partial load)
            var missing = new List<string>();
            if (string.IsNullOrWhiteSpace(csvResume)) missing.Add("Resume Progress");
            if (string.IsNullOrWhiteSpace(csvProg))   missing.Add("Progress Harian");
            if (!string.IsNullOrWhiteSpace(P.GidAktualStok) && string.IsNullOrWhiteSpace(csvAktual))
                missing.Add("Stok Gudang");
            if (missing.Count > 0)
                _cache.LoadWarning = $"⚠ Sheet tidak tersedia: {string.Join(", ", missing)}";

            // 4. Persist to disk — replaces old cache file
            await SaveToDiskAsync(_cache);

            return _cache;
        }

        // ── Daily Progress per Segment ───────────────────────────────────
        public async Task<List<ProgressItem>> FetchDailyProgressAsync(string segmentRute, bool forceRefresh = false)
        {
            if (_csvProgressRaw == null || forceRefresh)
            {
                if (!forceRefresh && !string.IsNullOrEmpty(_cache?.RawProgressCsv))
                {
                    _csvProgressRaw = _cache.RawProgressCsv;
                }
                else
                {
                    _csvProgressRaw = await TryGetStringAsync(Csv(P.SpreadsheetId, P.GidProgress));
                    if (string.IsNullOrWhiteSpace(_csvProgressRaw))
                        throw new Exception("Tidak dapat memuat data progress. Periksa koneksi internet.");
                    if (_csvProgressRaw.TrimStart().StartsWith("<"))
                        throw new Exception("Sheet tidak dapat diakses. Pastikan sheet sudah public.");
                }
            }
            return ParseProgressForSegment(ParseAllRows(_csvProgressRaw), segmentRute);
        }

        private static List<ProgressItem> ParseProgressForSegment(List<List<string>> rows, string segmentRute)
        {
            var result = new List<ProgressItem>();
            foreach (var row in rows)
            {
                if (row.Count < 5) continue;
                var c0 = C(row, 0);
                if (!HariIndo.Any(d => c0.StartsWith(d, StringComparison.OrdinalIgnoreCase))) continue;
                var segment = C(row, 1);
                if (!MatchesSegment(segment, segmentRute)) continue;
                result.Add(new ProgressItem
                {
                    Tanggal    = c0,
                    SortDate   = ParseTanggalDate(c0),
                    Segment    = segment,
                    Span       = C(row, 2),
                    NamaBarang = C(row, 3),
                    Progres    = ParseInt(C(row, 4)),
                    Keterangan = C(row, 5),
                    Homebase   = C(row, 6)
                });
            }
            return result.OrderByDescending(x => x.SortDate).ToList();
        }

        private static bool MatchesSegment(string segment, string rute)
        {
            if (string.IsNullOrWhiteSpace(segment)) return false;
            if (segment.Equals(rute, StringComparison.OrdinalIgnoreCase)) return true;
            if (segment.Contains(rute, StringComparison.OrdinalIgnoreCase)) return true;
            if (rute.Contains(segment, StringComparison.OrdinalIgnoreCase)) return true;
            var keywords = rute.Split(new[] { " - ", " – ", "-" }, StringSplitOptions.RemoveEmptyEntries)
                               .Select(k => k.Trim()).Where(k => k.Length > 3);
            return keywords.Any(k => segment.Contains(k, StringComparison.OrdinalIgnoreCase));
        }

        private static DateTime ParseHariDate(string s)
        {
            var m = Regex.Match(s, @"\d{1,2}/\d{1,2}/\d{4}");
            if (m.Success &&
                DateTime.TryParseExact(m.Value, new[] { "d/M/yyyy", "dd/MM/yyyy" },
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var dt))
                return dt;
            return DateTime.MinValue;
        }

        // ── CSV Parser ──────────────────────────────────────────────────
        private static List<List<string>> ParseAllRows(string csv)
        {
            var result = new List<List<string>>();
            foreach (var l in SplitLines(csv))
                result.Add(ParseLine(l));
            return result;
        }

        private static List<string> SplitLines(string csv)
        {
            var lines = new List<string>();
            var sb    = new StringBuilder();
            bool inQ  = false;
            foreach (char c in csv)
            {
                if (c == '"') inQ = !inQ;
                if (c == '\n' && !inQ) { lines.Add(sb.ToString().TrimEnd('\r')); sb.Clear(); }
                else sb.Append(c);
            }
            if (sb.Length > 0) lines.Add(sb.ToString());
            return lines;
        }

        private static List<string> ParseLine(string line)
        {
            var f    = new List<string>();
            var sb   = new StringBuilder();
            bool inQ = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (inQ && i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                    else inQ = !inQ;
                }
                else if (c == ',' && !inQ) { f.Add(sb.ToString().Trim()); sb.Clear(); }
                else sb.Append(c);
            }
            f.Add(sb.ToString().Trim());
            return f;
        }

        private static string C(List<string> r, int i) => i < r.Count ? r[i].Trim() : "";

        // ── Surat Jalan ─────────────────────────────────────────────────
        private static readonly HashSet<string> JenisSet = new(StringComparer.OrdinalIgnoreCase)
        {
            "BARANG MASUK", "BARANG KELUAR", "DIBAWA MANDOR", "MUTASI", "KEMBALIAN MANDOR"
        };

        // Extract Drive URLs from gviz HTML export — col I (index 8) has HYPERLINK formulas
        private static Dictionary<string, string> ParseSuratJalanLinks(string html)
        {
            var links = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var rowRx  = new Regex(@"<tr[^>]*>(.*?)</tr>",       RegexOptions.Singleline | RegexOptions.IgnoreCase);
                var cellRx = new Regex(@"<t[dh][^>]*>(.*?)</t[dh]>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                var hrefRx = new Regex(@"href=""([^""]+)""",          RegexOptions.IgnoreCase);

                foreach (Match rowMatch in rowRx.Matches(html))
                {
                    var cells = cellRx.Matches(rowMatch.Groups[1].Value);
                    if (cells.Count < 9) continue;

                    var href = hrefRx.Match(cells[8].Groups[1].Value); // col I = index 8
                    if (!href.Success) continue;

                    var url = System.Net.WebUtility.HtmlDecode(href.Groups[1].Value);
                    // Unwrap Google redirector https://www.google.com/url?q=<actual>&…
                    if (url.StartsWith("https://www.google.com/url?q=", StringComparison.OrdinalIgnoreCase))
                    {
                        var q   = url[29..];
                        var amp = q.IndexOf('&');
                        if (amp > 0) q = q[..amp];
                        url = Uri.UnescapeDataString(q);
                    }
                    if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase)) continue;

                    var tanggal    = Regex.Replace(cells[0].Groups[1].Value, "<[^>]+>", "").Trim();
                    var namaBarang = Regex.Replace(cells[2].Groups[1].Value, "<[^>]+>", "").Trim();
                    var key        = $"{tanggal}|{namaBarang}";
                    if (!links.ContainsKey(key)) links[key] = url;
                }
            }
            catch { }
            return links;
        }

        private static List<SuratJalanItem> ParseSuratJalan(List<List<string>> rows)
        {
            var result = new List<SuratJalanItem>();

            // Forward-fill state: handle Excel merged cells (CSV export = kosong setelah baris pertama)
            string lastTanggal  = "";
            string lastSegment  = "";
            string lastNoSJ     = "";
            string lastPengirim = "";
            string lastPenerima = "";

            foreach (var row in rows)
            {
                if (row.Count < 5) continue;

                // Ambil nilai, gunakan nilai baris sebelumnya jika kosong (merged cell)
                var tanggal    = C(row, 0); if (string.IsNullOrWhiteSpace(tanggal))    tanggal    = lastTanggal;
                var segment    = C(row, 1); if (string.IsNullOrWhiteSpace(segment))    segment    = lastSegment;
                var namaBarang = C(row, 2);
                var jenis      = C(row, 4);

                if (!JenisSet.Contains(jenis)) continue;

                var noSJ     = C(row, 5); if (string.IsNullOrWhiteSpace(noSJ))     noSJ     = lastNoSJ;
                var pengirim = C(row, 6); if (string.IsNullOrWhiteSpace(pengirim)) pengirim = lastPengirim;
                var penerima = C(row, 7); if (string.IsNullOrWhiteSpace(penerima)) penerima = lastPenerima;

                // Update forward-fill state dengan nilai non-kosong
                if (!string.IsNullOrWhiteSpace(tanggal))    lastTanggal  = tanggal;
                if (!string.IsNullOrWhiteSpace(segment))    lastSegment  = segment;
                if (!string.IsNullOrWhiteSpace(noSJ))       lastNoSJ     = noSJ;
                if (!string.IsNullOrWhiteSpace(pengirim))   lastPengirim = pengirim;
                if (!string.IsNullOrWhiteSpace(penerima))   lastPenerima = penerima;

                // Cari URL Drive di col I(8), J(9), atau K(10)
                string driveUrl = "";
                foreach (var ci in new[] { 8, 9, 10 })
                {
                    var v = C(row, ci);
                    if (v.StartsWith("http", StringComparison.OrdinalIgnoreCase)) { driveUrl = v; break; }
                }

                result.Add(new SuratJalanItem
                {
                    Tanggal    = tanggal,
                    SortDate   = ParseTanggalDate(tanggal),
                    Segment    = segment,
                    NamaBarang = namaBarang,
                    Qty        = ParseInt(C(row, 3)),
                    Jenis      = jenis,
                    NoSJ       = noSJ,
                    Pengirim   = pengirim,
                    Penerima   = penerima,
                    Keterangan = C(row, 9),
                    DriveUrl   = driveUrl,
                });
            }
            return result.OrderByDescending(x => x.SortDate).ToList();
        }

        private static DateTime ParseTanggalDate(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return DateTime.MinValue;
            var clean = s.Trim();
            string[] fmts = { "d/M/yyyy","dd/MM/yyyy","yyyy-MM-dd","d-M-yyyy","dd-MM-yyyy",
                              "d MMM yyyy","dd MMM yyyy","d MMMM yyyy","dd MMMM yyyy" };

            var ci = System.Globalization.CultureInfo.InvariantCulture;
            var ds = System.Globalization.DateTimeStyles.None;

            // Direct parse
            if (DateTime.TryParseExact(clean, fmts, ci, ds, out var dt)) return dt;

            // With Indonesian month names replaced
            var eng = ReplaceIndonesianMonths(clean);
            if (DateTime.TryParseExact(eng, fmts, ci, ds, out var dt2)) return dt2;

            // Strip leading day name e.g. "Sabtu, " / "Senin, "
            var stripped = Regex.Replace(clean, @"^[A-Za-z]+,\s*", "").Trim();
            if (stripped != clean)
            {
                if (DateTime.TryParseExact(stripped, fmts, ci, ds, out var dt3)) return dt3;
                var strEng = ReplaceIndonesianMonths(stripped);
                if (DateTime.TryParseExact(strEng, fmts, ci, ds, out var dt4)) return dt4;

                // No year: "11 April" → append current year → "11 April 2026"
                var strEngYear = strEng + " " + DateTime.Today.Year;
                if (DateTime.TryParseExact(strEngYear, fmts, ci, ds, out var dt5)) return dt5;
                var strOrigYear = stripped + " " + DateTime.Today.Year;
                if (DateTime.TryParseExact(strOrigYear, fmts, ci, ds, out var dt6)) return dt6;
            }

            // Regex fallback: extract d/M/yyyy pattern
            var m = Regex.Match(clean, @"\b(\d{1,2})[/\-](\d{1,2})[/\-](\d{4})\b");
            if (m.Success && DateTime.TryParseExact(
                    $"{m.Groups[1].Value}/{m.Groups[2].Value}/{m.Groups[3].Value}",
                    new[] { "d/M/yyyy", "dd/MM/yyyy" }, ci, ds, out var dt7)) return dt7;

            return DateTime.MinValue;
        }

        private static string ReplaceIndonesianMonths(string s)
        {
            string[][] map = {
                new[]{"Januari","January"}, new[]{"Februari","February"}, new[]{"Maret","March"},
                new[]{"April","April"},     new[]{"Mei","May"},            new[]{"Juni","June"},
                new[]{"Juli","July"},       new[]{"Agustus","August"},    new[]{"September","September"},
                new[]{"Oktober","October"}, new[]{"November","November"}, new[]{"Desember","December"}
            };
            foreach (var m in map)
                s = s.Replace(m[0], m[1], StringComparison.OrdinalIgnoreCase);
            return s;
        }

        // ── Progress ────────────────────────────────────────────────────
        // Spreadsheet baru pakai hari Indonesia (Senin, Selasa, ...)
        private static readonly string[] HariIndo =
            { "Senin,", "Selasa,", "Rabu,", "Kamis,", "Jumat,", "Sabtu,", "Minggu," };

        private static List<ProgressItem> ParseProgress(List<List<string>> rows)
        {
            var result = new List<ProgressItem>();
            foreach (var row in rows)
            {
                if (row.Count < 5) continue;
                var c0      = C(row, 0);
                bool isHari = HariIndo.Any(d => c0.StartsWith(d, StringComparison.OrdinalIgnoreCase));
                if (!isHari) continue;

                var nama = C(row, 3);
                if (string.IsNullOrWhiteSpace(nama)) continue;

                int progres = ParseInt(C(row, 4));
                if (progres <= 0) continue;

                result.Add(new ProgressItem
                {
                    Tanggal    = c0,
                    SortDate   = ParseTanggalDate(c0),
                    Segment    = C(row, 1),
                    Span       = C(row, 2),
                    NamaBarang = nama,
                    Progres    = progres,
                    Keterangan = C(row, 5),
                    Homebase   = C(row, 6)
                });
            }
            return result;
        }

        // ── Stok MRF per Homebase ───────────────────────────────────────
        // Kolom berdasarkan header row CSV (0-based index):
        //   Kebutuhan  : K12C=5,  K24C=6,  T7=7,  T9=8,  SC=9, XF=10, CL=11, ODP=12, PG=13, CN=14, PC=15
        //   Terima     : K12C=18, K24C=19, T7=20, T9=21, SC=22,XF=23, CL=24, ODP=25, PG=26, CN=27, PC=28
        //   Kekurangan : K12C=31, K24C=32(AG),T7=33,T9=34,SC=35,XF=36,CL=37,ODP=38, PG=39, CN=40, PC=41
        // KABEL 12C = additional (tidak punya Kebutuhan & Kekurangan, hanya Terima)
        private static readonly HashSet<string> HomebaseSet = new(StringComparer.OrdinalIgnoreCase)
        {
            "BREBES", "TASIKMALAYA", "PURWOKERTO", "SUKOHARJO", "SRAGEN", "GROBOGAN"
        };

        private static List<StokHomebaseItem> ParseStokMRF(List<List<string>> rows)
        {
            var result = new List<StokHomebaseItem>();
            foreach (var row in rows)
            {
                if (row.Count < 42) continue;
                var homebase = C(row, 2).ToUpperInvariant();
                if (!HomebaseSet.Contains(homebase)) continue;

                result.Add(new StokHomebaseItem
                {
                    Homebase = homebase,
                    Segment  = C(row, 4),
                    // 10 material inti
                    K24C_Keb = Fmt(C(row,  6)), K24C_Ter = Fmt(C(row, 19)), K24C_Kek = Fmt(C(row, 32)),
                    T7_Keb   = Fmt(C(row,  7)), T7_Ter   = Fmt(C(row, 20)), T7_Kek   = Fmt(C(row, 33)),
                    T9_Keb   = Fmt(C(row,  8)), T9_Ter   = Fmt(C(row, 21)), T9_Kek   = Fmt(C(row, 34)),
                    SC_Keb   = Fmt(C(row,  9)), SC_Ter   = Fmt(C(row, 22)), SC_Kek   = Fmt(C(row, 35)),
                    XF_Keb   = Fmt(C(row, 10)), XF_Ter   = Fmt(C(row, 23)), XF_Kek   = Fmt(C(row, 36)),
                    CL_Keb   = Fmt(C(row, 11)), CL_Ter   = Fmt(C(row, 24)), CL_Kek   = Fmt(C(row, 37)),
                    ODP_Keb  = Fmt(C(row, 12)), ODP_Ter  = Fmt(C(row, 25)), ODP_Kek  = Fmt(C(row, 38)),
                    PG_Keb   = Fmt(C(row, 13)), PG_Ter   = Fmt(C(row, 26)), PG_Kek   = Fmt(C(row, 39)),
                    CN_Keb   = Fmt(C(row, 14)), CN_Ter   = Fmt(C(row, 27)), CN_Kek   = Fmt(C(row, 40)),
                    PC_Keb   = Fmt(C(row, 15)), PC_Ter   = Fmt(C(row, 28)), PC_Kek   = Fmt(C(row, 41)),
                    // additional — hanya terima
                    K12C_Ter = Fmt(C(row, 18)),
                });
            }
            return result;
        }

        // ── Stok Gudang: Diterima / Keluar / Implementasi per material ──────
        // Kolom (0-based):
        //   0=NamaBarang, 1-6=Diterima per wilayah, 7=DitTotal, 8=SisaGdg,
        //   9=KelTotal(J), 10=KelBrebes, 11=KelTasik, 12=KelPurwokerto,
        //   13=KelSukoharjo, 14=KelSragen, 15=KelGrobogan, 16=KelGrandTotal(Q),
        //   17=GapDitKel, 18=ImpTotal, 19=ImpBrebes, 20=GapBrebes,
        //   21=ImpTasik, 22=GapTasik, 23=ImpPurwokerto, 24=GapPurwokerto,
        //   25=ImpSukoharjo, 26=GapSukoharjo, 27=ImpSragen, 28=GapSragen,
        //   29=ImpGrobogan, 30=GapGrobogan
        private static readonly string[] WarehouseNames =
            { "Brebes", "Tasikmalaya", "Purwokerto", "Sukoharjo", "Sragen", "Grobogan" };
        private static readonly int[] WDitCols = { 1, 2, 3, 4, 5, 6 };
        private static readonly int[] WKelCols = { 10, 11, 12, 13, 14, 15 };
        private static readonly int[] WImpCols = { 19, 21, 23, 25, 27, 29 };
        private static readonly int[] WGapCols = { 20, 22, 24, 26, 28, 30 };

        private static (List<StokGudangItem>, List<GudangWarehouse>) ParseStokGudangFull(List<List<string>> rows)
        {
            var items       = new List<StokGudangItem>();
            var segNames    = new string[6];
            var wItems      = new List<GudangWarehouseItem>[6];
            for (int w = 0; w < 6; w++) wItems[w] = new List<GudangWarehouseItem>();

            foreach (var row in rows)
            {
                if (row.Count < 10) continue;
                var nama = C(row, 0).Trim();
                if (string.IsNullOrWhiteSpace(nama)) continue;

                // Header/title rows: try to extract segment names from cols 1-6
                if (nama.StartsWith("STOK", StringComparison.OrdinalIgnoreCase))
                {
                    for (int w = 0; w < 6; w++)
                    {
                        if (!string.IsNullOrWhiteSpace(segNames[w])) continue;
                        var cell = C(row, w + 1).Replace("\r", " ").Replace("\n", " ").Trim();
                        if (string.IsNullOrWhiteSpace(cell)) continue;
                        // Strip "STOK DITERIMA" prefix (case insensitive)
                        var cleaned = Regex.Replace(cell, @"(?i)^STOK\s+DITERIMA\s*", "").Trim();
                        if (!string.IsNullOrWhiteSpace(cleaned)) segNames[w] = cleaned;
                    }
                    continue;
                }

                // Skip rows where all key numeric fields are empty
                if (string.IsNullOrWhiteSpace(C(row, 7)) && string.IsNullOrWhiteSpace(C(row, 9))) continue;

                var item = new StokGudangItem
                {
                    NamaBarang    = nama,
                    DitLok1       = ParseInt(C(row,  1)),
                    DitLok2       = ParseInt(C(row,  2)),
                    DitLok3       = ParseInt(C(row,  3)),
                    DitLok4       = ParseInt(C(row,  4)),
                    DitLok5       = ParseInt(C(row,  5)),
                    DitLok6       = ParseInt(C(row,  6)),
                    DitTotal      = ParseInt(C(row,  7)),
                    SisaGdg       = ParseInt(C(row,  8)),
                    KelTotal      = ParseInt(C(row,  9)),
                    KelBrebes     = ParseInt(C(row, 10)),
                    KelTasik      = ParseInt(C(row, 11)),
                    KelPurwokerto = ParseInt(C(row, 12)),
                    KelSukoharjo  = ParseInt(C(row, 13)),
                    KelSragen     = ParseInt(C(row, 14)),
                    KelGrobogan   = ParseInt(C(row, 15)),
                    ImpTotal      = ParseInt(C(row, 18)),
                    ImpBrebes     = ParseInt(C(row, 19)), GapBrebes     = ParseInt(C(row, 20)),
                    ImpTasik      = ParseInt(C(row, 21)), GapTasik      = ParseInt(C(row, 22)),
                    ImpPurwokerto = ParseInt(C(row, 23)), GapPurwokerto = ParseInt(C(row, 24)),
                    ImpSukoharjo  = ParseInt(C(row, 25)), GapSukoharjo  = ParseInt(C(row, 26)),
                    ImpSragen     = ParseInt(C(row, 27)), GapSragen     = ParseInt(C(row, 28)),
                    ImpGrobogan   = ParseInt(C(row, 29)), GapGrobogan   = ParseInt(C(row, 30)),
                };
                items.Add(item);

// Also add per-warehouse item
for (int w = 0; w < 6; w++)
{
    var dit = GetValueOrDefault(row, WDitCols[w]);
    var kel = GetValueOrDefault(row, WKelCols[w]);
    var imp = GetValueOrDefault(row, WImpCols[w]);
    var gap = GetValueOrDefault(row, WGapCols[w]);
    wItems[w].Add(new GudangWarehouseItem
    {
        NamaBarang   = nama,
        Diterima     = dit,
        Keluar       = kel,
        Implementasi = imp,
        Gap          = gap > 0 ? gap : kel - imp,
    });
}
            }

            var warehouses = new List<GudangWarehouse>();
            for (int w = 0; w < 6; w++)
            {
                warehouses.Add(new GudangWarehouse
                {
                    Name        = WarehouseNames[w],
                    SegmentName = !string.IsNullOrWhiteSpace(segNames[w]) ? segNames[w] : WarehouseNames[w],
                    Items       = wItems[w],
                });
            }

            return (items, warehouses);
        }

        // ── Helpers ─────────────────────────────────────────────────────
        private static string Fmt(string s)
        {
            var t = s.Replace(" ", "").Trim();
            if (string.IsNullOrEmpty(t) || t == "-") return "-";
            // "48,000" (English) → "48.000" (Indonesian)
            return t.Replace(",", ".");
        }

private static int ParseInt(string s)
{
    var clean = s.Replace(".", "").Replace(",", "").Replace(" ", "").Trim();
    // Accounting-style negatives: "(25)" → -25
    if (clean.Length > 2 && clean[0] == '(' && clean[^1] == ')')
        clean = "-" + clean[1..^1];
    return int.TryParse(clean, out int v) ? v : 0;
}

private static int GetValueOrDefault(List<string> row, int index)
{
    if (row == null || index < 0 || row.Count <= index)
        return 0;
    return ParseInt(C(row, index));
}

// ── Span Detail per Segment (offline-first) ──────────────────────
private string SpanCacheFilePath(int segNo) =>
    Path.Combine(FileSystem.AppDataDirectory,
        $"span_{GetSafeHash(P.SpreadsheetId)}_{segNo}.json");

        public async Task<List<SpanItem>> FetchSpanAsync(int segmentNo, bool forceRefresh = false)
        {
            var path = SpanCacheFilePath(segmentNo);

            // 1. Try disk cache first if not forcing
            if (!forceRefresh && File.Exists(path))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(path);
                    var cached = JsonSerializer.Deserialize<List<SpanItem>>(json, _jsonOpts);
                    if (cached != null && cached.Count > 0) return cached;
                }
                catch { /* fall through to network */ }
            }

            // 2. Download fresh
            var key = segmentNo.ToString();
            if (!P.SegmentGids.TryGetValue(key, out var gid) || string.IsNullOrWhiteSpace(gid))
                throw new Exception($"GID Segment {segmentNo} belum dikonfigurasi.");

            var csv = await TryGetStringAsync(Csv(P.ResumeSpreadsheetId, gid));
            if (string.IsNullOrWhiteSpace(csv))
            {
                // Network failed → try disk cache as last resort
                if (File.Exists(path))
                {
                    try
                    {
                        var json = await File.ReadAllTextAsync(path);
                        var cached = JsonSerializer.Deserialize<List<SpanItem>>(json, _jsonOpts);
                        if (cached != null) return cached;
                    }
                    catch { }
                }
                throw new Exception("Tidak ada koneksi internet dan belum ada data span tersimpan.");
            }
            if (csv.TrimStart().StartsWith("<"))
                throw new Exception("Sheet tidak dapat diakses. Pastikan sheet sudah public.");

            var spans = ParseSpanRows(ParseAllRows(csv));

            // 3. Persist to disk
            try
            {
                var json = JsonSerializer.Serialize(spans, _jsonOpts);
                await File.WriteAllTextAsync(path, json);
            }
            catch { /* disk full / permission — ignore */ }

            return spans;
        }

        // Kolom: 0=empty, 1=No, 2=RUTE, 3=KAB, 4=KabelMP, 5=KabelPlan, 6=KabelProg, 7=Kabel%,
        //        8=T7MP, 9=T7Plan, 10=T7Prog, 11=T7%, 12=T9MP, 13=T9Plan, 14=T9Prog, 15=T9%,
        //        16=Status, 17=TimeLine
        private static List<SpanItem> ParseSpanRows(List<List<string>> rows)
        {
            var result = new List<SpanItem>();
            foreach (var row in rows)
            {
                if (row.Count < 16) continue;
                var noStr = C(row, 1).Trim();
                if (!int.TryParse(noStr, out int no)) continue;
                var rute = C(row, 2).Trim();
                if (string.IsNullOrWhiteSpace(rute)) continue;
                result.Add(new SpanItem
                {
                    No            = no,
                    Rute          = rute,
                    Kab           = C(row, 3).Trim(),
                    KabelPlan     = ParseInt(C(row, 5)),
                    KabelProgress = ParseInt(C(row, 6)),
                    KabelPct      = C(row, 7).Trim(),
                    T7Plan        = ParseInt(C(row, 9)),
                    T7Progress    = ParseInt(C(row, 10)),
                    T7Pct         = C(row, 11).Trim(),
                    T9Plan        = ParseInt(C(row, 13)),
                    T9Progress    = ParseInt(C(row, 14)),
                    T9Pct         = C(row, 15).Trim(),
                    Status        = C(row, 16).Trim(),
                    TimeLine      = row.Count > 17 ? C(row, 17).Trim() : "",
                });
            }
            return result;
        }

        // ── Progress Resume per Segment ──────────────────────────────────
        // Kolom: 0="", 1=No, 2=RUTE, 3=blank, 4=KabelPlan, 5=KabelProg, 6=Kabel%,
        //        7=blank, 8=T7Plan, 9=T7Prog, 10=T7%, 11=blank,
        //        12=T9Plan, 13=T9Prog, 14=T9%, 15=Status, 16=TimeLine
        private static (List<ProgressResumeItem>, ProgressResumeTotal) ParseProgressResume(List<List<string>> rows)
        {
            var result = new List<ProgressResumeItem>();
            var seen   = new HashSet<int>(); // deduplikasi — ambil kemunculan PERTAMA No 1-6
            ProgressResumeTotal? total = null;

            foreach (var row in rows)
            {
                if (row.Count < 15) continue;
                var noStr = C(row, 1).Trim();
                var rute  = C(row, 2).Trim();

                // Baris total: No kosong, RUTE berisi "RESUME..."
                if (string.IsNullOrEmpty(noStr) && rute.Contains("RESUME", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrEmpty(C(row, 4)))
                {
                    total = new ProgressResumeTotal
                    {
                        KabelPlan     = ParseInt(C(row, 4)),
                        KabelProgress = ParseInt(C(row, 5)),
                        KabelPct      = C(row, 6).Trim(),
                        T7Plan        = ParseInt(C(row, 8)),
                        T7Progress    = ParseInt(C(row, 9)),
                        T7Pct         = C(row, 10).Trim(),
                        T9Plan        = ParseInt(C(row, 12)),
                        T9Progress    = ParseInt(C(row, 13)),
                        T9Pct         = C(row, 14).Trim(),
                    };
                    continue;
                }

                // Baris data: No 1–6, RUTE tidak kosong
                // seen.Add() returns false jika No sudah ada → skip baris duplikat (baris 10, 11, dst)
                if (!int.TryParse(noStr, out int no) || no < 1) continue;
                if (string.IsNullOrWhiteSpace(rute)) continue;
                if (!seen.Add(no)) continue; // sudah ada → lewati

                result.Add(new ProgressResumeItem
                {
                    No            = no,
                    Rute          = rute,
                    KabelPlan     = ParseInt(C(row, 4)),
                    KabelProgress = ParseInt(C(row, 5)),
                    KabelPct      = C(row, 6).Trim(),
                    T7Plan        = ParseInt(C(row, 8)),
                    T7Progress    = ParseInt(C(row, 9)),
                    T7Pct         = C(row, 10).Trim(),
                    T9Plan        = ParseInt(C(row, 12)),
                    T9Progress    = ParseInt(C(row, 13)),
                    T9Pct         = C(row, 14).Trim(),
                    Status        = C(row, 15).Trim(),
                    TimeLine      = C(row, 16).Trim(),
                });
            }
            return (result, total ?? new ProgressResumeTotal());
        }
    }
}
