namespace StokBarangMAUI.Models
{
    public class ProgressResumeItem
    {
        public int    No   { get; set; }
        public string Rute { get; set; } = "";   // = Segment (berisi nama-nama kota)

        // Penarikan Kabel 24C
        public int    KabelPlan     { get; set; }
        public int    KabelProgress { get; set; }
        public string KabelPct      { get; set; } = "0%";

        // Penanaman Tiang 7m
        public int    T7Plan     { get; set; }
        public int    T7Progress { get; set; }
        public string T7Pct      { get; set; } = "0%";

        // Penanaman Tiang 9m
        public int    T9Plan     { get; set; }
        public int    T9Progress { get; set; }
        public string T9Pct      { get; set; } = "0%";

        // Terminasi
        public string Status   { get; set; } = "";
        public string TimeLine { get; set; } = "";

        // ── Card color per segment ──────────────────────────────────────
        public string CardColor => No switch
        {
            1 => "#1E40AF", 2 => "#0E7490", 3 => "#166534",
            4 => "#9A3412", 5 => "#6D28D9", 6 => "#9D174D",
            7 => "#065F46", 8 => "#7C2D12", 9 => "#1E3A5F",
            10=> "#44403C", _ => "#374151"
        };
        public string CardColorLight => No switch
        {
            1 => "#DBEAFE", 2 => "#CFFAFE", 3 => "#DCFCE7",
            4 => "#FFEDD5", 5 => "#EDE9FE", 6 => "#FCE7F3",
            7 => "#D1FAE5", 8 => "#FEE2E2", 9 => "#E0F2FE",
            10=> "#F5F5F4", _ => "#F3F4F6"
        };
        public string CardColorMid => No switch
        {
            1 => "#3B82F6", 2 => "#06B6D4", 3 => "#22C55E",
            4 => "#F97316", 5 => "#A855F7", 6 => "#EC4899",
            7 => "#10B981", 8 => "#EF4444", 9 => "#38BDF8",
            10=> "#A8A29E", _ => "#6B7280"
        };

        // ── Computed ────────────────────────────────────────────────────
        // Hitung dari nilai aktual agar akurat (bypass format % dari Google Sheets)
        public double KabelPctVal   => KabelPlan > 0 ? (double)KabelProgress / KabelPlan : ParsePct(KabelPct);
        public double T7PctVal      => T7Plan    > 0 ? (double)T7Progress    / T7Plan    : ParsePct(T7Pct);
        public double T9PctVal      => T9Plan    > 0 ? (double)T9Progress    / T9Plan    : ParsePct(T9Pct);

        public string KabelPctDisplay => $"{KabelPctVal:P0}";
        public string T7PctDisplay    => $"{T7PctVal:P0}";
        public string T9PctDisplay    => $"{T9PctVal:P0}";
        public double OverallPct    => (KabelPctVal + T7PctVal + T9PctVal) / 3.0;

        public string RuteShort
        {
            get
            {
                var parts = Rute.Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                return parts.Length >= 2 ? $"{parts[0]} - {parts[1]}" : Rute.Trim();
            }
        }

        public string OverallPctText => $"{OverallPct:P0}";
        public string OverallColor   => OverallPct >= 0.8 ? "#16A34A" : OverallPct >= 0.4 ? "#D97706" : "#DC2626";
        public string OverallBg      => OverallPct >= 0.8 ? "#DCFCE7" : OverallPct >= 0.4 ? "#FEF3C7" : "#FEE2E2";

        public bool   IsOK        => Status.Contains("OK", StringComparison.OrdinalIgnoreCase)
                                  && !Status.Contains("NOK", StringComparison.OrdinalIgnoreCase);
        public string StatusColor => IsOK ? "#16A34A" : "#DC2626";
        public string StatusBg    => IsOK ? "#DCFCE7" : "#FEE2E2";
        public string StatusBadge => IsOK ? "✓ OK" : "✗ NOK";

        // Mini text bar (10 blok)
        public string KabelBar => MakeBar(KabelPctVal);
        public string T7Bar    => MakeBar(T7PctVal);
        public string T9Bar    => MakeBar(T9PctVal);

        // Daftar kota untuk filter
        public IEnumerable<string> KotaList =>
            Rute.Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        private static string MakeBar(double v)
        {
            int n = (int)Math.Round(v * 10);
            n = Math.Max(0, Math.Min(10, n));
            return new string('█', n) + new string('░', 10 - n);
        }

        private static double ParsePct(string s)
        {
            var c = s.Replace("%", "").Replace(" ", "").Trim();
            return double.TryParse(c, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double v) ? v / 100.0 : 0;
        }
    }

    public class ProgressResumeTotal
    {
        public int    KabelPlan     { get; set; }
        public int    KabelProgress { get; set; }
        public string KabelPct      { get; set; } = "0%";
        public int    T7Plan        { get; set; }
        public int    T7Progress    { get; set; }
        public string T7Pct         { get; set; } = "0%";
        public int    T9Plan        { get; set; }
        public int    T9Progress    { get; set; }
        public string T9Pct         { get; set; } = "0%";
    }
}
