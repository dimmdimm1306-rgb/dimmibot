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

                    case BotIntent.SuratJalanLatest:
                        // Fetch semua, sort desc by tanggal, ambil top 5
                        return await FetchLatestSjAsync();
                }

                var result = await _mcp.SearchSuratJalanAsync(keyword, dateIntent, 30);
                if (result?.Data == null || result.Data.Count == 0)
                    return BotResponse.Text_($"📭 Tidak ada SJ yang cocok.\n\n💡 Coba: `sj kemarin`, `sj masuk`, `sj-001`, `sj terakhir`");

                return BotResponse.Text_(FormatSj(result.Data, keyword ?? dateIntent ?? ""));
            }
            catch (Exception ex) { return BotResponse.Text_($"❌ {ex.Message}"); }
        }

        private async Task<BotResponse> FetchLatestSjAsync()
        {
            // Ambil banyak dulu, sort di client
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

            // Kalau gak ada yang ke-parse tanggalnya, fallback ke urutan asli
            if (sorted.Count == 0) sorted = result.Data.Take(5).ToList();

            return BotResponse.Text_(FormatSj(sorted, "5 SJ terakhir"));
        }

        // Parse tanggal SJ dari format Indonesia ("14 Mei 2026") atau ISO ("2026-05-14")
        private static DateTime? ParseSjDate(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw) || raw == "-") return null;
            raw = raw.Trim();

            // Direct ISO / standard formats
            if (DateTime.TryParse(raw, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeLocal, out var d1)) return d1;

            // Indonesian month names
            var idCulture = new System.Globalization.CultureInfo("id-ID");
            if (DateTime.TryParse(raw, idCulture,
                System.Globalization.DateTimeStyles.AssumeLocal, out var d2)) return d2;

            // Manual parse "DD MMMM YYYY"
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
                   "  • `sj terakhir` (5 SJ paling baru)\n" +
                   "  • `sj kemarin` / `sj hari ini`\n" +
                   "  • `sj masuk` / `sj keluar` / `sj dibawa`\n" +
                   "  • `sj-001` (cari nomor)\n" +
                   "  • `sj minggu ini`\n\n" +
                   "Atau ketik langsung keyword.";
        }
    }
}
