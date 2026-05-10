using StokBarangMAUI.Services;

namespace StokBarangMAUI.Pages
{
    public partial class DriveFolderPickerPage : ContentPage
    {
        private readonly DriveUploadService _drive;
        private readonly string             _rootFolderId;
        private readonly TaskCompletionSource<(string Id, string Name)?> _tcs = new();

        private List<DriveUploadService.DriveFolder> _all = new();

        public DriveFolderPickerPage(DriveUploadService drive, string rootFolderId)
        {
            InitializeComponent();
            _drive        = drive;
            _rootFolderId = rootFolderId;
        }

        // Caller awaits this; returns null kalau dibatalkan, atau (id, name) kalau dipilih.
        public Task<(string Id, string Name)?> WaitForResultAsync() => _tcs.Task;

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadFoldersAsync();
        }

        private async Task LoadFoldersAsync()
        {
            try
            {
                Loader.IsVisible = true;
                _all = await _drive.ListFoldersAsync(_rootFolderId);
                Render(_all);
            }
            finally { Loader.IsVisible = false; }
        }

        private void OnSearchChanged(object sender, TextChangedEventArgs e)
        {
            var q = (e.NewTextValue ?? "").Trim();
            if (q.Length == 0) { Render(_all); return; }
            Render(_all.Where(f => f.Name.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList());
        }

        private void Render(List<DriveUploadService.DriveFolder> list)
        {
            FoldersPanel.Children.Clear();

            if (list.Count == 0)
            {
                FoldersPanel.Children.Add(new Label
                {
                    Text              = _all.Count == 0 ? "Tidak ada folder atau gagal load.\nPastikan login Google sudah benar."
                                                       : "Tidak ada folder yang cocok.",
                    FontSize          = 11,
                    HorizontalOptions = LayoutOptions.Center,
                    HorizontalTextAlignment = TextAlignment.Center,
                    TextColor         = (Color)Application.Current!.Resources["TextMuted"],
                    Margin            = new Thickness(0, 30, 0, 0)
                });
                return;
            }

            var primary = (Color)Application.Current!.Resources["TextPrimary"];
            var muted   = (Color)Application.Current.Resources["TextMuted"];
            var card    = (Color)Application.Current.Resources["CardBg"];
            var border  = (Color)Application.Current.Resources["BorderClr"];

            foreach (var f in list)
            {
                var captured = f;
                var grid = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition { Width = new GridLength(40) },
                        new ColumnDefinition { Width = GridLength.Star },
                        new ColumnDefinition { Width = GridLength.Auto }
                    },
                    Padding = new Thickness(10, 8),
                    ColumnSpacing = 8,
                };
                grid.Add(new Label { Text = "📁", FontSize = 18,
                                     HorizontalOptions = LayoutOptions.Center,
                                     VerticalOptions = LayoutOptions.Center }, 0, 0);
                grid.Add(new Label { Text = f.Name, FontSize = 13, FontAttributes = FontAttributes.Bold,
                                     TextColor = primary, VerticalOptions = LayoutOptions.Center,
                                     LineBreakMode = LineBreakMode.TailTruncation }, 1, 0);
                grid.Add(new Label { Text = "›", FontSize = 18, TextColor = Color.FromArgb("#3B82F6"),
                                     VerticalOptions = LayoutOptions.Center }, 2, 0);

                var row = new Border
                {
                    BackgroundColor = card,
                    StrokeShape     = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
                    Stroke          = new SolidColorBrush(border),
                    StrokeThickness = 1,
                    Content         = grid
                };
                row.GestureRecognizers.Add(new TapGestureRecognizer
                {
                    Command = new Command(async () =>
                    {
                        HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                        _tcs.TrySetResult((captured.Id, captured.Name));
                        await Navigation.PopModalAsync();
                    })
                });
                FoldersPanel.Children.Add(row);
            }
        }

        private async void OnClose(object sender, TappedEventArgs e)
        {
            _tcs.TrySetResult(null);
            await Navigation.PopModalAsync();
        }

        protected override bool OnBackButtonPressed()
        {
            _tcs.TrySetResult(null);
            return base.OnBackButtonPressed();
        }
    }
}
