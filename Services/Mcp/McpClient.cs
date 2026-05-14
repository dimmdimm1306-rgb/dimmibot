using StokBarangMAUI.Services.AiChat;

namespace StokBarangMAUI.Services.Mcp
{
    /// <summary>
    /// Typed wrapper di atas GDriveReaderService yang pakai DataSchema sebagai
    /// single-source-of-truth. Bot flow gak perlu tahu file_id atau header_rows —
    /// cukup panggil method spesifik.
    /// </summary>
    public class McpClient
    {
        private readonly GDriveReaderService _drive;

        public McpClient(GDriveReaderService drive)
        {
            _drive = drive;
        }

        public bool IsEnabled => _drive.IsEnabled;
        public string BaseUrl => _drive.BaseUrl;

        // ── Generic call ─────────────────────────────────────────────

        public Task<SheetFilterResult?> FilterAsync(SheetSpec spec,
            IDictionary<string, object>? filters = null,
            IEnumerable<string>? columns = null,
            int limit = 100)
            => _drive.FilterSheetAsync(
                spec.FileId, filters, columns, spec.SheetName,
                limit, spec.HeaderRows, spec.HeaderRowStart);

        public Task<SheetFilterResult?> SmartFilterAsync(SheetSpec spec,
            string? keyword = null, string? intent = null,
            IEnumerable<string>? searchColumns = null, int limit = 30)
            => _drive.SmartFilterAsync(
                spec.FileId, spec.SheetName, keyword, intent, searchColumns,
                limit, spec.HeaderRows, spec.HeaderRowStart);

        // ── Convenience wrappers per sheet ───────────────────────────

        /// <summary>Search progres harian by keyword + optional date intent.</summary>
        public Task<SheetFilterResult?> SearchProgressAsync(string? keyword = null, string? dateIntent = null, int limit = 50)
            => SmartFilterAsync(DataSchema.Progress, keyword, dateIntent,
                new[] { "SITE ID", "Rute", "Nama Barang", "KAB/KOTA", "HOMEBASE", "Segment" }, limit);

        /// <summary>Cari rute/site di RESUME BY SITE ID (server-side).</summary>
        public Task<SheetFilterResult?> SearchSiteResumeAsync(string keyword, int limit = 50)
            => SmartFilterAsync(DataSchema.ResumeBySite, keyword, null,
                new[] { "SITE ID", "Rute", "KAB/KOTA" }, limit);

        /// <summary>Read SEMUA rute di RESUME BY SITE ID (no filter, untuk client-side analysis).</summary>
        public Task<SheetFilterResult?> ReadAllResumeBySiteAsync(int limit = 300)
            => FilterAsync(DataSchema.ResumeBySite, null, null, limit);

        /// <summary>Filter rute outstanding (% ada yang &lt; 100) di RESUME BY SITE ID.</summary>
        public Task<SheetFilterResult?> ResumeBySiteOutstandingAsync(int limit = 500)
            => SmartFilterAsync(DataSchema.ResumeBySite, null, "outstanding", null, limit);

        /// <summary>Filter rute selesai (semua % >= 100) di RESUME BY SITE ID.</summary>
        public Task<SheetFilterResult?> ResumeBySiteDoneAsync(int limit = 200)
            => SmartFilterAsync(DataSchema.ResumeBySite, null, "done", null, limit);

        /// <summary>Read RESUME (per segment summary) — selalu fetch semua (cuma 6-7 row).</summary>
        public Task<SheetFilterResult?> ReadResumeAsync()
            => FilterAsync(DataSchema.Resume, null, null, 50);

        /// <summary>Read Aktual Stok (crosstab, ~15 row).</summary>
        public Task<SheetFilterResult?> ReadAktualStokAsync()
            => FilterAsync(DataSchema.AktualStok, null, null, 50);

        /// <summary>Read sheet Stok (MRF kebutuhan) — full atau filter homebase.</summary>
        public Task<SheetFilterResult?> ReadStokKebutuhanAsync(string? homebaseFilter = null)
        {
            IDictionary<string, object>? filters = null;
            if (!string.IsNullOrWhiteSpace(homebaseFilter))
                filters = new Dictionary<string, object> { { "Homebase", homebaseFilter } };
            return FilterAsync(DataSchema.StokKebutuhan, filters, null, 100);
        }

        /// <summary>Read Surat Jalan with date/keyword filter.</summary>
        public Task<SheetFilterResult?> SearchSuratJalanAsync(string? keyword = null, string? dateIntent = null, int limit = 30)
            => SmartFilterAsync(DataSchema.SuratJalan, keyword, dateIntent,
                new[] { "NO_SJ", "Nama Barang", "PENGIRIM", "PENERIMA", "Segment", "Jenis" }, limit);

        /// <summary>Read Alamat (semua atau filter by gudang).</summary>
        public Task<SheetFilterResult?> ReadAlamatAsync(string? gudangFilter = null)
        {
            IDictionary<string, object>? filters = null;
            if (!string.IsNullOrWhiteSpace(gudangFilter))
                filters = new Dictionary<string, object> { { "GUDANG", gudangFilter } };
            return FilterAsync(DataSchema.Alamat, filters, null, 20);
        }
    }
}
