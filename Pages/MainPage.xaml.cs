using StokBarangMAUI.Models;
using StokBarangMAUI.Services;

namespace StokBarangMAUI.Pages
{
    public partial class MainPage : ContentPage
    {
        private readonly DatabaseService _db;

        public MainPage(DatabaseService db)
        {
            InitializeComponent();
            _db = db;
        }

protected override async void OnAppearing()
{
    base.OnAppearing();
    try
    {
        BarangList.ItemsSource = await _db.GetAllAsync();
    }
    catch (Exception ex)
    {
        await DisplayAlert("Error", $"Gagal memuat data: {ex.Message}", "OK");
    }
}

private async void OnTambahClicked(object sender, EventArgs e)
{
    try
    {
        await Navigation.PushAsync(new FormPage(_db, null));
    }
    catch (Exception ex)
    {
        await DisplayAlert("Error", $"Gagal membuka form: {ex.Message}", "OK");
    }
}

private async void OnBotClicked(object sender, EventArgs e)
{
    try
    {
        await Shell.Current.GoToAsync("openclawbot");
    }
    catch (Exception ex)
    {
        await DisplayAlert("Error", $"Gagal membuka bot: {ex.Message}", "OK");
    }
}

private async void OnEditClicked(object sender, EventArgs e)
{
    try
    {
        if (sender is Button btn && btn.CommandParameter is int id)
        {
            var barang = await _db.GetByIdAsync(id);
            if (barang != null)
                await Navigation.PushAsync(new FormPage(_db, barang));
        }
    }
    catch (Exception ex)
    {
        await DisplayAlert("Error", $"Gagal membuka form edit: {ex.Message}", "OK");
    }
}

private async void OnTambahStokClicked(object sender, EventArgs e)
{
    try
    {
        if (sender is Button btn && btn.CommandParameter is int id)
        {
            var barang = await _db.GetByIdAsync(id);
            if (barang != null)
            {
                barang.Stok++;
                await _db.SaveAsync(barang);
                BarangList.ItemsSource = await _db.GetAllAsync();
            }
        }
    }
    catch (Exception ex)
    {
        await DisplayAlert("Error", $"Gagal menambah stok: {ex.Message}", "OK");
    }
}

private async void OnKurangiStokClicked(object sender, EventArgs e)
{
    try
    {
        if (sender is Button btn && btn.CommandParameter is int id)
        {
            var barang = await _db.GetByIdAsync(id);
            if (barang != null && barang.Stok > 0)
            {
                barang.Stok--;
                await _db.SaveAsync(barang);
                BarangList.ItemsSource = await _db.GetAllAsync();
            }
        }
    }
    catch (Exception ex)
    {
        await DisplayAlert("Error", $"Gagal mengurangi stok: {ex.Message}", "OK");
    }
}

private async void OnHapusClicked(object sender, EventArgs e)
{
    try
    {
        if (sender is Button btn && btn.CommandParameter is int id)
        {
            var barang = await _db.GetByIdAsync(id);
            if (barang == null) return;

            bool confirm = await DisplayAlert("Hapus", $"Yakin ingin hapus {barang.NamaBarang}?", "Ya", "Batal");
            if (confirm)
            {
                await _db.DeleteAsync(barang);
                BarangList.ItemsSource = await _db.GetAllAsync();
            }
        }
    }
    catch (Exception ex)
    {
        await DisplayAlert("Error", $"Gagal menghapus barang: {ex.Message}", "OK");
    }
}
    }
}
