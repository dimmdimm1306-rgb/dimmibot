namespace StokBarangMAUI.Pages
{
    public partial class FullscreenPhotoPage : ContentPage
    {
        private double _scale = 1.0, _scaleStart = 1.0;
        private double _x = 0, _y = 0, _xStart = 0, _yStart = 0;
        private double _rot = 0;
        private const double MinScale = 1.0, MaxScale = 8.0;

        public FullscreenPhotoPage(string filePath)
        {
            InitializeComponent();
            ImgFoto.Source = ImageSource.FromFile(filePath);
        }

        // Untuk URL (foto dari Drive — pakai URI source)
        public FullscreenPhotoPage(Uri uri)
        {
            InitializeComponent();
            ImgFoto.Source = ImageSource.FromUri(uri);
        }

        private void OnPinch(object sender, PinchGestureUpdatedEventArgs e)
        {
            switch (e.Status)
            {
                case GestureStatus.Started: _scaleStart = _scale; break;
                case GestureStatus.Running:
                    _scale = Math.Clamp(_scaleStart * e.Scale, MinScale, MaxScale);
                    Apply();
                    break;
            }
        }

        private void OnPan(object sender, PanUpdatedEventArgs e)
        {
            switch (e.StatusType)
            {
                case GestureStatus.Started: _xStart = _x; _yStart = _y; break;
                case GestureStatus.Running:
                    _x = _xStart + e.TotalX;
                    _y = _yStart + e.TotalY;
                    Apply();
                    break;
            }
        }

        private void OnDoubleTap(object sender, TappedEventArgs e)
        {
            _scale = _scale > 1.5 ? 1.0 : 2.5;
            if (_scale <= 1.0) { _x = 0; _y = 0; }
            Apply();
        }

        private void OnRotate(object sender, TappedEventArgs e)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            _rot = (_rot + 90) % 360;
            Apply();
        }

        private void OnReset(object sender, TappedEventArgs e)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            _scale = 1.0; _rot = 0; _x = 0; _y = 0;
            Apply();
        }

        private async void OnClose(object sender, TappedEventArgs e)
            => await Navigation.PopModalAsync();

        private void Apply()
        {
            // Constrain pan dalam viewport
            var vw = Width  > 0 ? Width  : 360;
            var vh = Height > 0 ? Height : 640;
            var maxX = (vw * (_scale - 1)) / 2.0;
            var maxY = (vh * (_scale - 1)) / 2.0;
            _x = Math.Clamp(_x, -maxX, maxX);
            _y = Math.Clamp(_y, -maxY, maxY);

            ImgFoto.Scale        = _scale;
            ImgFoto.TranslationX = _x;
            ImgFoto.TranslationY = _y;
            ImgFoto.Rotation     = _rot;
        }
    }
}
