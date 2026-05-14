namespace StokBarangMAUI.Models.Bot
{
    /// <summary>
    /// Tipe intent yang bot bisa handle. Tiap intent map ke satu BotFlow.
    /// </summary>
    public enum BotIntent
    {
        // === Pattern A: Single-shot ===
        AlamatGudang,           // "alamat brebes"
        ProgresTotal,           // "progres total project"

        // === Pattern B/C: Filter + drill-down (with auto-skip) ===
        ProgresOutstanding,     // "yang belum", "site yang belum"
        ProgresDone,            // "yang selesai", "100%"
        ProgresSiteSearch,      // "site 0244", "rute brebes"
        ProgresDate,            // "tanggal 17", "kemarin", "hari ini"
        StokMaterial,           // "stok kabel 24c"
        StokGudang,             // "stok di brebes"
        StokKritis,             // "stok habis", "stok kritis"
        KebutuhanHomebase,      // "kebutuhan brebes"
        KebutuhanKurang,        // "material kurang"
        SuratJalanDate,         // "sj kemarin"
        SuratJalanJenis,        // "sj masuk hari ini"
        SuratJalanNomor,        // "sj-001"
        SuratJalanOrang,        // "sj dari budi"
        SuratJalanLatest,       // "sj terakhir", "sj terbaru"

        // === Pattern D: Vague menu ===
        StokMenu,               // "cek stok"
        SuratJalanMenu,         // "cek sj"
        ProgresMenu,            // "cek progres" (tanpa subject)

        // === Pattern E: LLM fallback ===
        ChatGeneral,            // "halo", "berapa total kabel"

        // === System ===
        Help,                   // "/help", "/bantu"
        Refresh,                // "refresh data"
        Cancel,                 // "batal", "cancel"
        DiniHari,               // "hari ini" tapi jam 00-05 WIB
        Weather,                // "cuaca"
        TimeQuery,              // "jam berapa", "hari apa", "tanggal berapa"
        Unknown,                // gak match apa-apa, pasrah ke LLM
    }

    /// <summary>Pattern dari decision tree.</summary>
    public enum BotPattern
    {
        SingleShot,         // A
        FilterDrillDown,    // B
        FilterAutoSkip,     // C
        VagueMenu,          // D
        LlmFallback,        // E
    }
}
