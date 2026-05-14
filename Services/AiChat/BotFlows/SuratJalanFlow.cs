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

            // Detail mode kalau cuma 1-3 hasil — tampilkan full info
            if (rows.Count <= 3)
            {
                int shown = 0;
                foreach (var row in rows)
                {
                    if (shown > 0) sb.AppendLine(BotFormatters.ThinDivider);
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
                    sb.AppendLine($"{icon} {jenis}" + (noSj != "-" ? $"  ·  {noSj}" : ""));
                    sb.AppendLine($"📅 {tanggal}  ·  {segment}");
                    sb.AppendLine($"📦 {barang} × {qty}");
                    if (pengirim != "-" || penerima != "-")
                        sb.AppendLine($"👤 {pengirim} → {penerima}");
                    if (ket != "-" && !string.IsNullOrWhiteSpace(ket))
                        sb.AppendLine($"📝 {ket}");
                    shown++;
                }
                return sb.ToString().TrimEnd();
            }

            // List mode — sejajar pakai PadR/PadL
            // Kolom: Icon Tanggal | Jenis | Barang × QTY | NoSJ
            BotFormatters.AppendTable(sb,
                new[] { "Tgl/Jenis", "Barang × QTY", "NoSJ" },
                labelWidth: 18,
                numWidths: new[] { 24, 10 });

            foreach (var row in rows.Take(20))
            {
                var tanggal = BotFormatters.FindCol(row, "Tanggal");
                var jenis = BotFormatters.FindCol(row, "Jenis");
                var barang = BotFormatters.FindCol(row, "Nama Barang");
                var qty = BotFormatters.FindCol(row, "QTY");
                var noSj = BotFormatters.FindCol(row, "NO_SJ");
                var icon = JenisIconAscii(jenis);

                var leftCol = $"{icon} {BotFormatters.Trunc(tanggal, 12)}";
                var midCol = $"{BotFormatters.Trunc(barang, 18)} x{qty}";
                sb.AppendLine(BotFormatters.TableRow(leftCol, 18,
                    (BotFormatters.Trunc(midCol, 24), 24),
                    (BotFormatters.Trunc(noSj, 10), 10)));
            }

            if (rows.Count > 20)
                sb.AppendLine($"\n📄 +{rows.Count - 20} SJ lagi");

            sb.AppendLine();
            sb.AppendLine("📊 ↓ masuk · ↑ keluar · → dibawa");
            return sb.ToString().TrimEnd();
        }

        private static string JenisIcon(string jenis)
        {
            var j = jenis.ToUpperInvariant();
            if (j.Contains("MASUK")) return "📥";
            if (j.Contains("KELUAR")) return "📤";
            if (j.Contains("DIBAWA")) return "🚚";
            return "📋";
        }

        // ASCII versions yang gak bikin kolom geser di list view
        private static string JenisIconAscii(string jenis)
        {
            var j = jenis.ToUpperInvariant();
            if (j.Contains("MASUK")) return "↓";
            if (j.Contains("KELUAR")) return "↑";
            if (j.Contains("DIBAWA")) return "→";
            return "·";
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
