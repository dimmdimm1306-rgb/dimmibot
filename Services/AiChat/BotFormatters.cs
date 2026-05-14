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

        /// <summary>Status overall berdasarkan threshold yang baru:
        /// &lt;30% = belum, 30-70% = sedang progres, &gt;=70% = hampir/selesai.
        /// Input: ratio 0..1 (atau >1 over).</summary>
        public static (string icon, string label, string status) StatusByOverall(double ratio)
        {
            if (ratio >= 1.0) return ("✅", "Selesai", "done");
            if (ratio >= 0.7) return ("🟢", "Hampir selesai", "almost");
            if (ratio >= 0.3) return ("🟡", "Sedang progres", "progress");
            if (ratio > 0)    return ("🟠", "Baru mulai", "started");
            return ("🔴", "Belum dikerjakan", "not_started");
        }

        /// <summary>Hitung overall ratio dari 3 kategori (kabel, T7, T9).
        /// Average dari kategori yang plan > 0.</summary>
        public static double ComputeOverall(double kPlan, double kProg, double t7Plan, double t7Prog, double t9Plan, double t9Prog)
        {
            double sum = 0;
            int n = 0;
            if (kPlan > 0)  { sum += kProg / kPlan;  n++; }
            if (t7Plan > 0) { sum += t7Prog / t7Plan; n++; }
            if (t9Plan > 0) { sum += t9Prog / t9Plan; n++; }
            return n > 0 ? sum / n : 0;
        }

        // ── Padding for column alignment ──────────────────────────────

        /// <summary>Pad string ke kanan sampai panjang width (untuk alignment kolom).</summary>
        public static string PadR(string s, int width)
        {
            s ??= "";
            if (s.Length >= width) return s.Substring(0, width);
            return s + new string(' ', width - s.Length);
        }

        /// <summary>Pad string ke kiri (untuk angka kanan-rata).</summary>
        public static string PadL(string s, int width)
        {
            s ??= "";
            if (s.Length >= width) return s.Substring(s.Length - width);
            return new string(' ', width - s.Length) + s;
        }

        /// <summary>Format kolom: label + value sejajar dengan separator.</summary>
        public static string Row(string label, int labelWidth, string value)
            => $"{PadR(label, labelWidth)} {value}";

        public static string Row3(string c1, int w1, string c2, int w2, string c3, int w3)
            => $"{PadR(c1, w1)}  {PadL(c2, w2)}  {PadL(c3, w3)}";

        public static string Row4(string c1, int w1, string c2, int w2, string c3, int w3, string c4, int w4)
            => $"{PadR(c1, w1)}  {PadL(c2, w2)}  {PadL(c3, w3)}  {PadL(c4, w4)}";

        // ── Table builder (header + separator + rows) ───────────────

        /// <summary>
        /// Build tabular block: Label kiri-rata, kolom angka kanan-rata.
        /// labelWidth = lebar kolom label, numWidths = lebar kolom angka.
        /// header[0] = label header, header[1..] = num headers.
        /// </summary>
        public static StringBuilder AppendTable(StringBuilder sb,
            string[] headers, int labelWidth, int[] numWidths)
        {
            // Header row
            var hdr = PadR(headers[0], labelWidth);
            for (int i = 1; i < headers.Length && i - 1 < numWidths.Length; i++)
                hdr += "  " + PadL(headers[i], numWidths[i - 1]);
            sb.AppendLine(hdr);

            // Separator
            int total = labelWidth;
            foreach (var w in numWidths) total += 2 + w;
            sb.AppendLine(new string('─', total));
            return sb;
        }

        public static string TableRow(string label, int labelWidth, params (string val, int width)[] cols)
        {
            var s = PadR(label, labelWidth);
            foreach (var (v, w) in cols)
                s += "  " + PadL(v, w);
            return s;
        }

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
