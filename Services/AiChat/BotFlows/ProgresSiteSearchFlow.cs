using System.Text;
using System.Text.RegularExpressions;
using StokBarangMAUI.Models.Bot;
using StokBarangMAUI.Services.Mcp;

namespace StokBarangMAUI.Services.AiChat.BotFlows
{
    /// <summary>
    /// Flow 1b: Search site/rute. Dua mode:
    ///   - "site 0244" / "rute JC2" → search → langsung detail kalau cuma 1, kalau banyak list pendek
    ///   - "site di sragen" / "cek site sragen" → list semua site di segment → user pilih → detail
    ///   - "yang mana belum" sebagai follow-up → filter overall < 30%
    /// </summary>
    public class ProgresSiteSearchFlow : IBotFlow
    {
        private readonly McpClient _mcp;
        private const int LIST_PAGE = 15;
        private const double THRESHOLD_BELUM = 0.30;

        public ProgresSiteSearchFlow(McpClient mcp) { _mcp = mcp; }

        public BotIntent Handles => BotIntent.ProgresSiteSearch;
        public BotPattern Pattern => BotPattern.FilterDrillDown;

        public async Task<BotResponse> ExecuteAsync(string userMessage, Dictionary<string, string> ctx)
        {
            if (!_mcp.IsEnabled)
                return BotResponse.Text_("⚠️ GDrive Reader belum aktif.");

            var lower = userMessage.ToLowerInvariant();

            // Detect mode "site di [segment]"
            var segMatch = Regex.Match(lower,
                @"\b(?:site|rute|cek\s+site)\s+(?:di|dari|pada)\s+(?<seg>\w+)",
                RegexOptions.IgnoreCase);
            if (!segMatch.Success)
            {
                // Atau pattern "cek site [segment]" tanpa "di"
                segMatch = Regex.Match(lower,
                    @"^(?:cek\s+)?site\s+(?<seg>brebes|tasik(?:malaya)?|purwokerto|sukoharjo|sragen|grobogan|klaten|solo|surakarta|wonogiri|cilacap|kebumen|banyumas|purworejo|tegal|cirebon|pekalongan|indramayu|semarang|banjar|karanganyar|blora)\b",
                    RegexOptions.IgnoreCase);
            }

            if (segMatch.Success)
            {
                var segName = DataSchema.ResolveSegmentFromCity(segMatch.Groups["seg"].Value);
                if (segName != null)
                    return await ListSitesInSegment(segName);
            }

            // Mode keyword search
            ctx.TryGetValue("query", out var query);
            if (string.IsNullOrWhiteSpace(query))
                query = BotTokens.FindSiteOrRuteQuery(userMessage);

            if (string.IsNullOrWhiteSpace(query))
                return BotResponse.Text_("🔍 Ketik site/rute yang dicari, misal:\n  • `site 0244`\n  • `rute JC2`\n  • `cek site di sragen`");

            return await SearchByKeyword(query.ToUpperInvariant());
        }

        public async Task<BotResponse?> ResumeAsync(string userMessage, BotPendingState state)
        {
            var step = state.Step;
            var lower = userMessage.Trim().ToLowerInvariant();

            // From list mode, user picks site
            if (step == "listShown")
            {
                var seg = state.Get("segment") ?? "";
                var siteIds = (state.Get("sites") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries);

                // "yang mana belum" → re-show filtered <30%
                if (Regex.IsMatch(lower, @"\b(yang\s+(mana|belum)|belum|outstanding|kurang)\b"))
                {
                    return await ListSitesInSegment(seg, onlyOutstanding: true);
                }

                // "yang sedang" / "sedang progres"
                if (Regex.IsMatch(lower, @"\b(sedang|jalan|progres|on going)\b"))
                {
                    return await ListSitesInSegment(seg, onlyMid: true);
                }

                // "yang selesai" / "done"
                if (Regex.IsMatch(lower, @"\b(selesai|done|hampir|finish|komplit)\b"))
                {
                    return await ListSitesInSegment(seg, onlyDone: true);
                }

                // Pick by number atau site ID
                var pickedSite = ResolveSitePick(lower, siteIds);
                if (pickedSite != null)
                {
                    var result = await _mcp.SearchSiteResumeAsync(pickedSite, 5);
                    var match = result?.Data?.FirstOrDefault();
                    if (match == null)
                        return BotResponse.Text_($"🔍 Site `{pickedSite}` tidak ketemu.");
                    BotState.Clear();
                    return BotResponse.Text_(FormatSiteDetail(match));
                }
                return null;
            }

            return null;
        }

        // ── Modes ───────────────────────────────────────────────────

        private async Task<BotResponse> ListSitesInSegment(string segment, bool onlyOutstanding = false, bool onlyMid = false, bool onlyDone = false)
        {
            try
            {
                // Cari segment file (sheet name = nama segment uppercase)
                var spec = DataSchema.SegmentSheets.FirstOrDefault(s =>
                    s.SheetName.Equals(segment, StringComparison.OrdinalIgnoreCase));
                if (spec == null)
                    return BotResponse.Text_($"⚠️ Segment {segment} tidak dikenal.");

                var result = await _mcp.FilterAsync(spec, null, null, 200);
                var rows = result?.Data ?? new();

                // Skip header rows yang gak punya RUTE
                rows = rows.Where(r =>
                {
                    var rute = BotFormatters.FindCol(r, "RUTE");
                    return rute != "-" && !string.IsNullOrWhiteSpace(rute);
                }).ToList();

                if (onlyOutstanding) rows = rows.Where(r => OverallOf(r) < THRESHOLD_BELUM).ToList();
                else if (onlyMid)    rows = rows.Where(r => OverallOf(r) >= THRESHOLD_BELUM && OverallOf(r) < 0.7).ToList();
                else if (onlyDone)   rows = rows.Where(r => OverallOf(r) >= 0.7).ToList();

                if (rows.Count == 0)
                {
                    var label = onlyOutstanding ? "belum (<30%)" : onlyMid ? "sedang progres (30-70%)" : onlyDone ? "hampir/selesai (>70%)" : "data";
                    return BotResponse.Text_($"📭 Tidak ada {label} di segment {segment}.");
                }

                // Save state
                var siteIdsCsv = string.Join(",", rows.Take(50).Select(r => BotFormatters.FindCol(r, "RUTE")));
                BotState.Save(nameof(BotIntent.ProgresSiteSearch), "listShown",
                    new Dictionary<string, string>
                    {
                        ["segment"] = segment,
                        ["sites"] = siteIdsCsv,
                    });

                return FormatSiteList(segment, rows, onlyOutstanding, onlyMid, onlyDone);
            }
            catch (Exception ex) { return BotResponse.Text_($"❌ {ex.Message}"); }
        }

        private async Task<BotResponse> SearchByKeyword(string query)
        {
            try
            {
                var result = await _mcp.SearchSiteResumeAsync(query, 50);
                var rows = result?.Data ?? new();
                if (rows.Count == 0)
                    return BotResponse.Text_($"🔍 Tidak ketemu `{query}` di RESUME BY SITE ID.");

                if (rows.Count == 1)
                    return BotResponse.Text_(FormatSiteDetail(rows[0]));

                // Multiple — show list
                var siteIdsCsv = string.Join(",", rows.Take(50).Select(r => BotFormatters.FindCol(r, "Rute")));
                BotState.Save(nameof(BotIntent.ProgresSiteSearch), "listShown",
                    new Dictionary<string, string> { ["sites"] = siteIdsCsv });

                return FormatSearchList(query, rows);
            }
            catch (Exception ex) { return BotResponse.Text_($"❌ {ex.Message}"); }
        }

        // ── Helpers ─────────────────────────────────────────────────

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

        private static string? ResolveSitePick(string input, string[] candidates)
        {
            // Number "1", "2"
            if (int.TryParse(input.Trim(), out var num) && num >= 1 && num <= candidates.Length)
                return candidates[num - 1];

            // "site XXX"
            var m = Regex.Match(input, @"\b(?:site|rute|cek\s+site)\s+(?<q>[a-z0-9][a-z0-9\-_\.]+)", RegexOptions.IgnoreCase);
            if (m.Success) return m.Groups["q"].Value;

            m = Regex.Match(input, @"\b(?<q>JAW-[A-Z0-9\-]+|JC\d+_\d+|\d{4})\b", RegexOptions.IgnoreCase);
            if (m.Success) return m.Groups["q"].Value;

            return null;
        }

        // ── Formatters ──────────────────────────────────────────────

        private static BotResponse FormatSiteList(string segment, List<Dictionary<string, object>> rows,
            bool only_outstanding, bool only_mid, bool only_done)
        {
            string headerLabel;
            if (only_outstanding) headerLabel = "🔴 Belum (<30%)";
            else if (only_mid)    headerLabel = "🟡 Sedang (30-70%)";
            else if (only_done)   headerLabel = "✅ Selesai (>70%)";
            else                  headerLabel = "📋 Semua";

            var sb = new StringBuilder();
            sb.AppendLine($"{headerLabel} — {segment} ({rows.Count} rute)");
            sb.AppendLine();

            int i = 1;
            foreach (var row in rows.Take(LIST_PAGE))
            {
                var rute = BotFormatters.FindCol(row, "RUTE");
                var kab = BotFormatters.FindCol(row, "KAB");
                var ruteShort = BotFormatters.Trunc(rute, 40);
                var kabShort = kab != "-" ? BotFormatters.Trunc(kab, 14) : "";
                sb.AppendLine($"{i,2}. {ruteShort,-40}  {kabShort}");
                i++;
            }

            if (rows.Count > LIST_PAGE)
                sb.AppendLine($"\n📄 +{rows.Count - LIST_PAGE} rute lagi");

            sb.AppendLine();
            sb.AppendLine("👉 Ketik nomor atau `site [ID]` untuk detail progres.");
            sb.AppendLine("📊 Filter: `yang belum`, `yang sedang`, `yang selesai`");
            return new BotResponse { Text = sb.ToString().TrimEnd(), HasPendingState = true };
        }

        private static BotResponse FormatSearchList(string query, List<Dictionary<string, object>> rows)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"🔎 Hasil cari `{query}` — {rows.Count} rute");
            sb.AppendLine();

            int i = 1;
            foreach (var row in rows.Take(LIST_PAGE))
            {
                var siteId = BotFormatters.FindCol(row, "SITE ID");
                var rute = BotFormatters.FindCol(row, "Rute");
                var kab = BotFormatters.FindCol(row, "KAB");
                var siteShort = siteId != "-" ? BotFormatters.Trunc(siteId, 22) : "-";
                var ruteShort = BotFormatters.Trunc(rute, 32);
                sb.AppendLine($"{i,2}. {siteShort,-22}  {ruteShort}");
                i++;
            }
            if (rows.Count > LIST_PAGE)
                sb.AppendLine($"\n📄 +{rows.Count - LIST_PAGE} lagi");

            sb.AppendLine();
            sb.AppendLine("👉 Ketik nomor untuk lihat detail.");
            return new BotResponse { Text = sb.ToString().TrimEnd(), HasPendingState = true };
        }

        private static string FormatSiteDetail(Dictionary<string, object> row)
        {
            var siteId = BotFormatters.FindCol(row, "SITE ID");
            if (siteId == "-") siteId = BotFormatters.FindCol(row, "No");
            var rute = BotFormatters.FindCol(row, "Rute");
            if (rute == "-") rute = BotFormatters.FindCol(row, "RUTE");
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
            sb.AppendLine("📍 Site Detail");
            sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━");
            if (siteId != "-") sb.AppendLine($"🆔 {siteId}");
            sb.AppendLine($"📌 {rute}");
            if (kota != "-") sb.AppendLine($"📍 {kota}");
            sb.AppendLine();
            sb.AppendLine($"{icon} Status: {label} ({BotFormatters.FormatPct(overall)})");
            sb.AppendLine();

            var lblW = 8; var numW = 8;
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
    }
}
