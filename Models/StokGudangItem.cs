namespace StokBarangMAUI.Models
{
    public class StokGudangItem
    {
        public string NamaBarang { get; set; } = "";

        // ── Stok Diterima per wilayah + total ──────────────────────────────
        public int DitLok1  { get; set; }  // Cirebon/Brebes/Tegal/Pekalongan/Indramayu/Semarang
        public int DitLok2  { get; set; }  // Tasikmalaya-Banjar
        public int DitLok3  { get; set; }  // Banyumas-Cilacap-Kebumen-Purworejo
        public int DitLok4  { get; set; }  // Sukoharjo-Klaten-Surakarta-Wonogiri
        public int DitLok5  { get; set; }  // Sragen-Karang Anyar
        public int DitLok6  { get; set; }  // Grobogan-Blora
        public int DitTotal { get; set; }  // Grand Total Diterima
        public int SisaGdg  { get; set; }  // Total Sisa Gudang

        // ── Stok Keluar per homebase + total ───────────────────────────────
        public int KelBrebes     { get; set; }
        public int KelTasik      { get; set; }
        public int KelPurwokerto { get; set; }
        public int KelSukoharjo  { get; set; }
        public int KelSragen     { get; set; }
        public int KelGrobogan   { get; set; }
        public int KelTotal      { get; set; }  // Grand Total Keluar (col Q/16)

        // ── Implementasi per homebase + total ──────────────────────────────
        public int ImpTotal      { get; set; }
        public int ImpBrebes     { get; set; }
        public int GapBrebes     { get; set; }
        public int ImpTasik      { get; set; }
        public int GapTasik      { get; set; }
        public int ImpPurwokerto { get; set; }
        public int GapPurwokerto { get; set; }
        public int ImpSukoharjo  { get; set; }
        public int GapSukoharjo  { get; set; }
        public int ImpSragen     { get; set; }
        public int GapSragen     { get; set; }
        public int ImpGrobogan   { get; set; }
        public int GapGrobogan   { get; set; }

        // ── Computed ───────────────────────────────────────────────────────
        public int  GapTotal   => KelTotal - ImpTotal;
        public bool IsMatch    => GapTotal == 0;

        // UI helpers
        public string GapColor   => GapTotal == 0 ? "#22C55E" : GapTotal > 0 ? "#F59E0B" : "#DC2626";
        public string GapBgColor => GapTotal == 0 ? "#064E3B" : GapTotal > 0 ? "#451A03" : "#450A0A";
        public string GapText    => GapTotal == 0 ? "✓ Match"
                                  : GapTotal > 0  ? $"▲ {GapTotal:N0}"
                                  :                 $"▼ {Math.Abs(GapTotal):N0}";
        public string GapDesc    => GapTotal == 0 ? "Keluar = Implementasi"
                                  : GapTotal > 0  ? $"{GapTotal:N0} belum diimplementasi"
                                  :                 $"{Math.Abs(GapTotal):N0} kelebihan implementasi";

        public string SisaColor  => SisaGdg > 0 ? "#3B82F6" : "#6B7280";
    }
}
