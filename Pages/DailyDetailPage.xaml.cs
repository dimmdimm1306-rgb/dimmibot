using System.Text;
using StokBarangMAUI.Models;

namespace StokBarangMAUI.Pages
{
    public partial class DailyDetailPage : ContentPage
    {
        private readonly DailyProgressGroup _group;
        private readonly string             _projectName;
        private string _search = "";

        public DailyDetailPage(DailyProgressGroup group, string projectName = "")
        {
            InitializeComponent();
            _group       = group;
            _projectName = projectName;
            LblHari.Text    = group.HariName;
            LblTanggal.Text = group.TanggalText;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LblTheme.Text = App.Theme.ThemeIcon;
            var activities = _group.Activities;
            LblTotal.Text = activities.Count.ToString();
            LblSeg.Text   = _group.SegmentCount.ToString();
            LblDone.Text  = activities.Count(a => a.IsDone).ToString();
            ApplyFilter();
        }

        private void OnThemeToggle(object s, TappedEventArgs e)
        {
            App.Theme.Toggle();
            LblTheme.Text = App.Theme.ThemeIcon;
        }

        private async void OnBack(object s, TappedEventArgs e)
            => await Navigation.PopAsync();

        private void OnSearchChanged(object s, TextChangedEventArgs e)
        {
            _search = e.NewTextValue ?? "";
            BtnClear.IsVisible = _search.Length > 0;
            ApplyFilter();
        }

        private void OnClearSearch(object s, TappedEventArgs e)
        {
            SearchEntry.Text   = "";
            _search            = "";
            BtnClear.IsVisible = false;
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            var activities = _group.Activities.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(_search))
            {
                var q = _search.Trim();
                activities = activities.Where(a =>
                    a.Span.Contains(q, StringComparison.OrdinalIgnoreCase)     ||
                    a.Segment.Contains(q, StringComparison.OrdinalIgnoreCase)  ||
                    a.Homebase.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    a.Items.Any(x => (x.NamaBarang ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)));
            }
            ItemList.ItemsSource = activities.ToList();
        }

        // ── WhatsApp Report ─────────────────────────────────────────────
        private async void OnShareReport(object s, TappedEventArgs e)
        {
            try
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                var text = GenerateWhatsAppReport();
                await Share.Default.RequestAsync(new ShareTextRequest
                {
                    Text  = text,
                    Title = $"Laporan Progress {_group.TanggalText}"
                });
            }
            catch (Exception ex) { await DisplayAlert("Gagal", ex.Message, "OK"); }
        }

        private static string ShortenMat(string nama)
        {
            var s = nama.Trim();
            // Kabel
            if (s.Contains("Kabel", StringComparison.OrdinalIgnoreCase))
            {
                var m = System.Text.RegularExpressions.Regex.Match(s, @"\d+C?", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                return m.Success ? $"Kabel {m.Value.ToUpper()}" : "Kabel";
            }
            // Tiang
            if (s.Contains("Tiang", StringComparison.OrdinalIgnoreCase) || s.StartsWith("T7", StringComparison.OrdinalIgnoreCase))
                return s.Contains('9') ? "T9" : "T7";
            // ODP
            if (s.Contains("ODP", StringComparison.OrdinalIgnoreCase)) return "ODP";
            // SC / Konektor
            if (s.Contains("SC", StringComparison.OrdinalIgnoreCase) && s.Contains("Kon", StringComparison.OrdinalIgnoreCase)) return "SC Kon";
            // Closure
            if (s.Contains("Closure", StringComparison.OrdinalIgnoreCase)) return "Closure";
            // Clamp
            if (s.Contains("Clamp", StringComparison.OrdinalIgnoreCase)) return "Clamp";
            // Pothead / Pig tail
            if (s.Contains("Pigtail", StringComparison.OrdinalIgnoreCase) || s.Contains("Pig tail", StringComparison.OrdinalIgnoreCase)) return "Pigtail";
            // Potong / Terminasi
            if (s.Contains("Terminasi", StringComparison.OrdinalIgnoreCase)) return "Terminasi";
            // Fallback: max 15 karakter
            return s.Length > 15 ? s[..15] : s;
        }

        private string GenerateWhatsAppReport()
        {
            var sb         = new StringBuilder();
            var activities = _group.Activities;

            // Header
            if (!string.IsNullOrWhiteSpace(_projectName))
                sb.AppendLine($"*{_projectName}*");
            sb.AppendLine($"*Progress {_group.HariName}, {_group.TanggalText}*");
            sb.AppendLine($"{_group.SegmentCount} segment | {activities.Count} aktivitas | {DateTime.Now:HH:mm} WIB");
            sb.AppendLine();

            // Total semua segment
            sb.AppendLine("*Total hari ini:*");
            var totalByMat = _group.Items
                .GroupBy(i => (i.NamaBarang ?? "").Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(g => !string.IsNullOrWhiteSpace(g.Key))
                .OrderBy(g => MaterialUnit.Get(g.Key) switch { "m" => 0, "btg" => 1, _ => 2 })
                .ThenBy(g => g.Key);
            foreach (var g in totalByMat)
                sb.AppendLine($"- {ShortenMat(g.Key)}: *{g.Sum(x => x.Progres):N0} {MaterialUnit.Get(g.Key)}*");
            sb.AppendLine();

            // Detail per segment
            sb.AppendLine("*Detail per segment:*");
            var bySegment = activities
                .GroupBy(a => a.Segment, StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g.Key);

            int segIdx = 1;
            foreach (var segGroup in bySegment)
            {
                sb.AppendLine();
                sb.AppendLine($"*{segIdx++}. {segGroup.Key}*");

                var segMats = segGroup
                    .SelectMany(a => a.Items)
                    .GroupBy(i => (i.NamaBarang ?? "").Trim(), StringComparer.OrdinalIgnoreCase)
                    .Where(g => !string.IsNullOrWhiteSpace(g.Key))
                    .OrderBy(g => MaterialUnit.Get(g.Key) switch { "m" => 0, "btg" => 1, _ => 2 })
                    .ThenBy(g => g.Key);
                foreach (var mg in segMats)
                    sb.AppendLine($"  > {ShortenMat(mg.Key)}: {mg.Sum(x => x.Progres):N0} {MaterialUnit.Get(mg.Key)}");

                foreach (var act in segGroup.OrderBy(a => a.Span))
                {
                    var spanLabel = string.IsNullOrWhiteSpace(act.Span) ? "(tanpa span)" : act.Span;
                    sb.AppendLine($"  - {spanLabel}");
                    foreach (var mat in act.Materials)
                        sb.AppendLine($"    {ShortenMat(mat.NamaBarang)}: {mat.QtyText}");
                    if (act.HasHomebase) sb.AppendLine($"    ({act.Homebase})");
                }
            }

            return sb.ToString().TrimEnd();
        }
    }
}
