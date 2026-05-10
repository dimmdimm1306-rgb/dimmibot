using StokBarangMAUI.Services;

namespace StokBarangMAUI.Pages.Stock
{
    public partial class StockMenuPage : ContentPage
    {
        readonly StockDatabaseService _db;

        public StockMenuPage(StockDatabaseService db)
        {
            InitializeComponent();
            _db = db;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadStatsAsync();
        }

        async Task LoadStatsAsync()
        {
            var s = await _db.GetStatsAsync();
            LblWarehouses.Text = s.TotalWarehouses.ToString();
            LblMaterials.Text  = s.TotalMaterials.ToString();
            LblLowStock.Text   = (s.LowStockCount + s.EmptyStockCount).ToString();

            var txToday = s.TodayTransactions;
            LblSubtitle.Text = $"{s.TotalTransactions} transaksi • {txToday} hari ini";

            if (s.EmptyStockCount > 0 || s.LowStockCount > 0)
            {
                AlertBanner.IsVisible = true;
                LblAlert.Text = $"⚠️  {s.EmptyStockCount} item habis, {s.LowStockCount} item stok kurang — segera lakukan pengadaan!";
            }
            else
            {
                AlertBanner.IsVisible = false;
            }
        }

        async void OnDashboard(object? s, EventArgs e) =>
            await Navigation.PushAsync(new DashboardPage(_db));

        async void OnTransaksi(object? s, EventArgs e) =>
            await Navigation.PushAsync(new TransactionFormPage(_db));

        async void OnRiwayat(object? s, EventArgs e) =>
            await Navigation.PushAsync(new HistoryPage(_db));

        async void OnLaporan(object? s, EventArgs e) =>
            await Navigation.PushAsync(new ReportPage(_db));

        async void OnGudang(object? s, EventArgs e) =>
            await Navigation.PushAsync(new WarehouseListPage(_db));

        async void OnBarang(object? s, EventArgs e) =>
            await Navigation.PushAsync(new MaterialListPage(_db));
    }
}
