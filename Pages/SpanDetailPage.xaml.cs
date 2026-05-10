using StokBarangMAUI.Models;
using StokBarangMAUI.Services;

namespace StokBarangMAUI.Pages
{
    public partial class SpanDetailPage : ContentPage
    {
        private readonly GoogleSheetsService _sheets;
        private readonly int    _segmentNo;
        private readonly string _segmentRute;

        private List<SpanItem>     _all       = new();
        private List<ProgressItem> _allHarian = new();

        private string _search    = "";
        private string _filter    = "semua";
        private string _mode      = "span";
        private bool   _harianSortDesc = true;

        public string? InitialSearch { get; set; }

        private Border[] _chips    = null!;
        private string[] _chipKeys = null!;

        public SpanDetailPage(GoogleSheetsService sheets, int segmentNo,
                              string segmentName, string accentColor)
        {
            InitializeComponent();
            _sheets      = sheets;
            _segmentNo   = segmentNo;
            _segmentRute = segmentName;
            Title            = segmentName;
            LblSegTitle.Text = segmentName.Trim(' ', '-');
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                TiTheme.Text = App.Theme.ThemeIcon;
                _chips    = new[] { ChipSemua, ChipOK, ChipNOK, ChipProgress, ChipNoProgress };
                _chipKeys = new[] { "semua", "ok", "nok", "progress", "noprogress" };
                if (!string.IsNullOrWhiteSpace(InitialSearch))
                {
                    SearchSpan.Text        = InitialSearch;
                    _search                = InitialSearch;
                    BtnClearSpan.IsVisible = true;
                    InitialSearch          = null;
                }
                if (_all.Count == 0)
                    _ = LoadData();
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[SpanDetail] OnAppearing: {ex.Message}"); }
        }

        private void OnThemeToggle(object s, EventArgs e)
        {
            App.Theme.Toggle();
            TiTheme.Text = App.Theme.ThemeIcon;
        }

        private async Task LoadData(bool force = false)
        {
            try
            {
                Loader.IsVisible = true;
                Loader.IsRunning = true;

                var spanTask   = _sheets.FetchSpanAsync(_segmentNo, force);
                var harianTask = _sheets.FetchDailyProgressAsync(_segmentRute, force);
                await Task.WhenAll(spanTask, harianTask);

                _all       = spanTask.Result;
                _allHarian = harianTask.Result;

                UpdateStats();
                ApplyFilter();
            }
            catch (Exception ex) { await DisplayAlert("Gagal", ex.Message, "OK"); }
            finally
            {
                Loader.IsVisible             = false;
                Loader.IsRunning             = false;
                Refresher.IsRefreshing       = false;
                HarianRefresher.IsRefreshing = false;
            }
        }

        private void UpdateStats()
        {
            LblTotalSpan.Text = _all.Count.ToString();
            LblKabelDone.Text = _all.Count(x => x.KabelPctVal > 0).ToString();
            LblStatusOK.Text  = _all.Count(x => x.IsOK).ToString();
        }

        // ── Mode toggle ─────────────────────────────────────────────────
        private void OnSwitchMode(object sender, TappedEventArgs e)
        {
            if (e.Parameter is not string mode || mode == _mode) return;
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            _mode = mode;

            bool isSpan = _mode == "span";
            FilterChipsRow.IsVisible  = isSpan;
            Refresher.IsVisible       = isSpan;
            HarianRefresher.IsVisible = !isSpan;
            HarianSortBtn.IsVisible   = !isSpan;
            ModeSpanBtn.BackgroundColor   = isSpan ? Color.FromArgb("#1D4ED8") : Colors.Transparent;
            ModeHarianBtn.BackgroundColor = isSpan ? Colors.Transparent : Color.FromArgb("#1D4ED8");
            var res = Application.Current?.Resources;
            var muted = res != null && res.TryGetValue("TextMuted", out var tm) ? (Color)tm : Colors.Gray;
            LblModeSpan.TextColor   = isSpan ? Colors.White : muted;
            LblModeHarian.TextColor = isSpan ? muted : Colors.White;
            SearchSpan.Placeholder  = isSpan ? "Cari span / kabupaten..." : "Cari span / nama barang...";

            ApplyFilter();
        }

        // ── Filter ──────────────────────────────────────────────────────
        private void ApplyFilter()
        {
            if (_mode == "harian") { ApplyHarianFilter(); return; }

            var f = _all.AsEnumerable();
            f = _filter switch
            {
                "ok"         => f.Where(x => x.IsOK),
                "nok"        => f.Where(x => !x.IsOK),
                "progress"   => f.Where(x => x.KabelPctVal > 0 || x.T7PctVal > 0 || x.T9PctVal > 0),
                "noprogress" => f.Where(x => x.KabelPctVal == 0 && x.T7PctVal == 0 && x.T9PctVal == 0),
                _            => f
            };
            if (!string.IsNullOrWhiteSpace(_search))
            {
                var q = _search.Trim();
                f = f.Where(x =>
                    x.Rute.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    x.Kab.Contains(q, StringComparison.OrdinalIgnoreCase));
            }
            // Sort by No ascending (default)
            SpanList.ItemsSource = f.OrderBy(x => x.No).ToList();
        }

        private void OnHarianSortToggle(object sender, TappedEventArgs e)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            _harianSortDesc       = !_harianSortDesc;
            LblHarianSort.Text    = _harianSortDesc ? "↓ Terbaru" : "↑ Terlama";
            ApplyHarianFilter();
        }

        private void ApplyHarianFilter()
        {
            var f = _allHarian.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(_search))
            {
                var q = _search.Trim();
                f = f.Where(x =>
                    x.Span.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    x.NamaBarang.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    x.Homebase.Contains(q, StringComparison.OrdinalIgnoreCase));
            }
            HarianList.ItemsSource = _harianSortDesc
                ? f.OrderByDescending(x => x.SortDate).ToList()
                : f.OrderBy(x => x.SortDate).ToList();
        }

        private void OnFilterChip(object sender, TappedEventArgs e)
        {
            if (e.Parameter is not string key) return;
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            _filter = key;
            RefreshChips();
            ApplyFilter();
        }

        private void RefreshChips()
        {
            var res = Application.Current?.Resources;
            if (res is null) return;
            res.TryGetValue("CardBg",    out var cbRaw); var inactiveBg     = cbRaw is Color cb ? cb : Color.FromArgb("#1E293B");
            res.TryGetValue("BorderClr", out var blRaw); var inactiveBorder = blRaw is Color bl ? bl : Color.FromArgb("#334155");
            for (int i = 0; i < _chips.Length; i++)
            {
                bool active = _chipKeys[i] == _filter;
                _chips[i].BackgroundColor = active ? Color.FromArgb("#1D4ED8") : inactiveBg;
                _chips[i].Stroke = active
                    ? new SolidColorBrush(Colors.Transparent)
                    : new SolidColorBrush(inactiveBorder);
            }
        }

        private void OnSearchChanged(object sender, TextChangedEventArgs e)
        {
            _search = e.NewTextValue ?? "";
            BtnClearSpan.IsVisible = _search.Length > 0;
            ApplyFilter();
        }

        private void OnClearSpanSearch(object sender, TappedEventArgs e)
        {
            SearchSpan.Text        = "";
            _search                = "";
            BtnClearSpan.IsVisible = false;
            ApplyFilter();
        }

        private async void OnRefreshing(object sender, EventArgs e)
        {
            _all       = new();
            _allHarian = new();
            await LoadData(force: true);
        }

        // ── Tap span card → detail popup ────────────────────────────────
        private async void OnSpanTapped(object sender, TappedEventArgs e)
        {
            if (sender is not Border border) return;
            if (border.BindingContext is not SpanItem span) return;

            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            await border.ScaleTo(0.97, 60, Easing.CubicOut);
            await border.ScaleTo(1.0,  60, Easing.CubicIn);

            var kabel = $"{span.KabelProgress:N0} m / {span.KabelPlan:N0} m  ({span.KabelPct})";
            var t7    = $"{span.T7Progress:N0} btg / {span.T7Plan:N0} btg  ({span.T7Pct})";
            var t9    = $"{span.T9Progress:N0} btg / {span.T9Plan:N0} btg  ({span.T9Pct})";
            var st    = string.IsNullOrWhiteSpace(span.Status) ? "–" : span.StatusBadge;
            var tl    = string.IsNullOrWhiteSpace(span.TimeLine) ? "–" : span.TimeLine;
            var kab   = string.IsNullOrWhiteSpace(span.Kab) ? "–" : span.Kab;

            var msg = $"📍 Kabupaten : {kab}\n" +
                      $"📊 Status    : {st}\n" +
                      $"🕒 Timeline  : {tl}\n\n" +
                      $"━━ Kabel ━━\n{kabel}\n\n" +
                      $"━━ Tiang 7m ━━\n{t7}\n\n" +
                      $"━━ Tiang 9m ━━\n{t9}";

            await DisplayAlert($"Span — {span.Rute}", msg, "Tutup");
        }
    }
}
