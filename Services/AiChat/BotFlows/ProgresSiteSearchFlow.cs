using System.Text;
using StokBarangMAUI.Models.Bot;
using StokBarangMAUI.Services.Mcp;

namespace StokBarangMAUI.Services.AiChat.BotFlows
{
    /// <summary>
    /// Flow 1b: Search site/rute di semua 6 segment RESUME.
    /// Pattern B/C: search → if >3 hits multi-segment → picker → detail.
    /// Auto-skip picker kalau ≤3 hits atau 1 segment.
    /// </summary>
    public class ProgresSiteSearchFlow : IBotFlow
    {
        private readonly McpClient _mcp;
        private const int PAGE_SIZE = 5;

        public ProgresSiteSearchFlow(McpClient mcp) { _mcp = mcp; }

        public BotIntent Handles => BotIntent.ProgresSiteSearch;
        public BotPattern Pattern => BotPattern.FilterDrillDown;

        public async Task<BotResponse> ExecuteAsync(string userMessage, Dictionary<string, string> ctx)
        {
            if (!_mcp.IsEnabled)
                return BotResponse.Text_("⚠️ GDrive Reader belum aktif.");

            ctx.TryGetValue("query", out var query);
            if (string.IsNullOrWhiteSpace(query))
            {
                query = BotTokens.FindSiteOrRuteQuery(userMessage);
                if (string.IsNullOrWhiteSpace(query))
                    return BotResponse.Text_("🔍 Ketik site/rute yang mau dicari, misal: `site 0244` atau `rute JC2`");
            }

            return await SearchAcrossSegments(query.ToUpperInvariant(), null);
        }

        public async Task<BotResponse?> ResumeAsync(string userMessage, BotPendingState state)
        {
            var step = state.Step;
            var lower = userMessage.Trim().ToLowerInvariant();

            if (step == "pickSegment")
            {
                var query = state.Get("query") ?? "";
                var seg = ResolveSegmentChoice(lower);
                if (seg == null) return null;

                return await SearchAcrossSegments(query, seg);
            }

            return null;
        }

        private async Task<BotResponse> SearchAcrossSegments(string query, string? onlySegment)
        {
            try
            {
                var segments = onlySegment != null
                    ? new[] { onlySegment }
                    : DataSchema.Segments;

                var bySegment = new Dictionary<string, List<Dictionary<string, object>>>(StringComparer.OrdinalIgnoreCase);

                foreach (var seg in segments)
                {
                    var spec = DataSchema.SegmentSheets.FirstOrDefault(s =>
                        s.SheetName.Equals(seg, StringComparison.OrdinalIgnoreCase));
                    if (spec == null) continue;

                    var result = await _mcp.SmartFilterAsync(spec, query, null,
                        new[] { "RUTE", "No" }, 50);

                    if (result?.Data != null && result.Data.Count > 0)
                        bySegment[seg] = result.Data;
                }

                var total = bySegment.Values.Sum(v => v.Count);

                if (total == 0)
                    return BotResponse.Text_($"🔍 Tidak ketemu `{query}` di 6 segment RESUME.\n\n💡 Coba keyword lebih pendek.");

                // Auto-skip: ≤3 total atau 1 segment → langsung detail
                if (total <= 3 || bySegment.Count == 1)
                {
                    var all = bySegment.SelectMany(kv => kv.Value.Select(r => (kv.Key, r))).ToList();
                    return FormatDetail(query, all);
                }

                // Multi-segment picker
                BotState.Save(nameof(BotIntent.ProgresSiteSearch), "pickSegment",
                    new Dictionary<string, string> { ["query"] = query });

                return FormatPicker(query, bySegment);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SiteSearch] error: {ex.Message}");
                return BotResponse.Text_($"❌ Gagal search: {ex.Message}");
            }
        }

        private static BotResponse FormatPicker(string query,
            Dictionary<string, List<Dictionary<string, object>>> bySegment)
        {
            var sb = new StringBuilder();
            var total = bySegment.Values.Sum(v => v.Count);
            sb.AppendLine($"🔎 Ditemukan {total} rute cocok `{query}` di {bySegment.Count} segment:");
            sb.AppendLine();

            int i = 1;
            var suggestions = new List<string>();
            foreach (var kv in bySegment.OrderBy(x => x.Key))
            {
                sb.AppendLine($"  {i}. {kv.Key} — {kv.Value.Count} rute");
                // Preview 2 rute
                foreach (var row in kv.Value.Take(2))
                {
                    var rute = BotFormatters.FindCol(row, "RUTE");
                    sb.AppendLine($"     • {BotFormatters.Trunc(rute, 40)}");
                }
                if (kv.Value.Count > 2)
                    sb.AppendLine($"     • ...+{kv.Value.Count - 2} lagi");
                sb.AppendLine();
                suggestions.Add(i.ToString());
                i++;
            }

            sb.AppendLine("Pilih segment (ketik angka atau nama):");
            return BotResponse.Menu(sb.ToString().TrimEnd(), suggestions);
        }

        private static BotResponse FormatDetail(string query,
            List<(string segment, Dictionary<string, object> row)> matches)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"📍 Progres: `{query}`");
            sb.AppendLine($"Ditemukan {matches.Count} rute");
            sb.AppendLine();

            string? currentSeg = null;
            foreach (var (seg, row) in matches.Take(PAGE_SIZE * 3)) // max 15
            {
                if (seg != currentSeg)
                {
                    if (currentSeg != null) sb.AppendLine();
                    sb.AppendLine(BotFormatters.SectionHeader(seg));
                    currentSeg = seg;
                }

                var rute = BotFormatters.FindCol(row, "RUTE");
                var kota = BotFormatters.FindCol(row, "KAB");

                var kPlan = BotFormatters.FindNum(row, "Kabel", "Plan");
                var kProg = BotFormatters.FindNum(row, "Kabel", "Progress");
                var t7Plan = BotFormatters.FindNum(row, "7m", "Plan");
                var t7Prog = BotFormatters.FindNum(row, "7m", "Progress");
                var t9Plan = BotFormatters.FindNum(row, "9m", "Plan");
                var t9Prog = BotFormatters.FindNum(row, "9m", "Progress");

                var kPct = kPlan > 0 ? kProg / kPlan * 100 : 0;
                var t7Pct = t7Plan > 0 ? t7Prog / t7Plan * 100 : 0;
                var t9Pct = t9Plan > 0 ? t9Prog / t9Plan * 100 : 0;

                sb.AppendLine();
                sb.AppendLine($"📌 {BotFormatters.Trunc(rute, 50)}");
                if (kota != "-") sb.AppendLine($"   Kota: {kota}");
                sb.AppendLine($"   {BotFormatters.StatusIcon(kPct)} Kabel: {kProg:N0}/{kPlan:N0} m ({kPct:N0}%)");
                sb.AppendLine($"   {BotFormatters.StatusIcon(t7Pct)} T7: {t7Prog:N0}/{t7Plan:N0} ({t7Pct:N0}%)");
                sb.AppendLine($"   {BotFormatters.StatusIcon(t9Pct)} T9: {t9Prog:N0}/{t9Plan:N0} ({t9Pct:N0}%)");
            }

            if (matches.Count > PAGE_SIZE * 3)
                sb.AppendLine($"\n📄 +{matches.Count - PAGE_SIZE * 3} rute lagi");

            sb.AppendLine();
            sb.AppendLine("Legend: ✅ selesai · 🟡 jalan · 🔴 belum");
            return BotResponse.Text_(sb.ToString().TrimEnd());
        }

        private static string? ResolveSegmentChoice(string input)
        {
            if (int.TryParse(input, out var num) && num >= 1 && num <= DataSchema.Segments.Length)
                return DataSchema.Segments[num - 1];
            return DataSchema.ResolveSegmentFromCity(input);
        }
    }
}
