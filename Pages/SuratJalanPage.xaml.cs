using StokBarangMAUI.Models;
using StokBarangMAUI.Services;

namespace StokBarangMAUI.Pages
{
    public partial class SuratJalanPage : ContentPage
    {
        private readonly GoogleSheetsService _sheets;
        private List<SuratJalanGroup> _all  = new();
        private string _activeFilter = "Semua";
        private string _searchText   = "";
        private bool   _sortDesc     = true;

        public SuratJalanPage(GoogleSheetsService sheets)
        {
            InitializeComponent();
            _sheets = sheets;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                LblTheme.Text = App.Theme.ThemeIcon;
                BuildFilterChips();
                if (_all.Count == 0) await LoadData(false);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[SuratJalan] OnAppearing: {ex.Message}"); }
        }

        private async Task LoadData(bool forceRefresh)
        {
            try
            {
                Loader.IsVisible = true;
                Loader.IsRunning = true;
                var data = await _sheets.FetchAsync(forceRefresh);
                _all = GroupItems(data.SuratJalan);
                BuildFilterChips();
                UpdateStats();
                ApplyFilter();
            }
            catch (Exception ex) { await DisplayAlert("Gagal", ex.Message, "OK"); }
            finally
            {
                Loader.IsVisible = false;
                Loader.IsRunning = false;
            }
        }

        private static List<SuratJalanGroup> GroupItems(List<SuratJalanItem> items)
        {
            var result  = new List<SuratJalanGroup>();
            var grouped = new Dictionary<string, SuratJalanGroup>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in items)
            {
                if (!item.HasNoSJ)
                {
                    result.Add(new SuratJalanGroup
                    {
                        Tanggal    = item.Tanggal,  Segment    = item.Segment,
                        NoSJ       = item.NoSJ,     Pengirim   = item.Pengirim,
                        Penerima   = item.Penerima, Keterangan = item.Keterangan,
                        DriveUrl   = item.DriveUrl, Jenis      = item.Jenis,
                        SortDate   = item.SortDate,
                        Items      = [item],
                    });
                }
                else
                {
                    var key = $"{item.Tanggal}|{item.Segment}|{item.NoSJ}";
                    if (!grouped.TryGetValue(key, out var group))
                    {
                        group = new SuratJalanGroup
                        {
                            Tanggal    = item.Tanggal,  Segment    = item.Segment,
                            NoSJ       = item.NoSJ,     Pengirim   = item.Pengirim,
                            Penerima   = item.Penerima, Keterangan = item.Keterangan,
                            DriveUrl   = item.DriveUrl, Jenis      = item.Jenis,
                            SortDate   = item.SortDate,
                        };
                        grouped[key] = group;
                        result.Add(group);
                    }
                    group.Items.Add(item);
                }
            }
            return result;
        }

        private void BuildFilterChips()
        {
            FilterChips.Children.Clear();
            var options     = new[] { "Semua", "MASUK", "KELUAR", "DIBAWA" };
            var activeBg    = Color.FromArgb("#1D4ED8");
            var res         = Application.Current?.Resources;
            var inactiveBg  = res != null && res.TryGetValue("CardBg",    out var r1) ? (Color)r1 : Color.FromArgb("#1E293B");
            var borderColor = res != null && res.TryGetValue("BorderClr", out var r2) ? (Color)r2 : Color.FromArgb("#334155");

            foreach (var opt in options)
            {
                bool isActive = opt == _activeFilter;
                var chip = new Border
                {
                    Padding         = new Thickness(16, 8),
                    StrokeShape     = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 20 },
                    BackgroundColor = isActive ? activeBg : inactiveBg,
                    Stroke          = isActive ? Colors.Transparent : borderColor,
                };
                chip.Content = new Label
                {
                    Text           = opt,
                    TextColor      = isActive ? Colors.White : Color.FromArgb("#94A3B8"),
                    FontSize       = 12,
                    FontAttributes = FontAttributes.Bold,
                };
                var captured = opt;
                chip.GestureRecognizers.Add(new TapGestureRecognizer
                {
                    Command = new Command(() => { _activeFilter = captured; BuildFilterChips(); ApplyFilter(); })
                });
                FilterChips.Children.Add(chip);
            }
        }

        private void UpdateStats()
        {
            TotalLabel.Text  = _all.Count.ToString();
            MasukLabel.Text  = _all.Count(x => x.IsMasuk).ToString();
            KeluarLabel.Text = _all.Count(x => !x.IsMasuk).ToString();
        }

        private void OnSortToggle(object sender, TappedEventArgs e)
        {
            _sortDesc    = !_sortDesc;
            LblSort.Text = _sortDesc ? "↓ Terbaru" : "↑ Terlama";
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            var f = _all.AsEnumerable();
            if (_activeFilter != "Semua")
                f = f.Where(x => x.JenisPendek == _activeFilter);
            if (!string.IsNullOrWhiteSpace(_searchText))
                f = f.Where(x =>
                    x.Segment.Contains(_searchText,  StringComparison.OrdinalIgnoreCase) ||
                    x.Penerima.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
                    x.NoSJ.Contains(_searchText,     StringComparison.OrdinalIgnoreCase) ||
                    x.Items.Any(i => i.NamaBarang.Contains(_searchText, StringComparison.OrdinalIgnoreCase)));
            List.ItemsSource = _sortDesc
                ? f.OrderByDescending(x => x.SortDate).ToList()
                : f.OrderBy(x => x.SortDate).ToList();
        }

        private void OnSearchChanged(object sender, TextChangedEventArgs e)
        {
            _searchText        = e.NewTextValue ?? "";
            BtnClear.IsVisible = _searchText.Length > 0;
            ApplyFilter();
        }

        private void OnClearSearch(object sender, TappedEventArgs e)
        {
            SearchEntry.Text   = "";
            _searchText        = "";
            BtnClear.IsVisible = false;
            ApplyFilter();
        }

        private async void OnItemTapped(object sender, TappedEventArgs e)
        {
            if (sender is not Border border) return;
            if (border.BindingContext is not SuratJalanGroup group) return;

            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            await border.ScaleTo(0.97, 60, Easing.CubicOut);
            await border.ScaleTo(1.0,  60, Easing.CubicIn);

            await Navigation.PushAsync(new SuratJalanDetailPage(group));
        }

        private async void OnUpdateTapped(object sender, TappedEventArgs e)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            await LoadData(true);
        }

        private void OnThemeToggle(object sender, TappedEventArgs e)
        {
            App.Theme.Toggle();
            LblTheme.Text = App.Theme.ThemeIcon;
            BuildFilterChips();
        }

        private async void OnAiBotClicked(object sender, TappedEventArgs e)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            await Navigation.PushModalAsync(new AiChatPopup());
        }

        private async void OnHamburger(object sender, TappedEventArgs e)
            => await (RootTabbedPage.OpenDrawer?.Invoke() ?? Task.CompletedTask);
    }
}
