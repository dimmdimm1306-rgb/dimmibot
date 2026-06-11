using StokBarangMAUI.Models;
using StokBarangMAUI.Services;

namespace StokBarangMAUI.Pages.Stock
{
    public partial class DashboardPage : ContentPage
    {
        readonly StockDatabaseService _db;
        List<Warehouse> _warehouses = [];
        List<StockBalance> _balances = [];
        int? _selectedWarehouseId;
        string _query = string.Empty;
        string _statusFilter = "ALL";
        bool _suppressPickerEvent;

        public DashboardPage(StockDatabaseService db)
        {
            InitializeComponent();
            _db = db;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadWarehousesAsync();
            await LoadDataAsync();
        }

        async Task LoadWarehousesAsync()
        {
            _suppressPickerEvent = true;
            _warehouses = await _db.GetWarehousesAsync();
            var items = new List<string> { "Semua Gudang" };
            items.AddRange(_warehouses.Select(w => w.Name));
            PickerGudang.ItemsSource = items;
            if (PickerGudang.SelectedIndex < 0)
                PickerGudang.SelectedIndex = 0;
            _suppressPickerEvent = false;
        }

        async Task LoadDataAsync()
        {
            try
            {
                SetLoading(true);
                _balances = await _db.GetBalancesAsync(_selectedWarehouseId);
                UpdateStats();
                ApplyFilters();
            }
            catch (Exception ex)
            {
                _balances = [];
                StockList.ItemsSource = _balances;
                EmptyView.IsVisible = true;
                await DisplayAlert("Gagal", ex.Message, "OK");
            }
            finally
            {
                RefreshV.IsRefreshing = false;
                SetLoading(false);
            }
        }

        void UpdateStats()
        {
            int ok = _balances.Count(b => !b.IsLow && !b.IsEmpty);
            int low = _balances.Count(b => b.IsLow);
            int empty = _balances.Count(b => b.IsEmpty);

            CardOk.CardValue = ok.ToString();
            CardOk.CardCaption = "siap dipakai";
            CardLow.CardValue = low.ToString();
            CardLow.CardCaption = "butuh restock";
            CardEmpty.CardValue = empty.ToString();
            CardEmpty.CardCaption = "stop pemakaian";
            CardTotal.CardValue = _balances.Count.ToString();
            CardTotal.CardCaption = _selectedWarehouseId.HasValue ? "di gudang ini" : "semua gudang";
        }

        void ApplyFilters()
        {
            IEnumerable<StockBalance> data = _balances;

            if (!string.IsNullOrWhiteSpace(_query))
            {
                data = data.Where(b =>
                    b.MaterialName.Contains(_query, StringComparison.OrdinalIgnoreCase) ||
                    b.WarehouseName.Contains(_query, StringComparison.OrdinalIgnoreCase) ||
                    b.Category.Contains(_query, StringComparison.OrdinalIgnoreCase));
            }

            data = _statusFilter switch
            {
                "OK" => data.Where(b => !b.IsLow && !b.IsEmpty),
                "LOW" => data.Where(b => b.IsLow),
                "EMPTY" => data.Where(b => b.IsEmpty),
                _ => data,
            };

            var filtered = data.ToList();
            StockList.ItemsSource = filtered;
            EmptyView.IsVisible = !LoadingState.IsVisible && filtered.Count == 0;
            LblCountSummary.Text = $"{filtered.Count} item tampil dari {_balances.Count} baris stok";
            UpdateFilterButtons();
        }

        async void OnGudangChanged(object? s, EventArgs e)
        {
            if (_suppressPickerEvent) return;
            int idx = PickerGudang.SelectedIndex;
            _selectedWarehouseId = idx <= 0 ? null : _warehouses[idx - 1].Id;
            await LoadDataAsync();
        }

        async void OnRefresh(object? s, EventArgs e) => await LoadDataAsync();

        void OnSearchChanged(object? sender, TextChangedEventArgs e)
        {
            _query = e.NewTextValue?.Trim() ?? string.Empty;
            ApplyFilters();
        }

        void OnFilterAll(object? sender, EventArgs e) => SetFilter("ALL");
        void OnFilterOk(object? sender, EventArgs e) => SetFilter("OK");
        void OnFilterLow(object? sender, EventArgs e) => SetFilter("LOW");
        void OnFilterEmpty(object? sender, EventArgs e) => SetFilter("EMPTY");

        void SetFilter(string filter)
        {
            _statusFilter = filter;
            ApplyFilters();
        }

        void SetLoading(bool isLoading)
        {
            LoadingState.IsVisible = isLoading;
            StockList.IsVisible = !isLoading;
            if (isLoading)
                EmptyView.IsVisible = false;
        }

        void UpdateFilterButtons()
        {
            var buttons = new[]
            {
                (Button: BtnAll, Filter: "ALL"),
                (Button: BtnOk, Filter: "OK"),
                (Button: BtnLow, Filter: "LOW"),
                (Button: BtnEmpty, Filter: "EMPTY"),
            };

            foreach (var item in buttons)
            {
                bool active = item.Filter == _statusFilter;
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
