namespace StokBarangMAUI.Models
{
    public class SuratJalanItem
    {
        public string Tanggal    { get; set; } = string.Empty;
        public string Segment    { get; set; } = string.Empty;
        public string NamaBarang { get; set; } = string.Empty;
        public int    Qty        { get; set; }
        public string Jenis      { get; set; } = string.Empty;
        public string NoSJ       { get; set; } = string.Empty;
        public string Pengirim   { get; set; } = string.Empty;
        public string Penerima   { get; set; } = string.Empty;
        public string   Keterangan { get; set; } = string.Empty;
        public string   DriveUrl   { get; set; } = string.Empty;  // Kolom J (index 9) — plain text URL
        public DateTime SortDate   { get; set; } = DateTime.MinValue;

        public bool   IsMasuk    => Jenis.Contains("MASUK", StringComparison.OrdinalIgnoreCase);
        public string BadgeColor => IsMasuk ? "#16A34A" : Jenis.Contains("DIBAWA", StringComparison.OrdinalIgnoreCase) ? "#D97706" : "#DC2626";
        public string JenisPendek => IsMasuk ? "MASUK" : Jenis.Contains("DIBAWA", StringComparison.OrdinalIgnoreCase) ? "DIBAWA" : "KELUAR";
        public bool   HasNoSJ    => !string.IsNullOrWhiteSpace(NoSJ);

        public bool   HasLink    => !string.IsNullOrWhiteSpace(DriveUrl)
                                 && (DriveUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                                  || DriveUrl.StartsWith("drive", StringComparison.OrdinalIgnoreCase));

        // Satuan berdasarkan nama barang
        public string Satuan => MaterialUnit.Get(NamaBarang);

        public string QtyText => $"{Qty:N0} {Satuan}";
    }
}
