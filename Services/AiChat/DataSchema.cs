namespace StokBarangMAUI.Services.AiChat
{
    /// <summary>
    /// Single-source-of-truth definisi struktur Google Drive sheets yang dipakai bot.
    /// Bot, MCP call, dan formatter semua reference dari sini supaya konsisten.
    /// </summary>
    public static class DataSchema
    {
        // ── File IDs (hardcoded, sama dengan cloudflare-config.json aliases) ──
        public const string FileIdStok    = "1RC2Ylo4DjIAJkNMLe6v0jMnupJMcrP2v5aFTauhhcsg";
        public const string FileIdResume  = "1d9GKDxcYGwURcVp-BvSYW4W0YQiNVZt_";

        // ── Sheet definitions ──
        public static readonly SheetSpec Progress = new()
        {
            FileId = FileIdStok,
            SheetName = "Progress",
            HeaderRows = 1,
            HeaderRowStart = 1,
            Description = "Log progres harian per material per hari (granular)",
            ColTanggal = "Tanggal",
            ColSegment = "Segment",
            ColRute = "Rute",
            ColNamaBarang = "Nama Barang",
            ColProgres = "Progres",
            ColKeterangan = "Keterangan",
            ColHomebase = "HOMEBASE",
            ColKabKota = "KAB/KOTA",
            ColSiteId = "SITE ID",
            CacheTtlSec = 60, // update real-time
        };

        public static readonly SheetSpec AktualStok = new()
        {
            FileId = FileIdStok,
            SheetName = "Aktual Stok",
            HeaderRows = 3,
            HeaderRowStart = 1,
            Description = "Crosstab: material × gudang. Diterima/Keluar/Implementasi/Sisa per gudang.",
            CacheTtlSec = 300,
        };

        public static readonly SheetSpec StokKebutuhan = new()
        {
            FileId = FileIdStok,
            SheetName = "Stok",
            HeaderRows = 2,
            HeaderRowStart = 3,
            Description = "Volume kebutuhan material per segment (planning MRF, bukan stok aktual)",
            CacheTtlSec = 300,
        };

        public static readonly SheetSpec SuratJalan = new()
        {
            FileId = FileIdStok,
            SheetName = "Surat Jalan",
            HeaderRows = 1,
            HeaderRowStart = 2,
            Description = "Riwayat SJ: Tanggal, Segment, Nama Barang, QTY, Jenis, NoSJ",
            CacheTtlSec = 120,
        };

        public static readonly SheetSpec Alamat = new()
        {
            FileId = FileIdStok,
            SheetName = "Alamat",
            HeaderRows = 1,
            HeaderRowStart = 1,
            Description = "Master alamat gudang per segment",
            CacheTtlSec = 600,
        };

        public static readonly SheetSpec Resume = new()
        {
            FileId = FileIdResume,
            SheetName = "RESUME",
            HeaderRows = 2,
            HeaderRowStart = 1,
            Description = "Rekap total per segment (1-6): Kabel/T7/T9 + Status",
            CacheTtlSec = 300,
        };

        public static readonly SheetSpec ResumeBySite = new()
        {
            FileId = FileIdResume,
            SheetName = "RESUME BY SITE ID",
            HeaderRows = 2,
            HeaderRowStart = 1,
            Description = "Breakdown progres per SITE ID (paling lengkap)",
            ColSiteId = "SITE ID",
            ColRute = "Rute",
            ColKabKota = "KAB/KOTA",
            CacheTtlSec = 300,
        };

        public static readonly SheetSpec[] SegmentSheets = new[]
        {
            new SheetSpec { FileId = FileIdResume, SheetName = "BREBES",       HeaderRows = 2, HeaderRowStart = 1, CacheTtlSec = 300 },
            new SheetSpec { FileId = FileIdResume, SheetName = "TASIKMALAYA",  HeaderRows = 2, HeaderRowStart = 1, CacheTtlSec = 300 },
            new SheetSpec { FileId = FileIdResume, SheetName = "PURWOKERTO",   HeaderRows = 2, HeaderRowStart = 1, CacheTtlSec = 300 },
            new SheetSpec { FileId = FileIdResume, SheetName = "SUKOHARJO",    HeaderRows = 2, HeaderRowStart = 1, CacheTtlSec = 300 },
            new SheetSpec { FileId = FileIdResume, SheetName = "SRAGEN",       HeaderRows = 2, HeaderRowStart = 1, CacheTtlSec = 300 },
            new SheetSpec { FileId = FileIdResume, SheetName = "GROBOGAN",     HeaderRows = 2, HeaderRowStart = 1, CacheTtlSec = 300 },
        };

        // ── Daftar 6 segment sebagai const ──
        public static readonly string[] Segments =
            { "BREBES", "TASIKMALAYA", "PURWOKERTO", "SUKOHARJO", "SRAGEN", "GROBOGAN" };

        // ── Mapping kota → segment ──
        // Kota Klaten/Solo masuk SUKOHARJO, Karanganyar masuk SRAGEN, dst.
        public static readonly Dictionary<string, string> KotaToSegment = new(StringComparer.OrdinalIgnoreCase)
        {
            // BREBES
            { "BREBES", "BREBES" }, { "CIREBON", "BREBES" }, { "TEGAL", "BREBES" },
            { "PEKALONGAN", "BREBES" }, { "INDRAMAYU", "BREBES" }, { "SEMARANG", "BREBES" },
            // TASIKMALAYA
            { "TASIKMALAYA", "TASIKMALAYA" }, { "TASIK", "TASIKMALAYA" }, { "BANJAR", "TASIKMALAYA" },
            // PURWOKERTO
            { "PURWOKERTO", "PURWOKERTO" }, { "BANYUMAS", "PURWOKERTO" },
            { "CILACAP", "PURWOKERTO" }, { "KEBUMEN", "PURWOKERTO" }, { "PURWOREJO", "PURWOKERTO" },
            // SUKOHARJO
            { "SUKOHARJO", "SUKOHARJO" }, { "KLATEN", "SUKOHARJO" },
            { "SURAKARTA", "SUKOHARJO" }, { "SOLO", "SUKOHARJO" }, { "WONOGIRI", "SUKOHARJO" },
            // SRAGEN
            { "SRAGEN", "SRAGEN" }, { "KARANGANYAR", "SRAGEN" }, { "KARANG ANYAR", "SRAGEN" },
            // GROBOGAN
            { "GROBOGAN", "GROBOGAN" }, { "BLORA", "GROBOGAN" },
        };

        public static string? ResolveSegmentFromCity(string city)
        {
            if (string.IsNullOrWhiteSpace(city)) return null;
            var k = city.Trim().ToUpperInvariant();
            return KotaToSegment.TryGetValue(k, out var seg) ? seg : null;
        }
    }

    /// <summary>Spesifikasi 1 sheet di Google Drive.</summary>
    public class SheetSpec
    {
        public string FileId         { get; init; } = "";
        public string SheetName      { get; init; } = "";
        public int    HeaderRows     { get; init; } = 1;
        public int    HeaderRowStart { get; init; } = 1;
        public string Description    { get; init; } = "";
        public int    CacheTtlSec    { get; init; } = 300;

        // Optional column name hints (untuk fuzzy match di MCP)
        public string ColTanggal    { get; init; } = "";
        public string ColSegment    { get; init; } = "";
        public string ColRute       { get; init; } = "";
        public string ColNamaBarang { get; init; } = "";
        public string ColProgres    { get; init; } = "";
        public string ColKeterangan { get; init; } = "";
        public string ColHomebase   { get; init; } = "";
        public string ColKabKota    { get; init; } = "";
        public string ColSiteId     { get; init; } = "";
    }
}
