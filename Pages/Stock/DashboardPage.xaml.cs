using StokBarangMAUI.Models;
using StokBarangMAUI.Services;

namespace StokBarangMAUI.Pages.Stock
{
    public partial class DashboardPage : ContentPage
    {
        readonly StockDatabaseService _db;
        List<Warehouse> _warehouses = [];
        int? _selectedWarehouseId;

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
            _warehouses = await _db.GetWarehousesAsync();
            var items = new List<string> { "Semua Gudang" };
            items.AddRange(_warehouses.Select(w => w.Name));
            PickerGudang.ItemsSource = items;
            if (PickerGudang.SelectedIndex < 0)
                PickerGudang.SelectedIndex = 0;
        }

        async Task LoadDataAsync()
        {
            var balances = await _db.GetBalancesAsync(_selectedWarehouseId);
            StockList.ItemsSource = balances;

            int ok    = balances.Count(b => !b.IsLow && !b.IsEmpty);
            int low   = balances.Count(b => b.IsLow);
            int empty = balances.Count(b => b.IsEmpty);

            LblOk.Text    = ok.ToString();
            LblLow.Text   = low.ToString();
            LblEmpty.Text = empty.ToString();
            LblTotal.Text = balances.Count.ToString();
            LblEmpty2.IsVisible = balances.Count == 0;
            RefreshV.IsRefreshing = false;
        }

        async void OnGudangChanged(object? s, EventArgs e)
        {
            int idx = PickerGudang.SelectedIndex;
            _selectedWarehouseId = idx <= 0 ? null : _warehouses[idx - 1].Id;
            await LoadDataAsync();
        }

        async void OnRefresh(object? s, EventArgs e) => await LoadDataAsync();
    }
}
