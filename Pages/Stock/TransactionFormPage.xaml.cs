using StokBarangMAUI.Models;
using StokBarangMAUI.Services;

namespace StokBarangMAUI.Pages.Stock
{
    public partial class TransactionFormPage : ContentPage
    {
        readonly StockDatabaseService _db;
        List<Warehouse> _warehouses = [];
        List<Material>  _materials  = [];
        string _type = "MASUK";

        public TransactionFormPage(StockDatabaseService db)
        {
            InitializeComponent();
            _db = db;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            _warehouses = await _db.GetWarehousesAsync();
            _materials  = await _db.GetMaterialsAsync();

            var wNames = _warehouses.Select(w => w.Name).ToList();
            PickerGudang.ItemsSource       = wNames;
            PickerGudangTujuan.ItemsSource = wNames;
            PickerMaterial.ItemsSource     = _materials.Select(m => $"{m.Name} ({m.Unit})").ToList();
        }

        void SetType(string type)
        {
            _type = type;
            var inactiveBg   = (Color)Application.Current!.Resources["CardBg"];
            var inactiveText = (Color)Application.Current!.Resources["TextMuted"];
            BtnMasuk.BackgroundColor    = type == "MASUK"    ? Color.FromArgb("#16A34A") : inactiveBg;
            BtnKeluar.BackgroundColor   = type == "KELUAR"   ? Color.FromArgb("#DC2626") : inactiveBg;
            BtnTransfer.BackgroundColor = type == "TRANSFER" ? Color.FromArgb("#2563EB") : inactiveBg;

            var masukLabel    = (Label)BtnMasuk.Content;
            var keluarLabel   = (Label)BtnKeluar.Content;
            var transferLabel = (Label)BtnTransfer.Content;
            masukLabel.TextColor    = type == "MASUK"    ? Colors.White : inactiveText;
            keluarLabel.TextColor   = type == "KELUAR"   ? Colors.White : inactiveText;
            transferLabel.TextColor = type == "TRANSFER" ? Colors.White : inactiveText;

            bool isTransfer = type == "TRANSFER";
            LblGudangTujuan.IsVisible    = isTransfer;
            BorderGudangTujuan.IsVisible = isTransfer;

            UpdateStokInfo();
        }

        void OnTypeMasuk(object? s, EventArgs e)    => SetType("MASUK");
        void OnTypeKeluar(object? s, EventArgs e)   => SetType("KELUAR");
        void OnTypeTransfer(object? s, EventArgs e) => SetType("TRANSFER");

        async void OnMaterialChanged(object? s, EventArgs e) => await UpdateStokInfoAsync();

        async Task UpdateStokInfoAsync()
        {
            if (_type == "MASUK") { BorderStokInfo.IsVisible = false; return; }
            int wIdx = PickerGudang.SelectedIndex;
            int mIdx = PickerMaterial.SelectedIndex;
            if (wIdx < 0 || mIdx < 0) { BorderStokInfo.IsVisible = false; return; }

            var balances = await _db.GetBalancesAsync(_warehouses[wIdx].Id);
            var bal = balances.FirstOrDefault(b => b.MaterialId == _materials[mIdx].Id);
            int stok = bal?.Balance ?? 0;
            string unit = _materials[mIdx].Unit;

            BorderStokInfo.IsVisible = true;
            LblStokTersedia.Text = $"Stok tersedia: {stok:N0} {unit}";
        }

        void UpdateStokInfo()
        {
            if (_type == "MASUK") BorderStokInfo.IsVisible = false;
        }

        async void OnSimpan(object? s, EventArgs e)
        {
            int wIdx = PickerGudang.SelectedIndex;
            int mIdx = PickerMaterial.SelectedIndex;

            if (wIdx < 0) { await DisplayAlert("Peringatan", "Pilih gudang terlebih dahulu.", "OK"); return; }
            if (mIdx < 0) { await DisplayAlert("Peringatan", "Pilih material terlebih dahulu.", "OK"); return; }
            if (!int.TryParse(EntryQty.Text, out int qty) || qty <= 0)
            {
                await DisplayAlert("Peringatan", "Masukkan jumlah yang valid (> 0).", "OK"); return;
            }
            if (_type == "TRANSFER" && PickerGudangTujuan.SelectedIndex < 0)
            {
                await DisplayAlert("Peringatan", "Pilih gudang tujuan.", "OK"); return;
            }
            if (_type == "TRANSFER" && PickerGudangTujuan.SelectedIndex == wIdx)
            {
                await DisplayAlert("Peringatan", "Gudang asal dan tujuan tidak boleh sama.", "OK"); return;
            }

            var tx = new StockTransaction
            {
                Type          = _type,
                WarehouseId   = _warehouses[wIdx].Id,
                ToWarehouseId = _type == "TRANSFER" ? _warehouses[PickerGudangTujuan.SelectedIndex].Id : 0,
                MaterialId    = _materials[mIdx].Id,
                Qty           = qty,
                ProjectName   = EntryProyek.Text?.Trim() ?? "",
                Notes         = EditorNotes.Text?.Trim() ?? "",
                Date          = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
            };

            var (ok, msg) = await _db.AddTransactionAsync(tx);
            if (!ok) { await DisplayAlert("Gagal", msg, "OK"); return; }

            await DisplayAlert("Berhasil", msg, "OK");
            EntryQty.Text    = "";
            EntryProyek.Text = "";
            EditorNotes.Text = "";
            PickerGudang.SelectedIndex       = -1;
            PickerGudangTujuan.SelectedIndex = -1;
            PickerMaterial.SelectedIndex     = -1;
            BorderStokInfo.IsVisible         = false;
        }
    }
}
