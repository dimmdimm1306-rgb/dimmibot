namespace StokBarangMAUI.Models
{
    // Absensi: 1 foto + caption + segment + 1+ ketua regu + waspang + pekerjaan.
    // Waktu otomatis dari SavedAt (saat user simpan/kirim).
    public class AbsensiDraft
    {
        public string   Id           { get; set; } = Guid.NewGuid().ToString();
        public DateTime SavedAt      { get; set; } = DateTime.Now;
        public string   ProjectId    { get; set; } = "";

        public string   PhotoPath    { get; set; } = "";
        public string   Caption      { get; set; } = "";
        public string   Segment      { get; set; } = "";
        public string   Pekerjaan    { get; set; } = "";
        public string   Waspang      { get; set; } = "";
        public List<string> KetuaRegu { get; set; } = new();
    }
}
