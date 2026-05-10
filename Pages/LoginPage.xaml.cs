using StokBarangMAUI.Services;

namespace StokBarangMAUI.Pages
{
    public partial class LoginPage : ContentPage
    {
        private readonly AuthService _auth;
        private readonly TaskCompletionSource<bool> _tcs = new();

        public LoginPage(AuthService auth)
        {
            InitializeComponent();
            _auth = auth;
            EntEmail.Text = _auth.CurrentEmail ?? "";
        }

        // Caller bisa await hasilnya: true = login sukses, false = batal/gagal.
        public Task<bool> WaitForResultAsync() => _tcs.Task;

        private async void OnLogin(object sender, TappedEventArgs e)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            var (ok, msg) = _auth.TryLogin(EntEmail.Text ?? "");
            if (!ok)
            {
                LblMsg.Text      = msg;
                LblMsg.IsVisible = true;
                return;
            }
            _tcs.TrySetResult(true);
            await Navigation.PopModalAsync();
        }

        private async void OnCancel(object sender, TappedEventArgs e)
        {
            _tcs.TrySetResult(false);
            await Navigation.PopModalAsync();
        }
    }
}
