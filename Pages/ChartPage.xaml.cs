namespace StokBarangMAUI.Pages
{
    public partial class ChartPage : ContentPage
    {
        private readonly string _html;
        private bool _isLandscape;

        public ChartPage(string title, string html)
        {
            InitializeComponent();
            LblTitle.Text = title;
            _html = html;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            ChartWebView.Source = new HtmlWebViewSource { Html = _html };
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            if (_isLandscape) SetOrientation(false);
        }

        private void OnRotate(object s, TappedEventArgs e)
        {
            try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
            _isLandscape = !_isLandscape;
            LblRotate.Text = _isLandscape ? "⟳ Portrait" : "⟳ Landscape";
            SetOrientation(_isLandscape);
        }

        private static void SetOrientation(bool landscape)
        {
#if ANDROID
            var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
            if (activity == null) return;
            activity.RequestedOrientation = landscape
                ? Android.Content.PM.ScreenOrientation.SensorLandscape
                : Android.Content.PM.ScreenOrientation.Portrait;
#endif
        }

        private async void OnClose(object s, TappedEventArgs e)
            => await Navigation.PopModalAsync();
    }
}
