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
                var result = await _mcp.SearchProgressAsync(keyword, dateIntent, 100);
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
            // Group by (tanggal | segment | rute) — material di-rangkap jadi list
            var grouped = rows
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
                .Select(g => new
                {
                    g.First().Tanggal,
                    g.First().Segment,
                    g.First().Rute,
                    g.First().Site,
                    g.First().Homebase,
                    g.First().Kab,
                    Materials = g.Select(x => (x.Barang, x.Progres, x.Ket)).Distinct().ToList(),
                })
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine($"📅 PROGRES `{label.ToUpperInvariant()}`");
            sb.AppendLine($"━━━━━━━━━━━━━━━━━━━━━━━");
            sb.AppendLine($"📊 {grouped.Count} aktivitas · {rows.Count} entri material");
            sb.AppendLine();

            int shown = 0;
            foreach (var g in grouped.Take(15))
            {
                // Header rute
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

                // Materials — sejajar dengan PadR/PadL
                var mats = g.Materials.Where(m => m.Barang != "-").ToList();
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
                shown++;
            }

            if (grouped.Count > shown)
                sb.AppendLine($"📄 +{grouped.Count - shown} aktivitas lagi");

            return sb.ToString().TrimEnd();
        }

        private static DateTime GetWibNow()
        {
            try { return DateTime.UtcNow.AddHours(7); }
            catch { return DateTime.Now; }
        }
    }
}
