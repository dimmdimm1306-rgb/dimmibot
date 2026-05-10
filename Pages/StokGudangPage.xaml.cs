using StokBarangMAUI.Models;
using StokBarangMAUI.Services;

namespace StokBarangMAUI.Pages
{
    public partial class StokGudangPage : ContentPage
    {
        private readonly GoogleSheetsService _sheets;
        private bool _loaded = false;

        // ── Combine view state ─────────────────────────────────────────
        private string _viewMode = "gudang"; // gudang | combine
        private List<GudangWarehouse> _warehouses = new();
        private List<string> _materials = new();
        private string _selectedMaterial = "";
        // key = warehouseName|nama → selected state
        private readonly HashSet<string> _selectedKeys = new(StringComparer.OrdinalIgnoreCase);

        public StokGudangPage(GoogleSheetsService sheets)
        {
            InitializeComponent();
            _sheets = sheets;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            LblTheme.Text = App.Theme.ThemeIcon;
            if (!_loaded) await LoadData(false);
        }

        private async Task LoadData(bool force)
        {
            try
            {
                Loader.IsVisible = true;
                Loader.IsRunning = true;
                var data = await _sheets.FetchAsync(force);
                _warehouses = data.GudangWarehouses?.ToList() ?? new();
                WarehouseList.ItemsSource = _warehouses;
                _loaded = true;

                BuildMaterialList();
                if (_viewMode == "combine") RebuildCombineRows();
            }
            catch (Exception ex) { await DisplayAlert("Gagal", ex.Message, "OK"); }
            finally
            {
                Loader.IsVisible = false;
                Loader.IsRunning = false;
            }
        }

        private async void OnUpdateTapped(object sender, TappedEventArgs e)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            _loaded = false;
            await LoadData(true);
        }

        private void OnThemeToggle(object sender, TappedEventArgs e)
        {
            App.Theme.Toggle();
            LblTheme.Text = App.Theme.ThemeIcon;
            if (_viewMode == "combine") RebuildCombineRows();
        }

        private async void OnAiBotClicked(object sender, TappedEventArgs e)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            await Navigation.PushModalAsync(new AiChatPopup());
        }

        private async void OnHamburger(object sender, TappedEventArgs e)
            => await (RootTabbedPage.OpenDrawer?.Invoke() ?? Task.CompletedTask);

        private async void OnWarehouseTapped(object sender, TappedEventArgs e)
        {
            if (sender is not Border border) return;
            if (border.BindingContext is not GudangWarehouse warehouse) return;

            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            await border.ScaleTo(0.97, 60, Easing.CubicOut);
            await border.ScaleTo(1.0,  60, Easing.CubicIn);

            await Navigation.PushAsync(new GudangDetailPage(warehouse));
        }

        // ── Mode toggle ────────────────────────────────────────────────
        private void OnSwitchMode(object sender, TappedEventArgs e)
        {
            if (e.Parameter is not string mode || mode == _viewMode) return;
            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
            _viewMode = mode;

            bool isGudang = _viewMode == "gudang";
            ModeGudangBtn.BackgroundColor  = isGudang ? Color.FromArgb("#1D4ED8") : Colors.Transparent;
            ModeCombineBtn.BackgroundColor = isGudang ? Colors.Transparent : Color.FromArgb("#1D4ED8");
            var res = Application.Current?.Resources;
            var muted = res != null && res.TryGetValue("TextMuted", out var tm) ? (Color)tm : Colors.Gray;
            LblModeGudang.TextColor  = isGudang ? Colors.White : muted;
            LblModeCombine.TextColor = isGudang ? muted : Colors.White;

            WarehouseList.IsVisible = isGudang;
            CombineView.IsVisible   = !isGudang;

            if (!isGudang)
            {
                BuildMaterialList();
                if (!string.IsNullOrEmpty(_selectedMaterial)) RebuildCombineRows();
            }
        }

        // ── Combine: material picker ───────────────────────────────────
        private void BuildMaterialList()
        {
            _materials = _warehouses
                .SelectMany(w => w.Items.Select(i => (i.NamaBarang ?? "").Trim()))
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToList();

            MaterialPicker.ItemsSource = _materials;
            if (!string.IsNullOrEmpty(_selectedMaterial) &&
                _materials.Contains(_selectedMaterial, StringComparer.OrdinalIgnoreCase))
            {
                MaterialPicker.SelectedItem = _selectedMaterial;
            }
        }

        private void OnMaterialPicked(object? sender, EventArgs e)
        {
            _selectedMaterial = MaterialPicker.SelectedItem as string ?? "";
            _selectedKeys.Clear();
            BtnResetCombine.IsVisible = !string.IsNullOrEmpty(_selectedMaterial);
            RebuildCombineRows();
        }

        private void OnResetCombine(object sender, TappedEventArgs e)
        {
            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
            _selectedMaterial = "";
            MaterialPicker.SelectedIndex = -1;
            _selectedKeys.Clear();
            BtnResetCombine.IsVisible = false;
            RebuildCombineRows();
        }

        private void OnCombineRowTapped(object sender, TappedEventArgs e)
        {
            if (sender is not Border border) return;
            if (border.BindingContext is not CombineRow row) return;
            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
            var key = row.Key;
            if (_selectedKeys.Contains(key)) _selectedKeys.Remove(key);
            else                              _selectedKeys.Add(key);
            RebuildCombineRows();
        }

        private void RebuildCombineRows()
        {
            if (string.IsNullOrEmpty(_selectedMaterial))
            {
                CombineList.ItemsSource = Array.Empty<CombineRow>();
                CombinedCard.IsVisible  = false;
                CombineHeader.IsVisible = false;
                LblCombineEmpty.Text    = "Pilih item dulu untuk melihat gudang yang punya stok";
                return;
            }

            var unit = MaterialUnit.Get(_selectedMaterial);
            var rows = new List<CombineRow>();
            foreach (var w in _warehouses)
            {
                var item = w.Items.FirstOrDefault(i =>
                    string.Equals((i.NamaBarang ?? "").Trim(), _selectedMaterial,
                                   StringComparison.OrdinalIgnoreCase));
                if (item == null) continue;
                rows.Add(new CombineRow
                {
                    WarehouseName = w.Name,
                    SegmentName   = w.SegmentName,
                    Item          = item,
                    Unit          = unit,
                    IsSelected    = _selectedKeys.Contains($"{w.Name}|{(item.NamaBarang ?? "").Trim()}"),
                });
            }

            // Urutkan: terpilih dulu, lalu sisa terbanyak
            rows = rows
                .OrderByDescending(r => r.IsSelected)
                .ThenByDescending(r => r.Item.SisaReal)
                .ThenBy(r => r.WarehouseName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            CombineList.ItemsSource = rows;
            CombineHeader.IsVisible = true;
            LblWarehouseCount.Text  = $"{rows.Count} gudang";
            LblCombineEmpty.Text    = $"Tidak ada gudang yang punya '{_selectedMaterial}'";

            // Recompute summary
            var sel = rows.Where(r => r.IsSelected).ToList();
            if (sel.Count == 0)
            {
                CombinedCard.IsVisible = false;
                return;
            }
            int sumDit = sel.Sum(r => r.Item.Diterima);
            int sumKel = sel.Sum(r => r.Item.Keluar);
            int sumImp = sel.Sum(r => r.Item.Implementasi);
            int sumSis = sel.Sum(r => r.Item.SisaReal);
            LblCombineCount.Text = $"{sel.Count} gudang";
            LblSumDiterima.Text  = $"{sumDit:N0} {unit}";
            LblSumKeluar.Text    = $"{sumKel:N0} {unit}";
            LblSumImp.Text       = $"{sumImp:N0} {unit}";
            LblSumSisa.Text      = $"{sumSis:N0} {unit}";
            CombinedCard.IsVisible = true;
        }

        // ── Helper view-model for Combine row (theme-aware) ────────────
        private class CombineRow
        {
            public string WarehouseName { get; set; } = "";
            public string SegmentName   { get; set; } = "";
            public GudangWarehouseItem Item { get; set; } = new();
            public string Unit { get; set; } = "pcs";
            public bool   IsSelected { get; set; }

            public string Key => $"{WarehouseName}|{(Item.NamaBarang ?? "").Trim()}";

            public string DiterimaText => $"D {Item.Diterima:N0}";
            public string KeluarText   => $"K {Item.Keluar:N0}";
            public string ImpText      => $"I {Item.Implementasi:N0}";
            public string SisaText     => $"{Item.SisaReal:N0} {Unit}";

            // Warna ikut tema (App.Theme.IsDark) supaya kebaca di mode terang.
            private static bool Dark => App.Theme.IsDark;

            public string SisaColor => Item.SisaReal > 0
                ? (Dark ? "#22C55E" : "#15803D")
                : Item.SisaReal == 0
                    ? (Dark ? "#94A3B8" : "#475569")
                    : (Dark ? "#F87171" : "#B91C1C");
            public string SisaBg => Item.SisaReal > 0
                ? (Dark ? "#064E3B" : "#DCFCE7")
                : Item.SisaReal == 0
                    ? (Dark ? "#334155" : "#E2E8F0")
                    : (Dark ? "#450A0A" : "#FEE2E2");

            public string CheckMark   => IsSelected ? "✓" : "";
            public string CheckBg     => IsSelected ? "#1D4ED8" : "Transparent";
            public string CheckStroke => IsSelected
                ? "#3B82F6"
                : (Dark ? "#475569" : "#94A3B8");

            public string RowBg => IsSelected
                ? (Dark ? "#1E3A6E" : "#DBEAFE")
                : (Dark ? "#1E293B" : "#FFFFFF");
            public string StrokeClr => IsSelected
                ? "#3B82F6"
                : (Dark ? "#334155" : "#CBD5E1");
        }
    }
}
