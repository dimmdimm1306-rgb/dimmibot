using System.Text;
using System.Text.RegularExpressions;
using StokBarangMAUI.Models.Bot;
using StokBarangMAUI.Services.Mcp;

namespace StokBarangMAUI.Services.AiChat.BotFlows
{
    /// <summary>
    /// Flow 1c: Progres by tanggal.
    ///
    /// Multi-step pipeline:
    ///   1. User input: "tanggal 17" (no month) → tanya bulan + tahun
    ///   2. Fetch result. Kalau >10 aktivitas → tanya segment dulu sebelum tampil
    ///   3. Filter & paginate (25/page, ketik `lanjut`)
    ///
    /// Format vertical: rute jadi judul, meta (tgl/segment/site) tiap di baris sendiri,
    /// material list di bawah. Lebar tetap, no horizontal scroll.
    /// </summary>
    public class ProgresDateFlow : IBotFlow
    {
        private readonly McpClient _mcp;
        private const int PAGE_SIZE = 25;
        private const int NARROW_THRESHOLD = 10; // di atas ini, tanya filter dulu

        public ProgresDateFlow(McpClient mcp) { _mcp = mcp; }

        public BotIntent Handles => BotIntent.ProgresDate;
        public BotPattern Pattern => BotPattern.SingleShot;

        public async Task<BotResponse> ExecuteAsync(string userMessage, Dictionary<string, string> ctx)
        {
            if (!_mcp.IsEnabled)
                return BotResponse.Text_("⚠️ GDrive Reader belum aktif.");

            ctx.TryGetValue("dateIntent", out var dateIntent);
            ctx.TryGetValue("day", out var dayStr);
            ctx.TryGetValue("month", out var month);

            string? keyword = null;
            string label;

            if (!string.IsNullOrEmpty(dateIntent))
            {
                label = dateIntent switch
                {
                    "date_today" => "hari ini",
                    "date_yesterday" => "kemarin",
                    "date_week" => "7 hari terakhir",
                    "date_month" => "bulan ini",
                    _ => "tanggal"
                };

                // Time-aware: "hari ini" jam 00-05 → suggest kemarin
                if (dateIntent == "date_today")
                {
                    var wib = GetWibNow();
                    if (wib.Hour < 5)
                    {
                        return BotResponse.Text_(
                            $"🌙 Jam {wib:HH:mm} WIB — progress biasanya diinput mulai jam 5 pagi.\n\n" +
                            "Coba: `progres kemarin` atau `tanggal " + wib.AddDays(-1).Day + "`");
                    }
                }

                // "minggu ini" / "bulan ini" → langsung ask filter segment dulu (kemungkinan banyak)
                if (dateIntent == "date_week" || dateIntent == "date_month")
                    return await AskSegmentFilter(label, dateIntent, null);
            }
            else if (!string.IsNullOrEmpty(dayStr))
            {
                // "tanggal 17" tanpa bulan/tahun → tanya bulan + tahun dulu
                if (string.IsNullOrEmpty(month))
                    return AskMonthYear(dayStr);

                keyword = $"{dayStr} {month}";
                label = $"tanggal {keyword}";
            }
            else
            {
                return BotResponse.Text_(
                    "📅 Format tanggal:\n" +
                    "  • `progres kemarin` / `progres hari ini`\n" +
                    "  • `progres tanggal 17 mei`\n" +
                    "  • `progres minggu ini` (bot akan tanya segment)\n");
            }

            return await FetchAndShow(label, dateIntent, keyword, segmentFilter: null);
        }

        public async Task<BotResponse?> ResumeAsync(string userMessage, BotPendingState state)
        {
            var step = state.Step;
            var lower = userMessage.Trim().ToLowerInvariant();

            switch (step)
            {
                case "askMonthYear":
                    return await HandleMonthYearAnswer(userMessage, state);

                case "askSegment":
                    return await HandleSegmentAnswer(userMessage, state);

                case "paginate":
                    if (!Regex.IsMatch(lower, @"^(lanjut|lanjutkan|next|berikut(nya)?|more|lainnya)\s*$"))
                        return null;
                    int page = state.GetInt("page", 0) + 1;
                    return await ShowPage(state, page);
            }

            return null;
        }

        // ── Step 1: Ask month + year ────────────────────────────────

        private static BotResponse AskMonthYear(string day)
        {
            var wib = GetWibNow();
            BotState.Save(nameof(BotIntent.ProgresDate), "askMonthYear",
                new Dictionary<string, string> { ["day"] = day });

            var sb = new StringBuilder();
            sb.AppendLine($"📅 Tanggal {day} bulan apa?");
            sb.AppendLine();
            sb.AppendLine("Contoh jawaban:");
            sb.AppendLine($"  • `mei 2026`");
            sb.AppendLine($"  • `mei` (asumsi tahun ini)");
            sb.AppendLine($"  • `bulan ini` (= {wib:MMMM yyyy})");
            sb.AppendLine($"  • `bulan kemarin`");
            return new BotResponse { Text = sb.ToString().TrimEnd(), HasPendingState = true };
        }

        private async Task<BotResponse?> HandleMonthYearAnswer(string input, BotPendingState state)
        {
            var day = state.Get("day") ?? "";
            var lower = input.Trim().ToLowerInvariant();
            var wib = GetWibNow();

            string? month = null;
            int year = wib.Year;

            // "bulan ini"
            if (Regex.IsMatch(lower, @"\bbulan\s+ini\b"))
            {
                month = MonthName(wib.Month);
                year = wib.Year;
            }
            // "bulan kemarin"
            else if (Regex.IsMatch(lower, @"\bbulan\s+(kemarin|lalu|sebelum)"))
            {
                var prev = wib.AddMonths(-1);
                month = MonthName(prev.Month);
                year = prev.Year;
            }
            else
            {
                // Cari nama bulan
                month = BotTokens.FindMonth(input);
                // Cari tahun (4 digit)
                var ym = Regex.Match(input, @"\b(20\d{2})\b");
                if (ym.Success) int.TryParse(ym.Groups[1].Value, out year);
            }

            if (string.IsNullOrEmpty(month))
                return null; // gak match → tetap di state, biarin user re-input

            var keyword = $"{day} {month} {year}";
            var label = $"tanggal {day} {month} {year}";
            return await FetchAndShow(label, dateIntent: null, keyword: keyword, segmentFilter: null);
        }

        // ── Step 2: Ask segment filter (untuk hasil yang banyak) ─────

        private static async Task<BotResponse> AskSegmentFilter(string label, string? dateIntent, string? keyword)
        {
            BotState.Save(nameof(BotIntent.ProgresDate), "askSegment",
                new Dictionary<string, string>
                {
                    ["label"] = label,
                    ["dateIntent"] = dateIntent ?? "",
                    ["keyword"] = keyword ?? "",
                });

            var sb = new StringBuilder();
            sb.AppendLine($"🔎 Progres `{label}` biasanya banyak.");
            sb.AppendLine("Pilih segment dulu biar lebih spesifik:");
            sb.AppendLine();
            for (int i = 0; i < DataSchema.Segments.Length; i++)
                sb.AppendLine($"  {i + 1}. {DataSchema.Segments[i]}");
            sb.AppendLine($"  7. SEMUA segment");
            sb.AppendLine();
            sb.AppendLine("👉 Ketik nomor (1-7) atau nama segment.");

            return await Task.FromResult(
                new BotResponse { Text = sb.ToString().TrimEnd(), HasPendingState = true });
        }

        private async Task<BotResponse?> HandleSegmentAnswer(string input, BotPendingState state)
        {
            var label = state.Get("label") ?? "tanggal";
            var dateIntent = state.Get("dateIntent");
            var keyword = state.Get("keyword");
            var lower = input.Trim().ToLowerInvariant();

            string? segFilter = null;

            // "7" / "semua" / "all" → no filter
            if (lower == "7" || lower == "semua" || lower == "all" || lower == "*")
            {
                segFilter = null;
            }
            else if (int.TryParse(lower, out var num) && num >= 1 && num <= 6)
            {
                segFilter = DataSchema.Segments[num - 1];
            }
            else
            {
                segFilter = DataSchema.ResolveSegmentFromText(input);
                if (segFilter == null) return null; // gak match → biar user re-input
            }

            return await FetchAndShow(label, dateIntent, keyword,
                segmentFilter: segFilter);
        }

        // ── Step 3: Fetch + show ────────────────────────────────────

        private async Task<BotResponse> FetchAndShow(string label, string? dateIntent, string? keyword, string? segmentFilter)
        {
            try
            {
                var result = await _mcp.SearchProgressAsync(keyword, dateIntent, 300);
                if (result?.Data == null || result.Data.Count == 0)
                {
                    BotState.Clear();
                    return BotResponse.Text_($"📅 Tidak ada progres untuk `{label}`.");
                }

                var rows = result.Data;

                // Apply segment filter (client-side)
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
                    return BotResponse.Text_($"📅 Tidak ada progres `{label}` di segment {segmentFilter}.");
                }

                var grouped = GroupActivities(rows);

                // Auto-ask filter kalau hasil terlalu banyak (>10 aktivitas) dan belum di-filter
                if (string.IsNullOrEmpty(segmentFilter) && grouped.Count > NARROW_THRESHOLD)
                    return await AskSegmentFilter(label, dateIntent, keyword);

                return ShowResultsPage(label, grouped, page: 0, dateIntent, keyword, segmentFilter, rows.Count);
            }
            catch (Exception ex)
            {
                BotState.Clear();
                return BotResponse.Text_($"❌ Gagal: {ex.Message}");
            }
        }

        private async Task<BotResponse?> ShowPage(BotPendingState state, int page)
        {
            var label = state.Get("label") ?? "tanggal";
            var dateIntent = state.Get("dateIntent");
            var keyword = state.Get("keyword");
            var segmentFilter = state.Get("segment");

            try
            {
                var result = await _mcp.SearchProgressAsync(keyword, dateIntent, 300);
                if (result?.Data == null) return BotResponse.Text_("📭 Data tidak tersedia lagi.");

                var rows = result.Data;
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

                var grouped = GroupActivities(rows);
                return ShowResultsPage(label, grouped, page, dateIntent, keyword, segmentFilter, rows.Count);
            }
            catch (Exception ex) { return BotResponse.Text_($"❌ {ex.Message}"); }
        }

        // ── Format helpers ──────────────────────────────────────────

        private static BotResponse ShowResultsPage(string label, List<GroupedActivity> grouped,
            int page, string? dateIntent, string? keyword, string? segmentFilter, int totalRows)
        {
            int totalPages = Math.Max(1, (int)Math.Ceiling(grouped.Count / (double)PAGE_SIZE));
            if (page >= totalPages)
            {
                BotState.Clear();
                return BotResponse.Text_("📭 Sudah semua aktivitas ditampilkan.");
            }

            var slice = grouped.Skip(page * PAGE_SIZE).Take(PAGE_SIZE).ToList();
            var text = FormatPage(label, segmentFilter, slice, page, totalPages, grouped.Count, totalRows);

            bool hasMore = page < totalPages - 1;
            if (hasMore)
            {
                BotState.Save(nameof(BotIntent.ProgresDate), "paginate",
                    new Dictionary<string, string>
                    {
                        ["label"] = label,
                        ["page"] = page.ToString(),
                        ["dateIntent"] = dateIntent ?? "",
                        ["keyword"] = keyword ?? "",
                        ["segment"] = segmentFilter ?? "",
                    });
                return new BotResponse { Text = text, HasPendingState = true };
            }

            BotState.Clear();
            return BotResponse.Text_(text);
        }

        private static string FormatPage(string label, string? segmentFilter, List<GroupedActivity> activities,
            int page, int totalPages, int totalActivities, int totalRows)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"📅 PROGRES — {label.ToUpperInvariant()}");
            if (!string.IsNullOrEmpty(segmentFilter))
                sb.AppendLine($"🗂  Segment: {segmentFilter}");
            sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━");
            sb.Append($"📊 {totalActivities} aktivitas · {totalRows} entri");
            if (totalPages > 1) sb.Append($" · Hal {page + 1}/{totalPages}");
            sb.AppendLine();
            sb.AppendLine();

            foreach (var g in activities)
            {
                // Header rute (tebal, sendirian)
                sb.AppendLine($"📌 {g.Rute}");

                // Tanggal di baris sendiri
                if (g.Tanggal != "-") sb.AppendLine($"   📅 {g.Tanggal}");

                // Segment di baris sendiri
                if (g.Segment != "-") sb.AppendLine($"   🗂  {g.Segment}");

                // Site di baris sendiri
                if (g.Site != "-") sb.AppendLine($"   🆔 Site {g.Site}");

                // Lokasi (homebase + kab)
                if (g.Homebase != "-" || g.Kab != "-")
                {
                    var loc = new List<string>();
                    if (g.Homebase != "-") loc.Add(g.Homebase);
                    if (g.Kab != "-") loc.Add(g.Kab);
                    sb.AppendLine($"   📍 {string.Join(" · ", loc)}");
                }

                // Materials
                var mats = g.Materials.Where(m => m.Item1 != "-").ToList();
                if (mats.Count > 0)
                {
                    sb.AppendLine($"   ─────────────");
                    foreach (var (barang, progres, ket) in mats)
                    {
                        var line = $"   • {barang}: {progres}";
                        if (ket != "-" && !string.IsNullOrWhiteSpace(ket))
                            line += $"  ({ket})";
                        sb.AppendLine(line);
                    }
                }
                sb.AppendLine();
            }

            int remaining = totalActivities - (page + 1) * PAGE_SIZE;
            if (remaining > 0)
                sb.AppendLine($"📄 +{remaining} aktivitas lagi · ketik `lanjut`");

            return sb.ToString().TrimEnd();
        }

        private static List<GroupedActivity> GroupActivities(List<Dictionary<string, object>> rows)
        {
            return rows
                .Select(r => new
                {
                    Tanggal = BotFormatters.FindCol(r, "Tanggal"),
                    Segment = BotFormatters.FindCol(r, "Segment"),
                    Rute    = BotFormatters.FindCol(r, "Rute"),
                    Site    = BotFormatters.FindCol(r, "SITE"),
                    Homebase= BotFormatters.FindCol(r, "Homebase"),
                    Kab     = BotFormatters.FindCol(r, "KAB"),
                    Barang  = BotFormatters.FindCol(r, "Nama Barang"),
                    Progres = BotFormatters.FindCol(r, "Progres"),
                    Ket     = BotFormatters.FindCol(r, "Keterangan"),
                })
                .GroupBy(x => $"{x.Tanggal}|{x.Segment}|{x.Rute}")
                .Select(g => new GroupedActivity
                {
                    Tanggal = g.First().Tanggal,
                    Segment = g.First().Segment,
                    Rute = g.First().Rute,
                    Site = g.First().Site,
                    Homebase = g.First().Homebase,
                    Kab = g.First().Kab,
                    Materials = g.Select(x => (x.Barang, x.Progres, x.Ket)).Distinct().ToList(),
                })
                .ToList();
        }

        private class GroupedActivity
        {
            public string Tanggal { get; set; } = "-";
            public string Segment { get; set; } = "-";
            public string Rute { get; set; } = "-";
            public string Site { get; set; } = "-";
            public string Homebase { get; set; } = "-";
            public string Kab { get; set; } = "-";
            public List<(string, string, string)> Materials { get; set; } = new();
        }

        private static DateTime GetWibNow()
        {
            try { return DateTime.UtcNow.AddHours(7); }
            catch { return DateTime.Now; }
        }

        private static string MonthName(int m) => m switch
        {
            1 => "Januari", 2 => "Februari", 3 => "Maret", 4 => "April",
            5 => "Mei", 6 => "Juni", 7 => "Juli", 8 => "Agustus",
            9 => "September", 10 => "Oktober", 11 => "November", 12 => "Desember",
            _ => ""
        };
    }
}
