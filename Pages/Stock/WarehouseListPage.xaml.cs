using StokBarangMAUI.Models;
using StokBarangMAUI.Services;

namespace StokBarangMAUI.Pages.Stock
{
    public partial class WarehouseListPage : ContentPage
    {
        readonly StockDatabaseService _db;

        public WarehouseListPage(StockDatabaseService db)
        {
            InitializeComponent();
            _db = db;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            WhList.ItemsSource = await _db.GetWarehousesAsync();
        }

        async void OnTambah(object? s, EventArgs e)
        {
            await Navigation.PushAsync(new WarehouseFormPage(_db, null));
        }

        async void OnEdit(object? s, EventArgs e)
        {
            if (s is Button btn && btn.CommandParameter is Warehouse w)
                await Navigation.PushAsync(new WarehouseFormPage(_db, w));
        }

        async void OnHapus(object? s, EventArgs e)
        {
            if (s is Button btn && btn.CommandParameter is Warehouse w)
            {
                bool ok = await DisplayAlert("Hapus", $"Hapus gudang '{w.Name}'?", "Ya", "Batal");
                if (!ok) return;
                await _db.DeleteWarehouseAsync(w);
                WhList.ItemsSource = await _db.GetWarehousesAsync();
            }
        }
    }
}
