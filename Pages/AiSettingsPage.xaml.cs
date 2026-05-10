using StokBarangMAUI.Services;

namespace StokBarangMAUI.Pages
{
    public partial class AiSettingsPage : ContentPage
    {
        private readonly AiChatService _aiService;
        private readonly AuthService _auth;
        private bool _isPasswordVisible = false;
        private bool _isAdmin = false;

        public AiSettingsPage()
        {
            InitializeComponent();
            
            _aiService = ((App)Application.Current!).Handler!.MauiContext!.Services.GetRequiredService<AiChatService>();
            _auth = ((App)Application.Current!).Handler!.MauiContext!.Services.GetRequiredService<AuthService>();
            
            _isAdmin = _auth.CanEdit;

            // Load saved settings
            LoadSettings();
            
            // Disable base URL and model for non-admin
            if (!_isAdmin)
            {
                BaseUrlEntry.IsEnabled = false;
                ModelEntry.IsEnabled = false;
                BaseUrlEntry.Placeholder = "🔒 Hanya Admin yang bisa ubah";
                ModelEntry.Placeholder = "🔒 Hanya Admin yang bisa ubah";
            }
        }

        private void LoadSettings()
        {
            // Load API key
            var apiKey = _aiService.GetApiKey();
            if (!string.IsNullOrEmpty(apiKey))
            {
                ApiKeyEntry.Text = apiKey;
            }

            // Load base URL
            var baseUrl = _aiService.GetBaseUrl();
            if (!string.IsNullOrEmpty(baseUrl))
            {
                BaseUrlEntry.Text = baseUrl;
            }

            // Load model
            var model = _aiService.GetModel();
            if (!string.IsNullOrEmpty(model))
            {
                ModelEntry.Text = model;
            }
        }

        private async void OnSaveApiKey(object sender, EventArgs e)
        {
            var apiKey = ApiKeyEntry.Text?.Trim();
            
            // API key is optional (for local servers like OpenClaw API)
            // If empty, set to empty string (server may not need it)
            if (string.IsNullOrEmpty(apiKey))
            {
                apiKey = "";
            }

            // Save API key (all users)
            _aiService.SetApiKey(apiKey);

            // Only admin can change base URL and model
            if (_isAdmin)
            {
                var baseUrl = BaseUrlEntry.Text?.Trim();
                var model = ModelEntry.Text?.Trim();

                // Allow empty values - server will use defaults
                if (string.IsNullOrEmpty(baseUrl))
                {
                    baseUrl = "";
                }

                if (string.IsNullOrEmpty(model))
                {
                    model = "";
                }

                _aiService.SetBaseUrl(baseUrl);
                _aiService.SetModel(model);
            }
            
            await DisplayAlert("Success", "✅ Settings berhasil disimpan!", "OK");
        }

        private void OnShowApiKey(object sender, EventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;
            ApiKeyEntry.IsPassword = !_isPasswordVisible;
        }

        private async void OnGetApiKey(object sender, EventArgs e)
        {
            try
            {
                await Browser.OpenAsync("https://openrouter.ai/keys", BrowserLaunchMode.SystemPreferred);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Tidak bisa membuka browser: {ex.Message}", "OK");
            }
        }

        private async void OnTestAi(object sender, EventArgs e)
        {
            TestResultLabel.IsVisible = true;
            TestResultLabel.Text = "⏳ Testing...";
            TestResultLabel.TextColor = Color.FromArgb("#F59E0B");

            try
            {
                var response = await _aiService.SendMessageAsync("Halo, test koneksi. Jawab dengan singkat.");
                
                if (response.StartsWith("❌") || response.StartsWith("⚠️"))
                {
                    TestResultLabel.Text = $"❌ Test gagal:\n{response}";
                    TestResultLabel.TextColor = Color.FromArgb("#DC2626");
                }
                else
                {
                    TestResultLabel.Text = $"✅ Test berhasil!\nAI Response: {response}";
                    TestResultLabel.TextColor = Color.FromArgb("#10B981");
                }
            }
            catch (Exception ex)
            {
                TestResultLabel.Text = $"❌ Error: {ex.Message}";
                TestResultLabel.TextColor = Color.FromArgb("#DC2626");
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
