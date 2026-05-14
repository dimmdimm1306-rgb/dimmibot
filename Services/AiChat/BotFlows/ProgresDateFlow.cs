using System.Text;
using StokBarangMAUI.Models.Bot;
using StokBarangMAUI.Services.Mcp;

namespace StokBarangMAUI.Services.AiChat.BotFlows
{
    /// <summary>
    /// Flow 1c: Progres by tanggal (kemarin, hari ini, tanggal X, tanggal X bulan).
    /// Pattern A/C: fetch → format. Kalau banyak segment, group.
    /// Support pagination via state pending: ketik `lanjut` untuk page next.
    /// </summary>
    public class ProgresDateFlow : IBotFlow
    {
        private readonly McpClient _mcp;
        private const int PAGE_SIZE = 25;

        public ProgresDateFlow(McpClient mcp) { _mcp = mcp; }

        public BotIntent Handles => BotIntent.ProgresDate;
        public BotPattern Pattern => BotPattern.SingleShot;

        public async Task<BotResponse> ExecuteAsync(string userMessage, Dictionary<string, string> ctx)
        {
            if (!_mcp.IsEnabled)
                return BotResponse.Text_("⚠️ GDrive Reader belum aktif.");

            // Determine date intent or keyword
            ctx.TryGetValue("dateIntent", out var dateIntent);
            ctx.TryGetValue("day", out var dayStr);
            ctx.TryGetValue("month", out var month);

            // Build keyword for server-side search
            string? keyword = null;
            string label;

            if (!string.IsNullOrEmpty(dateIntent))
            {
                label = dateIntent switch
                {
                    "date_today" => "hari ini",
                    "date_yesterday" => "kemarin",
                    "date_week" => "7 hari terakhir",
                    _ => "tanggal"
                };

                // Time-aware: kalau "hari ini" tapi dini hari (00-05 WIB)
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
            }
            else if (!string.IsNullOrEmpty(dayStr))
            {
                keyword = string.IsNullOrEmpty(month) ? dayStr : $"{dayStr} {month}";
                label = $"tanggal {keyword}";
            }
            else
            {
                return BotResponse.Text_(
                    "📅 Format tanggal:\n" +
                    "  • `progres kemarin`\n" +
                    "  • `progres tanggal 17`\n" +
                    "  • `progres tanggal 17 mei`\n" +
                    "  • `progres hari ini`");
            }

            try
            {
                // Limit naik dari 100 → 300 supaya cukup untuk semua segment per hari
                var result = await _mcp.SearchProgressAsync(keyword, dateIntent, 300);
                if (result?.Data == null || result.Data.Count == 0)
                    return BotResponse.Text_($"📅 Tidak ada progres untuk `{label}`.\n\n💡 Coba tanggal lain.");

                return BuildPagedResponse(label, result.Data, page: 0, dateIntent, keyword);
            }
            catch (Exception ex)
            {
                return BotResponse.Text_($"❌ Gagal: {ex.Message}");
            }
        }

        public async Task<BotResponse?> ResumeAsync(string userMessage, BotPendingState state)
        {
            if (state.Step != "paginate") return null;

            var lower = userMessage.Trim().ToLowerInvariant();
            if (!System.Text.RegularExpressions.Regex.IsMatch(lower,
                @"^(lanjut|lanjutkan|next|berikut(nya)?|more|lainnya)\s*$"))
                return null;

            int page = state.GetInt("page", 0) + 1;
            var label = state.Get("label") ?? "tanggal";
            var dateIntent = state.Get("dateIntent");
            var keyword = state.Get("keyword");

            try
            {
                var result = await _mcp.SearchProgressAsync(keyword, dateIntent, 300);
                if (result?.Data == null || result.Data.Count == 0)
                {
                    BotState.Clear();
                    return BotResponse.Text_("📭 Data tidak tersedia lagi.");
                }
                return BuildPagedResponse(label, result.Data, page, dateIntent, keyword);
            }
            catch (Exception ex)
            {
                return BotResponse.Text_($"❌ {ex.Message}");
            }
        }

        // ── Helpers ─────────────────────────────────────────────────

        private static BotResponse BuildPagedResponse(string label, List<Dictionary<string, object>> rows,
            int page, string? dateIntent, string? keyword)
        {
            var grouped = GroupActivities(rows);
            int totalPages = (int)Math.Ceiling(grouped.Count / (double)PAGE_SIZE);
            if (totalPages == 0) totalPages = 1;
            if (page >= totalPages)
            {
                BotState.Clear();
                return BotResponse.Text_("📭 Sudah semua aktivitas ditampilkan.");
            }

            var slice = grouped.Skip(page * PAGE_SIZE).Take(PAGE_SIZE).ToList();
            var text = FormatPage(label, slice, page, totalPages, grouped.Count, rows.Count);

            // Save state kalau masih ada page next
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
                    });
                return new BotResponse { Text = text, HasPendingState = true };
            }

            BotState.Clear();
            return BotResponse.Text_(text);
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

        private static string FormatPage(string label, List<GroupedActivity> activities,
            int page, int totalPages, int totalActivities, int totalRows)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"📅 PROGRES `{label.ToUpperInvariant()}`");
            sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━");
            sb.Append($"📊 {totalActivities} aktivitas · {totalRows} entri material");
            if (totalPages > 1) sb.Append($" · Halaman {page + 1}/{totalPages}");
            sb.AppendLine();
            sb.AppendLine();

            foreach (var g in activities)
            {
                sb.AppendLine($"📌 {BotFormatters.Trunc(g.Rute, 50)}");

                // Meta line
                var meta = new List<string>();
                if (g.Tanggal != "-") meta.Add($"📅 {g.Tanggal}");
                if (g.Segment != "-") meta.Add($"🗂 {BotFormatters.Trunc(g.Segment, 30)}");
                if (g.Site != "-") meta.Add($"🆔 {g.Site}");
                if (meta.Count > 0) sb.AppendLine("  " + string.Join("  ·  ", meta));

                if (g.Homebase != "-" || g.Kab != "-")
                {
                    var loc = new List<string>();
                    if (g.Homebase != "-") loc.Add(g.Homebase);
                    if (g.Kab != "-") loc.Add(g.Kab);
                    sb.AppendLine($"  📍 {string.Join(" · ", loc)}");
                }

                // Materials sejajar
                var mats = g.Materials.Where(m => m.Item1 != "-").ToList();
                if (mats.Count > 0)
                {
                    foreach (var (barang, progres, ket) in mats)
                    {
                        var matShort = BotFormatters.Trunc(barang, 22);
                        var line = $"  • {BotFormatters.PadR(matShort, 22)}  {BotFormatters.PadL(progres, 8)}";
                        if (ket != "-" && !string.IsNullOrWhiteSpace(ket))
                            line += $"  ({BotFormatters.Trunc(ket, 20)})";
                        sb.AppendLine(line);
                    }
                }
                sb.AppendLine();
            }

            int remaining = totalActivities - (page + 1) * PAGE_SIZE;
            if (remaining > 0)
                sb.AppendLine($"📄 +{remaining} aktivitas lagi · ketik `lanjut` untuk berikutnya");

            return sb.ToString().TrimEnd();
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
    }
}
