using StokBarangMAUI.Models;
using StokBarangMAUI.Services;

namespace StokBarangMAUI.Pages
{
    public partial class SuratJalanInputPage : ContentPage
    {
        private readonly GoogleOAuthService   _gauth;
        private readonly DraftService         _drafts;
        private readonly ProjectConfig        _project;
        private readonly GoogleSheetsService  _sheets;

        private string _currentDraftId  = Guid.NewGuid().ToString();
        private string _currentPhotoPath = "";

        private List<string> _materials = new();
        private static readonly string[] DefaultMaterials =
            { "Kabel 24C", "Tiang 7m", "Tiang 9m", "Terminasi" };

        // Image transform state
        private double _imgScale = 1.0;
        private double _imgX = 0, _imgY = 0;
        private double _imgRot = 0;
        private const double MinScale = 1.0, MaxScale = 6.0, ZoomStep = 0.4;
        private const double PanStepBase = 30.0;
        private const double PanSensitivity = 7.0;  // step per tap = base * sensitivity = 210

        // Tiap baris item: container + Picker nama + qty Entry + jenis Picker
        private readonly List<(Border Row, Picker Nm, Entry Qty, Picker Jn)> _itemRows = new();

        public SuratJalanInputPage(GoogleOAuthService gauth, DraftService drafts,
                                   ProjectConfig project, GoogleSheetsService sheets)
        {
            InitializeComponent();
            _gauth   = gauth;
            _drafts  = drafts;
            _project = project;
            _sheets  = sheets;
            DpTanggal.Date = DateTime.Today;



            PkSegment.Items.Clear();
            foreach (var (k, v) in _project.SegmentNames.OrderBy(x => int.TryParse(x.Key, out var n) ? n : 99))
                PkSegment.Items.Add($"{k}. {v}");

            AddItemRow();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await RefreshDraftsAsync();
            _ = LoadMaterialsAsync();
        }

        private async Task LoadMaterialsAsync()
        {
            try
            {
                var master = await _sheets.FetchMasterBarangAsync();
                if (master.Count > 0)
                {
                    _materials = master.OrderBy(x => x).ToList();
                }
                else
                {
                    var data    = await _sheets.FetchAsync(false);
                    var fromDP  = data.Progress.Select(p => p.NamaBarang)
                                               .Concat(data.SuratJalan.Select(s => s.NamaBarang))
                                               .Where(s => !string.IsNullOrWhiteSpace(s))
                                               .Distinct(StringComparer.OrdinalIgnoreCase);
                    _materials  = DefaultMaterials.Concat(fromDP)
                                                  .Distinct(StringComparer.OrdinalIgnoreCase)
                                                  .OrderBy(x => x)
                                                  .ToList();
                }
                foreach (var r in _itemRows) RepopulatePicker(r.Nm);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[SJInput] LoadMaterials: {ex.Message}"); }
        }

        private void RepopulatePicker(Picker pk)
        {
            var current = pk.SelectedItem?.ToString();
            pk.Items.Clear();
            foreach (var m in _materials) pk.Items.Add(m);
            if (!string.IsNullOrEmpty(current) && _materials.Contains(current))
                pk.SelectedItem = current;
        }

        // ── Photo handling ──────────────────────────────────────────────
        private async void OnTakePhoto(object sender, TappedEventArgs e)
        {
            try
            {
                if (!MediaPicker.Default.IsCaptureSupported)
                { await DisplayAlert("Kamera", "Perangkat tidak mendukung kamera.", "OK"); return; }
                var photo = await MediaPicker.Default.CapturePhotoAsync();
                if (photo != null) await SetPhotoAsync(photo);
            }
            catch (Exception ex) { await DisplayAlert("Gagal", ex.Message, "OK"); }
        }

        private async void OnPickPhoto(object sender, TappedEventArgs e)
        {
            try
            {
                var photo = await MediaPicker.Default.PickPhotoAsync();
                if (photo != null) await SetPhotoAsync(photo);
            }
            catch (Exception ex) { await DisplayAlert("Gagal", ex.Message, "OK"); }
        }

        private async Task SetPhotoAsync(FileResult photo)
        {
            _currentPhotoPath        = await _drafts.SavePhotoAsync(photo, _currentDraftId);
            ImgFoto.Source           = ImageSource.FromFile(_currentPhotoPath);
            ImgFoto.IsVisible        = true;
            LblFotoEmpty.IsVisible   = false;
            BtnRemovePhoto.IsVisible = true;
            ImgControls.IsVisible    = true;
            PanControls.IsVisible    = true;
            LblZoomBadge.IsVisible   = true;
            ResetImageTransform();
        }

        private void OnRemovePhoto(object sender, TappedEventArgs e)
        {
            try { if (File.Exists(_currentPhotoPath)) File.Delete(_currentPhotoPath); } catch { }
            _currentPhotoPath        = "";
            ImgFoto.Source           = null;
            ImgFoto.IsVisible        = false;
            LblFotoEmpty.IsVisible   = true;
            BtnRemovePhoto.IsVisible = false;
            ImgControls.IsVisible    = false;
            PanControls.IsVisible    = false;
            LblZoomBadge.IsVisible   = false;
        }

        // ── Pan via arrow buttons ───────────────────────────────────────
        private double PanStep => PanStepBase * PanSensitivity; // 30 * 7 = 210 per tap

        private void OnPanUp   (object sender, TappedEventArgs e) { Tap(); _imgY += PanStep; ApplyImageTransform(); }
        private void OnPanDown (object sender, TappedEventArgs e) { Tap(); _imgY -= PanStep; ApplyImageTransform(); }
        private void OnPanLeft (object sender, TappedEventArgs e) { Tap(); _imgX += PanStep; ApplyImageTransform(); }
        private void OnPanRight(object sender, TappedEventArgs e) { Tap(); _imgX -= PanStep; ApplyImageTransform(); }
        private static void Tap() { try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { } }

        private void OnZoomIn(object sender, TappedEventArgs e)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            _imgScale = Math.Min(_imgScale + ZoomStep, MaxScale);
            ApplyImageTransform();
        }

        private void OnZoomOut(object sender, TappedEventArgs e)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            _imgScale = Math.Max(_imgScale - ZoomStep, MinScale);
            if (_imgScale <= 1.0) { _imgX = 0; _imgY = 0; }
            ApplyImageTransform();
        }

        private void OnRotate(object sender, TappedEventArgs e)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            _imgRot = (_imgRot + 90) % 360;
            ApplyImageTransform();
        }

        private void OnResetTransform(object sender, TappedEventArgs e)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            ResetImageTransform();
        }

        private void ResetImageTransform()
        {
            _imgScale = 1.0; _imgRot = 0; _imgX = 0; _imgY = 0;
            ApplyImageTransform();
        }

        private void ApplyImageTransform()
        {
            var w = ImgBox.Width  > 0 ? ImgBox.Width  : 320;
            var h = ImgBox.Height > 0 ? ImgBox.Height : 380;
            var maxX = (w * (_imgScale - 1)) / 2.0;
            var maxY = (h * (_imgScale - 1)) / 2.0;
            _imgX = Math.Clamp(_imgX, -maxX, maxX);
            _imgY = Math.Clamp(_imgY, -maxY, maxY);

            ImgFoto.Scale        = _imgScale;
            ImgFoto.TranslationX = _imgX;
            ImgFoto.TranslationY = _imgY;
            ImgFoto.Rotation     = _imgRot;
            LblZoom.Text         = $"{_imgScale:F1}x";
        }

        // ── Item rows ───────────────────────────────────────────────────
        private void OnAddItem(object sender, TappedEventArgs e)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            AddItemRow();
        }

        private void AddItemRow(string nm = "", int qty = 0, string jn = "Masuk")
        {
            var muted    = (Color)Application.Current!.Resources["TextMuted"];
            var primary  = (Color)Application.Current.Resources["TextPrimary"];
            var border   = (Color)Application.Current.Resources["BorderClr"];
            var card2    = (Color)Application.Current.Resources["CardBg2"];

            var nmPk = new Picker { Title = "Pilih barang", TextColor = primary,
                                    BackgroundColor = card2, FontSize = 12 };
            var src = _materials.Count > 0 ? _materials : DefaultMaterials.ToList();
            foreach (var m in src) nmPk.Items.Add(m);
            if (!string.IsNullOrEmpty(nm))
            {
                if (!nmPk.Items.Contains(nm)) nmPk.Items.Add(nm);
                nmPk.SelectedItem = nm;
            }
            var qtyEnt = new Entry { Placeholder = "Qty", Text = qty > 0 ? qty.ToString() : "",
                                     Keyboard = Keyboard.Numeric,
                                     PlaceholderColor = muted, TextColor = primary,
                                     BackgroundColor = card2, FontSize = 12 };
            var jnPk   = new Picker { TextColor = primary, BackgroundColor = card2, FontSize = 11 };
            jnPk.Items.Add("Masuk"); jnPk.Items.Add("Keluar");
            jnPk.SelectedIndex = jn == "Keluar" ? 1 : 0;

            Border row = null!;
            var removeBtn = new Border
            {
                BackgroundColor = Color.FromArgb("#7F1D1D"),
                StrokeShape     = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
                Padding         = new Thickness(10, 5),
            };
            removeBtn.Content = new Label { Text = "✕", FontSize = 11, FontAttributes = FontAttributes.Bold,
                                            TextColor = Colors.White };
            removeBtn.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => RemoveItemRow(row)) });

            var grid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = new GridLength(70) },
                    new ColumnDefinition { Width = new GridLength(95) },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                ColumnSpacing = 6,
            };
            grid.Add(nmPk,     0, 0);
            grid.Add(qtyEnt,    1, 0);
            grid.Add(jnPk,      2, 0);
            grid.Add(removeBtn, 3, 0);

            row = new Border
            {
                BackgroundColor = card2,
                StrokeShape     = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
                Stroke          = new SolidColorBrush(border),
                StrokeThickness = 1,
                Padding         = new Thickness(8, 6),
                Content         = grid
            };

            _itemRows.Add((row, nmPk, qtyEnt, jnPk));
            ItemsPanel.Children.Add(row);
        }

        private void RemoveItemRow(Border row)
        {
            var entry = _itemRows.FirstOrDefault(x => x.Row == row);
            if (entry == default) return;
            _itemRows.Remove(entry);
            ItemsPanel.Children.Remove(row);
            if (_itemRows.Count == 0) AddItemRow(); // selalu sisakan minimal 1
        }

        // ── Save ────────────────────────────────────────────────────────
        private async void OnSave(object sender, TappedEventArgs e)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);

            var items = _itemRows
                .Select(r => new SuratJalanDraftItem
                {
                    NamaBarang = r.Nm.SelectedItem?.ToString() ?? "",
                    Qty        = int.TryParse(r.Qty.Text, out var q) ? q : 0,
                    Jenis      = r.Jn.SelectedItem?.ToString() ?? "Masuk"
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.NamaBarang) && x.Qty > 0)
                .ToList();

            if (items.Count == 0)
            { await DisplayAlert("Validasi", "Minimal 1 item dengan nama barang & qty > 0.", "OK"); return; }
            if (string.IsNullOrWhiteSpace(EntNoSJ.Text))
            { await DisplayAlert("Validasi", "No SJ wajib diisi.", "OK"); return; }

            var segText = PkSegment.SelectedItem?.ToString() ?? "";
            // Strip prefix "1. " jika ada → ambil RUTE saja
            var dotIdx = segText.IndexOf('.');
            var seg    = dotIdx > 0 && dotIdx < 4 ? segText[(dotIdx + 1)..].Trim() : segText;

            var draft = new SuratJalanDraft
            {
                Id         = _currentDraftId,
                ProjectId  = _project.Id,
                CreatedBy  = _gauth.AccountEmail ?? "",
                Tanggal    = DpTanggal.Date,
                NoSJ       = EntNoSJ.Text.Trim(),
                Pengirim   = EntPengirim.Text?.Trim() ?? "",
                Penerima   = EntPenerima.Text?.Trim() ?? "",
                Segment    = seg,
                Keterangan = EntKeterangan.Text?.Trim() ?? "",
                PhotoPath  = _currentPhotoPath,
                Items      = items
            };

            await _drafts.SaveSuratJalanAsync(_project.Id, draft);
            await DisplayAlert("Tersimpan", $"Draft SJ \"{draft.NoSJ}\" disimpan ({items.Count} item).", "OK");
            ResetForm();
            await RefreshDraftsAsync();
        }

        private void ResetForm()
        {
            _currentDraftId   = Guid.NewGuid().ToString();
            _currentPhotoPath = "";
            ImgFoto.Source           = null;
            ImgFoto.IsVisible        = false;
            LblFotoEmpty.IsVisible   = true;
            BtnRemovePhoto.IsVisible = false;
            ImgControls.IsVisible    = false;
            PanControls.IsVisible    = false;
            LblZoomBadge.IsVisible   = false;
            ResetImageTransform();
            DpTanggal.Date     = DateTime.Today;
            EntNoSJ.Text       = "";
            EntPengirim.Text   = "";
            EntPenerima.Text   = "";
            EntKeterangan.Text = "";
            ItemsPanel.Children.Clear();
            _itemRows.Clear();
            AddItemRow();
        }

        // ── Drafts list ─────────────────────────────────────────────────
        private async Task RefreshDraftsAsync()
        {
            var list = await _drafts.LoadSuratJalanAsync(_project.Id);
            DraftsPanel.Children.Clear();
            if (list.Count == 0)
            { LblDrafts.Text = "Belum ada draft tersimpan"; return; }
            LblDrafts.Text = $"📋 Draft tersimpan ({list.Count})";

            var muted    = (Color)Application.Current!.Resources["TextMuted"];
            var primary  = (Color)Application.Current.Resources["TextPrimary"];
            var border   = (Color)Application.Current.Resources["BorderClr"];
            var card     = (Color)Application.Current.Resources["CardBg"];

            foreach (var d in list.OrderByDescending(x => x.SavedAt))
            {
                var info = new VerticalStackLayout { Spacing = 2 };
                info.Children.Add(new Label { Text = $"📦 {d.NoSJ} · {d.Items.Count} item",
                                              FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = primary });
                info.Children.Add(new Label { Text = $"{d.Tanggal:d MMM yyyy} · {d.Segment}",
                                              FontSize = 10, TextColor = muted });
                if (!string.IsNullOrEmpty(d.PhotoPath) && File.Exists(d.PhotoPath))
                    info.Children.Add(new Label { Text = "📷 dengan foto", FontSize = 9, TextColor = Color.FromArgb("#3B82F6") });

                var del = new Border
                {
                    BackgroundColor = Color.FromArgb("#7F1D1D"),
                    StrokeShape     = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
                    Padding         = new Thickness(10, 6),
                };
                del.Content = new Label { Text = "Hapus", FontSize = 10, FontAttributes = FontAttributes.Bold,
                                          TextColor = Colors.White };
                var idCap = d.Id;
                del.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () =>
                {
                    bool ok = await DisplayAlert("Hapus", "Hapus draft ini?", "Ya", "Batal");
                    if (!ok) return;
                    await _drafts.DeleteSuratJalanAsync(_project.Id, idCap);
                    await RefreshDraftsAsync();
                })});

                var grid = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = GridLength.Star },
                        new ColumnDefinition { Width = GridLength.Auto }
                    },
                    ColumnSpacing = 8
                };
                grid.Add(info, 0, 0);
                grid.Add(del,  1, 0);

                var row = new Border
                {
                    BackgroundColor = card,
                    StrokeShape     = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
                    Stroke          = new SolidColorBrush(border),
                    StrokeThickness = 1,
                    Padding         = new Thickness(12, 10),
                    Content         = grid
                };
                DraftsPanel.Children.Add(row);
            }
        }
    }
}
