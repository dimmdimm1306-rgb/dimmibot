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
            try
            {
                var s = await _db.GetStatsAsync();
                CardWarehouses.CardValue = s.TotalWarehouses.ToString();
                CardWarehouses.CardCaption = "lokasi aktif";
                CardMaterials.CardValue = s.TotalMaterials.ToString();
                CardMaterials.CardCaption = "master barang";
                CardLowStock.CardValue = (s.LowStockCount + s.EmptyStockCount).ToString();
                CardLowStock.CardCaption = "habis / hampir habis";
                CardTodayTx.CardValue = s.TodayTransactions.ToString();
                CardTodayTx.CardCaption = "transaksi";

                LblSubtitle.Text = $"{s.TotalTransactions} transaksi total - {s.TodayTransactions} hari ini";

                if (s.EmptyStockCount > 0 || s.LowStockCount > 0)
                {
                    AlertBanner.IsVisible = true;
                    LblAlert.Text = $"Perhatian: {s.EmptyStockCount} item habis, {s.LowStockCount} item hampir habis. Cek sebelum tim lapangan berangkat.";
                }
                else
                {
                    AlertBanner.IsVisible = false;
                }

                var recent = await _db.GetTransactionsAsync(limit: 5);
                RecentList.ItemsSource = recent;
                RecentEmpty.IsVisible = recent.Count == 0;
            }
            catch (Exception ex)
            {
                LblSubtitle.Text = "Gagal memuat ringkasan stok.";
                RecentEmpty.IsVisible = true;
                await DisplayAlert("Gagal", ex.Message, "OK");
            }
            finally
            {
                RefreshV.IsRefreshing = false;
            }
        }

        async void OnRefresh(object? s, EventArgs e) => await LoadStatsAsync();

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
