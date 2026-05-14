using System.Text;
using StokBarangMAUI.Models.Bot;
using StokBarangMAUI.Services.Mcp;

namespace StokBarangMAUI.Services.AiChat.BotFlows
{
    /// <summary>Flow 3a/3b: Kebutuhan material per homebase atau yang kurang.</summary>
    public class KebutuhanFlow : IBotFlow
    {
        private readonly McpClient _mcp;
        private readonly bool _kurangOnly;

        public KebutuhanFlow(McpClient mcp, bool kurangOnly = false)
        {
            _mcp = mcp;
            _kurangOnly = kurangOnly;
        }

        public BotIntent Handles => _kurangOnly ? BotIntent.KebutuhanKurang : BotIntent.KebutuhanHomebase;
        public BotPattern Pattern => BotPattern.SingleShot;

        public async Task<BotResponse> ExecuteAsync(string userMessage, Dictionary<string, string> ctx)
        {
            if (!_mcp.IsEnabled) return BotResponse.Text_("⚠️ GDrive Reader belum aktif.");

            ctx.TryGetValue("segment", out var seg);

            try
            {
                var result = await _mcp.ReadStokKebutuhanAsync(seg);
                if (result?.Data == null || result.Data.Count == 0)
                    return BotResponse.Text_(string.IsNullOrEmpty(seg)
                        ? "📭 Data kebutuhan material kosong."
                        : $"🔍 Homebase `{seg}` tidak ditemukan di sheet Stok.");

                var sb = new StringBuilder();

                if (_kurangOnly)
                {
                    sb.AppendLine("🚨 MATERIAL KEKURANGAN");
                    sb.AppendLine();
                    int found = 0;
                    foreach (var row in result.Data)
                    {
                        var homebase = BotFormatters.FindCol(row, "Homebase");
                        // Cari kolom yang mengandung "Kekurangan" atau "Kek" dengan value > 0
                        var shortages = new List<string>();
                        foreach (var kv in row)
                        {
                            var key = kv.Key.ToLowerInvariant();
                            if (!key.Contains("kekurangan") && !key.Contains("kek")) continue;
                            var val = BotFormatters.ParseNumber(kv.Value);
                            if (val > 0)
                            {
                                var matName = kv.Key.Split('-', '(')[0].Trim();
                                shortages.Add($"{matName}: {BotFormatters.FormatNum(val)}");
                            }
                        }
                        if (shortages.Count > 0)
                        {
                            sb.AppendLine($"📍 {homebase}");
                            foreach (var s in shortages.Take(5))
                                sb.AppendLine($"   ⚠️ {s}");
                            sb.AppendLine();
                            found++;
                        }
                    }
                    if (found == 0)
                        return BotResponse.Text_("✅ Semua material tercukupi. Tidak ada kekurangan.");
                }
                else
                {
                    var title = string.IsNullOrEmpty(seg) ? "KEBUTUHAN MATERIAL" : $"KEBUTUHAN {seg}";
                    sb.AppendLine($"📋 {title}");
                    sb.AppendLine();

                    foreach (var row in result.Data.Take(8))
                    {
                        var homebase = BotFormatters.FindCol(row, "Homebase");
                        var segment = BotFormatters.FindCol(row, "Segment");
                        sb.AppendLine($"📍 {homebase}" + (segment != "-" ? $" ({segment})" : ""));

                        // Show key materials: Kabel, T7, T9
                        var k24Keb = BotFormatters.FindNum(row, "K24C", "Keb");
                        var k24Ter = BotFormatters.FindNum(row, "K24C", "Ter");
                        var t7Keb = BotFormatters.FindNum(row, "T7", "Keb");
                        var t7Ter = BotFormatters.FindNum(row, "T7", "Ter");
                        var t9Keb = BotFormatters.FindNum(row, "T9", "Keb");
                        var t9Ter = BotFormatters.FindNum(row, "T9", "Ter");

                        if (k24Keb > 0 || k24Ter > 0)
                            sb.AppendLine($"   Kabel 24C: Keb {BotFormatters.FormatNum(k24Keb)} · Ter {BotFormatters.FormatNum(k24Ter)}");
                        if (t7Keb > 0 || t7Ter > 0)
                            sb.AppendLine($"   Tiang 7M:  Keb {BotFormatters.FormatNum(t7Keb)} · Ter {BotFormatters.FormatNum(t7Ter)}");
                        if (t9Keb > 0 || t9Ter > 0)
                            sb.AppendLine($"   Tiang 9M:  Keb {BotFormatters.FormatNum(t9Keb)} · Ter {BotFormatters.FormatNum(t9Ter)}");
                        sb.AppendLine();
                    }
                }

                return BotResponse.Text_(sb.ToString().TrimEnd());
            }
            catch (Exception ex) { return BotResponse.Text_($"❌ {ex.Message}"); }
        }

        public Task<BotResponse?> ResumeAsync(string m, BotPendingState s) => Task.FromResult<BotResponse?>(null);
    }
}
