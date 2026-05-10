using System.Text.Json;
using StokBarangMAUI.Models;
using StokBarangMAUI.Services;

namespace StokBarangMAUI.Pages
{
    public partial class AbsensiFeedPage : ContentPage
    {
        private readonly DraftService        _drafts;
        private readonly ProjectConfig       _project;
        private readonly GoogleOAuthService  _gauth;
        private readonly DriveUploadService  _drive;
        private readonly GoogleSheetsService _sheets;

        private string FolderIdKey   => $"absensi.folder.{_project.Id}.id";
        private string FolderNameKey => $"absensi.folder.{_project.Id}.name";
        private string SelectedFolderId   => Preferences.Get(FolderIdKey, "");
        private string SelectedFolderName => Preferences.Get(FolderNameKey, "");

        public AbsensiFeedPage(DraftService drafts, ProjectConfig project,
                               GoogleOAuthService gauth, DriveUploadService drive,
                               GoogleSheetsService sheets)
        {
            InitializeComponent();
            _drafts  = drafts;
            _project = project;
            _gauth   = gauth;
            _drive   = drive;
            _sheets  = sheets;
            LblHeader.Text = "Feed Kegiatan";
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            UpdateFolderUi();
            await LoadFeedAsync();
        }

        private void UpdateFolderUi()
        {
            var n = SelectedFolderName;
            LblFolder.Text = string.IsNullOrWhiteSpace(n) ? "📁 Pilih folder dulu" : $"📁 {n}";
        }

        private async Task LoadFeedAsync()
        {
            FeedPanel.Children.Clear();
            if (!_gauth.IsSignedIn)
            { ShowEmpty("🔑 Sign in Google dulu di tab Input untuk lihat feed kegiatan."); return; }
            if (string.IsNullOrWhiteSpace(SelectedFolderId))
            { ShowEmpty("📁 Belum pilih folder Drive — tap ikon folder di bawah."); return; }

            try
            {
                Loader.IsVisible = true;
                var photos = await _drive.ListImageFilesAsync(SelectedFolderId, 200);
                if (photos.Count == 0)
                { ShowEmpty("Belum ada foto di folder ini.\nTap '+ Post' untuk upload pertama."); return; }

                // Group by groupId from description metadata
                var groups = new Dictionary<string, List<(DriveUploadService.DriveImage Img, KegiatanMeta? Meta)>>();
                foreach (var img in photos)
                {
                    KegiatanMeta? meta = null;
                    if (!string.IsNullOrWhiteSpace(img.Description))
                    {
                        try { meta = JsonSerializer.Deserialize<KegiatanMeta>(img.Description); } catch { }
                    }
                    var key = !string.IsNullOrWhiteSpace(meta?.GroupId) ? meta!.GroupId! : img.Id;
                    if (!groups.ContainsKey(key)) groups[key] = new();
                    groups[key].Add((img, meta));
                }

                // Sort each group by index, sort groups by latest createdTime desc
                var orderedGroups = groups.Values
                    .Select(g => g.OrderBy(x => x.Meta?.Index ?? 0).ToList())
                    .OrderByDescending(g => ParseTime(g[0].Img.CreatedTime))
                    .ToList();

                foreach (var group in orderedGroups)
                    FeedPanel.Children.Add(BuildPostCard(group));
            }
            catch (Exception ex) { ShowEmpty("Gagal load: " + ex.Message); }
            finally { Loader.IsVisible = false; }
        }

        private void ShowEmpty(string msg)
        {
            FeedPanel.Children.Add(new Label
            {
                Text = msg, FontSize = 12,
                HorizontalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center,
                TextColor = (Color)Application.Current!.Resources["TextMuted"],
                Margin = new Thickness(20, 60, 20, 0),
            });
        }

        // Build a single post card (1 group = N photos with shared metadata)
        private View BuildPostCard(List<(DriveUploadService.DriveImage Img, KegiatanMeta? Meta)> group)
        {
            var muted   = (Color)Application.Current!.Resources["TextMuted"];
            var primary = (Color)Application.Current.Resources["TextPrimary"];
            var border  = (Color)Application.Current.Resources["BorderClr"];
            var card    = (Color)Application.Current.Resources["CardBg"];

            var first = group[0];
            var meta  = first.Meta;
            var time  = ParseTime(first.Img.CreatedTime);
            var timeText = time.HasValue ? time.Value.ToLocalTime().ToString("d MMM yyyy · HH:mm") : "—";

            var stack = new VerticalStackLayout { Spacing = 0 };

            // Header
            var head = new VerticalStackLayout { Padding = new Thickness(14, 10, 14, 8), Spacing = 2 };
            var title = string.IsNullOrWhiteSpace(meta?.Pekerjaan) ? meta?.Span ?? first.Img.Name : meta!.Pekerjaan!;
            head.Children.Add(new Label { Text = $"📌 {title}",
                                          FontSize = 12, FontAttributes = FontAttributes.Bold, TextColor = primary });
            var subParts = new List<string> { timeText };
            if (!string.IsNullOrWhiteSpace(meta?.Segment)) subParts.Add(meta!.Segment!);
            if (!string.IsNullOrWhiteSpace(meta?.Span))    subParts.Add($"📍 {meta!.Span}");
            head.Children.Add(new Label { Text = string.Join("  ·  ", subParts),
                                          FontSize = 10, TextColor = muted });
            stack.Children.Add(head);

            // Carousel of photos (Instagram-style swipe)
            var imageSources = group.Select(x => (object)ImageSource.FromUri(new Uri(x.Img.ImageUrl))).ToList();
            var indicator = new IndicatorView
            {
                IndicatorColor         = Color.FromArgb("#33FFFFFF"),
                SelectedIndicatorColor = Color.FromArgb("#FFFFFF"),
                IndicatorSize          = 6,
                HorizontalOptions      = LayoutOptions.Center,
                Margin                 = new Thickness(0, 6, 0, 6),
            };
            var carousel = new CarouselView
            {
                ItemsSource    = imageSources,
                HeightRequest  = 360,
                Loop           = false,
                IndicatorView  = group.Count > 1 ? indicator : null,
                BackgroundColor = Color.FromArgb("#000000"),
            };
            carousel.ItemTemplate = new DataTemplate(() =>
            {
                var img = new Image { Aspect = Aspect.AspectFill, BackgroundColor = Color.FromArgb("#0B1220") };
                img.SetBinding(Image.SourceProperty, ".");
                return img;
            });
            // Tap any photo → open in fullscreen viewer
            var carouselTap = new TapGestureRecognizer();
            carouselTap.Tapped += async (s, e) =>
            {
                var idx = carousel.Position;
                if (idx >= 0 && idx < group.Count)
                    await Navigation.PushModalAsync(new FullscreenPhotoPage(new Uri(group[idx].Img.ImageUrl)));
            };
            carousel.GestureRecognizers.Add(carouselTap);

            stack.Children.Add(carousel);
            if (group.Count > 1) stack.Children.Add(indicator);

            // Caption (plain text, below image)
            if (!string.IsNullOrWhiteSpace(meta?.Caption))
            {
                stack.Children.Add(new Label
                {
                    Text       = meta!.Caption,
                    FontSize   = 13,
                    TextColor  = primary,
                    Margin     = new Thickness(14, 10, 14, 0),
                    LineBreakMode = LineBreakMode.WordWrap,
                });
            }

            // Meta footer
            var foot = new VerticalStackLayout { Padding = new Thickness(14, 6, 14, 8), Spacing = 3 };
            if (meta?.KetuaRegu != null && meta.KetuaRegu.Count > 0)
                foot.Children.Add(new Label { Text = $"👷 {string.Join(", ", meta.KetuaRegu)}",
                                              FontSize = 10, TextColor = Color.FromArgb("#22C55E") });
            if (!string.IsNullOrWhiteSpace(meta?.Waspang))
                foot.Children.Add(new Label { Text = $"🛡 Waspang: {meta!.Waspang}",
                                              FontSize = 10, TextColor = muted });
            stack.Children.Add(foot);

            // Action bar: Share button
            var shareBtn = new Border
            {
                BackgroundColor = Color.FromArgb("#25D366"), // WhatsApp green
                StrokeShape     = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
                Padding         = new Thickness(14, 9),
                Margin          = new Thickness(14, 4, 14, 12),
            };
            shareBtn.Content = new Label
            {
                Text              = "📤  Share ke WhatsApp",
                FontSize          = 12,
                FontAttributes    = FontAttributes.Bold,
                TextColor         = Colors.White,
                HorizontalOptions = LayoutOptions.Center,
            };
            var capturedGroup = group;
            var capturedMeta  = meta;
            shareBtn.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(async () => await ShareGroupAsync(capturedGroup, capturedMeta, shareBtn))
            });
            stack.Children.Add(shareBtn);

            return new Border
            {
                BackgroundColor = card,
                StrokeShape     = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
                Stroke          = new SolidColorBrush(border),
                StrokeThickness = 1,
                Margin          = new Thickness(12, 0),
                Content         = stack,
            };
        }

        // ── Share to WhatsApp (download foto ke cache lalu kirim via system share sheet) ──
        private async Task ShareGroupAsync(
            List<(DriveUploadService.DriveImage Img, KegiatanMeta? Meta)> group,
            KegiatanMeta? meta, Border btn)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            var origLabel = (btn.Content as Label)?.Text ?? "";

            try
            {
                if (btn.Content is Label l) l.Text = "⏳  Menyiapkan...";

                // Download semua foto group ke cache
                var localPaths = new List<string>();
                int idx = 0;
                foreach (var (img, _) in group)
                {
                    if (btn.Content is Label l2) l2.Text = $"⏳  Download {++idx}/{group.Count}...";
                    var bytes = await _drive.DownloadFileAsync(img.Id);
                    if (bytes == null || bytes.Length == 0) continue;
                    var path = Path.Combine(FileSystem.CacheDirectory, $"share_{img.Id}.jpg");
                    await File.WriteAllBytesAsync(path, bytes);
                    localPaths.Add(path);
                }
                if (localPaths.Count == 0)
                {
                    await DisplayAlert("Gagal", "Tidak bisa download foto dari Drive.", "OK");
                    return;
                }

                var caption = BuildShareCaption(meta);

#if ANDROID
                StokBarangMAUI.Platforms.Android.WhatsAppShare.ShareImagesWithText(localPaths, caption);
#else
                await DisplayAlert("Tidak didukung", "Share hanya tersedia di Android.", "OK");
#endif
            }
            catch (Exception ex) { await DisplayAlert("Gagal", ex.Message, "OK"); }
            finally
            {
                if (btn.Content is Label lbl) lbl.Text = origLabel;
            }
        }

        // Format caption sesuai request:
        //   SEGMEN: {Segment}
        //   {Span} (kalau ada)
        //   {Pekerjaan}
        // (waspang & ketua regu TIDAK dimasukkan)
        private static string BuildShareCaption(KegiatanMeta? m)
        {
            if (m == null) return "";
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(m.Segment))   parts.Add($"SEGMEN: {m.Segment}");
            if (!string.IsNullOrWhiteSpace(m.Span))      parts.Add(m.Span!);
            if (!string.IsNullOrWhiteSpace(m.Pekerjaan)) parts.Add(m.Pekerjaan!);
            return string.Join("\n", parts);
        }

        private static DateTime? ParseTime(string? iso)
        {
            if (string.IsNullOrWhiteSpace(iso)) return null;
            return DateTime.TryParse(iso, out var t) ? t : null;
        }

        // ── Actions ─────────────────────────────────────────────────────
        private async void OnRefresh(object sender, EventArgs e)
        {
            await LoadFeedAsync();
            Refresher.IsRefreshing = false;
        }

        private async void OnBack(object sender, TappedEventArgs e)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            await Navigation.PopAsync();
        }

        private async void OnPickFolder(object sender, TappedEventArgs e)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            if (!_gauth.IsSignedIn)
            { await DisplayAlert("Login dulu", "Sign in Google dulu di tab Input.", "OK"); return; }
            if (string.IsNullOrWhiteSpace(_project.DriveFolderIdSuratJalan))
            { await DisplayAlert("Belum di-set", "Drive root folder belum di-set di project config.", "OK"); return; }

            var picker = new DriveFolderPickerPage(_drive, _project.DriveFolderIdSuratJalan);
            await Navigation.PushModalAsync(picker);
            var result = await picker.WaitForResultAsync();
            if (result is { } pick)
            {
                Preferences.Set(FolderIdKey,   pick.Id);
                Preferences.Set(FolderNameKey, pick.Name);
                UpdateFolderUi();
                await LoadFeedAsync();
            }
        }

        private async void OnNewPost(object sender, TappedEventArgs e)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            if (!_gauth.IsSignedIn)
            { await DisplayAlert("Login dulu", "Sign in Google dulu di tab Input.", "OK"); return; }
            if (string.IsNullOrWhiteSpace(SelectedFolderId))
            { await DisplayAlert("Pilih folder", "Pilih folder Drive dulu sebelum post.", "OK"); return; }

            await Navigation.PushAsync(new AbsensiInputPage(_drafts, _project, _gauth, _drive, _sheets, SelectedFolderId));
        }

        // Metadata DTO untuk parse description Drive file
        private class KegiatanMeta
        {
            public string?       GroupId      { get; set; }
            public int?          Index        { get; set; }
            public int?          TotalInGroup { get; set; }
            public string?       Caption      { get; set; }
            public string?       Segment      { get; set; }
            public string?       Span         { get; set; }
            public string?       Pekerjaan    { get; set; }
            public string?       Waspang      { get; set; }
            public List<string>? KetuaRegu    { get; set; }
            public string?       SavedAt      { get; set; }
            public string?       CreatedBy    { get; set; }
        }
    }
}
