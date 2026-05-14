using System.Text;
using StokBarangMAUI.Models.Bot;
using StokBarangMAUI.Services.Mcp;

namespace StokBarangMAUI.Services.AiChat.BotFlows
{
    /// <summary>
    /// Flow 1d: Progres total project / per segment.
    /// Pattern A. Baca sheet RESUME (6 segment + grand total).
    /// </summary>
    public class ProgresTotalFlow : IBotFlow
    {
        private readonly McpClient _mcp;
        public ProgresTotalFlow(McpClient mcp) { _mcp = mcp; }

        public BotIntent Handles => BotIntent.ProgresTotal;
        public BotPattern Pattern => BotPattern.SingleShot;

        public async Task<BotResponse> ExecuteAsync(string userMessage, Dictionary<string, string> ctx)
        {
            if (!_mcp.IsEnabled)
                return BotResponse.Text_("⚠️ GDrive Reader belum aktif.");

            ctx.TryGetValue("segment", out var segFilter);

            try
            {
                var result = await _mcp.ReadResumeAsync();
                if (result?.Data == null || result.Data.Count == 0)
                    return BotResponse.Text_("📭 Data Resume kosong.");

                return BotResponse.Text_(FormatResume(segFilter, result.Data));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProgresTotalFlow] error: {ex.Message}");
                return BotResponse.Text_($"❌ Gagal ambil data resume: {ex.Message}");
            }
        }

        public Task<BotResponse?> ResumeAsync(string userMessage, BotPendingState state)
            => Task.FromResult<BotResponse?>(null);

        private static string FormatResume(string? segFilter, List<Dictionary<string, object>> rows)
        {
            var sb = new StringBuilder();
            sb.AppendLine("📊 PROGRES PROJECT — Per Segment");
            sb.AppendLine();

            int shown = 0;
            Dictionary<string, object>? grandTotalRow = null;

            foreach (var row in rows)
            {
                var noVal = BotFormatters.GetString(row, "No");
                var segment = BotFormatters.FindCol(row, "Segment");

                // Skip baris title atau kosong
                if (segment == "-" || string.IsNullOrWhiteSpace(segment)) continue;

                // Detect grand total row: No berisi "RESUME" string
                if (noVal.Contains("RESUME", StringComparison.OrdinalIgnoreCase))
                {
                    grandTotalRow = row;
                    continue;
                }

                // Validate No is numeric (1-6)
                if (!int.TryParse(noVal, out var segNo)) continue;
                if (segNo < 1 || segNo > 10) continue;

                // Filter by segment kalau ada
                if (!string.IsNullOrEmpty(segFilter))
                {
                    var segUpper = segment.ToUpperInvariant();
                    var resolvedSeg = DataSchema.ResolveSegmentFromText(segment);
                    if (resolvedSeg != segFilter && !segUpper.Contains(segFilter, StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                AppendSegment(sb, segNo, segment, row);
                shown++;
            }

            if (shown == 0)
            {
                sb.AppendLine("(Tidak ada data segment yang match.)");
                return sb.ToString().TrimEnd();
            }

            // Grand total
            if (grandTotalRow != null && string.IsNullOrEmpty(segFilter))
            {
                sb.AppendLine(BotFormatters.DividerLine);
                sb.AppendLine("📈 GRAND TOTAL PROJECT");
                AppendNumbers(sb, grandTotalRow, "   ");
            }

            return sb.ToString().TrimEnd();
        }

        private static void AppendSegment(StringBuilder sb, int no, string segment, Dictionary<string, object> row)
        {
            var kPlan = BotFormatters.FindNum(row, "Kabel", "Plan");
            var kProg = BotFormatters.FindNum(row, "Kabel", "Progress");
            var t7Plan = BotFormatters.FindNum(row, "7m", "Plan");
            var t7Prog = BotFormatters.FindNum(row, "7m", "Progress");
            var t9Plan = BotFormatters.FindNum(row, "9m", "Plan");
            var t9Prog = BotFormatters.FindNum(row, "9m", "Progress");

            // Overall: rata-rata 3 kategori (yang plan>0)
            double overall = 0;
            int cnt = 0;
            if (kPlan > 0) { overall += kProg / kPlan; cnt++; }
            if (t7Plan > 0) { overall += t7Prog / t7Plan; cnt++; }
            if (t9Plan > 0) { overall += t9Prog / t9Plan; cnt++; }
            if (cnt > 0) overall /= cnt;

            var icon = overall >= 1.0 ? "✅" : overall >= 0.5 ? "🟡" : "🔴";
            var segShort = BotFormatters.Trunc(segment, 50);

            sb.AppendLine($"{icon} Seg {no} · {segShort}");
            AppendNumbers(sb, row, "   ");
            sb.AppendLine();
        }

        private static void AppendNumbers(StringBuilder sb, Dictionary<string, object> row, string indent)
        {
            var kPlan = BotFormatters.FindNum(row, "Kabel", "Plan");
            var kProg = BotFormatters.FindNum(row, "Kabel", "Progress");
            var t7Plan = BotFormatters.FindNum(row, "7m", "Plan");
            var t7Prog = BotFormatters.FindNum(row, "7m", "Progress");
            var t9Plan = BotFormatters.FindNum(row, "9m", "Plan");
            var t9Prog = BotFormatters.FindNum(row, "9m", "Progress");

            var kPct = kPlan > 0 ? kProg / kPlan : 0;
            var t7Pct = t7Plan > 0 ? t7Prog / t7Plan : 0;
            var t9Pct = t9Plan > 0 ? t9Prog / t9Plan : 0;

            sb.AppendLine($"{indent}Kabel  {BotFormatters.FormatNum(kProg)}/{BotFormatters.FormatNum(kPlan)} m ({BotFormatters.FormatPct(kPct)})");
            sb.AppendLine($"{indent}T7m    {BotFormatters.FormatNum(t7Prog)}/{BotFormatters.FormatNum(t7Plan)} btg ({BotFormatters.FormatPct(t7Pct)})");
            sb.AppendLine($"{indent}T9m    {BotFormatters.FormatNum(t9Prog)}/{BotFormatters.FormatNum(t9Plan)} btg ({BotFormatters.FormatPct(t9Pct)})");
        }
    }
}
