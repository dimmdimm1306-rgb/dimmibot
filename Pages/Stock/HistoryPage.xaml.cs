using StokBarangMAUI.Models;
using StokBarangMAUI.Services;

namespace StokBarangMAUI.Pages.Stock
{
    public partial class HistoryPage : ContentPage
    {
        readonly StockDatabaseService _db;
        List<Warehouse> _warehouses = [];

        public HistoryPage(StockDatabaseService db)
        {
            InitializeComponent();
            _db = db;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            _warehouses = await _db.GetWarehousesAsync();
            var items = new List<string> { "Semua Gudang" };
            items.AddRange(_warehouses.Select(w => w.Name));
            PickerGudang.ItemsSource = items;
            await LoadAsync();
        }

        async Task LoadAsync()
        {
            int? wId = null;
            int wIdx = PickerGudang.SelectedIndex;
            if (wIdx > 0) wId = _warehouses[wIdx - 1].Id;

            string? type = null;
            int tIdx = PickerType.SelectedIndex;
            if (tIdx > 0) type = PickerType.Items[tIdx];

            TxList.ItemsSource = await _db.GetTransactionsAsync(warehouseId: wId, type: type);
            RefreshV.IsRefreshing = false;
        }

        async void OnFilterChanged(object? s, EventArgs e) => await LoadAsync();
        async void OnRefresh(object? s, EventArgs e)       => await LoadAsync();
        async void OnRefreshView(object? s, EventArgs e)   => await LoadAsync();
    }
}
