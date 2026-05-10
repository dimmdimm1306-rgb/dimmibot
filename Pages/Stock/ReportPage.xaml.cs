using StokBarangMAUI.Models;
using StokBarangMAUI.Services;

namespace StokBarangMAUI.Pages.Stock
{
    public partial class ReportPage : ContentPage
    {
        readonly StockDatabaseService _db;
        List<Warehouse> _warehouses = [];
        int? _selectedWarehouseId;

        public ReportPage(StockDatabaseService db)
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
            await LoadDataAsync();
        }

        async Task LoadDataAsync()
        {
            var balances = await _db.GetBalancesAsync(_selectedWarehouseId);
            BalanceList.ItemsSource = balances;

            int idx = PickerGudang.SelectedIndex;
            LblTitle.Text = idx <= 0 ? "Stok Semua Gudang" :
                $"Stok Gudang {_warehouses[idx - 1].Name}";
        }

        async void OnGudangChanged(object? s, EventArgs e)
        {
            int idx = PickerGudang.SelectedIndex;
            _selectedWarehouseId = idx <= 0 ? null : _warehouses[idx - 1].Id;
            await LoadDataAsync();
        }

        async void OnExportStok(object? s, EventArgs e)
        {
            var csv = await _db.ExportCsvAsync(_selectedWarehouseId);
            await ShareCsvAsync(csv, "stok_gudang.csv");
        }

        async void OnExportTx(object? s, EventArgs e)
        {
            var csv = await _db.ExportTransactionCsvAsync(_selectedWarehouseId);
            await ShareCsvAsync(csv, "transaksi_gudang.csv");
        }

        static async Task ShareCsvAsync(string content, string fileName)
        {
            var path = Path.Combine(FileSystem.CacheDirectory, fileName);
            await File.WriteAllTextAsync(path, content, System.Text.Encoding.UTF8);
            await Share.RequestAsync(new ShareFileRequest
            {
                Title = "Export Data",
                File  = new ShareFile(path, "text/csv")
            });
        }
    }
}
