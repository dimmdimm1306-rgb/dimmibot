using StokBarangMAUI.Services;

namespace StokBarangMAUI.Pages
{
    public partial class OpenClawBotPage : ContentPage
    {
        private readonly OpenClawBotService _botService;
        private readonly ThemeService _themeService;
        private readonly AuthService _auth;
        private readonly List<string> _consoleLines = new();
        private const int MaxConsoleLines = 500;
        private DateTime? _startTime;
        private System.Timers.Timer? _uptimeTimer;
        private int _messageCount = 0;

        public OpenClawBotPage()
        {
            InitializeComponent();

            _botService = ((App)Application.Current!).Handler!.MauiContext!.Services.GetRequiredService<OpenClawBotService>();
            _themeService = ((App)Application.Current!).Handler!.MauiContext!.Services.GetRequiredService<ThemeService>();
            _auth = ((App)Application.Current!).Handler!.MauiContext!.Services.GetRequiredService<AuthService>();

            // Admin-only check
            if (!_auth.CanEdit)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await DisplayAlert("⚠️ Akses Ditolak", "Halaman Bot WhatsApp hanya bisa diakses oleh Admin.", "OK");
                    await Navigation.PopAsync();
                });
                return;
            }

            // Subscribe to bot events
            _botService.OnOutput += OnBotOutput;
            _botService.OnError += OnBotError;
            _botService.OnStatusChanged += OnBotStatusChanged;

            // Update theme icon
            LblTheme.Text = _themeService.ThemeIcon;

            // Update initial status
            UpdateStatus();

            // Start uptime timer
            StartUptimeTimer();
        }

        private void StartUptimeTimer()
        {
            _uptimeTimer = new System.Timers.Timer(1000); // Update every second
            _uptimeTimer.Elapsed += (s, e) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    UpdateUptime();
                });
            };
            _uptimeTimer.Start();
        }

        private void UpdateUptime()
        {
            if (_startTime.HasValue && _botService.IsRunning)
            {
                var uptime = DateTime.Now - _startTime.Value;
                UptimeText.Text = $"{uptime.Hours:D2}:{uptime.Minutes:D2}:{uptime.Seconds:D2}";
            }
            else
            {
                UptimeText.Text = "00:00:00";
            }
        }

        private void OnBotOutput(string message)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Count messages (simple heuristic)
                if (message.Contains("message") || message.Contains("pesan"))
                {
                    _messageCount++;
                    MessagesText.Text = _messageCount.ToString();
                }

                AddConsoleLog($"[INFO] {message}");
            });
        }

        private void OnBotError(string message)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                AddConsoleLog($"[ERROR] {message}");
            });
        }

        private void OnBotStatusChanged(bool isRunning)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (isRunning)
                {
                    _startTime = DateTime.Now;
                    _messageCount = 0;
                    MessagesText.Text = "0";
                }
                else
                {
                    _startTime = null;
                }
                UpdateStatus();
            });
        }

        private void AddConsoleLog(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            _consoleLines.Add($"[{timestamp}] {message}");

            // Keep only last N lines
            if (_consoleLines.Count > MaxConsoleLines)
            {
                _consoleLines.RemoveAt(0);
            }

            ConsoleOutput.Text = string.Join("\n", _consoleLines);

            // Auto scroll to bottom
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(100);
                await ConsoleScrollView.ScrollToAsync(0, ConsoleOutput.Height, false);
            });
        }

        private void UpdateStatus()
        {
            if (_botService.IsRunning)
            {
                StatusLabel.Text = "✅ Bot aktif dan terhubung";
                StatusIcon.Text = "✅";
                StatusText.Text = "Online";
                StatusText.TextColor = Color.FromArgb("#10B981");
                
                StartButton.BackgroundColor = Color.FromArgb("#6B7280");
                StopButton.BackgroundColor = Color.FromArgb("#DC2626");
                RestartButton.BackgroundColor = Color.FromArgb("#F59E0B");
            }
            else
            {
                StatusLabel.Text = "⭕ Bot tidak aktif";
                StatusIcon.Text = "⭕";
                StatusText.Text = "Offline";
                StatusText.TextColor = Color.FromArgb("#6B7280");
                
                StartButton.BackgroundColor = Color.FromArgb("#16A34A");
                StopButton.BackgroundColor = Color.FromArgb("#6B7280");
                RestartButton.BackgroundColor = Color.FromArgb("#6B7280");
            }
        }

        private async void OnStartBot(object sender, EventArgs e)
        {
            if (_botService.IsRunning)
            {
                await DisplayAlert("Info", "Bot sudah berjalan", "OK");
                return;
            }

            AddConsoleLog("[System] Memulai bot WhatsApp OPENCLAW...");
            AddConsoleLog("[System] Memeriksa Node.js dan dependencies...");
            
            var success = await _botService.StartBotAsync();
            
            if (!success)
            {
                await DisplayAlert("Error", "Gagal memulai bot. Cek console log untuk detail.", "OK");
            }
            else
            {
                AddConsoleLog("[System] Bot berhasil dimulai!");
                AddConsoleLog("[System] Tunggu hingga QR code muncul untuk scan dengan WhatsApp");
            }
        }

        private async void OnStopBot(object sender, EventArgs e)
        {
            if (!_botService.IsRunning)
            {
                await DisplayAlert("Info", "Bot tidak sedang berjalan", "OK");
                return;
            }

            var confirm = await DisplayAlert("Konfirmasi", "Stop bot WhatsApp?", "Ya", "Tidak");
            if (confirm)
            {
                AddConsoleLog("[System] Menghentikan bot...");
                _botService.StopBot();
                AddConsoleLog("[System] Bot dihentikan");
            }
        }

        private async void OnRestartBot(object sender, EventArgs e)
        {
            if (!_botService.IsRunning)
            {
                await DisplayAlert("Info", "Bot tidak sedang berjalan. Gunakan tombol Start.", "OK");
                return;
            }

            var confirm = await DisplayAlert("Konfirmasi", "Restart bot WhatsApp?", "Ya", "Tidak");
            if (confirm)
            {
                AddConsoleLog("[System] Restarting bot...");
                _botService.StopBot();
                await Task.Delay(2000);
                AddConsoleLog("[System] Memulai ulang bot...");
                await _botService.StartBotAsync();
            }
        }

        private async void OnOpenConfig(object sender, EventArgs e)
        {
            var action = await DisplayActionSheet(
                "Konfigurasi Bot",
                "Batal",
                null,
                "📝 Edit .env File",
                "📂 Buka Folder OPENCLAW",
                "🔧 Install Dependencies",
                "📋 Lihat Setup Guide"
            );

            switch (action)
            {
                case "📝 Edit .env File":
                    await DisplayAlert("Info", 
                        "Untuk edit konfigurasi:\n\n" +
                        "1. Buka folder OPENCLAW\n" +
                        "2. Edit file .env\n" +
                        "3. Isi Google Sheets ID, Drive Folder ID, dan OpenRouter API Key\n" +
                        "4. Restart bot setelah perubahan",
                        "OK");
                    break;

                case "📂 Buka Folder OPENCLAW":
                    await DisplayAlert("Info", 
                        "Folder OPENCLAW berada di:\n" +
                        "StokBarangMAUI/OPENCLAW/\n\n" +
                        "Buka dengan file manager untuk melihat file-file bot.",
                        "OK");
                    break;

                case "🔧 Install Dependencies":
                    AddConsoleLog("[System] Installing npm dependencies...");
                    await DisplayAlert("Info", 
                        "Untuk install dependencies manual:\n\n" +
                        "1. Buka terminal/command prompt\n" +
                        "2. cd ke folder OPENCLAW\n" +
                        "3. Jalankan: npm install\n" +
                        "4. Tunggu hingga selesai",
                        "OK");
                    break;

                case "📋 Lihat Setup Guide":
                    await DisplayAlert("Setup Guide", 
                        "SETUP OPENCLAW BOT:\n\n" +
                        "1️⃣ Install Node.js\n" +
                        "   Download dari nodejs.org\n\n" +
                        "2️⃣ Setup Google Sheets API\n" +
                        "   - Buat project di Google Cloud Console\n" +
                        "   - Enable Sheets & Drive API\n" +
                        "   - Download credentials.json\n\n" +
                        "3️⃣ Setup OpenRouter\n" +
                        "   - Daftar di openrouter.ai\n" +
                        "   - Dapatkan API key gratis\n\n" +
                        "4️⃣ Konfigurasi .env\n" +
                        "   - Copy .env.example ke .env\n" +
                        "   - Isi semua konfigurasi\n\n" +
                        "5️⃣ Start Bot & Scan QR",
                        "OK");
                    break;
            }
        }

        private async void OnViewLogs(object sender, EventArgs e)
        {
            var action = await DisplayActionSheet(
                "Log Actions",
                "Batal",
                null,
                "🗑️ Clear Console",
                "💾 Save Logs to File",
                "📤 Share Logs"
            );

            switch (action)
            {
                case "🗑️ Clear Console":
                    OnClearConsole(sender, e);
                    break;

                case "💾 Save Logs to File":
                    await DisplayAlert("Info", "Fitur save logs akan segera ditambahkan", "OK");
                    break;

                case "📤 Share Logs":
                    await DisplayAlert("Info", "Fitur share logs akan segera ditambahkan", "OK");
                    break;
            }
        }

        private void OnClearConsole(object sender, EventArgs e)
        {
            _consoleLines.Clear();
            ConsoleOutput.Text = "[System] Console cleared";
            AddConsoleLog("[System] Console log dibersihkan");
        }

        private void OnShowHelp(object sender, EventArgs e)
        {
            DisplayAlert("Help - OPENCLAW Bot", 
                "CARA MENGGUNAKAN BOT:\n\n" +
                "📱 KIRIM PESAN KE BOT:\n" +
                "• Format: masuk/keluar/dibawa [jumlah] [nama barang]\n" +
                "• Contoh: masuk 100 kabel fiber\n" +
                "• Bisa kirim foto barang\n\n" +
                "📊 CEK PROGRESS:\n" +
                "• Kirim: cek [lokasi]\n" +
                "• Contoh: cek Jakarta\n\n" +
                "🤖 KONSULTASI AI:\n" +
                "• Kirim pertanyaan langsung\n" +
                "• Bot akan jawab dengan AI\n\n" +
                "⚙️ STATUS:\n" +
                "• Hijau = Bot online\n" +
                "• Abu-abu = Bot offline",
                "OK");
        }

        private void OnRefreshStatus(object sender, EventArgs e)
        {
            UpdateStatus();
            AddConsoleLog("[System] Status refreshed");
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            if (_botService.IsRunning)
            {
                var confirm = await DisplayAlert("Peringatan", 
                    "Bot masih berjalan. Bot akan tetap aktif di background. Lanjutkan?", 
                    "Ya", "Tidak");
                
                if (!confirm) return;
            }

            await Navigation.PopAsync();
        }

        private void OnThemeToggle(object sender, EventArgs e)
        {
            _themeService.Toggle();
            LblTheme.Text = _themeService.ThemeIcon;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            
            // Stop uptime timer
            _uptimeTimer?.Stop();
            _uptimeTimer?.Dispose();
            
            // Note: Bot will continue running in background
            // Unsubscribe from events to prevent memory leaks
            // But don't stop the bot
        }
    }
}
