using StokBarangMAUI.Models;
using StokBarangMAUI.Services;

namespace StokBarangMAUI.Pages.Stock
{
    public partial class MaterialFormPage : ContentPage
    {
        readonly StockDatabaseService _db;
        readonly Material? _existing;

        public MaterialFormPage(StockDatabaseService db, Material? existing)
        {
            InitializeComponent();
            _db       = db;
            _existing = existing;
            Title     = existing == null ? "Tambah Material" : "Edit Material";
            if (existing != null)
            {
                EntryName.Text     = existing.Name;
                EntryUnit.Text     = existing.Unit;
                EntryCategory.Text = existing.Category;
                EntryMinStock.Text = existing.MinStock.ToString();
            }
        }

        async void OnSimpan(object? s, EventArgs e)
        {
            string name = EntryName.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(name))
            {
                await DisplayAlert("Peringatan", "Nama material tidak boleh kosong.", "OK");
                return;
            }

            int minStock = 0;
            int.TryParse(EntryMinStock.Text, out minStock);

            var m = _existing ?? new Material();
            m.Name     = name;
            m.Unit     = EntryUnit.Text?.Trim() ?? "pcs";
            m.Category = EntryCategory.Text?.Trim() ?? "";
            m.MinStock = minStock;
            await _db.SaveMaterialAsync(m);
            await Navigation.PopAsync();
        }
    }
}
