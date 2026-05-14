using System.Text;
using StokBarangMAUI.Models.Bot;
using StokBarangMAUI.Services.Mcp;

namespace StokBarangMAUI.Services.AiChat.BotFlows
{
    /// <summary>Flow 4: Surat Jalan (date, jenis, nomor, menu).</summary>
    public class SuratJalanFlow : IBotFlow
    {
        private readonly McpClient _mcp;
        private readonly BotIntent _intent;

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

                switch (_intent)
                {
                    case BotIntent.SuratJalanDate:
                        ctx.TryGetValue("dateIntent", out dateIntent);
                        ctx.TryGetValue("day", out var day);
                        ctx.TryGetValue("month", out var month);
                        if (string.IsNullOrEmpty(dateIntent) && !string.IsNullOrEmpty(day))
                            keyword = string.IsNullOrEmpty(month) ? day : $"{day} {month}";
                        break;

                    case BotIntent.SuratJalanJenis:
                        ctx.TryGetValue("jenis", out keyword);
                        break;

                    case BotIntent.SuratJalanNomor:
                        ctx.TryGetValue("nomor", out keyword);
                        break;

                    case BotIntent.SuratJalanOrang:
                        ctx.TryGetValue("orang", out keyword);
                        break;
                }

                var result = await _mcp.SearchSuratJalanAsync(keyword, dateIntent, 30);
                if (result?.Data == null || result.Data.Count == 0)
                    return BotResponse.Text_($"📭 Tidak ada SJ yang cocok.\n\n💡 Coba: `sj kemarin`, `sj masuk`, `sj-001`");

                return BotResponse.Text_(FormatSj(result.Data, keyword ?? dateIntent ?? ""));
            }
            catch (Exception ex) { return BotResponse.Text_($"❌ {ex.Message}"); }
        }

        public Task<BotResponse?> ResumeAsync(string m, BotPendingState s) => Task.FromResult<BotResponse?>(null);

        private static string FormatSj(List<Dictionary<string, object>> rows, string label)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"📜 SURAT JALAN — {rows.Count} hasil" + (!string.IsNullOrEmpty(label) ? $" ({label})" : ""));
            sb.AppendLine();

            int shown = 0;
            foreach (var row in rows.Take(10))
            {
                var tanggal = BotFormatters.FindCol(row, "Tanggal");
                var segment = BotFormatters.FindCol(row, "Segment");
                var barang = BotFormatters.FindCol(row, "Nama Barang");
                var qty = BotFormatters.FindCol(row, "QTY");
                var jenis = BotFormatters.FindCol(row, "Jenis");
                var noSj = BotFormatters.FindCol(row, "NO_SJ");
                var pengirim = BotFormatters.FindCol(row, "PENGIRIM");
                var penerima = BotFormatters.FindCol(row, "PENERIMA");

                var jenisIcon = jenis.ToUpperInvariant() switch
                {
                    var j when j.Contains("MASUK") => "📥",
                    var j when j.Contains("KELUAR") => "📤",
                    var j when j.Contains("DIBAWA") => "🚚",
                    _ => "📋"
                };

                sb.AppendLine($"{jenisIcon} {jenis}");
                if (noSj != "-") sb.AppendLine($"   No: {noSj}");
                sb.AppendLine($"   📅 {tanggal} · {segment}");
                sb.AppendLine($"   📦 {barang} × {qty}");
                if (pengirim != "-" || penerima != "-")
                    sb.AppendLine($"   👤 {pengirim} → {penerima}");
                sb.AppendLine();
                shown++;
            }

            if (rows.Count > shown)
                sb.AppendLine($"📄 +{rows.Count - shown} SJ lagi");

            return sb.ToString().TrimEnd();
        }

        private static string BuildSjMenu()
        {
            return "📜 SURAT JALAN — Pilih:\n\n" +
                   "  • `sj kemarin` / `sj hari ini`\n" +
                   "  • `sj masuk` / `sj keluar` / `sj dibawa`\n" +
                   "  • `sj-001` (cari nomor)\n" +
                   "  • `sj minggu ini`\n\n" +
                   "Atau ketik langsung keyword.";
        }
    }
}
