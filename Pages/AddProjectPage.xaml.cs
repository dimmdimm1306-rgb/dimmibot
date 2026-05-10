using StokBarangMAUI.Models;
using StokBarangMAUI.Services;

namespace StokBarangMAUI.Pages
{
    public partial class AddProjectPage : ContentPage
    {
        private readonly ProjectService _service;
        private readonly ProjectConfig  _editing;
        private string _selectedColor = "#1D4ED8";

        private readonly List<(Entry NameEnt, Entry GidEnt, Border Block)> _segEntries = new();

        private static readonly (string Hex, string Label)[] ColorPresets =
        {
            ("#1D4ED8","Biru"),("#0E7490","Cyan"),("#166534","Hijau"),
            ("#9A3412","Oranye"),("#6D28D9","Ungu"),("#9D174D","Pink"),
            ("#374151","Abu"),("#0F172A","Gelap"),
        };

        public AddProjectPage(ProjectService service, ProjectConfig? existing)
        {
            InitializeComponent();
            _service = service;
            _editing = existing ?? new ProjectConfig();
            BuildColorPresets();
            FillForm(_editing);
        }

        private void BuildColorPresets()
        {
            foreach (var (hex, label) in ColorPresets)
            {
                var chip = new Border
                {
                    BackgroundColor = Color.FromArgb(hex),
                    StrokeShape     = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
                    Stroke          = new SolidColorBrush(Colors.Transparent),
                    WidthRequest    = 36,
                    HeightRequest   = 36
                };
                var captured = hex;
                chip.GestureRecognizers.Add(new TapGestureRecognizer
                {
                    Command = new Command(() =>
                    {
                        _selectedColor = captured;
                        EntColor.Text  = captured;
                    })
                });
                ColorPresetsPanel.Children.Add(chip);
            }
        }

        private void FillForm(ProjectConfig p)
        {
            EntName.Text          = (p.Name == "Project Baru" || p.Name == "Project") ? "" : p.Name;
            EntDesc.Text          = p.Description;
            EntIcon.Text          = p.Icon        == "📁" ? "" : p.Icon;
            EntColor.Text         = p.Color;
            _selectedColor        = p.Color;
            EntSpreadsheetId.Text = p.SpreadsheetId;
            EntGidSJ.Text         = p.GidSuratJalan;
            EntGidProg.Text       = p.GidProgress;
            EntGidStok.Text        = p.GidStok;
            EntGidAktualStok.Text  = p.GidAktualStok;
            EntResumeId.Text       = p.ResumeSpreadsheetId;
            EntGidResume.Text      = p.GidResume;
            EntGidConfig.Text       = p.GidConfig;
            EntGidMasterBarang.Text = p.GidMasterBarang;
            EntSheetNameSJ.Text     = p.SheetNameSuratJalan;
            EntSheetNameProg.Text   = p.SheetNameProgress;
            EntDriveFolderId.Text   = p.DriveFolderIdSuratJalan;

            // Load config sync ref
            _ = LoadConfigSyncRefAsync();

            // Build segment blocks dynamically
            SegmentsPanel.Children.Clear();
            _segEntries.Clear();

            var maxNo = 0;
            foreach (var k in p.SegmentGids.Keys.Concat(p.SegmentNames.Keys))
                if (int.TryParse(k, out var n) && n > maxNo) maxNo = n;

            if (maxNo == 0) maxNo = 6; // default 6 segments for new project

            for (int i = 1; i <= maxNo; i++)
            {
                p.SegmentNames.TryGetValue(i.ToString(), out var name);
                p.SegmentGids.TryGetValue(i.ToString(), out var gid);
                AddSegmentBlock(name ?? "", gid ?? "");
            }
        }

        private async Task LoadConfigSyncRefAsync()
        {
            var r = await _service.GetRemoteConfigRefAsync();
            if (r == null) return;
            EntConfigSyncUrl.Text = r.Url;
        }

        private void AddSegmentBlock(string name = "", string gid = "")
        {
            int idx = _segEntries.Count + 1;

            var nameEnt = new Entry
            {
                Placeholder      = $"contoh: KOTA A - KOTA B",
                Text             = name,
                BackgroundColor  = Color.FromArgb("#0F172A"),
                TextColor        = (Color)Application.Current!.Resources["TextPrimary"],
                PlaceholderColor = (Color)Application.Current!.Resources["TextMuted"],
            };
            var gidEnt = new Entry
            {
                Placeholder      = "GID sheet...",
                Text             = gid,
                Keyboard         = Keyboard.Numeric,
                BackgroundColor  = Color.FromArgb("#0F172A"),
                TextColor        = (Color)Application.Current!.Resources["TextPrimary"],
                PlaceholderColor = (Color)Application.Current!.Resources["TextMuted"],
            };

            Border block = null!;

            var removeBtn = new Border
            {
                BackgroundColor = Color.FromArgb("#450A0A"),
                StrokeShape     = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
                Stroke          = new SolidColorBrush(Color.FromArgb("#DC2626")),
                StrokeThickness = 1,
                Padding         = new Thickness(10, 5),
            };
            removeBtn.Content = new Label
            {
                Text           = "✕",
                FontSize       = 12,
                FontAttributes = FontAttributes.Bold,
                TextColor      = Color.FromArgb("#FCA5A5"),
            };
            removeBtn.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(() => RemoveSegment(block))
            });

            block = new Border
            {
                BackgroundColor = (Color)Application.Current!.Resources["CardBg2"],
                StrokeShape     = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
                Stroke          = new SolidColorBrush((Color)Application.Current!.Resources["BorderClr"]),
                StrokeThickness = 1,
                Padding         = new Thickness(12, 10),
            };

            var segLabel = new Label
            {
                Text           = $"Segment {idx}",
                FontSize       = 11,
                FontAttributes = FontAttributes.Bold,
                TextColor      = Color.FromArgb("#F97316"),
            };

            block.Content = new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    new Grid
                    {
                        ColumnDefinitions =
                        {
                            new ColumnDefinition { Width = GridLength.Star },
                            new ColumnDefinition { Width = GridLength.Auto }
                        },
                        Children = { segLabel, removeBtn }
                    }.WithColumn(segLabel, 0).WithColumn(removeBtn, 1),
                    new VerticalStackLayout
                    {
                        Spacing = 3,
                        Children =
                        {
                            new Label { Text = "Nama RUTE", FontSize = 10, TextColor = (Color)Application.Current!.Resources["TextMuted"] },
                            nameEnt,
                        }
                    },
                    new VerticalStackLayout
                    {
                        Spacing = 3,
                        Children =
                        {
                            new Label { Text = "GID Sheet", FontSize = 10, TextColor = (Color)Application.Current!.Resources["TextMuted"] },
                            gidEnt,
                        }
                    },
                }
            };

            _segEntries.Add((nameEnt, gidEnt, block));
            SegmentsPanel.Children.Add(block);
        }

        private void RemoveSegment(Border block)
        {
            var entry = _segEntries.FirstOrDefault(e => e.Block == block);
            if (entry == default) return;
            _segEntries.Remove(entry);
            SegmentsPanel.Children.Remove(block);
        }

        private void OnAddSegment(object sender, TappedEventArgs e)
            => AddSegmentBlock();

        private async void OnExportJson(object sender, TappedEventArgs e)
        {
            // Build config dari form saat ini (tanpa save dulu)
            var tmp = new ProjectConfig
            {
                Id                   = _editing.Id,
                Name                 = EntName.Text?.Trim() ?? _editing.Name,
                Description          = EntDesc.Text?.Trim() ?? "",
                Icon                 = string.IsNullOrWhiteSpace(EntIcon.Text) ? "📁" : EntIcon.Text.Trim(),
                Color                = EntColor.Text?.Trim() ?? _selectedColor,
                SpreadsheetId        = EntSpreadsheetId.Text?.Trim() ?? "",
                GidSuratJalan        = EntGidSJ.Text?.Trim() ?? "",
                GidProgress          = EntGidProg.Text?.Trim() ?? "",
                GidStok              = EntGidStok.Text?.Trim() ?? "",
                GidAktualStok        = EntGidAktualStok.Text?.Trim() ?? "",
                ResumeSpreadsheetId  = EntResumeId.Text?.Trim() ?? "",
                GidResume            = EntGidResume.Text?.Trim() ?? "",
                GidConfig            = EntGidConfig.Text?.Trim() ?? "",
                GidMasterBarang      = EntGidMasterBarang.Text?.Trim() ?? "",
                SheetNameSuratJalan  = EntSheetNameSJ.Text?.Trim() is { Length: > 0 } sj ? sj : "Surat Jalan",
                SheetNameProgress    = EntSheetNameProg.Text?.Trim() is { Length: > 0 } sp ? sp : "Progress",
                DriveFolderIdSuratJalan = EntDriveFolderId.Text?.Trim() ?? "",
            };
            for (int i = 0; i < _segEntries.Count; i++)
            {
                var key = (i + 1).ToString();
                tmp.SegmentGids[key]  = _segEntries[i].GidEnt.Text?.Trim() ?? "";
                tmp.SegmentNames[key] = _segEntries[i].NameEnt.Text?.Trim() ?? "";
            }

            var json = System.Text.Json.JsonSerializer.Serialize(tmp,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

            await Share.Default.RequestAsync(new ShareTextRequest
            {
                Text  = json,
                Title = "Export Config Project"
            });
        }

        private async void OnSave(object sender, TappedEventArgs e)
        {
            var name     = EntName.Text?.Trim() ?? "";
            var id       = EntSpreadsheetId.Text?.Trim() ?? "";
            var resumeId = EntResumeId.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(name))
            { await DisplayAlert("Validasi", "Nama project wajib diisi.", "OK"); return; }
            if (string.IsNullOrWhiteSpace(id))
            { await DisplayAlert("Validasi", "Spreadsheet ID wajib diisi.", "OK"); return; }
            if (string.IsNullOrWhiteSpace(resumeId))
            { await DisplayAlert("Validasi", "Resume Spreadsheet ID wajib diisi.", "OK"); return; }

            var color = EntColor.Text?.Trim();
            if (string.IsNullOrWhiteSpace(color) || !color.StartsWith('#'))
                color = _selectedColor;

            _editing.Name                = name;
            _editing.Description         = EntDesc.Text?.Trim() ?? "";
            _editing.Icon                = string.IsNullOrWhiteSpace(EntIcon.Text) ? "📁" : EntIcon.Text.Trim();
            _editing.Color               = color;
            _editing.SpreadsheetId       = id;
            _editing.GidSuratJalan       = EntGidSJ.Text?.Trim()   ?? "";
            _editing.GidProgress         = EntGidProg.Text?.Trim()  ?? "";
            _editing.GidStok             = EntGidStok.Text?.Trim()       ?? "";
            _editing.GidAktualStok       = EntGidAktualStok.Text?.Trim() ?? "";
            _editing.ResumeSpreadsheetId = resumeId;
            _editing.GidResume           = EntGidResume.Text?.Trim()  ?? "";
            _editing.GidConfig                = EntGidConfig.Text?.Trim()       ?? "";
            _editing.GidMasterBarang          = EntGidMasterBarang.Text?.Trim() ?? "";
            var sjName  = EntSheetNameSJ.Text?.Trim();
            var progName= EntSheetNameProg.Text?.Trim();
            _editing.SheetNameSuratJalan      = string.IsNullOrWhiteSpace(sjName)   ? "Surat Jalan" : sjName;
            _editing.SheetNameProgress        = string.IsNullOrWhiteSpace(progName) ? "Progress"    : progName;
            _editing.DriveFolderIdSuratJalan  = EntDriveFolderId.Text?.Trim() ?? "";

            _editing.SegmentGids  = new Dictionary<string, string>();
            _editing.SegmentNames = new Dictionary<string, string>();
            for (int i = 0; i < _segEntries.Count; i++)
            {
                var key = (i + 1).ToString();
                _editing.SegmentGids[key]  = _segEntries[i].GidEnt.Text?.Trim()  ?? "";
                _editing.SegmentNames[key] = _segEntries[i].NameEnt.Text?.Trim() ?? "";
            }

            await _service.SaveAsync(_editing);

            // Simpan config sync URL jika diisi
            var syncUrl = EntConfigSyncUrl.Text?.Trim() ?? "";
            if (!string.IsNullOrWhiteSpace(syncUrl))
                await _service.SaveRemoteConfigRefAsync(syncUrl);

            await Navigation.PopAsync();
        }
    }

    // Helper extension to set Grid.Column without XAML
    internal static class GridExt
    {
        public static Grid WithColumn(this Grid g, View v, int col)
        { Grid.SetColumn(v, col); return g; }
    }
}
