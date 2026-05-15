using System.Text.RegularExpressions;
using StokBarangMAUI.Models.Bot;

namespace StokBarangMAUI.Services.AiChat
{
    /// <summary>
    /// Result dari intent classification. Carry ctx (extracted entities) ke flow.
    /// </summary>
    public class IntentMatch
    {
        public BotIntent Intent { get; init; }
        public Dictionary<string, string> Context { get; init; } = new();

        public static IntentMatch Of(BotIntent intent, Dictionary<string, string>? ctx = null)
            => new() { Intent = intent, Context = ctx ?? new() };
    }

    /// <summary>
    /// Decision tree intent classification. Pure function: input string → IntentMatch.
    /// Tidak panggil tool atau MCP. Cuma regex/keyword matching.
    /// Order penting: command pasti dicek dulu, lalu pattern A/B/C/D, fallback E.
    /// </summary>
    public static class BotIntentRouter
    {
        public static IntentMatch Classify(string userMessage)
        {
            var raw = (userMessage ?? "").Trim();
            var lower = raw.ToLowerInvariant();

            if (string.IsNullOrEmpty(lower))
                return IntentMatch.Of(BotIntent.Unknown);

            // ── System commands (always priority 1) ───────────────────
            if (BotTokens.ContainsAny(lower, BotTokens.HelpWords))
                return IntentMatch.Of(BotIntent.Help);

            if (BotTokens.ContainsAny(lower, BotTokens.RefreshWords))
                return IntentMatch.Of(BotIntent.Refresh);

            if (BotTokens.ContainsAny(lower, BotTokens.CancelWords))
                return IntentMatch.Of(BotIntent.Cancel);

            // ── ALAMAT (Pattern A — single-shot) ──────────────────────
            if (BotTokens.ContainsAny(lower, BotTokens.AlamatWords))
            {
                var gudang = ExtractGudangName(lower);
                var ctx = new Dictionary<string, string>();
                if (gudang != null) ctx["gudang"] = gudang;
                return IntentMatch.Of(BotIntent.AlamatGudang, ctx);
            }

            // ── WEATHER ───────────────────────────────────────────────
            if (Regex.IsMatch(lower, @"\b(cuaca|weather|hujan|panas|dingin|udara|temperatur)\b"))
            {
                var ctx = new Dictionary<string, string>();
                // Detect "cuaca [kota]"
                var m = Regex.Match(lower,
                    @"\b(cuaca|weather)\s+(?:di\s+)?(?<city>[a-z][a-z\s]{2,30})",
                    RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    var city = m.Groups["city"].Value.Trim().TrimEnd('?', '!', '.', ',');
                    city = Regex.Replace(city, @"\b(hari\s*ini|sekarang|today|kemarin|besok|lusa|nanti|prediksi|prakiraan|forecast|gimana|bagaimana|dong|ya)\b", " ", RegexOptions.IgnoreCase);
                    city = Regex.Replace(city, @"\b(dan|atau)\b", " ", RegexOptions.IgnoreCase);
                    city = Regex.Replace(city, @"\s+", " ").Trim();
                    if (!string.IsNullOrWhiteSpace(city))
                        ctx["city"] = city;
                }
                return IntentMatch.Of(BotIntent.Weather, ctx);
            }

            // ── TIME QUERY ────────────────────────────────────────────
            // Pure time/calendar questions only. JANGAN match kalau ada subject lain (progres/cuaca/sj/dll).
            // "hari apa", "tanggal berapa", "besok tanggal berapa", "hari ini ada event apa", dll
            if (Regex.IsMatch(lower, @"\b(hari\s+(apa|ini\s+(apa|tanggal|ada|event|agenda))|tanggal\s+(apa|berapa|merah)|tgl\s+(apa|berapa)|jam\s+berapa|pukul\s+berapa|bulan\s+(apa|berapa|sekarang|ini)|tahun\s+(apa|berapa|sekarang|ini)|sekarang\s+(jam|tanggal|tgl|hari|bulan|tahun)|besok\s+(tanggal|hari)|kemarin\s+(tanggal|hari)|lusa\s+(tanggal|hari)|event|agenda|hari\s+besar|libur|cuti|setahun\s+ini)\b") ||
                Regex.IsMatch(lower, @"^\s*(jam|hari|tanggal|tgl|bulan|tahun|waktu|event|agenda)\s*(berapa|apa|sekarang|ini)?\s*\??\s*$"))
            {
                return IntentMatch.Of(BotIntent.TimeQuery,
                    new Dictionary<string, string> { ["subQuery"] = lower });
            }

            // ── PROGRES ──────────────────────────────────────────────
            if (BotTokens.ContainsAny(lower, BotTokens.ProgresWords))
            {
                // 1d: "progres total" / "progres overall"
                if (Regex.IsMatch(lower, @"\b(total|overall|keseluruhan|semua|all|grand)\b"))
                    return IntentMatch.Of(BotIntent.ProgresTotal);

                // 1a: "yang belum"
                if (BotTokens.ContainsAny(lower, BotTokens.OutstandingWords) ||
                    BotTokens.ContainsAny(lower, BotTokens.ZeroProgresWords))
                    return IntentMatch.Of(BotIntent.ProgresOutstanding);

                // 1e: "yang selesai"
                if (BotTokens.ContainsAny(lower, BotTokens.DoneWords))
                    return IntentMatch.Of(BotIntent.ProgresDone);

                // 1c: tanggal
                if (HasDateContext(lower))
                    return IntentMatch.Of(BotIntent.ProgresDate, ExtractDateContext(lower));

                // 1b: "progres site X"
                var query = BotTokens.FindSiteOrRuteQuery(raw);
                if (query != null)
                    return IntentMatch.Of(BotIntent.ProgresSiteSearch,
                        new Dictionary<string, string> { ["query"] = query });

                // 1d: "progres brebes" tanpa keyword status → arahkan ke total
                var seg = ExtractSegmentName(lower);
                if (seg != null)
                    return IntentMatch.Of(BotIntent.ProgresTotal,
                        new Dictionary<string, string> { ["segment"] = seg });

                // Pattern D: vague "cek progres"
                return IntentMatch.Of(BotIntent.ProgresMenu);
            }

            // ── Site/Rute search tanpa "progres" prefix ─────────────
            // Misal: "site 0244", "rute brebes" → langsung site search
            if (Regex.IsMatch(lower, @"^(cek\s+)?(site|rute|span)\s+", RegexOptions.IgnoreCase) ||
                Regex.IsMatch(lower, @"\b(site|rute)\s+(yang\s+)?(belum|outstanding)\b", RegexOptions.IgnoreCase))
            {
                if (BotTokens.ContainsAny(lower, BotTokens.OutstandingWords) ||
                    BotTokens.ContainsAny(lower, BotTokens.ZeroProgresWords))
                    return IntentMatch.Of(BotIntent.ProgresOutstanding);

                if (BotTokens.ContainsAny(lower, BotTokens.DoneWords))
                    return IntentMatch.Of(BotIntent.ProgresDone);

                var q = BotTokens.FindSiteOrRuteQuery(raw);
                if (q != null)
                    return IntentMatch.Of(BotIntent.ProgresSiteSearch,
                        new Dictionary<string, string> { ["query"] = q });
            }

            // ── KEBUTUHAN MATERIAL (cek dulu sebelum stok karena lebih spesifik) ─
            if (BotTokens.ContainsAny(lower, BotTokens.KebutuhanWords))
            {
                if (BotTokens.ContainsAny(lower, BotTokens.KurangMaterialWords) ||
                    BotTokens.ContainsAny(lower, BotTokens.OutstandingWords))
                    return IntentMatch.Of(BotIntent.KebutuhanKurang);

                var seg = ExtractSegmentName(lower);
                if (seg != null)
                    return IntentMatch.Of(BotIntent.KebutuhanHomebase,
                        new Dictionary<string, string> { ["segment"] = seg });

                return IntentMatch.Of(BotIntent.KebutuhanHomebase);
            }

            // ── STOK ─────────────────────────────────────────────────
            if (BotTokens.ContainsAny(lower, BotTokens.StokWords))
            {
                if (BotTokens.ContainsAny(lower, BotTokens.KritisWords))
                    return IntentMatch.Of(BotIntent.StokKritis);

                // "stok di brebes" / "stok brebes"
                var seg = ExtractSegmentName(lower);
                var material = ExtractMaterial(lower);

                if (material != null)
                {
                    var ctx = new Dictionary<string, string> { ["material"] = material };
                    if (seg != null) ctx["segment"] = seg;
                    return IntentMatch.Of(BotIntent.StokMaterial, ctx);
                }
                if (seg != null)
                    return IntentMatch.Of(BotIntent.StokGudang,
                        new Dictionary<string, string> { ["segment"] = seg });

                // Pattern D: vague "cek stok"
                return IntentMatch.Of(BotIntent.StokMenu);
            }

            // ── SURAT JALAN ─────────────────────────────────────────
            if (BotTokens.ContainsAny(lower, BotTokens.SuratJalanWords))
            {
                // "sj-001"
                var noSj = Regex.Match(lower, @"\bsj[\s\-]*(\d+)", RegexOptions.IgnoreCase);
                if (noSj.Success && noSj.Groups[1].Value.Length >= 2)
                    return IntentMatch.Of(BotIntent.SuratJalanNomor,
                        new Dictionary<string, string> { ["nomor"] = noSj.Groups[1].Value });

                // "sj terakhir" / "sj terbaru" — cek dulu sebelum date
                if (BotTokens.ContainsAny(lower, BotTokens.LatestWords))
                    return IntentMatch.Of(BotIntent.SuratJalanLatest);

                if (HasDateContext(lower))
                    return IntentMatch.Of(BotIntent.SuratJalanDate, ExtractDateContext(lower));

                if (Regex.IsMatch(lower, @"\b(masuk|keluar|dibawa)\b"))
                {
                    var jenis = Regex.Match(lower, @"\b(masuk|keluar|dibawa)\b").Value.ToUpperInvariant();
                    return IntentMatch.Of(BotIntent.SuratJalanJenis,
                        new Dictionary<string, string> { ["jenis"] = jenis });
                }

                return IntentMatch.Of(BotIntent.SuratJalanMenu);
            }

            // ── Site/segment standalone ("brebes", "sragen") ────────
            // Ini agak berbahaya karena ambiguous (bisa progres/stok). Anggap ProgresTotal default.
            var standaloneSeg = ExtractSegmentName(lower);
            if (standaloneSeg != null && lower.Length < 20)
                return IntentMatch.Of(BotIntent.ProgresTotal,
                    new Dictionary<string, string> { ["segment"] = standaloneSeg });

            // ── Fallback: ChatGeneral (Pattern E) ────────────────────
            return IntentMatch.Of(BotIntent.ChatGeneral);
        }

        // ── Entity extraction helpers ─────────────────────────────────

        /// <summary>Extract nama gudang/segment dari teks. Return canonical UPPERCASE atau null.</summary>
        public static string? ExtractSegmentName(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            // Cek nama segment langsung (dan kota-nya)
            foreach (var kv in DataSchema.KotaToSegment)
            {
                var kw = kv.Key.ToLowerInvariant();
                if (Regex.IsMatch(text, $@"\b{Regex.Escape(kw)}\b", RegexOptions.IgnoreCase))
                    return kv.Value;
            }
            return null;
        }

        public static string? ExtractGudangName(string text)
            => ExtractSegmentName(text); // Gudang = segment dalam project ini

        public static string? ExtractMaterial(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            var lower = text.ToLowerInvariant();
            // Common material keywords
            if (Regex.IsMatch(lower, @"\bkabel\s*(?:24c?|24\s*core)?\b") || Regex.IsMatch(lower, @"\bcable\s*24c?\b"))
                return "Cable 24C";
            if (Regex.IsMatch(lower, @"\bkabel\s*12c?\b") || Regex.IsMatch(lower, @"\bcable\s*12c?\b"))
                return "Cable 12C";
            if (Regex.IsMatch(lower, @"\btiang\s*7\s*m?\b") || Regex.IsMatch(lower, @"\bt7\b"))
                return "Tiang 7M";
            if (Regex.IsMatch(lower, @"\btiang\s*9\s*m?\b") || Regex.IsMatch(lower, @"\bt9\b"))
                return "Tiang 9M";
            if (Regex.IsMatch(lower, @"\bclosure\b"))
                return "Closure";
            if (Regex.IsMatch(lower, @"\bodp\b") || Regex.IsMatch(lower, @"\b8\s*port\b"))
                return "ODP";
            if (Regex.IsMatch(lower, @"\bstrength\s*clamp\b") || Regex.IsMatch(lower, @"\bclamp\b"))
                return "Strength Clamp";
            if (Regex.IsMatch(lower, @"\bx\s*frame\b"))
                return "X Frame";
            if (Regex.IsMatch(lower, @"\bpigtail\b"))
                return "Pigtail";
            if (Regex.IsMatch(lower, @"\bpatchcord\b"))
                return "Patchcord";
            return null;
        }

        public static bool HasDateContext(string text)
        {
            return BotTokens.ContainsAny(text, BotTokens.HariIniWords) ||
                   BotTokens.ContainsAny(text, BotTokens.KemarinWords) ||
                   BotTokens.ContainsAny(text, BotTokens.MingguIniWords) ||
                   BotTokens.ContainsAny(text, BotTokens.BulanIniWords) ||
                   BotTokens.FindMonth(text) != null ||
                   Regex.IsMatch(text, @"\btanggal\s+\d", RegexOptions.IgnoreCase) ||
                   Regex.IsMatch(text, @"\btgl\s+\d", RegexOptions.IgnoreCase);
        }

        public static Dictionary<string, string> ExtractDateContext(string text)
        {
            var ctx = new Dictionary<string, string>();
            if (BotTokens.ContainsAny(text, BotTokens.HariIniWords)) ctx["dateIntent"] = "date_today";
            else if (BotTokens.ContainsAny(text, BotTokens.KemarinWords)) ctx["dateIntent"] = "date_yesterday";
            else if (BotTokens.ContainsAny(text, BotTokens.MingguIniWords)) ctx["dateIntent"] = "date_week";
            else if (BotTokens.ContainsAny(text, BotTokens.BulanIniWords)) ctx["dateIntent"] = "date_month";

            var month = BotTokens.FindMonth(text);
            if (month != null) ctx["month"] = month;

            var day = BotTokens.FindDay(text);
            if (day.HasValue) ctx["day"] = day.Value.ToString();

            return ctx;
        }
    }
}
