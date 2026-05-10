using StokBarangMAUI.Models;
using StokBarangMAUI.Services;

namespace StokBarangMAUI.Pages.Stock
{
    public partial class WarehouseFormPage : ContentPage
    {
        readonly StockDatabaseService _db;
        readonly Warehouse? _existing;

        public WarehouseFormPage(StockDatabaseService db, Warehouse? existing)
        {
            InitializeComponent();
            _db       = db;
            _existing = existing;
            Title     = existing == null ? "Tambah Gudang" : "Edit Gudang";
            if (existing != null)
            {
                EntryName.Text     = existing.Name;
                EntryLocation.Text = existing.Location;
            }
        }

        async void OnSimpan(object? s, EventArgs e)
        {
            string name = EntryName.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(name))
            {
                await DisplayAlert("Peringatan", "Nama gudang tidak boleh kosong.", "OK");
                return;
            }

            var w = _existing ?? new Warehouse();
            w.Name     = name;
            w.Location = EntryLocation.Text?.Trim() ?? "";
            await _db.SaveWarehouseAsync(w);
            await Navigation.PopAsync();
        }
    }
}
