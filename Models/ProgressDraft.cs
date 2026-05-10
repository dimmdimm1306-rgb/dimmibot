namespace StokBarangMAUI.Models
{
    // 1 Progress entry = 1 tanggal + 1 segment + 1 span + N item barang.
    // Dikirim ke spreadsheet sebagai N baris yang share Tanggal/Segment/Span/Homebase.
    public class ProgressDraft
    {
        public string   Id            { get; set; } = Guid.NewGuid().ToString();
        public DateTime SavedAt       { get; set; } = DateTime.Now;
        public string   ProjectId     { get; set; } = "";
        public string   CreatedBy     { get; set; } = "";

        public DateTime Tanggal       { get; set; } = DateTime.Today;
        public string   Segment       { get; set; } = "";  // RUTE segment
        public string   Span          { get; set; } = "";
        public string   Homebase      { get; set; } = "";
        public string   Kabupaten     { get; set; } = "";
        public List<ProgressDraftItem> Items { get; set; } = new();
    }

    public class ProgressDraftItem
    {
        public string NamaBarang { get; set; } = "";   // Kabel / Tiang 7m / Tiang 9m / dll
        public int    Progres    { get; set; }
        public string Keterangan { get; set; } = "done"; // done / proses / kurang
    }
}
