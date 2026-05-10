using StokBarangMAUI.Services;

namespace StokBarangMAUI.Pages
{
    public partial class StokAktualPage : ContentPage
    {
        private readonly GoogleSheetsService _sheets;
        private bool _loaded = false;

        public StokAktualPage(GoogleSheetsService sheets)
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
                List.ItemsSource = data.StokAktual;
                _loaded = true;
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
        }

        private async void OnAiBotClicked(object sender, TappedEventArgs e)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            await Navigation.PushModalAsync(new AiChatPopup());
        }

        private async void OnHamburger(object sender, TappedEventArgs e)
            => await (RootTabbedPage.OpenDrawer?.Invoke() ?? Task.CompletedTask);
    }
}
