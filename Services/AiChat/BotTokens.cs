using System.Text.RegularExpressions;

namespace StokBarangMAUI.Services.AiChat
{
    /// <summary>
    /// Kamus semantic token dipakai untuk intent classification di BotIntentRouter.
    /// Centralized supaya gampang nambah sinonim baru tanpa nyebar.
    /// </summary>
    public static class BotTokens
    {
        // ── Status / outcome words ─────────────────────────────────────
        public static readonly string[] OutstandingWords =
        {
            "belum", "outstanding", "blm", "kurang", "blum", "belom",
            "ga selesai", "gak selesai", "gak done", "ga done",
        };

        public static readonly string[] DoneWords =
        {
            "selesai", "done", "komplit", "100%", "tuntas", "kelar",
        };

        public static readonly string[] ZeroProgresWords =
        {
            "0%", "kosong", "nol persen", "progres 0", "progress 0", "belum mulai",
        };

        public static readonly string[] KritisWords =
        {
            "habis", "kritis", "kosong", "tipis", "menipis",
        };

        public static readonly string[] KurangMaterialWords =
        {
            "kurang", "kekurangan", "shortage", "minus",
        };

        // ── Date words ────────────────────────────────────────────────
        public static readonly string[] HariIniWords =
        {
            "hari ini", "today", "skrg", "sekarang",
        };

        public static readonly string[] KemarinWords =
        {
            "kemarin", "kmrn", "kmaren", "yesterday",
        };

        public static readonly string[] MingguIniWords =
        {
            "minggu ini", "7 hari", "seminggu", "weekly",
        };

        public static readonly Dictionary<string, string> IndonesianMonths = new(StringComparer.OrdinalIgnoreCase)
        {
            { "januari", "Januari" }, { "jan", "Januari" },
            { "februari", "Februari" }, { "feb", "Februari" },
            { "maret", "Maret" }, { "mar", "Maret" },
            { "april", "April" }, { "apr", "April" },
            { "mei", "Mei" },
            { "juni", "Juni" }, { "jun", "Juni" },
            { "juli", "Juli" }, { "jul", "Juli" },
            { "agustus", "Agustus" }, { "agu", "Agustus" }, { "agt", "Agustus" },
            { "september", "September" }, { "sep", "September" }, { "sept", "September" },
            { "oktober", "Oktober" }, { "okt", "Oktober" },
            { "november", "November" }, { "nov", "November" },
            { "desember", "Desember" }, { "des", "Desember" },
        };

        // ── Subject keywords (data type) ──────────────────────────────
        public static readonly string[] StokWords =
        {
            "stok", "stock", "material gudang", "stok gudang", "aktual stok", "stok aktual", "inventory",
        };

        public static readonly string[] LatestWords =
        {
            "terakhir", "terbaru", "latest", "paling baru", "yang baru", "recent",
        };

        public static readonly string[] KebutuhanWords =
        {
            "kebutuhan", "kebutuhan material", "stok kebutuhan", "vol kebutuhan", "mrf", "planning",
        };

        public static readonly string[] ProgresWords =
        {
            "progres", "progress", "kemajuan", "pengerjaan", "pekerjaan",
        };

        public static readonly string[] SuratJalanWords =
        {
            "surat jalan", "sj", "delivery order", "do",
        };

        public static readonly string[] AlamatWords =
        {
            "alamat", "lokasi", "lokasi gudang", "address",
        };

        public static readonly string[] SiteWords = { "site", "site id", "siteid" };
        public static readonly string[] RuteWords = { "rute", "route", "span" };

        // ── Cancel / reset ────────────────────────────────────────────
        public static readonly string[] CancelWords =
        {
            "batal", "cancel", "stop", "keluar", "exit", "quit",
        };

        public static readonly string[] HelpWords =
        {
            "/help", "/bantu", "/menu", "help", "bantu", "bantuan", "menu",
        };

        public static readonly string[] RefreshWords =
        {
            "refresh", "reload", "muat ulang", "update data",
        };

        // ── Helper methods ────────────────────────────────────────────

        /// <summary>True kalau text mengandung salah satu word (case-insensitive, word boundary aware).</summary>
        public static bool ContainsAny(string text, IEnumerable<string> words)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            var lower = text.ToLowerInvariant();
            foreach (var w in words)
            {
                if (string.IsNullOrEmpty(w)) continue;
                // Word containing space → just check substring
                if (w.Contains(' '))
                {
                    if (lower.Contains(w, StringComparison.OrdinalIgnoreCase)) return true;
                }
                else
                {
                    var pattern = $@"\b{Regex.Escape(w)}\b";
                    if (Regex.IsMatch(lower, pattern, RegexOptions.IgnoreCase)) return true;
                }
            }
            return false;
        }

        /// <summary>Cari nama bulan Indonesia di teks. Return canonical (e.g. "Mei") atau null.</summary>
        public static string? FindMonth(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            var lower = text.ToLowerInvariant();
            foreach (var kv in IndonesianMonths)
            {
                if (Regex.IsMatch(lower, $@"\b{Regex.Escape(kv.Key)}\b"))
                    return kv.Value;
            }
            return null;
        }

        /// <summary>Extract day number (1-31) dari teks. Return null kalau gak ada.</summary>
        public static int? FindDay(string text)
        {
            var m = Regex.Match(text ?? "", @"\b(?:tanggal|tgl)?\s*([0-9]{1,2})\b");
            if (m.Success && int.TryParse(m.Groups[1].Value, out var d) && d >= 1 && d <= 31)
                return d;
            return null;
        }

        /// <summary>Extract site/rute query (e.g. "0244", "JAW-CJV-0172", "JC2") setelah keyword "site"/"rute".</summary>
        public static string? FindSiteOrRuteQuery(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            var m = Regex.Match(text, @"\b(?:site|rute|span)\s+(?<q>[a-zA-Z0-9][a-zA-Z0-9\-_\.]{2,})", RegexOptions.IgnoreCase);
            if (m.Success) return m.Groups["q"].Value;
            // Or pattern bare "JAW-CJV-XXX" / "JC2_xxx" / 4-digit number
            m = Regex.Match(text, @"\b(?<q>JAW-[A-Za-z0-9\-]+|JC\d+_\d+|\d{4})\b", RegexOptions.IgnoreCase);
            if (m.Success) return m.Groups["q"].Value;
            return null;
        }
    }
}
