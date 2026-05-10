namespace StokBarangMAUI.Models
{
    public class SpanItem
    {
        public int    No            { get; set; }
        public string Rute          { get; set; } = "";
        public string Kab           { get; set; } = "";

        public int    KabelPlan     { get; set; }
        public int    KabelProgress { get; set; }
        public string KabelPct      { get; set; } = "0%";

        public int    T7Plan        { get; set; }
        public int    T7Progress    { get; set; }
        public string T7Pct         { get; set; } = "0%";

        public int    T9Plan        { get; set; }
        public int    T9Progress    { get; set; }
        public string T9Pct         { get; set; } = "0%";

        public string Status        { get; set; } = "";
        public string TimeLine      { get; set; } = "";

        // ── Computed ──────────────────────────────────────────────────────
        public double KabelPctVal => ParsePct(KabelPct);
        public double T7PctVal    => ParsePct(T7Pct);
        public double T9PctVal    => ParsePct(T9Pct);
        public double OverallPct  => (KabelPctVal + T7PctVal + T9PctVal) / 3.0;

        public string OverallPctText => $"{OverallPct:P0}";
        public string OverallColor   => OverallPct >= 0.8 ? "#16A34A" : OverallPct >= 0.4 ? "#D97706" : "#DC2626";

        public bool   IsOK        => Status.Contains("OK", StringComparison.OrdinalIgnoreCase)
                                  && !Status.Contains("NOK", StringComparison.OrdinalIgnoreCase);
        public string StatusColor => IsOK ? "#DCFCE7" : "#FEE2E2";
        public string StatusBadge => IsOK ? "✓ OK" : "✗ NOK";
        public string StatusTextColor => IsOK ? "#16A34A" : "#DC2626";

        public string KabelBar => MakeBar(KabelPctVal);
        public string T7Bar    => MakeBar(T7PctVal);
        public string T9Bar    => MakeBar(T9PctVal);

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
}
