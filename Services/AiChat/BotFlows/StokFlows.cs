using System.Text;
using StokBarangMAUI.Models.Bot;
using StokBarangMAUI.Services.Mcp;

namespace StokBarangMAUI.Services.AiChat.BotFlows
{
    /// <summary>Flow 2a: Stok material tertentu (e.g. "stok kabel 24c").</summary>
    public class StokMaterialFlow : IBotFlow
    {
        private readonly McpClient _mcp;
        public StokMaterialFlow(McpClient mcp) { _mcp = mcp; }
        public BotIntent Handles => BotIntent.StokMaterial;
        public BotPattern Pattern => BotPattern.SingleShot;

        public async Task<BotResponse> ExecuteAsync(string userMessage, Dictionary<string, string> ctx)
        {
            if (!_mcp.IsEnabled) return BotResponse.Text_("⚠️ GDrive Reader belum aktif.");
            ctx.TryGetValue("material", out var mat);
            if (string.IsNullOrEmpty(mat)) return BotResponse.Text_("🔍 Material apa? Misal: `stok kabel 24c`");

            try
            {
                var result = await _mcp.ReadAktualStokAsync();
                if (result?.Data == null) return BotResponse.Text_("📭 Data Aktual Stok kosong.");

                var rows = result.Data.Where(r =>
                    (r.Values.FirstOrDefault()?.ToString() ?? "").Contains(mat, StringComparison.OrdinalIgnoreCase)).ToList();

                if (rows.Count == 0) return BotResponse.Text_($"🔍 Material `{mat}` tidak ditemukan.");

                var sb = new StringBuilder();
                foreach (var row in rows.Take(3))
                {
                    var nama = row.Values.FirstOrDefault()?.ToString()?.Trim() ?? mat;
                    sb.AppendLine($"📦 {nama}");
                    sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━");
                    sb.AppendLine();

                    double totalDit = 0, totalKel = 0;
                    for (int i = 0; i < DataSchema.Segments.Length; i++)
                    {
                        var seg = DataSchema.Segments[i];
                        var dit = FindGudangVal(row, result.Columns, i, false);
                        var kel = FindGudangVal(row, result.Columns, i, true);
                        var sisa = dit - kel;
                        totalDit += dit; totalKel += kel;

                        var icon = sisa > 0 ? "✅" : sisa == 0 ? "⚪" : "🚨";
                        sb.AppendLine($"{icon} {seg}");
                        sb.AppendLine($"   📥 Masuk  : {BotFormatters.FormatNum(dit)}");
                        sb.AppendLine($"   📤 Keluar : {BotFormatters.FormatNum(kel)}");
                        sb.AppendLine($"   📦 Sisa   : {BotFormatters.FormatNum(sisa)}");
                        sb.AppendLine();
                    }

                    sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━");
                    sb.AppendLine($"📊 TOTAL");
                    sb.AppendLine($"   Diterima : {BotFormatters.FormatNum(totalDit)}");
                    sb.AppendLine($"   Terpakai : {BotFormatters.FormatNum(totalKel)}");
                    sb.AppendLine($"   Sisa     : {BotFormatters.FormatNum(totalDit - totalKel)}");
                    sb.AppendLine();
                }

                return BotResponse.Text_(sb.ToString().TrimEnd());
            }
            catch (Exception ex) { return BotResponse.Text_($"❌ {ex.Message}"); }
        }

        public Task<BotResponse?> ResumeAsync(string m, BotPendingState s) => Task.FromResult<BotResponse?>(null);

        private static double FindGudangVal(Dictionary<string, object> row, List<string>? cols, int gudangIdx, bool isKeluar)
        {
            if (cols == null) return 0;
            // Diterima = cols 1-6, Keluar = after "Grand Total" section (cols ~10-15)
            // Simplified: search by segment name in column header
            var segName = DataSchema.Segments[gudangIdx];
            bool passedGrand = false;
            foreach (var kv in row)
            {
                var cu = kv.Key.ToUpperInvariant();
                if (cu.Contains("GRAND TOTAL") || cu.Contains("SISA")) { passedGrand = true; continue; }
                if (cu.Contains(segName) || MatchesSegmentCols(cu, gudangIdx))
                {
                    if (!isKeluar && !passedGrand) return BotFormatters.ParseNumber(kv.Value);
                    if (isKeluar && passedGrand) return BotFormatters.ParseNumber(kv.Value);
                }
            }
            return 0;
        }

        private static bool MatchesSegmentCols(string colUpper, int idx) => idx switch
        {
            0 => colUpper.Contains("CIREBON") || colUpper.Contains("BREBES") || colUpper.Contains("TEGAL"),
            1 => colUpper.Contains("TASIK") || colUpper.Contains("BANJAR"),
            2 => colUpper.Contains("BANYUMAS") || colUpper.Contains("CILACAP") || colUpper.Contains("KEBUMEN"),
            3 => colUpper.Contains("SUKOHARJO") || colUpper.Contains("KLATEN") || colUpper.Contains("WONOGIRI"),
            4 => colUpper.Contains("SRAGEN") || colUpper.Contains("KARANG"),
            5 => colUpper.Contains("GROBOGAN") || colUpper.Contains("BLORA"),
            _ => false
        };
    }

    /// <summary>Flow 2b: Stok per gudang (e.g. "stok di brebes").</summary>
    public class StokGudangFlow : IBotFlow
    {
        private readonly McpClient _mcp;
        public StokGudangFlow(McpClient mcp) { _mcp = mcp; }
        public BotIntent Handles => BotIntent.StokGudang;
        public BotPattern Pattern => BotPattern.SingleShot;

        public async Task<BotResponse> ExecuteAsync(string userMessage, Dictionary<string, string> ctx)
        {
            if (!_mcp.IsEnabled) return BotResponse.Text_("⚠️ GDrive Reader belum aktif.");
            ctx.TryGetValue("segment", out var seg);
            if (string.IsNullOrEmpty(seg)) return BotResponse.Text_("🔍 Gudang mana? Misal: `stok di brebes`");

            try
            {
                var result = await _mcp.ReadAktualStokAsync();
                if (result?.Data == null) return BotResponse.Text_("📭 Data kosong.");

                var sb = new StringBuilder();
                sb.AppendLine($"📦 STOK GUDANG {seg}");
                sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━");
                sb.AppendLine();

                int idx = Array.IndexOf(DataSchema.Segments, seg);
                if (idx < 0) return BotResponse.Text_($"⚠️ Segment {seg} tidak dikenal.");

                int rowsPrinted = 0;
                int maxRows = 25;
                int adaCount = 0, habisCount = 0, minusCount = 0;

                foreach (var row in result.Data.Take(40))
                {
                    var nama = row.Values.FirstOrDefault()?.ToString()?.Trim() ?? "";
                    if (string.IsNullOrWhiteSpace(nama) || nama.Length < 3) continue;
                    if (nama.StartsWith("STOK", StringComparison.OrdinalIgnoreCase)) continue;
                    if (rowsPrinted >= maxRows) break;

                    var dit = StokFindGudangVal(row, idx, false);
                    var kel = StokFindGudangVal(row, idx, true);
                    var sisa = dit - kel;

                    string statusIcon;
                    if (sisa > 0)      { statusIcon = "✅"; adaCount++; }
                    else if (sisa == 0){ statusIcon = "⚪"; habisCount++; }
                    else               { statusIcon = "🚨"; minusCount++; }

                    // Format vertical: nama jadi judul, detail di bawah
                    sb.AppendLine($"{statusIcon} {nama}");
                    sb.AppendLine($"   📥 Masuk  : {BotFormatters.FormatNum(dit)}");
                    sb.AppendLine($"   📤 Keluar : {BotFormatters.FormatNum(kel)}");
                    sb.AppendLine($"   📦 Sisa   : {BotFormatters.FormatNum(sisa)}");
                    sb.AppendLine();
                    rowsPrinted++;
                }

                sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━");
                sb.AppendLine($"📊 Total: {rowsPrinted} material");
                sb.AppendLine($"   ✅ {adaCount} ada  ·  ⚪ {habisCount} habis  ·  🚨 {minusCount} minus");
                return BotResponse.Text_(sb.ToString().TrimEnd());
            }
            catch (Exception ex) { return BotResponse.Text_($"❌ {ex.Message}"); }
        }

        public Task<BotResponse?> ResumeAsync(string m, BotPendingState s) => Task.FromResult<BotResponse?>(null);

        internal static double StokFindGudangVal(Dictionary<string, object> row, int gudangIdx, bool isKeluar)
        {
            bool passedGrand = false;
            var segName = DataSchema.Segments[gudangIdx];
            foreach (var kv in row)
            {
                var cu = kv.Key.ToUpperInvariant();
                if (cu.Contains("GRAND TOTAL") || cu.Contains("SISA")) { passedGrand = true; continue; }
                if (cu.Contains(segName) || MatchesSeg(cu, gudangIdx))
                {
                    if (!isKeluar && !passedGrand) return BotFormatters.ParseNumber(kv.Value);
                    if (isKeluar && passedGrand) return BotFormatters.ParseNumber(kv.Value);
                }
            }
            return 0;
        }

        private static bool MatchesSeg(string cu, int idx) => idx switch
        {
            0 => cu.Contains("CIREBON") || cu.Contains("BREBES") || cu.Contains("TEGAL"),
            1 => cu.Contains("TASIK") || cu.Contains("BANJAR"),
            2 => cu.Contains("BANYUMAS") || cu.Contains("CILACAP"),
            3 => cu.Contains("SUKOHARJO") || cu.Contains("KLATEN"),
            4 => cu.Contains("SRAGEN") || cu.Contains("KARANG"),
            5 => cu.Contains("GROBOGAN") || cu.Contains("BLORA"),
            _ => false
        };
    }

    /// <summary>Flow 2c: Stok kritis / habis.</summary>
    public class StokKritisFlow : IBotFlow
    {
        private readonly McpClient _mcp;
        public StokKritisFlow(McpClient mcp) { _mcp = mcp; }
        public BotIntent Handles => BotIntent.StokKritis;
        public BotPattern Pattern => BotPattern.SingleShot;

        public async Task<BotResponse> ExecuteAsync(string userMessage, Dictionary<string, string> ctx)
        {
            if (!_mcp.IsEnabled) return BotResponse.Text_("⚠️ GDrive Reader belum aktif.");
            try
            {
                var result = await _mcp.ReadAktualStokAsync();
                if (result?.Data == null) return BotResponse.Text_("📭 Data kosong.");

                var sb = new StringBuilder();
                sb.AppendLine("🚨 STOK KRITIS (Sisa Gudang ≤ 0)");
                sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━");
                sb.AppendLine();

                int found = 0;
                foreach (var row in result.Data)
                {
                    var nama = row.Values.FirstOrDefault()?.ToString()?.Trim() ?? "";
                    if (string.IsNullOrWhiteSpace(nama) || nama.Length < 3) continue;
                    var sisa = BotFormatters.FindNum(row, "SISA", "GUDANG");
                    if (sisa <= 0)
                    {
                        sb.AppendLine($"🚨 {nama}");
                        sb.AppendLine($"   Sisa: {BotFormatters.FormatNum(sisa)}");
                        sb.AppendLine();
                        found++;
                    }
                }

                if (found == 0)
                    return BotResponse.Text_("✅ Semua material masih ada stok. Tidak ada yang kritis.");

                sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━");
                sb.AppendLine($"📊 Total {found} material kritis.");
                sb.AppendLine($"💡 Ketik `stok [nama]` untuk detail per gudang.");
                return BotResponse.Text_(sb.ToString().TrimEnd());
            }
            catch (Exception ex) { return BotResponse.Text_($"❌ {ex.Message}"); }
        }

        public Task<BotResponse?> ResumeAsync(string m, BotPendingState s) => Task.FromResult<BotResponse?>(null);
    }

    /// <summary>Flow 2d: Stok menu (vague "cek stok").</summary>
    public class StokMenuFlow : IBotFlow
    {
        private readonly McpClient _mcp;
        public StokMenuFlow(McpClient mcp) { _mcp = mcp; }
        public BotIntent Handles => BotIntent.StokMenu;
        public BotPattern Pattern => BotPattern.VagueMenu;

        public Task<BotResponse> ExecuteAsync(string userMessage, Dictionary<string, string> ctx)
        {
            var sb = new StringBuilder();
            sb.AppendLine("📦 CEK STOK — Pilih salah satu:");
            sb.AppendLine();
            sb.AppendLine("  1. Stok per gudang → ketik `stok di brebes`");
            sb.AppendLine("  2. Stok material → ketik `stok kabel 24c`");
            sb.AppendLine("  3. Stok kritis → ketik `stok habis`");
            sb.AppendLine();
            sb.AppendLine("Atau langsung ketik nama gudang/material.");

            return Task.FromResult(BotResponse.Text_(sb.ToString().TrimEnd()));
        }

        public Task<BotResponse?> ResumeAsync(string m, BotPendingState s) => Task.FromResult<BotResponse?>(null);
    }
}
