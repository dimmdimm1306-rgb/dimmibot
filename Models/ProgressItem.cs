namespace StokBarangMAUI.Models
{
    public class MaterialQty
    {
        public string NamaBarang { get; set; } = "";
        public int    Qty        { get; set; }
        public string Satuan     { get; set; } = "";
        public string QtyText    => $"{Qty:N0} {Satuan}";
    }

    public class SpanActivity
    {
        public string Span     { get; set; } = "";
        public string Segment  { get; set; } = "";
        public string Homebase { get; set; } = "";
        public List<ProgressItem> Items { get; set; } = new();

        public List<MaterialQty> Materials => Items
            .GroupBy(i => (i.NamaBarang ?? "").Trim(), StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => MaterialUnit.Get(g.Key) switch { "m" => 0, "btg" => 1, _ => 2 })
            .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => new MaterialQty
            {
                NamaBarang = g.Key,
                Qty        = g.Sum(x => x.Progres),
                Satuan     = MaterialUnit.Get(g.Key),
            }).ToList();

        public bool   IsDone        => Items.Count > 0 && Items.All(x => x.IsDone);
        public string StatusColor   => IsDone ? "#16A34A" : "#D97706";
        public string StatusText    => IsDone ? "✓ Done" : "⏳ Proses";
        public bool   HasHomebase   => !string.IsNullOrWhiteSpace(Homebase);
        public bool   HasKeterangan => Items.Any(x => x.HasKeterangan);
        public string KeteranganText => string.Join(" • ",
            Items.Where(x => x.HasKeterangan)
                 .Select(x => x.Keterangan.Trim())
                 .Distinct(StringComparer.OrdinalIgnoreCase));
    }

    public class DailyProgressGroup
    {
        private static readonly string[] _hari =
            { "Minggu","Senin","Selasa","Rabu","Kamis","Jumat","Sabtu" };
        private static readonly string[] _bulan =
            { "","Januari","Februari","Maret","April","Mei","Juni",
              "Juli","Agustus","September","Oktober","November","Desember" };

        public DateTime          Date  { get; set; }
        public List<ProgressItem> Items { get; set; } = new();

        public string HariName    => _hari[(int)Date.DayOfWeek];
        public string TanggalText => $"{Date.Day} {_bulan[Date.Month]} {Date.Year}";
        public string ShortLabel  => $"{Date.Day:00}/{Date.Month:00}";

        // Aktivitas = unik per (segment + span). Satu span dengan 2 material
        // tetap dihitung 1 aktivitas.
        public int    ActivityCount => Activities.Count;
        public string CountText     => $"{ActivityCount} aktivitas";
        public int    SegmentCount  => Items.Select(x => x.Segment ?? "")
                                            .Where(s => !string.IsNullOrWhiteSpace(s))
                                            .Distinct(StringComparer.OrdinalIgnoreCase).Count();
        public string SegmentSummary => SegmentCount > 0 ? $"{SegmentCount} segment" : "";

        public List<SpanActivity> Activities => Items
            .GroupBy(p => ((p.Segment ?? "").Trim() + "|" + (p.Span ?? "").Trim()),
                     StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var first = g.First();
                return new SpanActivity
                {
                    Span     = first.Span     ?? "",
                    Segment  = first.Segment  ?? "",
                    Homebase = g.Select(x => x.Homebase ?? "")
                                .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s)) ?? "",
                    Items    = g.ToList(),
                };
            })
            .OrderBy(a => a.Segment, StringComparer.OrdinalIgnoreCase)
            .ThenBy(a => a.Span, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Ringkasan total per material — tampilkan semua material dengan nama
        public string MaterialSummary
        {
            get
            {
                var parts = Items
                    .GroupBy(i => (i.NamaBarang ?? "").Trim(), StringComparer.OrdinalIgnoreCase)
                    .Where(g => !string.IsNullOrWhiteSpace(g.Key))
                    .OrderBy(g => MaterialUnit.Get(g.Key) switch { "m" => 0, "btg" => 1, _ => 2 })
                    .ThenBy(g => g.Key)
                    .Select(g => $"{g.Key} {g.Sum(x => x.Progres):N0} {MaterialUnit.Get(g.Key)}");
                return string.Join("  •  ", parts);
            }
        }

        // Pewarnaan kartu — rotasi berdasarkan day-of-year supaya konsisten
        public string CardColor
        {
            get
            {
                string[] palette = { "#1E40AF","#0E7490","#166534","#9A3412","#6D28D9",
                                     "#9D174D","#065F46","#7C2D12","#1E3A5F","#44403C" };
                return palette[Math.Abs(Date.DayOfYear) % palette.Length];
            }
        }
        public string CardColorMid
        {
            get
            {
                string[] palette = { "#3B82F6","#06B6D4","#22C55E","#F97316","#A855F7",
                                     "#EC4899","#10B981","#EF4444","#38BDF8","#A8A29E" };
                return palette[Math.Abs(Date.DayOfYear) % palette.Length];
            }
        }
    }

    public class ProgressItem
    {
        public string   Tanggal  { get; set; } = string.Empty;
        public DateTime SortDate { get; set; } = DateTime.MinValue;
        public string Segment    { get; set; } = string.Empty;
        public string Span       { get; set; } = string.Empty;  // kolom C — secara semantik = Rute
        public string NamaBarang { get; set; } = string.Empty;
        public int    Progres    { get; set; }
        public string Keterangan { get; set; } = string.Empty;
        public string Homebase   { get; set; } = string.Empty;
        public string KabKota    { get; set; } = string.Empty;  // kolom H
        public string SiteId     { get; set; } = string.Empty;  // kolom I

        public bool   IsDone        => Keterangan.Contains("done", StringComparison.OrdinalIgnoreCase)
                                    && !Keterangan.Contains("belum", StringComparison.OrdinalIgnoreCase)
                                    && !Keterangan.Contains("kurang", StringComparison.OrdinalIgnoreCase);
        public string StatusColor   => IsDone ? "#16A34A" : "#D97706";
        public string StatusText    => IsDone ? "✓ Done" : "⏳ Proses";
        public bool   HasKeterangan => !string.IsNullOrWhiteSpace(Keterangan);
        public bool   HasHomebase   => !string.IsNullOrWhiteSpace(Homebase);
        public bool   HasKabKota    => !string.IsNullOrWhiteSpace(KabKota);
        public bool   HasSiteId     => !string.IsNullOrWhiteSpace(SiteId);

        public string Satuan         => MaterialUnit.Get(NamaBarang);
        public string ProgresDisplay => $"{Progres:N0} {Satuan}";
    }
}
