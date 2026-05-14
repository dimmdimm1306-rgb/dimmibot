using System.Text;
using StokBarangMAUI.Models.Bot;
using StokBarangMAUI.Services.Mcp;

namespace StokBarangMAUI.Services.AiChat.BotFlows
{
    /// <summary>
    /// Flow 1c: Progres by tanggal (kemarin, hari ini, tanggal X, tanggal X bulan).
    /// Pattern A/C: fetch → format. Kalau banyak segment, group.
    /// </summary>
    public class ProgresDateFlow : IBotFlow
    {
        private readonly McpClient _mcp;

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
                var result = await _mcp.SearchProgressAsync(keyword, dateIntent, 50);
                if (result?.Data == null || result.Data.Count == 0)
                    return BotResponse.Text_($"📅 Tidak ada progres untuk `{label}`.\n\n💡 Coba tanggal lain.");

                return BotResponse.Text_(FormatDateResult(label, result.Data));
            }
            catch (Exception ex)
            {
                return BotResponse.Text_($"❌ Gagal: {ex.Message}");
            }
        }

        public Task<BotResponse?> ResumeAsync(string userMessage, BotPendingState state)
            => Task.FromResult<BotResponse?>(null);

        private static string FormatDateResult(string label, List<Dictionary<string, object>> rows)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"📅 Progres `{label}` — {rows.Count} entry");
            sb.AppendLine();

            int shown = 0;
            foreach (var row in rows.Take(15))
            {
                var tanggal = BotFormatters.FindCol(row, "Tanggal");
                var rute = BotFormatters.FindCol(row, "Rute");
                var barang = BotFormatters.FindCol(row, "Nama Barang");
                var progres = BotFormatters.FindCol(row, "Progres");
                var ket = BotFormatters.FindCol(row, "Keterangan");
                var segment = BotFormatters.FindCol(row, "Segment");
                var site = BotFormatters.FindCol(row, "SITE");

                sb.AppendLine($"📌 {BotFormatters.Trunc(rute, 40)}");
                var meta = new List<string>();
                if (segment != "-") meta.Add(segment);
                if (site != "-") meta.Add($"Site {site}");
                if (meta.Count > 0) sb.AppendLine($"   {string.Join(" · ", meta)}");
                sb.AppendLine($"   📦 {barang}: {progres}" + (ket != "-" ? $" ({ket})" : ""));
                sb.AppendLine();
                shown++;
            }

            if (rows.Count > shown)
                sb.AppendLine($"📄 +{rows.Count - shown} entry lagi");

            return sb.ToString().TrimEnd();
        }

        private static DateTime GetWibNow()
        {
            try { return DateTime.UtcNow.AddHours(7); }
            catch { return DateTime.Now; }
        }
    }
}
