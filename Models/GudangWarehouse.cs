namespace StokBarangMAUI.Models
{
    public class GudangWarehouseItem
    {
        public string NamaBarang   { get; set; } = "";
        public int    Diterima     { get; set; }
        public int    Keluar       { get; set; }
        public int    Implementasi { get; set; }
        public int    Gap          { get; set; }  // from sheet column (Keluar - Implementasi)

        public int    SisaReal      => Diterima - Implementasi;
        public string SisaRealColor => SisaReal > 0 ? "#3B82F6" : SisaReal == 0 ? "#22C55E" : "#DC2626";
        public string SisaRealBgColor => SisaReal > 0 ? "#1E3A6E" : SisaReal == 0 ? "#064E3B" : "#450A0A";
        public string SisaRealText  => SisaReal >= 0 ? $"{SisaReal:N0}" : $"({Math.Abs(SisaReal):N0})";
        private string Unit => MaterialUnit.Get(NamaBarang);
        public string SisaRealDesc  => SisaReal > 0  ? $"{SisaReal:N0} {Unit} tersisa di gudang"
                                     : SisaReal == 0 ? "Semua terpakai"
                                     :                 $"{Math.Abs(SisaReal):N0} {Unit} lebih dari diterima";

        public double ImpPct     => Diterima > 0 ? Math.Clamp((double)Implementasi / Diterima, 0, 1) : 0;
        public string ImpPctText => $"{ImpPct:P0}";

        public string GapColor   => Gap == 0 ? "#22C55E" : Gap > 0 ? "#F59E0B" : "#DC2626";
        public string GapBgColor => Gap == 0 ? "#064E3B" : Gap > 0 ? "#451A03" : "#450A0A";
        public string GapText    => Gap == 0 ? "✓ Match"
                                  : Gap > 0  ? $"▲ {Gap:N0}"
                                  :             $"▼ {Math.Abs(Gap):N0}";
        public string GapDesc    => Gap == 0 ? "Keluar = Implementasi"
                                  : Gap > 0  ? $"{Gap:N0} belum diimplementasi"
                                  :             $"{Math.Abs(Gap):N0} kelebihan implementasi";
    }

    public class GudangWarehouse
    {
        public string                    Name        { get; set; } = "";
        public string                    SegmentName { get; set; } = "";
        public List<GudangWarehouseItem> Items       { get; set; } = new();

        public int TotalDiterima => Items.Sum(x => x.Diterima);
        public int TotalKeluar   => Items.Sum(x => x.Keluar);
        public int TotalImp      => Items.Sum(x => x.Implementasi);
        public int TotalSisaReal  => TotalDiterima - TotalImp;
        public int TotalGap      => TotalKeluar - TotalImp;
        public int ItemCount     => Items.Count;

        public string TotalGapColor   => TotalGap == 0 ? "#22C55E" : TotalGap > 0 ? "#F59E0B" : "#DC2626";
        public string TotalGapBgColor => TotalGap == 0 ? "#064E3B" : TotalGap > 0 ? "#451A03" : "#450A0A";
        public string TotalGapText    => TotalGap == 0 ? "✓ Match"
                                       : TotalGap > 0  ? $"▲ {TotalGap:N0}"
                                       :                  $"▼ {Math.Abs(TotalGap):N0}";

        // Kabel-only (satuan "m") untuk tampilan kartu list
        private IEnumerable<GudangWarehouseItem> KabelItems => Items.Where(x => MaterialUnit.Get(x.NamaBarang) == "m");
        public int KabelDiterima => KabelItems.Sum(x => x.Diterima);
        public int KabelKeluar   => KabelItems.Sum(x => x.Keluar);
        public int KabelImp      => KabelItems.Sum(x => x.Implementasi);
        public int KabelGap      => KabelKeluar - KabelImp;

        public double KabelImpPct     => KabelDiterima > 0 ? Math.Clamp((double)KabelImp / KabelDiterima, 0, 1) : 0;
        public string KabelImpPctText => $"{KabelImpPct:P0}";
        public double TotalImpPct     => TotalDiterima > 0 ? Math.Clamp((double)TotalImp / TotalDiterima, 0, 1) : 0;
        public string TotalImpPctText => $"{TotalImpPct:P0}";

        public string KabelGapColor   => KabelGap == 0 ? "#22C55E" : KabelGap > 0 ? "#F59E0B" : "#DC2626";
        public string KabelGapBgColor => KabelGap == 0 ? "#064E3B" : KabelGap > 0 ? "#451A03" : "#450A0A";
        public string KabelGapText    => KabelGap == 0 ? "✓ Match"
                                       : KabelGap > 0  ? $"▲ {KabelGap:N0}"
                                       :                  $"▼ {Math.Abs(KabelGap):N0}";
    }
}
