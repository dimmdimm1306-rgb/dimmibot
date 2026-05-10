namespace StokBarangMAUI.Models
{
    public class ProjectConfig
    {
        public string Id          { get; set; } = Guid.NewGuid().ToString();
        public string Name        { get; set; } = "Project";
        public string Description { get; set; } = "";
        public string Icon        { get; set; } = "📁";
        public string Color       { get; set; } = "#1D4ED8";

        // Spreadsheet utama (Surat Jalan, Progress Harian, Stok)
        public string SpreadsheetId  { get; set; } = "";
        public string GidStok        { get; set; } = "";
        public string GidAktualStok  { get; set; } = "";  // Aktual Stok sheet (Diterima & Imp per gudang)
        public string GidSuratJalan  { get; set; } = "";
        public string GidProgress    { get; set; } = "";

        // Spreadsheet Resume (ringkasan per segment)
        public string ResumeSpreadsheetId { get; set; } = "";
        public string GidResume           { get; set; } = "";

        // GID detail per segment (key = "1" s/d "6")
        public Dictionary<string, string> SegmentGids  { get; set; } = new();
        // Nama/RUTE per segment (key = "1" s/d "6")
        public Dictionary<string, string> SegmentNames { get; set; } = new();

        // Opsional: GID sheet "Config" untuk sinkronisasi nama project antar HP
        // Sheet ini hanya 1-2 baris: A1=nama project, A2=deskripsi (opsional)
        public string GidConfig { get; set; } = "";

        // Opsional: GID sheet master daftar barang untuk dropdown di form Input.
        // Kolom A = nama barang. Kalau kosong, app coba auto-detect sheet bernama
        // "Master"/"MasterBarang"/"Validasi"/"DropDown"/"List".
        public string GidMasterBarang { get; set; } = "";

        // Untuk fitur Upload (OAuth Sheets API) — pakai NAMA tab, bukan GID.
        public string SheetNameSuratJalan     { get; set; } = "Surat Jalan";
        public string SheetNameProgress       { get; set; } = "Progress";
        public string SheetNameAbsensi        { get; set; } = "Absensi";
        // Opsional: Drive folder ID tempat foto SJ disimpan. Kosong = root My Drive.
        public string DriveFolderIdSuratJalan { get; set; } = "";
        public string DriveFolderIdAbsensi    { get; set; } = "";
    }
}
