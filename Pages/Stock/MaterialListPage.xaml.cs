using StokBarangMAUI.Models;
using StokBarangMAUI.Services;

namespace StokBarangMAUI.Pages.Stock
{
    public partial class MaterialListPage : ContentPage
    {
        readonly StockDatabaseService _db;

        public MaterialListPage(StockDatabaseService db)
        {
            InitializeComponent();
            _db = db;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            MatList.ItemsSource = await _db.GetMaterialsAsync();
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
            if (s is Button btn && btn.CommandParameter is Material m)
            {
                bool ok = await DisplayAlert("Hapus", $"Hapus material '{m.Name}'?", "Ya", "Batal");
                if (!ok) return;
                await _db.DeleteMaterialAsync(m);
                MatList.ItemsSource = await _db.GetMaterialsAsync();
            }
        }
    }
}
