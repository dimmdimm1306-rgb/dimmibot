using System.Text;
using System.Text.Json;

namespace StokBarangMAUI.Services.AiChat
{
    /// <summary>
    /// Helpers untuk format output bot — angka, persen, baris pemisah, emoji status.
    /// Dipakai oleh semua BotFlow supaya tampilan konsisten.
    /// </summary>
    public static class BotFormatters
    {
        // ── Number parsing (handles "44.000" Indonesian thousand vs "2000.0" decimal) ─

        /// <summary>
        /// Parse angka. Auto-detect:
        /// - Sudah numeric (double/int/JsonElement) → langsung pakai.
        /// - String "44.000" "220.000" "14.272" → thousand separator (semua grup setelah dot pertama harus 3 digit).
        /// - String "2000.0" "0.7476" → decimal.
        /// - String "(6.522)" → negatif accounting.
        /// </summary>
        public static double ParseNumber(object? val)
        {
            if (val == null) return 0;

            if (val is double d) return d;
            if (val is float f) return f;
            if (val is int i) return i;
            if (val is long l) return l;
            if (val is decimal dec) return (double)dec;
            if (val is JsonElement je)
            {
                if (je.ValueKind == JsonValueKind.Number && je.TryGetDouble(out var jd)) return jd;
                if (je.ValueKind == JsonValueKind.Null) return 0;
                val = je.ToString();
            }

            var s = val.ToString();
            if (string.IsNullOrWhiteSpace(s) || s == "null" || s == "-" || s.Equals("nan", StringComparison.OrdinalIgnoreCase))
                return 0;
            s = s.Trim();
            bool negative = s.StartsWith("(") && s.EndsWith(")");
            if (negative) s = s.Trim('(', ')').Trim();

            // Indonesian thousand: "44.000" → 44000 (semua grup setelah dot pertama tepat 3 digit)
            if (s.Contains('.') && !s.Contains(','))
            {
                var parts = s.Split('.');
                bool isThousand = parts.Length >= 2 &&
                    parts.Skip(1).All(p => p.Length == 3 && p.All(char.IsDigit));
                if (isThousand)
                {
                    var joined = string.Concat(parts);
                    if (double.TryParse(joined, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out var thousand))
                        return negative ? -thousand : thousand;
                }
            }

            // Direct parse
            if (double.TryParse(s, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var direct))
                return negative ? -direct : direct;

            // Comma decimal "1,25"
            s = s.Replace(".", "").Replace(",", ".");
            if (double.TryParse(s, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var d2))
                return negative ? -d2 : d2;
            return 0;
        }

        public static string FormatNum(double v)
        {
            if (v == 0) return "0";
            var n = Math.Abs(v).ToString("N0").Replace(",", ".");
            return v < 0 ? $"({n})" : n;
        }

        public static string FormatPct(double ratio01)
        {
            // ratio01: 0..1 (atau >1 kalau over)
            if (ratio01 >= 0 && ratio01 <= 2.0)
                return $"{ratio01 * 100:N0}%";
            return $"{ratio01:N0}%";
        }

        // ── Status emoji ───────────────────────────────────────────────

        /// <summary>Emoji status untuk persentase 0..100+.</summary>
        public static string StatusIcon(double pct100)
            => pct100 >= 100 ? "✅" : pct100 > 0 ? "🟡" : "🔴";

        public static string StatusIconRatio(double r01) => StatusIcon(r01 * 100);

        // ── Section dividers ──────────────────────────────────────────

        public const string DividerLine = "━━━━━━━━━━━━━━━━━━━━━━━";
        public const string ThinDivider = "───────────────";

        // ── Truncation ────────────────────────────────────────────────

        public static string Trunc(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            if (s.Length <= max) return s;
            return s.Substring(0, max - 1) + "…";
        }

        // ── Row / column extractors (untuk Dictionary<string,object> dari MCP) ──

        /// <summary>Cari kolom yang nama-nya match keyword (substring, case-insensitive).</summary>
        public static string FindCol(IDictionary<string, object> row, string keyword)
        {
            foreach (var kv in row)
                if (kv.Key.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    var s = kv.Value?.ToString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(s) && s != "null") return s;
                }
            return "-";
        }

        /// <summary>Cari kolom yang nama-nya match 2 keyword sekaligus (e.g. "Kabel" + "Plan").</summary>
        public static double FindNum(IDictionary<string, object> row, string kw1, string kw2)
        {
            foreach (var kv in row)
            {
                var key = kv.Key;
                if (key.Contains(kw1, StringComparison.OrdinalIgnoreCase) &&
                    key.Contains(kw2, StringComparison.OrdinalIgnoreCase))
                    return ParseNumber(kv.Value);
            }
            return 0;
        }

        public static string GetString(IDictionary<string, object> row, params string[] keys)
        {
            foreach (var k in keys)
            {
                if (row.TryGetValue(k, out var v) && v != null)
                {
                    var s = v.ToString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(s) && s != "null") return s;
                }
            }
            return "-";
        }

        // ── Header builder ────────────────────────────────────────────

        public static string Header(string emoji, string title) => $"{emoji} {title}";

        public static string SectionHeader(string label) => $"━━ {label} ━━";

        // ── List builders ─────────────────────────────────────────────

        public static StringBuilder StartReply() => new();

        public static void AppendDivider(this StringBuilder sb)
            => sb.AppendLine(DividerLine);

        public static void AppendKv(this StringBuilder sb, string label, string value)
            => sb.AppendLine($"{label}: {value}");
    }
}
