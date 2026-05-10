namespace StokBarangMAUI.Models
{
    public class StokHomebaseItem
    {
        public string Homebase { get; set; } = string.Empty;
        public string Segment  { get; set; } = string.Empty;

        // ── 10 Material Inti (punya Kebutuhan + Terima + Kekurangan) ────
        // KABEL 24C  — col Keb=6, Terima=19, Kekurangan=32 (AG)
        public string K24C_Keb  { get; set; } = "-";
        public string K24C_Ter  { get; set; } = "-";
        public string K24C_Kek  { get; set; } = "-";

        // Tiang 7  — 7, 20, 33
        public string T7_Keb  { get; set; } = "-";
        public string T7_Ter  { get; set; } = "-";
        public string T7_Kek  { get; set; } = "-";

        // Tiang 9  — 8, 21, 34
        public string T9_Keb  { get; set; } = "-";
        public string T9_Ter  { get; set; } = "-";
        public string T9_Kek  { get; set; } = "-";

        // Strength Clamp 25/50  — 9, 22, 35
        public string SC_Keb  { get; set; } = "-";
        public string SC_Ter  { get; set; } = "-";
        public string SC_Kek  { get; set; } = "-";

        // X Frame 80x80  — 10, 23, 36
        public string XF_Keb  { get; set; } = "-";
        public string XF_Ter  { get; set; } = "-";
        public string XF_Kek  { get; set; } = "-";

        // Closure 24C (Komplit)  — 11, 24, 37
        public string CL_Keb  { get; set; } = "-";
        public string CL_Ter  { get; set; } = "-";
        public string CL_Kek  { get; set; } = "-";

        // ODP 8 Port, Inc. Board, Protection Sleeve, Clamp, Fisher  — 12, 25, 38
        public string ODP_Keb  { get; set; } = "-";
        public string ODP_Ter  { get; set; } = "-";
        public string ODP_Kek  { get; set; } = "-";

        // Pigtail SC/UPC 1 meter  — 13, 26, 39
        public string PG_Keb  { get; set; } = "-";
        public string PG_Ter  { get; set; } = "-";
        public string PG_Kek  { get; set; } = "-";

        // Connector/Adapter/Barrel SC UPC - Feeder  — 14, 27, 40
        public string CN_Keb  { get; set; } = "-";
        public string CN_Ter  { get; set; } = "-";
        public string CN_Kek  { get; set; } = "-";

        // Patchcord 5M SC/UPC-LC/UPC Duplex Outdoor  — 15, 28, 41
        public string PC_Keb  { get; set; } = "-";
        public string PC_Ter  { get; set; } = "-";
        public string PC_Kek  { get; set; } = "-";

        // ── Material Additional (hanya Terima, tanpa Kebutuhan/Kekurangan) ─
        // KABEL 12C  — Terima=18 saja
        public string K12C_Ter { get; set; } = "-";

        // ── Status keseluruhan ───────────────────────────────────────────
        public bool   HasKekurangan =>
            IsPositive(K24C_Kek) || IsPositive(T7_Kek)  || IsPositive(T9_Kek) ||
            IsPositive(SC_Kek)   || IsPositive(XF_Kek)  || IsPositive(CL_Kek) ||
            IsPositive(ODP_Kek)  || IsPositive(PG_Kek)  || IsPositive(CN_Kek) ||
            IsPositive(PC_Kek);

        public string StatusColor => HasKekurangan ? "#DC2626" : "#16A34A";
        public string StatusText  => HasKekurangan ? "⚠ Kurang" : "✓ Cukup";

        private static bool IsPositive(string s)
        {
            var clean = s.Replace(".", "").Replace(",", "").Replace("-", "").Replace(" ", "").Trim();
            return int.TryParse(clean, out int v) && v > 0;
        }
    }
}
