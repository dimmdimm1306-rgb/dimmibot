using System.Text;
using StokBarangMAUI.Models.Bot;
using StokBarangMAUI.Services.Mcp;

namespace StokBarangMAUI.Services.AiChat.BotFlows
{
    /// <summary>
    /// Flow 1d: Progres total project / per segment.
    /// Pattern A (single-shot). Baca sheet RESUME (6 segment + grand total).
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
            sb.AppendLine("📊 PROGRES PROJECT");
            sb.AppendLine();

            double totalKabelPlan = 0, totalKabelProg = 0;
            double totalT7Plan = 0, totalT7Prog = 0;
            double totalT9Plan = 0, totalT9Prog = 0;
            int shown = 0;

            foreach (var row in rows)
            {
                var no = BotFormatters.FindCol(row, "No");
                var rute = BotFormatters.FindCol(row, "Rute");
                if (rute == "-" || string.IsNullOrWhiteSpace(rute)) continue;

                // Skip kalau filter segment dan gak match
                if (!string.IsNullOrEmpty(segFilter) &&
                    !rute.Contains(segFilter, StringComparison.OrdinalIgnoreCase))
                    continue;

                var kPlan = BotFormatters.FindNum(row, "Kabel", "Plan");
                var kProg = BotFormatters.FindNum(row, "Kabel", "Progress");
                var t7Plan = BotFormatters.FindNum(row, "7m", "Plan");
                var t7Prog = BotFormatters.FindNum(row, "7m", "Progress");
                var t9Plan = BotFormatters.FindNum(row, "9m", "Plan");
                var t9Prog = BotFormatters.FindNum(row, "9m", "Progress");

                totalKabelPlan += kPlan; totalKabelProg += kProg;
                totalT7Plan += t7Plan; totalT7Prog += t7Prog;
                totalT9Plan += t9Plan; totalT9Prog += t9Prog;

                var kPct = kPlan > 0 ? kProg / kPlan : 0;
                var t7Pct = t7Plan > 0 ? t7Prog / t7Plan : 0;
                var t9Pct = t9Plan > 0 ? t9Prog / t9Plan : 0;
                var overall = 0.0;
                int cnt = 0;
                if (kPlan > 0) { overall += kPct; cnt++; }
                if (t7Plan > 0) { overall += t7Pct; cnt++; }
                if (t9Plan > 0) { overall += t9Pct; cnt++; }
                if (cnt > 0) overall /= cnt;

                var icon = overall >= 1.0 ? "✅" : overall >= 0.5 ? "🟡" : "🔴";
                var ruteShort = BotFormatters.Trunc(rute, 35);

                sb.AppendLine($"{icon} Seg {no} · {ruteShort}");
                sb.AppendLine($"   Kabel: {BotFormatters.FormatNum(kProg)}/{BotFormatters.FormatNum(kPlan)} m ({BotFormatters.FormatPct(kPct)})");
                sb.AppendLine($"   T7: {BotFormatters.FormatNum(t7Prog)}/{BotFormatters.FormatNum(t7Plan)} btg ({BotFormatters.FormatPct(t7Pct)})");
                sb.AppendLine($"   T9: {BotFormatters.FormatNum(t9Prog)}/{BotFormatters.FormatNum(t9Plan)} btg ({BotFormatters.FormatPct(t9Pct)})");
                sb.AppendLine();
                shown++;
            }

            if (shown == 0)
            {
                sb.AppendLine("(Tidak ada data segment yang match.)");
                return sb.ToString().TrimEnd();
            }

            // Grand total
            sb.AppendLine(BotFormatters.DividerLine);
            var gkPct = totalKabelPlan > 0 ? totalKabelProg / totalKabelPlan : 0;
            var gt7Pct = totalT7Plan > 0 ? totalT7Prog / totalT7Plan : 0;
            var gt9Pct = totalT9Plan > 0 ? totalT9Prog / totalT9Plan : 0;
            sb.AppendLine($"📈 TOTAL PROJECT");
            sb.AppendLine($"   Kabel: {BotFormatters.FormatNum(totalKabelProg)}/{BotFormatters.FormatNum(totalKabelPlan)} m ({BotFormatters.FormatPct(gkPct)})");
            sb.AppendLine($"   T7: {BotFormatters.FormatNum(totalT7Prog)}/{BotFormatters.FormatNum(totalT7Plan)} btg ({BotFormatters.FormatPct(gt7Pct)})");
            sb.AppendLine($"   T9: {BotFormatters.FormatNum(totalT9Prog)}/{BotFormatters.FormatNum(totalT9Plan)} btg ({BotFormatters.FormatPct(gt9Pct)})");

            return sb.ToString().TrimEnd();
        }
    }
}
