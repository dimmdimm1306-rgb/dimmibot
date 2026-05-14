using System.Text;
using System.Text.RegularExpressions;
using StokBarangMAUI.Models.Bot;
using StokBarangMAUI.Services.Mcp;

namespace StokBarangMAUI.Services.AiChat.BotFlows
{
    /// <summary>
    /// Flow 3a/3b: Kebutuhan material per homebase atau yang kurang.
    /// "kebutuhan material" tanpa segment → tanya segment dulu (atau pilih "yg kurang aja").
    /// Output vertical, lebar tetap.
    /// </summary>
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

            // Kalau gak ada filter segment dan bukan kurang-only → tanya dulu
            if (!_kurangOnly && string.IsNullOrEmpty(seg))
                return AskSegment();

            return await Fetch(seg);
        }

        public async Task<BotResponse?> ResumeAsync(string userMessage, BotPendingState state)
        {
            if (state.Step != "askSegment") return null;
            var lower = userMessage.Trim().ToLowerInvariant();

            // "kurang" / "yang kurang"
            if (Regex.IsMatch(lower, @"\b(kurang|kekurangan|minus|short)\b"))
            {
                var flow = new KebutuhanFlow(_mcp, kurangOnly: true);
                return await flow.Fetch(null);
            }

            string? seg = null;
            if (lower == "7" || lower == "semua" || lower == "all" || lower == "*")
            {
                seg = null;
            }
            else if (int.TryParse(lower, out var num) && num >= 1 && num <= 6)
            {
                seg = DataSchema.Segments[num - 1];
            }
            else
            {
                seg = DataSchema.ResolveSegmentFromText(userMessage);
                if (seg == null) return null;
            }

            return await Fetch(seg);
        }

        // ── Steps ───────────────────────────────────────────────────

        private static BotResponse AskSegment()
        {
            BotState.Save(nameof(BotIntent.KebutuhanHomebase), "askSegment", new());

            var sb = new StringBuilder();
            sb.AppendLine("📋 KEBUTUHAN MATERIAL");
            sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━");
            sb.AppendLine("Mau cek per segment mana?");
            sb.AppendLine();
            for (int i = 0; i < DataSchema.Segments.Length; i++)
                sb.AppendLine($"  {i + 1}. {DataSchema.Segments[i]}");
            sb.AppendLine($"  7. SEMUA segment");
            sb.AppendLine();
            sb.AppendLine("  💡 Atau ketik `kurang` — tampilkan yang kekurangan saja");

            return new BotResponse { Text = sb.ToString().TrimEnd(), HasPendingState = true };
        }

        public async Task<BotResponse> Fetch(string? seg)
        {
            try
            {
                var result = await _mcp.ReadStokKebutuhanAsync(seg);
                if (result?.Data == null || result.Data.Count == 0)
                {
                    BotState.Clear();
                    return BotResponse.Text_(string.IsNullOrEmpty(seg)
                        ? "📭 Data kebutuhan material kosong."
                        : $"🔍 Homebase `{seg}` tidak ditemukan.");
                }

                BotState.Clear();
                var sb = new StringBuilder();

                if (_kurangOnly)
                {
                    sb.AppendLine("🚨 MATERIAL KEKURANGAN");
                    sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━");
                    sb.AppendLine();

                    int found = 0;
                    foreach (var row in result.Data)
                    {
                        var homebase = BotFormatters.FindCol(row, "Homebase");
                        var shortages = new List<(string mat, double val)>();
                        foreach (var kv in row)
                        {
                            var key = kv.Key.ToLowerInvariant();
                            if (!key.Contains("kekurangan") && !key.Contains("kek")) continue;
                            var val = BotFormatters.ParseNumber(kv.Value);
                            if (val > 0)
                            {
                                var matName = kv.Key.Split('-', '(')[0].Trim();
                                shortages.Add((matName, val));
                            }
                        }

                        if (shortages.Count > 0)
                        {
                            sb.AppendLine($"📍 {homebase}");
                            foreach (var (mat, val) in shortages.Take(8))
                                sb.AppendLine($"   ⚠️ {mat}: {BotFormatters.FormatNum(val)}");
                            sb.AppendLine();
                            found += shortages.Count;
                        }
                    }

                    if (found == 0)
                        return BotResponse.Text_("✅ Semua material tercukupi. Tidak ada kekurangan.");

                    sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━");
                    sb.AppendLine($"📊 Total {found} item kekurangan.");
                }
                else
                {
                    var title = string.IsNullOrEmpty(seg) ? "KEBUTUHAN MATERIAL" : $"KEBUTUHAN {seg}";
                    sb.AppendLine($"📋 {title}");
                    sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━");
                    sb.AppendLine();

                    int rowsShown = 0;
                    foreach (var row in result.Data.Take(8))
                    {
                        var homebase = BotFormatters.FindCol(row, "Homebase");
                        var segment = BotFormatters.FindCol(row, "Segment");

                        var k24Keb = BotFormatters.FindNum(row, "K24C", "Keb");
                        var k24Ter = BotFormatters.FindNum(row, "K24C", "Ter");
                        var t7Keb = BotFormatters.FindNum(row, "T7", "Keb");
                        var t7Ter = BotFormatters.FindNum(row, "T7", "Ter");
                        var t9Keb = BotFormatters.FindNum(row, "T9", "Keb");
                        var t9Ter = BotFormatters.FindNum(row, "T9", "Ter");

                        if (k24Keb == 0 && t7Keb == 0 && t9Keb == 0 &&
                            k24Ter == 0 && t7Ter == 0 && t9Ter == 0) continue;

                        sb.AppendLine($"📍 {homebase}");
                        if (segment != "-") sb.AppendLine($"   🗂  {segment}");

                        if (k24Keb > 0 || k24Ter > 0)
                        {
                            sb.AppendLine($"   📦 Kabel 24C");
                            sb.AppendLine($"      Kebutuhan : {BotFormatters.FormatNum(k24Keb)}");
                            sb.AppendLine($"      Terpasang : {BotFormatters.FormatNum(k24Ter)}");
                        }
                        if (t7Keb > 0 || t7Ter > 0)
                        {
                            sb.AppendLine($"   📦 Tiang 7M");
                            sb.AppendLine($"      Kebutuhan : {BotFormatters.FormatNum(t7Keb)}");
                            sb.AppendLine($"      Terpasang : {BotFormatters.FormatNum(t7Ter)}");
                        }
                        if (t9Keb > 0 || t9Ter > 0)
                        {
                            sb.AppendLine($"   📦 Tiang 9M");
                            sb.AppendLine($"      Kebutuhan : {BotFormatters.FormatNum(t9Keb)}");
                            sb.AppendLine($"      Terpasang : {BotFormatters.FormatNum(t9Ter)}");
                        }

                        sb.AppendLine();
                        rowsShown++;
                    }

                    if (rowsShown == 0)
                        return BotResponse.Text_("📭 Tidak ada data material yang relevan.");
                }

                return BotResponse.Text_(sb.ToString().TrimEnd());
            }
            catch (Exception ex) { return BotResponse.Text_($"❌ {ex.Message}"); }
        }
    }
}
