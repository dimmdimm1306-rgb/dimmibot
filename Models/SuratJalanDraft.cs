namespace StokBarangMAUI.Models
{
    // 1 Surat Jalan = 1 foto + 1 No SJ + N item barang.
    // Ketika dikirim ke spreadsheet nanti, tiap item jadi 1 baris yang share metadata.
    public class SuratJalanDraft
    {
        public string   Id            { get; set; } = Guid.NewGuid().ToString();
        public DateTime SavedAt       { get; set; } = DateTime.Now;
        public string   ProjectId     { get; set; } = "";
        public string   CreatedBy     { get; set; } = "";

        public DateTime Tanggal       { get; set; } = DateTime.Today;
        public string   NoSJ          { get; set; } = "";
        public string   Pengirim      { get; set; } = "";
        public string   Penerima      { get; set; } = "";
        public string   Segment       { get; set; } = "";
        public string   Keterangan    { get; set; } = "";
        public string   PhotoPath     { get; set; } = "";  // absolute path di local storage
        public List<SuratJalanDraftItem> Items { get; set; } = new();
    }

    public class SuratJalanDraftItem
    {
        public string NamaBarang { get; set; } = "";
        public int    Qty        { get; set; }
        public string Jenis      { get; set; } = "Masuk";  // Masuk | Keluar
    }
}
