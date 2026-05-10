using System.Text.RegularExpressions;

namespace StokBarangMAUI.Pages
{
    public partial class DrivePreviewPage : ContentPage
    {
        private readonly string _originalUrl;
        private readonly string _previewUrl;
        private readonly string _imageUrl;

        public DrivePreviewPage(string driveUrl, string title = "", string subtitle = "Surat Jalan")
        {
            InitializeComponent();
            _originalUrl = driveUrl;
            _previewUrl  = ToPreviewUrl(driveUrl);
            _imageUrl    = ToDirectImageUrl(driveUrl);

            if (!string.IsNullOrWhiteSpace(title))    LblTitle.Text    = title;
            if (!string.IsNullOrWhiteSpace(subtitle)) LblSubtitle.Text = subtitle;

            LoadImage();
        }

        private async void LoadImage()
        {
            if (string.IsNullOrWhiteSpace(_imageUrl))
            {
                ShowWebView();
                return;
            }

            try
            {
                DocImage.Source = ImageSource.FromUri(new Uri(_imageUrl));
                ImageScroll.IsVisible  = true;
                LoaderOverlay.IsVisible = false;
            }
            catch
            {
                ShowWebView();
            }

            // Hide loader after brief delay (image loads async)
            await Task.Delay(800);
            LoaderOverlay.IsVisible = false;
        }

        private void ShowWebView()
        {
            ImageScroll.IsVisible   = false;
            LoaderOverlay.IsVisible = true;
            PreviewWeb.IsVisible    = true;
            PreviewWeb.Source       = _previewUrl;
        }

        // https://drive.google.com/file/d/FILE_ID/... → direct image URL
        public static string ToDirectImageUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return url;
            var m = Regex.Match(url, @"drive\.google\.com/file/d/([^/?]+)");
            if (m.Success)
                return $"https://drive.google.com/uc?export=view&id={m.Groups[1].Value}";
            m = Regex.Match(url, @"[?&]id=([^&]+)");
            if (m.Success)
                return $"https://drive.google.com/uc?export=view&id={m.Groups[1].Value}";
            return url;
        }

        // For WebView fallback: /preview URL (embedded viewer)
        public static string ToPreviewUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return url;
            var m = Regex.Match(url, @"drive\.google\.com/file/d/([^/?]+)");
            if (m.Success)
                return $"https://drive.google.com/file/d/{m.Groups[1].Value}/preview";
            m = Regex.Match(url, @"[?&]id=([^&]+)");
            if (m.Success)
                return $"https://drive.google.com/file/d/{m.Groups[1].Value}/preview";
            return url;
        }

        private void OnNavigating(object sender, WebNavigatingEventArgs e)
            => LoaderOverlay.IsVisible = true;

        private void OnNavigated(object sender, WebNavigatedEventArgs e)
            => LoaderOverlay.IsVisible = false;

        private void OnSwitchToWebView(object sender, TappedEventArgs e)
            => ShowWebView();

        private async void OnClose(object sender, TappedEventArgs e)
            => await Navigation.PopModalAsync();

        private async void OnOpenBrowser(object sender, TappedEventArgs e)
        {
            try { await Launcher.OpenAsync(_originalUrl); }
            catch { await DisplayAlert("Gagal", "Tidak dapat membuka browser.", "OK"); }
        }
    }
}
