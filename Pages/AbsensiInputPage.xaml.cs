using System.Text.Json;
using StokBarangMAUI.Models;
using StokBarangMAUI.Services;

namespace StokBarangMAUI.Pages
{
    public partial class AbsensiInputPage : ContentPage
    {
        private readonly DraftService        _drafts;
        private readonly ProjectConfig       _project;
        private readonly GoogleOAuthService  _gauth;
        private readonly DriveUploadService  _drive;
        private readonly GoogleSheetsService _sheets;
        private readonly string              _folderId;

        private string _draftId = Guid.NewGuid().ToString();
        private readonly List<string> _photoPaths = new();

        // Cached span list for currently selected segment
        private List<SpanItem> _spans = new();
        // Map index Picker → segment number
        private readonly List<int> _segmentNos = new();

        private readonly List<(Border Row, Entry Ent)> _ketuaRows = new();

        public AbsensiInputPage(DraftService drafts, ProjectConfig project,
                                GoogleOAuthService gauth, DriveUploadService drive,
                                GoogleSheetsService sheets, string folderId)
        {
            InitializeComponent();
            _drafts   = drafts;
            _project  = project;
            _gauth    = gauth;
            _drive    = drive;
            _sheets   = sheets;
            _folderId = folderId;

            BuildSegmentPicker();
            AddKetuaRow();
            UpdateTimeLabel();
            UpdatePhotoCount();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            UpdateTimeLabel();
        }

        private void BuildSegmentPicker()
        {
            PkSegment.Items.Clear();
            _segmentNos.Clear();
            foreach (var (k, v) in _project.SegmentNames.OrderBy(x => int.TryParse(x.Key, out var n) ? n : 99))
            {
                if (!int.TryParse(k, out var no)) continue;
                _segmentNos.Add(no);
                PkSegment.Items.Add($"{k}. {v}");
            }
        }

        private void UpdateTimeLabel() =>
            LblTime.Text = $"⏱ Waktu otomatis: {DateTime.Now:dddd, d MMM yyyy · HH:mm}";

        // ── Photos ──────────────────────────────────────────────────────
        private async void OnTakePhoto(object sender, TappedEventArgs e)
        {
            try
            {
                if (!MediaPicker.Default.IsCaptureSupported)
                { await DisplayAlert("Kamera", "Perangkat tidak dukung kamera.", "OK"); return; }
                var photo = await MediaPicker.Default.CapturePhotoAsync();
                if (photo != null) await AddPhotoAsync(photo);
            }
            catch (Exception ex) { await DisplayAlert("Gagal", ex.Message, "OK"); }
        }

        private async void OnPickPhotos(object sender, TappedEventArgs e)
        {
            try
            {
                var results = await FilePicker.Default.PickMultipleAsync(new PickOptions
                {
                    PickerTitle = "Pilih foto kegiatan",
                    FileTypes   = FilePickerFileType.Images,
                });
                if (results == null) return;
                foreach (var f in results) await AddPhotoAsync(f);
            }
            catch (Exception ex) { await DisplayAlert("Gagal", ex.Message, "OK"); }
        }

        private async Task AddPhotoAsync(FileResult photo)
        {
            var path = await _drafts.SavePhotoAsync(photo, _draftId);
            _photoPaths.Add(path);
            RefreshPhotoStrip();
        }

        private void RefreshPhotoStrip()
        {
            PhotosPanel.Children.Clear();
            foreach (var path in _photoPaths)
            {
                var captured = path;
                var img = new Image
                {
                    Source        = ImageSource.FromFile(path),
                    Aspect        = Aspect.AspectFill,
                    WidthRequest  = 90,
                    HeightRequest = 90,
                };
                var del = new Border
                {
                    BackgroundColor   = Color.FromArgb("#000000B0"),
                    StrokeShape       = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
                    WidthRequest      = 24, HeightRequest = 24,
                    HorizontalOptions = LayoutOptions.End,
                    VerticalOptions   = LayoutOptions.Start,
                    Margin            = new Thickness(0, 4, 4, 0),
                };
                del.Content = new Label { Text = "✕", FontSize = 11, FontAttributes = FontAttributes.Bold,
                                          TextColor = Colors.White, HorizontalOptions = LayoutOptions.Center,
                                          VerticalOptions = LayoutOptions.Center };
                del.GestureRecognizers.Add(new TapGestureRecognizer
                {
                    Command = new Command(() =>
                    {
                        try { File.Delete(captured); } catch { }
                        _photoPaths.Remove(captured);
                        RefreshPhotoStrip();
                    })
                });

                var thumb = new Border
                {
                    StrokeShape     = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
                    Stroke          = new SolidColorBrush(Color.FromArgb("#334155")),
                    StrokeThickness = 1,
                    BackgroundColor = Color.FromArgb("#0B1220"),
                    Content         = new Grid { Children = { img, del } },
                };
                PhotosPanel.Children.Add(thumb);
            }
            UpdatePhotoCount();
        }

        private void UpdatePhotoCount() => LblPhotoCount.Text = $"{_photoPaths.Count} foto";

        // ── Segment & Span dropdown (cascading + typeahead) ─────────────
        private async void OnSegmentChanged(object sender, EventArgs e)
        {
            await ReloadSpansAsync();
            EntSpan.Text = "";
            UpdateSpanSuggestions("");
        }

        private async Task ReloadSpansAsync()
        {
            if (PkSegment.SelectedIndex < 0 || PkSegment.SelectedIndex >= _segmentNos.Count) return;
            var segNo = _segmentNos[PkSegment.SelectedIndex];
            try   { _spans = await _sheets.FetchSpanAsync(segNo, false); }
            catch { _spans = new(); }
        }

        private void OnSpanTextChanged(object sender, TextChangedEventArgs e)
            => UpdateSpanSuggestions(e.NewTextValue ?? "");

        private void UpdateSpanSuggestions(string filter)
        {
            SpanSuggestionsPanel.Children.Clear();
            filter = (filter ?? "").Trim();
            if (filter.Length == 0 || _spans.Count == 0)
            { SpanSuggestionsCard.IsVisible = false; return; }

            var matches = _spans
                .Where(s => s.Rute.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                            s.Kab.Contains(filter, StringComparison.OrdinalIgnoreCase))
                .Take(10).ToList();
            if (matches.Count == 0) { SpanSuggestionsCard.IsVisible = false; return; }

            var muted   = (Color)Application.Current!.Resources["TextMuted"];
            var primary = (Color)Application.Current.Resources["TextPrimary"];

            foreach (var s in matches)
            {
                var grid = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = GridLength.Star },
                        new ColumnDefinition { Width = GridLength.Auto }
                    },
                    Padding = new Thickness(10, 8),
                };
                var info = new VerticalStackLayout { Spacing = 1 };
                info.Children.Add(new Label { Text = s.Rute, FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = primary });
                info.Children.Add(new Label { Text = $"#{s.No} · {s.Kab}", FontSize = 9, TextColor = muted });
                grid.Add(info, 0, 0);
                grid.Add(new Label { Text = "›", FontSize = 16, TextColor = Color.FromArgb("#3B82F6"),
                                     VerticalOptions = LayoutOptions.Center }, 1, 0);

                var captured = s;
                var border = new Border
                {
                    BackgroundColor = Color.FromArgb("#0F172A40"),
                    StrokeShape     = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
                    Stroke          = (Brush)Brush.Transparent,
                    Content         = grid,
                };
                border.GestureRecognizers.Add(new TapGestureRecognizer
                {
                    Command = new Command(() =>
                    {
                        EntSpan.TextChanged -= OnSpanTextChanged;
                        EntSpan.Text = captured.Rute;
                        EntSpan.TextChanged += OnSpanTextChanged;
                        SpanSuggestionsCard.IsVisible = false;
                        SpanSuggestionsPanel.Children.Clear();
                    })
                });
                SpanSuggestionsPanel.Children.Add(border);
            }
            SpanSuggestionsCard.IsVisible = true;
        }

        // ── Ketua regu ──────────────────────────────────────────────────
        private void OnAddKetua(object sender, TappedEventArgs e)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            AddKetuaRow();
        }

        private void AddKetuaRow(string name = "")
        {
            var muted   = (Color)Application.Current!.Resources["TextMuted"];
            var primary = (Color)Application.Current.Resources["TextPrimary"];
            var border  = (Color)Application.Current.Resources["BorderClr"];
            var card2   = (Color)Application.Current.Resources["CardBg2"];

            var ent = new Entry
            {
                Placeholder      = "Nama ketua regu",
                Text             = name,
                PlaceholderColor = muted,
                TextColor        = primary,
                BackgroundColor  = card2,
                FontSize         = 12,
            };

            Border row = null!;
            var del = new Border
            {
                BackgroundColor = Color.FromArgb("#7F1D1D"),
                StrokeShape     = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
                Padding         = new Thickness(10, 5),
            };
            del.Content = new Label { Text = "✕", FontSize = 11, FontAttributes = FontAttributes.Bold, TextColor = Colors.White };
            del.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => RemoveKetuaRow(row)) });

            var grid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                ColumnSpacing = 6
            };
            grid.Add(ent, 0, 0);
            grid.Add(del, 1, 0);

            row = new Border
            {
                BackgroundColor = card2,
                StrokeShape     = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
                Stroke          = new SolidColorBrush(border),
                StrokeThickness = 1,
                Padding         = new Thickness(8, 6),
                Content         = grid
            };
            _ketuaRows.Add((row, ent));
            KetuaPanel.Children.Add(row);
        }

        private void RemoveKetuaRow(Border row)
        {
            var item = _ketuaRows.FirstOrDefault(x => x.Row == row);
            if (item == default) return;
            _ketuaRows.Remove(item);
            KetuaPanel.Children.Remove(row);
            if (_ketuaRows.Count == 0) AddKetuaRow();
        }

        // ── Post (upload semua foto ke Drive dengan groupId) ────────────
        private async void OnPost(object sender, TappedEventArgs e)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);

            if (_photoPaths.Count == 0)
            { await DisplayAlert("Validasi", "Wajib ada minimal 1 foto.", "OK"); return; }

            var ketuas = _ketuaRows.Select(r => r.Ent.Text?.Trim() ?? "")
                                   .Where(s => !string.IsNullOrWhiteSpace(s))
                                   .ToList();
            if (ketuas.Count == 0)
            { await DisplayAlert("Validasi", "Minimal 1 nama ketua regu.", "OK"); return; }
            if (PkSegment.SelectedIndex < 0)
            { await DisplayAlert("Validasi", "Pilih segment dulu.", "OK"); return; }
            if (string.IsNullOrWhiteSpace(EntSpan.Text))
            { await DisplayAlert("Validasi", "Span wajib diisi.", "OK"); return; }
            if (!_gauth.IsSignedIn)
            { await DisplayAlert("Login", "Sign in Google dulu di tab Input.", "OK"); return; }

            var segText = PkSegment.SelectedItem?.ToString() ?? "";
            var dotIdx  = segText.IndexOf('.');
            var seg     = dotIdx > 0 && dotIdx < 4 ? segText[(dotIdx + 1)..].Trim() : segText;

            var groupId = Guid.NewGuid().ToString("N");
            var savedAt = DateTime.Now;

            LblPostBtn.Text = $"⏳  Mengunggah 0/{_photoPaths.Count}...";
            int success = 0;
            try
            {
                for (int i = 0; i < _photoPaths.Count; i++)
                {
                    var meta = new
                    {
                        groupId,
                        index        = i,
                        totalInGroup = _photoPaths.Count,
                        caption      = EntCaption.Text?.Trim() ?? "",
                        segment      = seg,
                        span         = EntSpan.Text.Trim(),
                        pekerjaan    = EntPekerjaan.Text?.Trim() ?? "",
                        waspang      = EntWaspang.Text?.Trim() ?? "",
                        ketuaRegu    = ketuas,
                        savedAt      = savedAt.ToString("o"),
                        createdBy    = _gauth.AccountEmail ?? ""
                    };
                    var description = JsonSerializer.Serialize(meta);
                    var fileName    = $"KG_{savedAt:yyyyMMdd_HHmmss}_{i + 1:D2}.jpg";

                    var fileId = await _drive.UploadPhotoWithDescriptionAsync(
                        _photoPaths[i], fileName, description, _folderId);
                    if (!string.IsNullOrEmpty(fileId)) success++;
                    LblPostBtn.Text = $"⏳  Mengunggah {i + 1}/{_photoPaths.Count}...";
                }

                if (success == 0)
                {
                    await DisplayAlert("Gagal", "Tidak ada foto yang berhasil di-upload.", "OK");
                    return;
                }
                // Hapus foto local yang sudah berhasil di-upload
                foreach (var p in _photoPaths) { try { File.Delete(p); } catch { } }

                var msg = success == _photoPaths.Count
                    ? $"{success} foto berhasil di-post."
                    : $"{success}/{_photoPaths.Count} foto berhasil di-post.";
                await DisplayAlert("Sukses", msg, "OK");
                await Navigation.PopAsync();
            }
            finally { LblPostBtn.Text = "🚀  Post ke Drive"; }
        }
    }
}
