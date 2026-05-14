using System.Text;
using StokBarangMAUI.Models.Bot;
using StokBarangMAUI.Services.Mcp;

namespace StokBarangMAUI.Services.AiChat.BotFlows
{
    /// <summary>
    /// Flow 1a: Progres yang belum 100% (outstanding).
    /// Pattern B/C: fetch → group by segment → menu picker → detail.
    /// Auto-skip menu kalau cuma 1 segment.
    /// Pagination 5 per page.
    /// </summary>
    public class ProgresOutstandingFlow : IBotFlow
    {
        private readonly McpClient _mcp;
        private const int PAGE_SIZE = 5;

        public ProgresOutstandingFlow(McpClient mcp) { _mcp = mcp; }

        public BotIntent Handles => BotIntent.ProgresOutstanding;
        public BotPattern Pattern => BotPattern.FilterDrillDown;

        public async Task<BotResponse> ExecuteAsync(string userMessage, Dictionary<string, string> ctx)
        {
            if (!_mcp.IsEnabled)
                return BotResponse.Text_("⚠️ GDrive Reader belum aktif.");

            try
            {
                var result = await _mcp.ResumeBySiteOutstandingAsync(500);
                if (result?.Data == null || result.Data.Count == 0)
                    return BotResponse.Text_("🎉 Semua rute sudah 100%! Mantap!");

                // Group by segment (pakai KAB/KOTA → segment mapping)
                var bySegment = GroupBySegment(result.Data);

                if (bySegment.Count == 0)
                    return BotResponse.Text_("🎉 Semua rute sudah 100%!");

                // Auto-skip: kalau cuma 1 segment, langsung detail
                if (bySegment.Count == 1)
                {
                    var seg = bySegment.First();
                    return FormatDetail(seg.Key, seg.Value, 0);
                }

                // Multi-segment: tampilkan summary menu
                var segmentOrder = bySegment.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
                BotState.Save(nameof(BotIntent.ProgresOutstanding), "pickSegment",
                    new Dictionary<string, string>
                    {
                        ["total"] = result.RowsAfterFilter.ToString(),
                        ["segments"] = string.Join("|", segmentOrder)
                    });

                return FormatSegmentMenu(bySegment, result.RowsAfterFilter);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProgresOutstanding] error: {ex.Message}");
                return BotResponse.Text_($"❌ Gagal: {ex.Message}");
            }
        }

        public async Task<BotResponse?> ResumeAsync(string userMessage, BotPendingState state)
        {
            var step = state.Step;
            var lower = userMessage.Trim().ToLowerInvariant();

            if (step == "pickSegment")
            {
                // User pilih segment (angka 1-6 atau nama)
                var seg = DataSchema.ResolveSegmentChoice(lower, ParseSegmentOrder(state.Get("segments")));
                if (seg == null) return null; // gak match → cancel flow

                // Fetch ulang filtered by segment
                try
                {
                    var result = await _mcp.ResumeBySiteOutstandingAsync(500);
                    if (result?.Data == null) return BotResponse.Text_("📭 Data kosong.");

                    var bySegment = GroupBySegment(result.Data);
                    if (!bySegment.TryGetValue(seg, out var rows) || rows.Count == 0)
                        return BotResponse.Text_($"✅ Segment {seg} sudah semua 100%!");

                    BotState.Save(nameof(BotIntent.ProgresOutstanding), "paginate",
                        new Dictionary<string, string> { ["segment"] = seg, ["offset"] = "0" });

                    return FormatDetail(seg, rows, 0);
                }
                catch (Exception ex)
                {
                    return BotResponse.Text_($"❌ {ex.Message}");
                }
            }

            if (step == "paginate")
            {
                // "lanjut" / "lainnya" / "next"
                if (lower == "lanjut" || lower == "lainnya" || lower == "next" || lower == "more")
                {
                    var seg = state.Get("segment") ?? "";
                    var offset = state.GetInt("offset") + PAGE_SIZE;

                    try
                    {
                        var result = await _mcp.ResumeBySiteOutstandingAsync(500);
                        var bySegment = GroupBySegment(result?.Data ?? new());
                        if (!bySegment.TryGetValue(seg, out var rows))
                            return BotResponse.Text_("📭 Habis.");

                        if (offset >= rows.Count)
                            return BotResponse.Text_("📭 Sudah semua ditampilkan.");

                        BotState.Save(nameof(BotIntent.ProgresOutstanding), "paginate",
                            new Dictionary<string, string> { ["segment"] = seg, ["offset"] = offset.ToString() });

                        return FormatDetail(seg, rows, offset);
                    }
                    catch (Exception ex)
                    {
                        return BotResponse.Text_($"❌ {ex.Message}");
                    }
                }
                return null; // input lain → cancel
            }

            return null;
        }

        // ── Helpers ──────────────────────────────────────────────────

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
                    // Fallback 1: cari di Rute
                    var rute = BotFormatters.FindCol(row, "Rute");
                    seg = DataSchema.ResolveSegmentFromText(rute);
                }
                if (seg == null)
                {
                    // Fallback 2: cek di SITE ID prefix (mis. "JAW-CJV-..." gak punya, skip)
                    Console.WriteLine($"[Outstanding] cannot resolve segment for kota='{kota}' rute='{BotFormatters.FindCol(row, "Rute")}'");
                    seg = "LAINNYA";
                }
                if (!result.ContainsKey(seg)) result[seg] = new();
                result[seg].Add(row);
            }
            return result;
        }

        private static BotResponse FormatSegmentMenu(
            Dictionary<string, List<Dictionary<string, object>>> bySegment, int total)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"🔍 Ada {total} rute belum 100% di {bySegment.Count} segment:");
            sb.AppendLine();

            int i = 1;
            var suggestions = new List<string>();
            foreach (var kv in bySegment.OrderBy(x => x.Key))
            {
                sb.AppendLine($"  {i}. {kv.Key} — {kv.Value.Count} rute");
                suggestions.Add(i.ToString());
                i++;
            }

            sb.AppendLine();
            sb.AppendLine("Pilih segment (ketik angka atau nama):");

            return BotResponse.Menu(sb.ToString().TrimEnd(), suggestions);
        }

        private BotResponse FormatDetail(string segment, List<Dictionary<string, object>> rows, int offset)
        {
            var page = rows.Skip(offset).Take(PAGE_SIZE).ToList();
            var sb = new StringBuilder();
            sb.AppendLine($"🔴 {segment} — belum 100%");
            sb.AppendLine($"📊 Total {rows.Count} rute · halaman {offset / PAGE_SIZE + 1}");
            sb.AppendLine();

            int idx = offset + 1;
            foreach (var row in page)
            {
                var rute = BotFormatters.FindCol(row, "Rute");
                var kota = BotFormatters.FindCol(row, "KAB");
                var siteId = BotFormatters.FindCol(row, "SITE ID");

                var kPlan = BotFormatters.FindNum(row, "Kabel", "Plan");
                var kProg = BotFormatters.FindNum(row, "Kabel", "Progress");
                var t7Plan = BotFormatters.FindNum(row, "7m", "Plan");
                var t7Prog = BotFormatters.FindNum(row, "7m", "Progress");
                var t9Plan = BotFormatters.FindNum(row, "9m", "Plan");
                var t9Prog = BotFormatters.FindNum(row, "9m", "Progress");

                var kPct = kPlan > 0 ? kProg / kPlan * 100 : 0;
                var t7Pct = t7Plan > 0 ? t7Prog / t7Plan * 100 : 0;
                var t9Pct = t9Plan > 0 ? t9Prog / t9Plan * 100 : 0;

                sb.AppendLine($"━ #{idx}. {BotFormatters.Trunc(rute, 50)}");
                if (siteId != "-") sb.Append($"   🆔 {siteId}");
                if (kota != "-") sb.Append(siteId != "-" ? $" · 📍 {kota}\n" : $"   📍 {kota}\n");
                else if (siteId != "-") sb.AppendLine();
                sb.AppendLine($"   {BotFormatters.StatusIcon(kPct)} Kabel  {kProg:N0}/{kPlan:N0} m ({kPct:N0}%)");
                sb.AppendLine($"   {BotFormatters.StatusIcon(t7Pct)} T7m   {t7Prog:N0}/{t7Plan:N0} btg ({t7Pct:N0}%)");
                sb.AppendLine($"   {BotFormatters.StatusIcon(t9Pct)} T9m   {t9Prog:N0}/{t9Plan:N0} btg ({t9Pct:N0}%)");
                sb.AppendLine();
                idx++;
            }

            var remaining = rows.Count - offset - page.Count;
            if (remaining > 0)
            {
                sb.AppendLine($"📄 +{remaining} rute lagi · ketik `lanjut` untuk halaman berikutnya");
                return new BotResponse { Text = sb.ToString().TrimEnd(), HasPendingState = true };
            }

            sb.AppendLine("Legenda: ✅ selesai · 🟡 jalan · 🔴 belum");
            return BotResponse.Text_(sb.ToString().TrimEnd());
        }

        private static List<string> ParseSegmentOrder(string? raw)
            => string.IsNullOrWhiteSpace(raw)
                ? new List<string>()
                : raw.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }
}
