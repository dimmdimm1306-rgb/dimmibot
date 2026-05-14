using System.Text;
using System.Text.RegularExpressions;
using StokBarangMAUI.Models.Bot;
using StokBarangMAUI.Services.Mcp;

namespace StokBarangMAUI.Services.AiChat.BotFlows
{
    /// <summary>
    /// Flow 4: Surat Jalan.
    ///
    /// Multi-step pipeline:
    ///   - "sj minggu ini" / "sj bulan ini" → tanya filter (jenis & segment) dulu kalau hasil banyak
    ///   - "sj kemarin" / "sj-001" → langsung tampil
    ///   - Output vertical, lebar tetap, no horizontal scroll
    /// </summary>
    public class SuratJalanFlow : IBotFlow
    {
        private readonly McpClient _mcp;
        private readonly BotIntent _intent;
        private const int PAGE_SIZE = 15;
        private const int NARROW_THRESHOLD = 12;

        public SuratJalanFlow(McpClient mcp, BotIntent intent)
        {
            _mcp = mcp;
            _intent = intent;
        }

        public BotIntent Handles => _intent;
        public BotPattern Pattern => _intent == BotIntent.SuratJalanMenu ? BotPattern.VagueMenu : BotPattern.SingleShot;

        public async Task<BotResponse> ExecuteAsync(string userMessage, Dictionary<string, string> ctx)
        {
            if (!_mcp.IsEnabled) return BotResponse.Text_("⚠️ GDrive Reader belum aktif.");

            if (_intent == BotIntent.SuratJalanMenu)
                return BotResponse.Text_(BuildSjMenu());

            try
            {
                string? keyword = null;
                string? dateIntent = null;
                string label = "";

                switch (_intent)
                {
                    case BotIntent.SuratJalanDate:
                        ctx.TryGetValue("dateIntent", out dateIntent);
                        ctx.TryGetValue("day", out var day);
                        ctx.TryGetValue("month", out var month);
                        if (string.IsNullOrEmpty(dateIntent) && !string.IsNullOrEmpty(day))
                            keyword = string.IsNullOrEmpty(month) ? day : $"{day} {month}";
                        label = dateIntent switch
                        {
                            "date_today" => "hari ini",
                            "date_yesterday" => "kemarin",
                            "date_week" => "minggu ini",
                            "date_month" => "bulan ini",
                            _ => keyword ?? "tanggal"
                        };
                        // Ambiguous: "minggu ini" / "bulan ini" → langsung tanya filter
                        if (dateIntent == "date_week" || dateIntent == "date_month")
                            return await AskFilter(label, dateIntent, null);
                        break;

                    case BotIntent.SuratJalanJenis:
                        ctx.TryGetValue("jenis", out keyword);
                        label = $"jenis {keyword}";
                        break;

                    case BotIntent.SuratJalanNomor:
                        ctx.TryGetValue("nomor", out keyword);
                        label = $"no SJ {keyword}";
                        break;

                    case BotIntent.SuratJalanOrang:
                        ctx.TryGetValue("orang", out keyword);
                        label = $"orang {keyword}";
                        break;

                    case BotIntent.SuratJalanLatest:
                        return await FetchLatestSjAsync();
                }

                return await FetchAndShow(label, dateIntent, keyword, jenisFilter: null, segmentFilter: null);
            }
            catch (Exception ex) { return BotResponse.Text_($"❌ {ex.Message}"); }
        }

        public async Task<BotResponse?> ResumeAsync(string userMessage, BotPendingState state)
        {
            var step = state.Step;
            var lower = userMessage.Trim().ToLowerInvariant();

            switch (step)
            {
                case "askFilter":
                    return await HandleFilterAnswer(userMessage, state);

                case "paginate":
                    if (!Regex.IsMatch(lower, @"^(lanjut|lanjutkan|next|berikut(nya)?|more|lainnya)\s*$"))
                        return null;
                    int page = state.GetInt("page", 0) + 1;
                    return await ShowPage(state, page);
            }

            return null;
        }

        // ── Step: Ask filter (jenis / segment) ──────────────────────

        private async Task<BotResponse> AskFilter(string label, string? dateIntent, string? keyword)
        {
            BotState.Save(nameof(BotIntent.SuratJalanDate), "askFilter",
                new Dictionary<string, string>
                {
                    ["label"] = label,
                    ["dateIntent"] = dateIntent ?? "",
                    ["keyword"] = keyword ?? "",
                });

            var sb = new StringBuilder();
            sb.AppendLine($"📜 SJ `{label}` biasanya banyak.");
            sb.AppendLine("Mau filter berdasarkan apa?");
            sb.AppendLine();
            sb.AppendLine("Pilih atau ketik:");
            sb.AppendLine("  📥 `masuk`    — yang masuk gudang");
            sb.AppendLine("  📤 `keluar`   — yang keluar");
            sb.AppendLine("  🚚 `dibawa`   — yang dibawa orang");
            sb.AppendLine("  🗂  Nama segment, misal `brebes`, `sragen`");
            sb.AppendLine("  📦 Nama material, misal `kabel`, `tiang`");
            sb.AppendLine("  *  `semua` — tampilkan semua");

            return await Task.FromResult(
                new BotResponse { Text = sb.ToString().TrimEnd(), HasPendingState = true });
        }

        private async Task<BotResponse?> HandleFilterAnswer(string input, BotPendingState state)
        {
            var label = state.Get("label") ?? "";
            var dateIntent = state.Get("dateIntent");
            var keyword = state.Get("keyword");
            var lower = input.Trim().ToLowerInvariant();

            string? jenisFilter = null;
            string? segmentFilter = null;
            string? materialFilter = null;

            // "semua" / "all" / "*"
            if (lower == "semua" || lower == "all" || lower == "*" || lower == "skip")
            {
                // no filter
            }
            else if (Regex.IsMatch(lower, @"\b(masuk|keluar|dibawa)\b"))
            {
                jenisFilter = Regex.Match(lower, @"\b(masuk|keluar|dibawa)\b").Value.ToUpperInvariant();
            }
            else
            {
                // Coba sebagai segment
                segmentFilter = DataSchema.ResolveSegmentFromText(input);

                // Kalau bukan segment, anggap material keyword
                if (segmentFilter == null)
                    materialFilter = input.Trim();
            }

            return await FetchAndShow(label, dateIntent, keyword,
                jenisFilter: jenisFilter,
                segmentFilter: segmentFilter,
                materialFilter: materialFilter);
        }

        // ── Fetch + show ────────────────────────────────────────────

        private async Task<BotResponse> FetchAndShow(string label, string? dateIntent, string? keyword,
            string? jenisFilter = null, string? segmentFilter = null, string? materialFilter = null)
        {
            try
            {
                // Kalau ada material filter, kirim ke server-side search
                var serverKeyword = !string.IsNullOrEmpty(materialFilter) ? materialFilter : keyword;

                var result = await _mcp.SearchSuratJalanAsync(serverKeyword, dateIntent, 200);
                if (result?.Data == null || result.Data.Count == 0)
                {
                    BotState.Clear();
                    return BotResponse.Text_($"📭 Tidak ada SJ yang cocok.\n\n💡 Coba: `sj kemarin`, `sj-001`, `sj terakhir`");
                }

                var rows = result.Data;

                // Apply client-side filters
                if (!string.IsNullOrEmpty(jenisFilter))
                {
                    rows = rows.Where(r => BotFormatters.FindCol(r, "Jenis")
                        .Contains(jenisFilter, StringComparison.OrdinalIgnoreCase)).ToList();
                }
                if (!string.IsNullOrEmpty(segmentFilter))
                {
                    rows = rows.Where(r =>
                    {
                        var seg = BotFormatters.FindCol(r, "Segment");
                        var resolved = DataSchema.ResolveSegmentFromText(seg);
                        return resolved == segmentFilter ||
                               seg.Contains(segmentFilter, StringComparison.OrdinalIgnoreCase);
                    }).ToList();
                }

                if (rows.Count == 0)
                {
                    BotState.Clear();
                    return BotResponse.Text_("📭 Tidak ada SJ yang cocok dengan filter itu.");
                }

                // Auto-ask filter kalau hasil banyak DAN belum ada filter
                bool noFilter = string.IsNullOrEmpty(jenisFilter) &&
                                string.IsNullOrEmpty(segmentFilter) &&
                                string.IsNullOrEmpty(materialFilter);
                if (noFilter && rows.Count > NARROW_THRESHOLD)
                    return await AskFilter(label, dateIntent, keyword);

                return ShowSjPage(label, rows, page: 0, dateIntent, keyword,
                    jenisFilter, segmentFilter, materialFilter);
            }
            catch (Exception ex)
            {
                BotState.Clear();
                return BotResponse.Text_($"❌ {ex.Message}");
            }
        }

        private async Task<BotResponse?> ShowPage(BotPendingState state, int page)
        {
            var label = state.Get("label") ?? "";
            var dateIntent = state.Get("dateIntent");
            var keyword = state.Get("keyword");
            var jenisFilter = state.Get("jenis");
            var segmentFilter = state.Get("segment");
            var materialFilter = state.Get("material");

            try
            {
                var serverKeyword = !string.IsNullOrEmpty(materialFilter) ? materialFilter : keyword;
                var result = await _mcp.SearchSuratJalanAsync(serverKeyword, dateIntent, 200);
                if (result?.Data == null) return BotResponse.Text_("📭 Data tidak tersedia lagi.");

                var rows = result.Data;
                if (!string.IsNullOrEmpty(jenisFilter))
                    rows = rows.Where(r => BotFormatters.FindCol(r, "Jenis")
                        .Contains(jenisFilter, StringComparison.OrdinalIgnoreCase)).ToList();
                if (!string.IsNullOrEmpty(segmentFilter))
                    rows = rows.Where(r =>
                    {
                        var seg = BotFormatters.FindCol(r, "Segment");
                        var resolved = DataSchema.ResolveSegmentFromText(seg);
                        return resolved == segmentFilter ||
                               seg.Contains(segmentFilter, StringComparison.OrdinalIgnoreCase);
                    }).ToList();

                return ShowSjPage(label, rows, page, dateIntent, keyword,
                    jenisFilter, segmentFilter, materialFilter);
            }
            catch (Exception ex) { return BotResponse.Text_($"❌ {ex.Message}"); }
        }

        // ── Format ──────────────────────────────────────────────────

        private static BotResponse ShowSjPage(string label, List<Dictionary<string, object>> rows,
            int page, string? dateIntent, string? keyword,
            string? jenisFilter, string? segmentFilter, string? materialFilter)
        {
            int totalPages = Math.Max(1, (int)Math.Ceiling(rows.Count / (double)PAGE_SIZE));
            if (page >= totalPages)
            {
                BotState.Clear();
                return BotResponse.Text_("📭 Sudah semua SJ ditampilkan.");
            }

            var slice = rows.Skip(page * PAGE_SIZE).Take(PAGE_SIZE).ToList();
            var text = FormatSjVertical(label, slice, page, totalPages, rows.Count,
                jenisFilter, segmentFilter, materialFilter);

            bool hasMore = page < totalPages - 1;
            if (hasMore)
            {
                BotState.Save(nameof(BotIntent.SuratJalanDate), "paginate",
                    new Dictionary<string, string>
                    {
                        ["label"] = label,
                        ["page"] = page.ToString(),
                        ["dateIntent"] = dateIntent ?? "",
                        ["keyword"] = keyword ?? "",
                        ["jenis"] = jenisFilter ?? "",
                        ["segment"] = segmentFilter ?? "",
                        ["material"] = materialFilter ?? "",
                    });
                return new BotResponse { Text = text, HasPendingState = true };
            }

            BotState.Clear();
            return BotResponse.Text_(text);
        }

        private async Task<BotResponse> FetchLatestSjAsync()
        {
            var result = await _mcp.SearchSuratJalanAsync(null, null, 200);
            if (result?.Data == null || result.Data.Count == 0)
                return BotResponse.Text_("📭 Belum ada surat jalan tercatat.");

            var sorted = result.Data
                .Select(r => new { Row = r, Date = ParseSjDate(BotFormatters.FindCol(r, "Tanggal")) })
                .Where(x => x.Date.HasValue)
                .OrderByDescending(x => x.Date!.Value)
                .Take(5)
                .Select(x => x.Row)
                .ToList();

            if (sorted.Count == 0) sorted = result.Data.Take(5).ToList();
            return BotResponse.Text_(FormatSjVertical("5 SJ terakhir", sorted, 0, 1, sorted.Count, null, null, null));
        }

        private static string FormatSjVertical(string label, List<Dictionary<string, object>> rows,
            int page, int totalPages, int totalCount,
            string? jenisFilter, string? segmentFilter, string? materialFilter)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"📜 SURAT JALAN — {label.ToUpperInvariant()}");
            // Tampilkan filter aktif kalau ada
            var filters = new List<string>();
            if (!string.IsNullOrEmpty(jenisFilter)) filters.Add($"jenis: {jenisFilter}");
            if (!string.IsNullOrEmpty(segmentFilter)) filters.Add($"segment: {segmentFilter}");
            if (!string.IsNullOrEmpty(materialFilter)) filters.Add($"material: {materialFilter}");
            if (filters.Count > 0) sb.AppendLine($"🔎 Filter: {string.Join(" · ", filters)}");
            sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━");
            sb.Append($"📊 {totalCount} SJ");
            if (totalPages > 1) sb.Append($" · Hal {page + 1}/{totalPages}");
            sb.AppendLine();
            sb.AppendLine();

            foreach (var row in rows)
            {
                var tanggal = BotFormatters.FindCol(row, "Tanggal");
                var segment = BotFormatters.FindCol(row, "Segment");
                var barang = BotFormatters.FindCol(row, "Nama Barang");
                var qty = BotFormatters.FindCol(row, "QTY");
                var jenis = BotFormatters.FindCol(row, "Jenis");
                var noSj = BotFormatters.FindCol(row, "NO_SJ");
                var pengirim = BotFormatters.FindCol(row, "PENGIRIM");
                var penerima = BotFormatters.FindCol(row, "PENERIMA");
                var ket = BotFormatters.FindCol(row, "Keterangan");

                var icon = JenisIcon(jenis);
                sb.AppendLine($"{icon} {jenis}" + (noSj != "-" ? $"  ·  No: {noSj}" : ""));
                if (tanggal != "-") sb.AppendLine($"   📅 {tanggal}");
                if (segment != "-") sb.AppendLine($"   🗂  {segment}");
                sb.AppendLine($"   📦 {barang} × {qty}");
                if (pengirim != "-" && penerima != "-")
                    sb.AppendLine($"   👤 {pengirim} → {penerima}");
                else if (pengirim != "-")
                    sb.AppendLine($"   👤 Dari: {pengirim}");
                else if (penerima != "-")
                    sb.AppendLine($"   👤 Ke: {penerima}");
                if (ket != "-" && !string.IsNullOrWhiteSpace(ket))
                    sb.AppendLine($"   📝 {ket}");
                sb.AppendLine();
            }

            int remaining = totalCount - (page + 1) * PAGE_SIZE;
            if (remaining > 0)
                sb.AppendLine($"📄 +{remaining} SJ lagi · ketik `lanjut`");

            return sb.ToString().TrimEnd();
        }

        // ── Helpers ─────────────────────────────────────────────────

        private static string JenisIcon(string jenis)
        {
            var j = jenis.ToUpperInvariant();
            if (j.Contains("MASUK")) return "📥";
            if (j.Contains("KELUAR")) return "📤";
            if (j.Contains("DIBAWA")) return "🚚";
            return "📋";
        }

        private static DateTime? ParseSjDate(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw) || raw == "-") return null;
            raw = raw.Trim();

            if (DateTime.TryParse(raw, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeLocal, out var d1)) return d1;

            var idCulture = new System.Globalization.CultureInfo("id-ID");
            if (DateTime.TryParse(raw, idCulture,
                System.Globalization.DateTimeStyles.AssumeLocal, out var d2)) return d2;

            var months = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                {"januari",1},{"jan",1},{"februari",2},{"feb",2},{"maret",3},{"mar",3},
                {"april",4},{"apr",4},{"mei",5},{"juni",6},{"jun",6},{"juli",7},{"jul",7},
                {"agustus",8},{"agu",8},{"agt",8},{"september",9},{"sep",9},{"sept",9},
                {"oktober",10},{"okt",10},{"november",11},{"nov",11},{"desember",12},{"des",12},
            };
            var parts = raw.Split(new[] { ' ', '-', '/', '.' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 3 &&
                int.TryParse(parts[0], out var dd) &&
                months.TryGetValue(parts[1], out var mm) &&
                int.TryParse(parts[2], out var yy))
            {
                if (yy < 100) yy += 2000;
                if (dd >= 1 && dd <= 31 && yy >= 2020 && yy <= 2100)
                {
                    try { return new DateTime(yy, mm, dd); } catch { }
                }
            }
            return null;
        }

        private static string BuildSjMenu()
        {
            return "📜 SURAT JALAN — Pilih:\n\n" +
                   "  • `sj terakhir` (5 SJ paling baru)\n" +
                   "  • `sj kemarin` / `sj hari ini`\n" +
                   "  • `sj minggu ini` / `sj bulan ini` (bot tanya filter)\n" +
                   "  • `sj masuk` / `sj keluar` / `sj dibawa`\n" +
                   "  • `sj-001` (cari nomor)\n\n" +
                   "Atau ketik langsung keyword.";
        }
    }
}
