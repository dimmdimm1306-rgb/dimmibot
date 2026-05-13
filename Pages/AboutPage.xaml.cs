using System.Net.Http;
using System.Text;
using System.Text.Json;
using StokBarangMAUI.Services;

namespace StokBarangMAUI.Pages
{
    public partial class AboutPage : ContentPage
    {
        private readonly AiChatService _aiService;
        private readonly GDriveReaderService _drive;
        private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };

        public AboutPage()
        {
            InitializeComponent();
            _aiService = ((App)Application.Current!).Handler!.MauiContext!.Services.GetRequiredService<AiChatService>();
            _drive = ((App)Application.Current!).Handler!.MauiContext!.Services.GetRequiredService<GDriveReaderService>();

            LoadLocal();
            _ = LoadGithubAsync();
        }

        //  Load info 

        private void LoadLocal()
        {
            // App version
            try
            {
                LblAppName.Text = AppInfo.Name;
                LblAppVersion.Text = AppInfo.VersionString;
                LblAppBuild.Text = AppInfo.BuildString;
            }
            catch { }

            // Device
            try
            {
                LblPlatform.Text = $"{DeviceInfo.Platform} ({DeviceInfo.Idiom})";
                LblOsVersion.Text = DeviceInfo.VersionString;
                LblDeviceModel.Text = $"{DeviceInfo.Manufacturer} {DeviceInfo.Model}";
            }
            catch { }

            // Local preferences
            LblLocalAiUrl.Text = string.IsNullOrEmpty(_aiService.GetBaseUrl()) ? "(kosong)" : _aiService.GetBaseUrl();
            LblLocalAiModel.Text = string.IsNullOrEmpty(_aiService.GetModel()) ? "(kosong)" : _aiService.GetModel();
            LblLocalDriveUrl.Text = string.IsNullOrEmpty(_drive.BaseUrl) ? "(kosong)" : _drive.BaseUrl;
            LblLocalDriveToken.Text = string.IsNullOrEmpty(_drive.Token) ? " Belum di-set" : $" Terisi ({_drive.Token.Length} chars)";
        }

        private async Task LoadGithubAsync()
        {
            var url = _aiService.GetGithubConfigUrl();
            LblGithubUrl.Text = string.IsNullOrEmpty(url) ? "(disabled)" : url;

            if (string.IsNullOrEmpty(url))
            {
                LblCfgVersion.Text = "(no config URL)";
                return;
            }

            LblCfgVersion.Text = " Fetching...";

            try
            {
                var resp = await _http.GetAsync(url);
                if (!resp.IsSuccessStatusCode)
                {
                    LblCfgVersion.Text = $" HTTP {(int)resp.StatusCode}";
                    return;
                }
                var json = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                LblCfgVersion.Text = root.TryGetProperty("version", out var v) ? v.ToString() : "-";
                LblCfgLastTested.Text = root.TryGetProperty("lastTested", out var lt) ? lt.GetString() ?? "-" : "-";
                LblAiBaseUrl.Text = root.TryGetProperty("baseUrl", out var ab) ? ab.GetString() ?? "-" : "-";
                LblAiModel.Text = root.TryGetProperty("model", out var am) ? am.GetString() ?? "-" : "-";
                LblDriveUrl.Text = root.TryGetProperty("gdriveReaderUrl", out var du) ? du.GetString() ?? "-" : "-";
                LblDriveEnabled.Text = root.TryGetProperty("gdriveReaderEnabled", out var de)
                    ? (de.GetBoolean() ? " true" : " false") : "-";
            }
            catch (Exception ex)
            {
                LblCfgVersion.Text = $" {ex.Message}";
            }
        }

        //  Actions 

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private void OnRefresh(object sender, EventArgs e)
        {
            LoadLocal();
            _ = LoadGithubAsync();
        }

        private async void OnFetchGithub(object sender, EventArgs e)
        {
            LblCfgVersion.Text = " Re-fetching & applying config...";
            try
            {
                await _aiService.FetchServerConfigAsync();
                await Task.Delay(500);
                LoadLocal();
                await LoadGithubAsync();
                await DisplayAlert("Done", " Config dari GitHub sudah di-fetch & apply ke local.", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }

        private async void OnRunDiagnostics(object sender, EventArgs e)
        {
            LblDiagnostics.Text = " Testing...\n";
            var sb = new StringBuilder();

            // 1. GitHub reachable
            sb.AppendLine("1  GitHub config:");
            try
            {
                var r = await _http.GetAsync(_aiService.GetGithubConfigUrl());
                sb.AppendLine(r.IsSuccessStatusCode ? $"     OK (HTTP {(int)r.StatusCode})" : $"     HTTP {(int)r.StatusCode}");
            }
            catch (Exception ex) { sb.AppendLine($"     {ex.Message}"); }

            // 2. AI base URL reachable (check /config)
            sb.AppendLine();
            sb.AppendLine("2  AI server:");
            try
            {
                var aiUrl = _aiService.GetBaseUrl();
                if (string.IsNullOrEmpty(aiUrl)) sb.AppendLine("     URL kosong");
                else
                {
                    var test = aiUrl.Replace("/v1", "") + "/config";
                    var r = await _http.GetAsync(test);
                    sb.AppendLine(r.IsSuccessStatusCode ? $"     {test} OK" : $"     HTTP {(int)r.StatusCode}");
                }
            }
            catch (Exception ex) { sb.AppendLine($"     {ex.Message}"); }

            // 3. Drive Reader health
            sb.AppendLine();
            sb.AppendLine("3  GDrive Reader:");
            if (!_drive.IsEnabled)
            {
                sb.AppendLine("     Disabled (toggle ON di AI Settings)");
            }
            else if (string.IsNullOrEmpty(_drive.BaseUrl))
            {
                sb.AppendLine("     URL kosong");
            }
            else
            {
                var (ok, msg, email) = await _drive.CheckHealthAsync();
                if (ok)
                {
                    sb.AppendLine($"     Server OK");
                    sb.AppendLine($"     {email}");
                }
                else
                {
                    sb.AppendLine($"     {msg}");
                }
            }

            // 4. Token setup
            sb.AppendLine();
            sb.AppendLine("4  Drive token:");
            if (string.IsNullOrEmpty(_drive.Token))
                sb.AppendLine("     Belum di-set (akan 401 kalau server pakai auth)");
            else
                sb.AppendLine($"     Set ({_drive.Token.Length} chars)");

            LblDiagnostics.Text = sb.ToString().TrimEnd();
        }

        private async void OnCopyGithubUrl(object sender, EventArgs e)
        {
            await Clipboard.SetTextAsync(LblGithubUrl.Text);
            await DisplayAlert(" Copied", "URL GitHub disalin ke clipboard.", "OK");
        }

        private async void OnCopyAiUrl(object sender, EventArgs e)
        {
            await Clipboard.SetTextAsync(LblAiBaseUrl.Text);
            await DisplayAlert(" Copied", "AI URL disalin.", "OK");
        }

        private async void OnCopyDriveUrl(object sender, EventArgs e)
        {
            await Clipboard.SetTextAsync(LblDriveUrl.Text);
            await DisplayAlert(" Copied", "Drive URL disalin.", "OK");
        }

        private async void OnResetPreferences(object sender, EventArgs e)
        {
            var ok = await DisplayAlert("Reset?",
                "Semua setting lokal (URL, model, token) akan dihapus.\n\nApp akan pakai config dari GitHub di next start.\n\nLanjut?",
                "Reset", "Batal");
            if (!ok) return;

            // Clear AI prefs
            Preferences.Remove("ai_api_key");
            Preferences.Remove("ai_base_url");
            Preferences.Remove("ai_model");

            // Clear Drive prefs
            Preferences.Remove(GDriveReaderService.PREF_URL);
            Preferences.Remove(GDriveReaderService.PREF_TOKEN);
            Preferences.Remove(GDriveReaderService.PREF_ENABLED);

            // Re-fetch from GitHub immediately
            try { await _aiService.FetchServerConfigAsync(); } catch { }

            await Task.Delay(500);
            LoadLocal();
            await LoadGithubAsync();
            await DisplayAlert("Done", " Preferences di-reset. Config GitHub sudah diterapkan.", "OK");
        }
    }
}
