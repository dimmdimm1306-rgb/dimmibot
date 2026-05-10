using StokBarangMAUI.Models;
using StokBarangMAUI.Services;

namespace StokBarangMAUI.Pages
{
    public partial class FormPage : ContentPage
    {
        private readonly DatabaseService _db;
        private readonly Barang _barang;

        public FormPage(DatabaseService db, Barang barang)
        {
            InitializeComponent();
            _db = db;
            _barang = barang ?? new Barang();
            Title = barang == null ? "Tambah Barang" : "Edit Barang";

            KodeEntry.Text = _barang.KodeBarang;
            NamaEntry.Text = _barang.NamaBarang;
            StokEntry.Text = _barang.Stok.ToString();
        }

        private async void OnSimpanClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(KodeEntry.Text) || string.IsNullOrWhiteSpace(NamaEntry.Text))
            {
                await DisplayAlert("Perhatian", "Kode dan Nama barang harus diisi.", "OK");
                return;
            }

            _barang.KodeBarang = KodeEntry.Text.Trim();
            _barang.NamaBarang = NamaEntry.Text.Trim();
            _barang.Stok = int.TryParse(StokEntry.Text, out int stok) ? stok : 0;

            await _db.SaveAsync(_barang);
            await Navigation.PopAsync();
        }

        private async void OnBatalClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
