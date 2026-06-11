using StokBarangMAUI.Models;
using StokBarangMAUI.Services;

namespace StokBarangMAUI.Pages.Stock
{
    public partial class MaterialListPage : ContentPage
    {
        readonly StockDatabaseService _db;
        List<Material> _materials = [];
        string _query = string.Empty;
        string _categoryFilter = "ALL";

        public MaterialListPage(StockDatabaseService db)
        {
            InitializeComponent();
            _db = db;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadMaterialsAsync();
        }

        async Task LoadMaterialsAsync()
        {
            try
            {
                _materials = await _db.GetMaterialsAsync();
                ApplyFilters();
            }
            catch (Exception ex)
            {
                _materials = [];
                MatList.ItemsSource = _materials;
                EmptyView.IsVisible = true;
                await DisplayAlert("Gagal", ex.Message, "OK");
            }
        }

        void ApplyFilters()
        {
            IEnumerable<Material> data = _materials;

            if (!string.IsNullOrWhiteSpace(_query))
            {
                data = data.Where(m =>
                    m.Name.Contains(_query, StringComparison.OrdinalIgnoreCase) ||
                    m.Unit.Contains(_query, StringComparison.OrdinalIgnoreCase) ||
                    m.Category.Contains(_query, StringComparison.OrdinalIgnoreCase));
            }

            if (_categoryFilter != "ALL")
                data = data.Where(m => m.Category.Contains(_categoryFilter, StringComparison.OrdinalIgnoreCase));

            var filtered = data.ToList();
            MatList.ItemsSource = filtered;
            EmptyView.IsVisible = filtered.Count == 0;
            LblCountSummary.Text = $"{filtered.Count} material tampil dari {_materials.Count} master";
            UpdateFilterButtons();
        }

        async void OnTambah(object? s, EventArgs e) =>
            await Navigation.PushAsync(new MaterialFormPage(_db, null));

        async void OnEdit(object? s, EventArgs e)
        {
            if (s is Button btn && btn.CommandParameter is Material m)
                await Navigation.PushAsync(new MaterialFormPage(_db, m));
        }

        async void OnHapus(object? s, EventArgs e)
        {
            if (s is not Button btn || btn.CommandParameter is not Material m)
                return;

            bool ok = await DisplayAlert("Hapus material", $"Hapus '{m.Name}' dari master barang?", "Hapus", "Batal");
            if (!ok) return;

            await _db.DeleteMaterialAsync(m);
            await LoadMaterialsAsync();
        }

        void OnSearchChanged(object? sender, TextChangedEventArgs e)
        {
            _query = e.NewTextValue?.Trim() ?? string.Empty;
            ApplyFilters();
        }

        void OnFilterAll(object? sender, EventArgs e) => SetCategory("ALL");
        void OnFilterKabel(object? sender, EventArgs e) => SetCategory("Kabel");
        void OnFilterAksesori(object? sender, EventArgs e) => SetCategory("Aksesori");
        void OnFilterTiang(object? sender, EventArgs e) => SetCategory("Tiang");

        void SetCategory(string category)
        {
            _categoryFilter = category;
            ApplyFilters();
        }

        void UpdateFilterButtons()
        {
            var buttons = new[]
            {
                (Button: BtnAll, Filter: "ALL"),
                (Button: BtnKabel, Filter: "Kabel"),
                (Button: BtnAksesori, Filter: "Aksesori"),
                (Button: BtnTiang, Filter: "Tiang"),
            };

            foreach (var item in buttons)
            {
                bool active = item.Filter == _categoryFilter;
                item.Button.BackgroundColor = ResourceColor(active ? "ChipSelectedBg" : "ChipBg", active ? "#0E7490" : "#10243A");
                item.Button.TextColor = active ? Colors.White : ResourceColor("TextSecond", "#B8D7EA");
                item.Button.BorderColor = active ? ResourceColor("AccentBlueBorder", "#0EA5E9") : ResourceColor("BorderClr", "#1C3950");
            }
        }

        static Color ResourceColor(string key, string fallback)
        {
            if (Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color)
                return color;

            return Color.FromArgb(fallback);
        }
    }
}
