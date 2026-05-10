namespace StokBarangMAUI.Models
{
    public class SuratJalanGroup
    {
        public string   Tanggal    { get; set; } = "";
        public string   Segment    { get; set; } = "";
        public string   NoSJ       { get; set; } = "";
        public string   Pengirim   { get; set; } = "";
        public string   Penerima   { get; set; } = "";
        public string   Keterangan { get; set; } = "";
        public string   DriveUrl   { get; set; } = "";
        public string   Jenis      { get; set; } = "";
        public DateTime SortDate   { get; set; }

        public List<SuratJalanItem> Items { get; set; } = new();

        public bool   IsMasuk     => Jenis.Contains("MASUK",  StringComparison.OrdinalIgnoreCase);
        public string BadgeColor  => IsMasuk ? "#16A34A"
                                   : Jenis.Contains("DIBAWA", StringComparison.OrdinalIgnoreCase)
                                     ? "#D97706" : "#DC2626";
        public string JenisPendek => IsMasuk ? "MASUK"
                                   : Jenis.Contains("DIBAWA", StringComparison.OrdinalIgnoreCase)
                                     ? "DIBAWA" : "KELUAR";
        public bool   HasNoSJ     => !string.IsNullOrWhiteSpace(NoSJ);
        public bool   HasLink     => !string.IsNullOrWhiteSpace(DriveUrl)
                                  && (DriveUrl.StartsWith("http",  StringComparison.OrdinalIgnoreCase)
                                   || DriveUrl.StartsWith("drive", StringComparison.OrdinalIgnoreCase));

        public string NamaBarang  => Items.Count == 1
                                   ? Items[0].NamaBarang
                                   : $"{Items.Count} jenis material";
    }
}
