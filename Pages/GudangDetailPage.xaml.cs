using StokBarangMAUI.Models;

namespace StokBarangMAUI.Pages
{
    public partial class GudangDetailPage : ContentPage
    {
        private readonly GudangWarehouse _warehouse;
        private string _activeFilter = "";

        public GudangDetailPage(GudangWarehouse warehouse)
        {
            InitializeComponent();
            _warehouse = warehouse;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                Title = _warehouse.Name;
                LblWarehouseName.Text = $"Gudang {_warehouse.Name}";
                LblSegmentName.Text   = _warehouse.SegmentName;

                BuildFilterChips();
                ApplyFilter();
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[GudangDetail] OnAppearing: {ex.Message}"); }
        }

        private void BuildFilterChips()
        {
            FilterChips.Children.Clear();
            var names  = _warehouse.Items.Select(i => i.NamaBarang).Distinct().ToList();
            var all    = new[] { "Semua" }.Concat(names);
            var res    = Application.Current?.Resources;
            var active = res != null && res.TryGetValue("TextPrimary", out var v1) ? (Color)v1 : Colors.White;
            var muted  = res != null && res.TryGetValue("TextMuted",   out var v2) ? (Color)v2 : Colors.Gray;
            var cardBg = res != null && res.TryGetValue("CardBg",      out var v3) ? (Color)v3 : Color.FromArgb("#1E293B");
            var border = res != null && res.TryGetValue("BorderClr",   out var v4) ? (Color)v4 : Color.FromArgb("#334155");

            foreach (var name in all)
            {
                bool isAll    = name == "Semua";
                bool isSel    = isAll ? string.IsNullOrEmpty(_activeFilter) : _activeFilter == name;
                var chip = new Border
                {
                    Padding         = new Thickness(12, 6),
                    StrokeShape     = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 20 },
                    BackgroundColor = isSel ? Color.FromArgb("#1D4ED8") : cardBg,
                    Stroke          = isSel ? Colors.Transparent : border,
                };
                chip.Content = new Label
                {
                    Text           = name,
                    FontSize       = 11,
                    FontAttributes = FontAttributes.Bold,
                    TextColor      = isSel ? Colors.White : muted,
                };
                var captured = isAll ? "" : name;
                chip.GestureRecognizers.Add(new TapGestureRecognizer
                {
                    Command = new Command(() =>
                    {
                        _activeFilter = captured;
                        BuildFilterChips();
                        ApplyFilter();
                    })
                });
                FilterChips.Children.Add(chip);
            }
        }

        private void ApplyFilter()
        {
            var filtered = string.IsNullOrEmpty(_activeFilter)
                ? _warehouse.Items
                : _warehouse.Items.Where(i => i.NamaBarang == _activeFilter).ToList();

            ItemList.ItemsSource = filtered;
            LblItemCount.Text    = $"{filtered.Count} material" +
                (string.IsNullOrEmpty(_activeFilter) ? "" : $" · {_activeFilter}");
        }
    }
}
