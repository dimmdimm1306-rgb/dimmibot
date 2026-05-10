using StokBarangMAUI.Models;
using StokBarangMAUI.Services;

namespace StokBarangMAUI.Pages
{
    public partial class ProgressInputPage : ContentPage
    {
        private readonly GoogleOAuthService   _gauth;
        private readonly DraftService         _drafts;
        private readonly ProjectConfig        _project;
        private readonly GoogleSheetsService  _sheets;

        // Cached dropdown sources
        private List<SpanItem> _spans      = new();
        private List<string>   _materials  = new();

        // Default fallback materials (canonical) — selalu ada minimal ini
        private static readonly string[] DefaultMaterials =
            { "Kabel 24C", "Tiang 7m", "Tiang 9m", "Terminasi" };
        private static readonly string[] StatusOptions =
            { "done", "proses", "kurang" };

        // Map index Picker → segment number
        private readonly List<int> _segmentNos = new();

        private readonly List<(Border Row, Picker Nm, Entry Pg, Picker St)> _itemRows = new();

        public ProgressInputPage(GoogleOAuthService gauth, DraftService drafts,
                                 ProjectConfig project, GoogleSheetsService sheets)
        {
            InitializeComponent();
            _gauth   = gauth;
            _drafts  = drafts;
            _project = project;
            _sheets  = sheets;
            DpTanggal.Date = DateTime.Today;

            BuildSegmentPicker();
            AddItemRow();
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

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await RefreshDraftsAsync();
            _ = LoadDropdownsAsync();
        }

        private async Task LoadDropdownsAsync()
        {
            try
            {
                LoaderDD.IsVisible = true;
                // 1. Material list (master sheet → fallback ke distinct dari Progress data)
                var master = await _sheets.FetchMasterBarangAsync();
                if (master.Count > 0)
                {
                    _materials = master.OrderBy(x => x).ToList();
                }
                else
                {
                    var data    = await _sheets.FetchAsync(false);
                    var fromDP  = data.Progress.Select(p => p.NamaBarang)
                                               .Where(s => !string.IsNullOrWhiteSpace(s))
                                               .Distinct(StringComparer.OrdinalIgnoreCase);
                    _materials  = DefaultMaterials.Concat(fromDP)
                                                  .Distinct(StringComparer.OrdinalIgnoreCase)
                                                  .OrderBy(x => x)
                                                  .ToList();
                }
                // Refresh existing item rows agar Picker mereka punya item terbaru
                foreach (var r in _itemRows) RepopulatePicker(r.Nm);

                // 2. Span list untuk segment yang sedang dipilih
                if (PkSegment.SelectedIndex >= 0) await ReloadSpansAsync();
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[ProgressInput] LoadDropdowns: {ex.Message}"); }
            finally { LoaderDD.IsVisible = false; }
        }

        private void RepopulatePicker(Picker pk)
        {
            var current = pk.SelectedItem?.ToString();
            pk.Items.Clear();
            foreach (var m in _materials) pk.Items.Add(m);
            if (!string.IsNullOrEmpty(current) && _materials.Contains(current))
                pk.SelectedItem = current;
        }

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
            try
            {
                LoaderDD.IsVisible = true;
                _spans = await _sheets.FetchSpanAsync(segNo, false);
            }
            catch { _spans = new(); }
            finally { LoaderDD.IsVisible = false; }
        }

        // ── Span typeahead ──────────────────────────────────────────────
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
                .Take(10)
                .ToList();
            if (matches.Count == 0) { SpanSuggestionsCard.IsVisible = false; return; }

            var muted   = (Color)Application.Current!.Resources["TextMuted"];
            var primary = (Color)Application.Current.Resources["TextPrimary"];

            foreach (var s in matches)
            {
                var rowGrid = new Grid
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
                rowGrid.Add(info, 0, 0);
                rowGrid.Add(new Label { Text = "›", FontSize = 16, TextColor = Color.FromArgb("#3B82F6"),
                                        VerticalOptions = LayoutOptions.Center }, 1, 0);

                var border = new Border
                {
                    BackgroundColor = Color.FromArgb("#0F172A40"),
                    StrokeShape     = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
                    Stroke          = (Brush)Brush.Transparent,
                    Content         = rowGrid,
                };
                var captured = s;
                border.GestureRecognizers.Add(new TapGestureRecognizer
                {
                    Command = new Command(() =>
                    {
                        EntSpan.TextChanged -= OnSpanTextChanged;
                        EntSpan.Text = captured.Rute;
                        EntSpan.TextChanged += OnSpanTextChanged;
                        if (string.IsNullOrWhiteSpace(EntKabupaten.Text))
                            EntKabupaten.Text = captured.Kab;
                        SpanSuggestionsCard.IsVisible = false;
                        SpanSuggestionsPanel.Children.Clear();
                    })
                });
                SpanSuggestionsPanel.Children.Add(border);
            }
            SpanSuggestionsCard.IsVisible = true;
        }

        // ── Item rows ───────────────────────────────────────────────────
        private void OnAddItem(object sender, TappedEventArgs e)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            AddItemRow();
        }

        private void AddItemRow(string nm = "", int pg = 0, string st = "done")
        {
            var muted    = (Color)Application.Current!.Resources["TextMuted"];
            var primary  = (Color)Application.Current.Resources["TextPrimary"];
            var border   = (Color)Application.Current.Resources["BorderClr"];
            var card2    = (Color)Application.Current.Resources["CardBg2"];

            var nmPk = new Picker { Title = "Material", TextColor = primary,
                                    BackgroundColor = card2, FontSize = 12 };
            var src = _materials.Count > 0 ? _materials : DefaultMaterials.ToList();
            foreach (var m in src) nmPk.Items.Add(m);
            if (!string.IsNullOrEmpty(nm))
            {
                if (!nmPk.Items.Contains(nm)) nmPk.Items.Add(nm);
                nmPk.SelectedItem = nm;
            }

            var pgEnt = new Entry { Placeholder = "Qty", Text = pg > 0 ? pg.ToString() : "",
                                    Keyboard = Keyboard.Numeric,
                                    PlaceholderColor = muted, TextColor = primary,
                                    BackgroundColor = card2, FontSize = 12 };

            var stPk  = new Picker { TextColor = primary, BackgroundColor = card2, FontSize = 11 };
            foreach (var s in StatusOptions) stPk.Items.Add(s);
            stPk.SelectedItem = StatusOptions.Contains(st) ? st : "done";

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
                    new ColumnDefinition { Width = new GridLength(85) },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                ColumnSpacing = 6,
            };
            grid.Add(nmPk,      0, 0);
            grid.Add(pgEnt,     1, 0);
            grid.Add(stPk,      2, 0);
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

            _itemRows.Add((row, nmPk, pgEnt, stPk));
            ItemsPanel.Children.Add(row);
        }

        private void RemoveItemRow(Border row)
        {
            var entry = _itemRows.FirstOrDefault(x => x.Row == row);
            if (entry == default) return;
            _itemRows.Remove(entry);
            ItemsPanel.Children.Remove(row);
            if (_itemRows.Count == 0) AddItemRow();
        }

        private async void OnSave(object sender, TappedEventArgs e)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);

            var items = _itemRows
                .Select(r => new ProgressDraftItem
                {
                    NamaBarang = r.Nm.SelectedItem?.ToString() ?? "",
                    Progres    = int.TryParse(r.Pg.Text, out var q) ? q : 0,
                    Keterangan = r.St.SelectedItem?.ToString() ?? "done",
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.NamaBarang) && x.Progres > 0)
                .ToList();

            if (items.Count == 0)
            { await DisplayAlert("Validasi", "Minimal 1 material dengan qty > 0.", "OK"); return; }
            if (string.IsNullOrWhiteSpace(EntSpan.Text))
            { await DisplayAlert("Validasi", "Span wajib diisi.", "OK"); return; }
            if (PkSegment.SelectedIndex < 0)
            { await DisplayAlert("Validasi", "Pilih segment dulu.", "OK"); return; }

            var segText = PkSegment.SelectedItem?.ToString() ?? "";
            var dotIdx = segText.IndexOf('.');
            var seg    = dotIdx > 0 && dotIdx < 4 ? segText[(dotIdx + 1)..].Trim() : segText;

            var draft = new ProgressDraft
            {
                ProjectId  = _project.Id,
                CreatedBy  = _gauth.AccountEmail ?? "",
                Tanggal    = DpTanggal.Date,
                Segment    = seg,
                Span       = EntSpan.Text.Trim(),
                Homebase   = EntHomebase.Text?.Trim().ToUpperInvariant() ?? "",
                Kabupaten  = EntKabupaten.Text?.Trim() ?? "",
                Items      = items
            };

            await _drafts.SaveProgressAsync(_project.Id, draft);
            await DisplayAlert("Tersimpan", $"Draft progress span \"{draft.Span}\" disimpan ({items.Count} material).", "OK");
            ResetForm();
            await RefreshDraftsAsync();
        }

        private void ResetForm()
        {
            DpTanggal.Date     = DateTime.Today;
            EntSpan.Text       = "";
            EntKabupaten.Text  = "";
            UpdateSpanSuggestions("");
            // Homebase & Segment biasanya sama untuk batch — biarkan
            ItemsPanel.Children.Clear();
            _itemRows.Clear();
            AddItemRow();
        }

        private async Task RefreshDraftsAsync()
        {
            var list = await _drafts.LoadProgressAsync(_project.Id);
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
                info.Children.Add(new Label { Text = $"📡 {d.Span}",
                                              FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = primary });
                info.Children.Add(new Label { Text = $"{d.Tanggal:d MMM yyyy} · {d.Items.Count} material · {d.Homebase}",
                                              FontSize = 10, TextColor = muted });
                var sumStr = string.Join(" · ", d.Items.Select(it => $"{it.NamaBarang} {it.Progres}"));
                info.Children.Add(new Label { Text = sumStr, FontSize = 9, TextColor = Color.FromArgb("#22C55E") });

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
                    await _drafts.DeleteProgressAsync(_project.Id, idCap);
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
