using System.Text;
using StokBarangMAUI.Models.Bot;
using StokBarangMAUI.Services.Mcp;

namespace StokBarangMAUI.Services.AiChat.BotFlows
{
    /// <summary>
    /// Flow 1a: Site/rute yang belum dikerjakan (overall &lt; 30%).
    /// Pattern B/C: list site IDs by segment → detail kalau user lanjut tanya.
    /// State pending menyimpan list buat follow-up "yang mana belum".
    /// </summary>
    public class ProgresOutstandingFlow : IBotFlow
    {
        private readonly McpClient _mcp;
        private const int LIST_PAGE = 25;
        private const double THRESHOLD_BELUM = 0.30; // <30% = belum

        public ProgresOutstandingFlow(McpClient mcp) { _mcp = mcp; }

        public BotIntent Handles => BotIntent.ProgresOutstanding;
        public BotPattern Pattern => BotPattern.FilterDrillDown;

        public async Task<BotResponse> ExecuteAsync(string userMessage, Dictionary<string, string> ctx)
        {
            if (!_mcp.IsEnabled)
                return BotResponse.Text_("⚠️ GDrive Reader belum aktif.");

            try
            {
                // Fetch SEMUA rute (bukan cuma yang outstanding di server) supaya kita
                // bisa apply threshold 30% sendiri di client.
                var result = await _mcp.SearchSiteResumeAsync("", 200);
                if (result?.Data == null || result.Data.Count == 0)
                    return BotResponse.Text_("📭 Data RESUME BY SITE ID kosong.");

                // Filter sendiri: overall < THRESHOLD_BELUM
                var outstanding = result.Data
                    .Where(r => OverallOf(r) < THRESHOLD_BELUM)
                    .ToList();

                if (outstanding.Count == 0)
                    return BotResponse.Text_("🎉 Tidak ada rute yang belum dikerjakan (overall < 30%)!");

                var bySegment = GroupBySegment(outstanding);
                if (bySegment.Count == 0)
                    return BotResponse.Text_("🎉 Semua rute sudah jalan!");

                // Auto-skip kalau cuma 1 segment
                if (bySegment.Count == 1)
                {
                    var seg = bySegment.First();
                    SaveListState(seg.Key, seg.Value);
                    return FormatSiteList(seg.Key, seg.Value);
                }

                // Multi-segment menu
                BotState.Save(nameof(BotIntent.ProgresOutstanding), "pickSegment",
                    new Dictionary<string, string>
                    {
                        ["total"] = outstanding.Count.ToString(),
                        ["mode"]  = "belum",
                    });

                return FormatSegmentMenu(bySegment, outstanding.Count);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Outstanding] error: {ex.Message}");
                return BotResponse.Text_($"❌ {ex.Message}");
            }
        }

        public async Task<BotResponse?> ResumeAsync(string userMessage, BotPendingState state)
        {
            var step = state.Step;
            var lower = userMessage.Trim().ToLowerInvariant();

            // Step: pilih segment dari menu
            if (step == "pickSegment")
            {
                var seg = ResolveSegmentChoice(lower);
                if (seg == null) return null;

                try
                {
                    var result = await _mcp.SearchSiteResumeAsync("", 200);
                    if (result?.Data == null) return BotResponse.Text_("📭 Data kosong.");

                    var outstanding = result.Data
                        .Where(r => OverallOf(r) < THRESHOLD_BELUM)
                        .ToList();
                    var bySegment = GroupBySegment(outstanding);

                    if (!bySegment.TryGetValue(seg, out var rows) || rows.Count == 0)
                        return BotResponse.Text_($"✅ Segment {seg} sudah jalan semua!");

                    SaveListState(seg, rows);
                    return FormatSiteList(seg, rows);
                }
                catch (Exception ex)
                {
                    return BotResponse.Text_($"❌ {ex.Message}");
                }
            }

            // Step: dari list site, user pilih site untuk detail
            if (step == "listShown")
            {
                // User mungkin ketik "site 0244" / nomor / "no 3" / SiteID langsung
                var segment = state.Get("segment") ?? "";
                var pickedSite = ResolveSitePick(lower, state);

                if (pickedSite != null)
                {
                    try
                    {
                        var result = await _mcp.SearchSiteResumeAsync("", 200);
                        if (result?.Data == null) return BotResponse.Text_("📭 Data kosong.");

                        var match = result.Data.FirstOrDefault(r =>
                            string.Equals(BotFormatters.FindCol(r, "SITE ID"), pickedSite, StringComparison.OrdinalIgnoreCase) ||
                            BotFormatters.FindCol(r, "Rute").Contains(pickedSite, StringComparison.OrdinalIgnoreCase));

                        if (match == null)
                            return BotResponse.Text_($"🔍 Site `{pickedSite}` tidak ketemu di {segment}.");

                        BotState.Clear();
                        return BotResponse.Text_(FormatSiteDetail(match));
                    }
                    catch (Exception ex)
                    {
                        return BotResponse.Text_($"❌ {ex.Message}");
                    }
                }

                // "yang mana belum" → re-show list current segment
                if (lower.Contains("belum") || lower.Contains("mana"))
                {
                    var segNow = state.Get("segment") ?? "";
                    if (string.IsNullOrEmpty(segNow)) return null;
                    try
                    {
                        var result = await _mcp.SearchSiteResumeAsync("", 200);
                        var outstanding = (result?.Data ?? new()).Where(r => OverallOf(r) < THRESHOLD_BELUM).ToList();
                        var bySegment = GroupBySegment(outstanding);
                        if (!bySegment.TryGetValue(segNow, out var rows))
                            return BotResponse.Text_($"✅ {segNow} sudah jalan semua!");
                        SaveListState(segNow, rows);
                        return FormatSiteList(segNow, rows);
                    }
                    catch (Exception ex) { return BotResponse.Text_($"❌ {ex.Message}"); }
                }

                return null;
            }

            return null;
        }

        // ── State helpers ────────────────────────────────────────────

        private void SaveListState(string segment, List<Dictionary<string, object>> rows)
        {
            // Save site IDs sebagai csv di state (untuk lookup later)
            var siteIds = string.Join(",", rows.Take(50).Select(r => BotFormatters.FindCol(r, "SITE ID")));
            BotState.Save(nameof(BotIntent.ProgresOutstanding), "listShown",
                new Dictionary<string, string>
                {
                    ["segment"] = segment,
                    ["sites"]   = siteIds,
                });
        }

        private static string? ResolveSitePick(string input, BotPendingState state)
        {
            // 1. Nomor "1", "2"
            if (int.TryParse(input.Trim(), out var num))
            {
                var sites = (state.Get("sites") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (num >= 1 && num <= sites.Length) return sites[num - 1];
            }

            // 2. "site XXX" / "cek site XXX"
            var m = System.Text.RegularExpressions.Regex.Match(input,
                @"\b(?:site|rute|cek\s+site|cek\s+rute)\s+(?<q>[a-z0-9][a-z0-9\-_\.]+)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (m.Success) return m.Groups["q"].Value.ToUpperInvariant();

            // 3. Pattern site ID langsung
            m = System.Text.RegularExpressions.Regex.Match(input,
                @"\b(?<q>JAW-[A-Z0-9\-]+|JC\d+_\d+|\d{4})\b",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (m.Success) return m.Groups["q"].Value.ToUpperInvariant();

            return null;
        }

        // ── Grouping & overall ──────────────────────────────────────

        private static double OverallOf(Dictionary<string, object> row)
        {
            var kPlan = BotFormatters.FindNum(row, "Kabel", "Plan");
            var kProg = BotFormatters.FindNum(row, "Kabel", "Progress");
            var t7Plan = BotFormatters.FindNum(row, "7m", "Plan");
            var t7Prog = BotFormatters.FindNum(row, "7m", "Progress");
            var t9Plan = BotFormatters.FindNum(row, "9m", "Plan");
            var t9Prog = BotFormatters.FindNum(row, "9m", "Progress");
            return BotFormatters.ComputeOverall(kPlan, kProg, t7Plan, t7Prog, t9Plan, t9Prog);
        }

        private static Dictionary<string, List<Dictionary<string, object>>> GroupBySegment(
            List<Dictionary<string, object>> rows)
        {
            var result = new Dictionary<string, List<Dictionary<string, object>>>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows)
            {
                var kota = BotFormatters.FindCol(row, "KAB");
                var seg = DataSchema.ResolveSegmentFromCity(kota);
                if (seg == null)
                {
                    var rute = BotFormatters.FindCol(row, "Rute");
                    seg = DataSchema.ResolveSegmentFromText(rute);
                }
                seg ??= "LAINNYA";
                if (!result.ContainsKey(seg)) result[seg] = new();
                result[seg].Add(row);
            }
            return result;
        }

        // ── Formatters ──────────────────────────────────────────────

        private static BotResponse FormatSegmentMenu(
            Dictionary<string, List<Dictionary<string, object>>> bySegment, int total)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"🔴 Belum dikerjakan (<30%) — {total} rute di {bySegment.Count} segment:");
            sb.AppendLine();

            int i = 1;
            var sugg = new List<string>();
            // Width untuk align: nomor (2) + segment (12) + count
            foreach (var kv in bySegment.OrderByDescending(x => x.Value.Count))
            {
                sb.AppendLine(BotFormatters.Row3(
                    $"{i}. {kv.Key}", 18,
                    $"{kv.Value.Count}", 4,
                    "rute", 5));
                sugg.Add(i.ToString());
                i++;
            }
            sb.AppendLine();
            sb.AppendLine("👉 Pilih segment (ketik angka atau nama):");
            return BotResponse.Menu(sb.ToString().TrimEnd(), sugg);
        }

        private static BotResponse FormatSiteList(string segment, List<Dictionary<string, object>> rows)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"🔴 {segment} — {rows.Count} rute belum (<30%):");
            sb.AppendLine();

            int i = 1;
            foreach (var row in rows.Take(LIST_PAGE))
            {
                var siteId = BotFormatters.FindCol(row, "SITE ID");
                var rute = BotFormatters.FindCol(row, "Rute");
                if (siteId.Length > 22) siteId = siteId.Substring(0, 22);
                var ruteShort = BotFormatters.Trunc(rute, 38);
                sb.AppendLine($"{i,2}. {siteId,-22}  {ruteShort}");
                i++;
            }

            if (rows.Count > LIST_PAGE)
                sb.AppendLine($"\n📄 +{rows.Count - LIST_PAGE} rute lagi");

            sb.AppendLine();
            sb.AppendLine("👉 Ketik nomor atau `site [ID]` untuk lihat detail progres.");
            return new BotResponse { Text = sb.ToString().TrimEnd(), HasPendingState = true };
        }

        private static string FormatSiteDetail(Dictionary<string, object> row)
        {
            var siteId = BotFormatters.FindCol(row, "SITE ID");
            var rute = BotFormatters.FindCol(row, "Rute");
            var kota = BotFormatters.FindCol(row, "KAB");

            var kPlan = BotFormatters.FindNum(row, "Kabel", "Plan");
            var kProg = BotFormatters.FindNum(row, "Kabel", "Progress");
            var t7Plan = BotFormatters.FindNum(row, "7m", "Plan");
            var t7Prog = BotFormatters.FindNum(row, "7m", "Progress");
            var t9Plan = BotFormatters.FindNum(row, "9m", "Plan");
            var t9Prog = BotFormatters.FindNum(row, "9m", "Progress");

            var overall = BotFormatters.ComputeOverall(kPlan, kProg, t7Plan, t7Prog, t9Plan, t9Prog);
            var (icon, label, _) = BotFormatters.StatusByOverall(overall);

            var kPct = kPlan > 0 ? kProg / kPlan : 0;
            var t7Pct = t7Plan > 0 ? t7Prog / t7Plan : 0;
            var t9Pct = t9Plan > 0 ? t9Prog / t9Plan : 0;

            var sb = new StringBuilder();
            sb.AppendLine($"📍 Site Detail");
            sb.AppendLine($"━━━━━━━━━━━━━━━━━━━━━━━");
            sb.AppendLine($"🆔 {siteId}");
            sb.AppendLine($"📌 {rute}");
            if (kota != "-") sb.AppendLine($"📍 {kota}");
            sb.AppendLine();
            sb.AppendLine($"{icon} Status: {label} ({BotFormatters.FormatPct(overall)})");
            sb.AppendLine();

            var lblW = 8;
            var numW = 8;
            sb.AppendLine(BotFormatters.PadR("Kategori", lblW) + "  " +
                          BotFormatters.PadL("Plan", numW) + "  " +
                          BotFormatters.PadL("Progress", numW) + "  " +
                          BotFormatters.PadL("%", 5));
            sb.AppendLine("─────────────────────────────────────");
            sb.AppendLine(BotFormatters.PadR("Kabel", lblW) + "  " +
                          BotFormatters.PadL(BotFormatters.FormatNum(kPlan), numW) + "  " +
                          BotFormatters.PadL(BotFormatters.FormatNum(kProg), numW) + "  " +
                          BotFormatters.PadL(BotFormatters.FormatPct(kPct), 5));
            sb.AppendLine(BotFormatters.PadR("Tiang 7m", lblW) + "  " +
                          BotFormatters.PadL(BotFormatters.FormatNum(t7Plan), numW) + "  " +
                          BotFormatters.PadL(BotFormatters.FormatNum(t7Prog), numW) + "  " +
                          BotFormatters.PadL(BotFormatters.FormatPct(t7Pct), 5));
            sb.AppendLine(BotFormatters.PadR("Tiang 9m", lblW) + "  " +
                          BotFormatters.PadL(BotFormatters.FormatNum(t9Plan), numW) + "  " +
                          BotFormatters.PadL(BotFormatters.FormatNum(t9Prog), numW) + "  " +
                          BotFormatters.PadL(BotFormatters.FormatPct(t9Pct), 5));

            return sb.ToString().TrimEnd();
        }

        private static string? ResolveSegmentChoice(string input)
        {
            if (int.TryParse(input, out var num) && num >= 1 && num <= DataSchema.Segments.Length)
                return DataSchema.Segments[num - 1];
            return DataSchema.ResolveSegmentFromCity(input);
        }
    }
}
