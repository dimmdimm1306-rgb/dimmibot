namespace StokBarangMAUI.Controls
{
    public partial class FloatingAiButton : ContentView
    {
        public FloatingAiButton()
        {
            InitializeComponent();
        }

        private async void OnAiButtonTapped(object sender, EventArgs e)
        {
            // Buka popup AI chat
            var popup = new Pages.AiChatPopup();
            await Application.Current!.MainPage!.Navigation.PushModalAsync(popup, true);
        }
    }
}
