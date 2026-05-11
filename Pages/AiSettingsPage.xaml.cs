using StokBarangMAUI.Services;

namespace StokBarangMAUI.Pages
{
    public partial class AiSettingsPage : ContentPage
    {
        private readonly AiChatService _aiService;
        private readonly AuthService _auth;
        private readonly GDriveReaderService _drive;
        private bool _isPasswordVisible = false;
        private bool _isAdmin = false;

        public AiSettingsPage()
        {
            InitializeComponent();
            
            _aiService = ((App)Application.Current!).Handler!.MauiContext!.Services.GetRequiredService<AiChatService>();
            _auth = ((App)Application.Current!).Handler!.MauiContext!.Services.GetRequiredService<AuthService>();
            _drive = ((App)Application.Current!).Handler!.MauiContext!.Services.GetRequiredService<GDriveReaderService>();
            
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

            // Load Drive Reader settings
            DriveEnabledSwitch.IsToggled = _drive.IsEnabled;
            DriveUrlEntry.Text = _drive.BaseUrl;
            DriveTokenEntry.Text = _drive.Token;
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

        // ── Google Drive Reader handlers ─────────────────────────────────

        private void OnDriveEnabledToggled(object sender, ToggledEventArgs e)
        {
            _drive.SetEnabled(e.Value);
        }

        private void OnSaveDriveSettings(object sender, EventArgs e)
        {
            var url = DriveUrlEntry.Text?.Trim() ?? "";
            var token = DriveTokenEntry.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(url))
            {
                DriveStatusLabel.IsVisible = true;
                DriveStatusLabel.Text = "⚠️ URL tidak boleh kosong";
                DriveStatusLabel.TextColor = Color.FromArgb("#F59E0B");
                return;
            }

            _drive.SetBaseUrl(url);
            _drive.SetToken(token);

            DriveStatusLabel.IsVisible = true;
            DriveStatusLabel.Text = "✅ Settings Drive tersimpan";
            DriveStatusLabel.TextColor = Color.FromArgb("#10B981");
        }

        private async void OnTestDrive(object sender, EventArgs e)
        {
            // Save first so test reflects what user typed
            OnSaveDriveSettings(sender, e);

            DriveStatusLabel.IsVisible = true;
            DriveStatusLabel.Text = "⏳ Testing koneksi...";
            DriveStatusLabel.TextColor = Color.FromArgb("#F59E0B");

            var (ok, message, email) = await _drive.CheckHealthAsync();
            if (ok)
            {
                DriveStatusLabel.Text = $"✅ Connected!\nService account: {email}\n\n💡 Share Drive folder-mu ke email di atas (Viewer).";
                DriveStatusLabel.TextColor = Color.FromArgb("#10B981");
            }
            else
            {
                DriveStatusLabel.Text = $"❌ Tidak bisa connect: {message}\n\nCek:\n• Apakah start_server.bat jalan di laptop?\n• URL benar?\n• Dari HP: pakai IP/tunnel, bukan localhost";
                DriveStatusLabel.TextColor = Color.FromArgb("#DC2626");
            }
        }
    }
}
