using StokBarangMAUI.Services;
using Microsoft.Maui.Controls.Shapes;

namespace StokBarangMAUI.Pages
{
    public partial class AiChatPopup : ContentPage
    {
        private readonly AiChatService _aiService;
        private readonly AuthService _authService;
        private System.Timers.Timer? _authCheckTimer;

        public AiChatPopup()
        {
            InitializeComponent();
            _aiService = ((App)Application.Current!).Handler!.MauiContext!.Services.GetRequiredService<AiChatService>();
            _authService = ((App)Application.Current!).Handler!.MauiContext!.Services.GetRequiredService<AuthService>();
            
            // Update auth status on load
            UpdateAuthStatus();
            
            // Start timer to check auth status periodically
            StartAuthCheckTimer();
        }

        // Disable hardware back button — user must tap the X button
        protected override bool OnBackButtonPressed()
        {
            return true; // true = handled, don't navigate back
        }

        private void StartAuthCheckTimer()
        {
            _authCheckTimer = new System.Timers.Timer(5000); // Check every 5 seconds
            _authCheckTimer.Elapsed += (s, e) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    UpdateAuthStatus();
                });
            };
            _authCheckTimer.Start();
        }

        private void UpdateAuthStatus()
        {
            var userEmail = _authService.CurrentEmail ?? "";
            var isAuthenticated = _aiService.IsAuthenticatedForChanges(userEmail);
            
            if (isAuthenticated)
            {
                AuthStatusLabel.Text = "🔓 Admin Mode";
                AuthStatusLabel.IsVisible = true;
            }
            else
            {
                AuthStatusLabel.IsVisible = false;
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _authCheckTimer?.Stop();
            _authCheckTimer?.Dispose();
        }

        private async void OnSendMessage(object sender, EventArgs e)
        {
            var message = MessageEntry.Text?.Trim();
            if (string.IsNullOrEmpty(message))
                return;

            // Clear input
            MessageEntry.Text = string.Empty;

            // Add user message
            AddUserMessage(message);

            // Show loading
            LoadingOverlay.IsVisible = true;

            try
            {
                // Get AI response
                var response = await _aiService.SendMessageAsync(message);
                
                // Add AI response
                AddAiMessage(response);
                
                // Update auth status (in case user authenticated via chat)
                UpdateAuthStatus();
            }
            catch (Exception ex)
            {
                AddAiMessage($"❌ Error: {ex.Message}");
            }
            finally
            {
                LoadingOverlay.IsVisible = false;
            }

            // Scroll to bottom
            await Task.Delay(100);
            await ChatScrollView.ScrollToAsync(0, MessagesContainer.Height, true);
        }

        private void AddUserMessage(string message)
        {
            var res = Application.Current?.Resources;
            var cardBg = TryGetColor(res, "AccentGreenBg", "#064E3B");
            var textClr = TryGetColor(res, "AccentGreenBorder", "#10B981");

            var border = new Border
            {
                BackgroundColor = cardBg,
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
                Padding = new Thickness(12),
                HorizontalOptions = LayoutOptions.End,
                MaximumWidthRequest = 280,
            };

            var label = new Label
            {
                Text = message,
                TextColor = textClr,
                FontSize = 13,
                LineBreakMode = LineBreakMode.WordWrap
            };

            border.Content = label;
            MessagesContainer.Children.Add(border);
        }

        private void AddAiMessage(string message)
        {
            var res = Application.Current?.Resources;
            var cardBg = TryGetColor(res, "CardBg", "#001e40");
            var borderClr = TryGetColor(res, "BorderClr", "#1f477b");
            var textClr = TryGetColor(res, "TextPrimary", "#d5e3ff");

            // Grid with cat avatar + message bubble
            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Star)
                },
                ColumnSpacing = 8,
                HorizontalOptions = LayoutOptions.Start
            };

            var avatar = new Image
            {
                Source = "claw_cat.png",
                WidthRequest = 28,
                HeightRequest = 28,
                VerticalOptions = LayoutOptions.Start,
                Margin = new Thickness(0, 4, 0, 0)
            };
            Grid.SetColumn(avatar, 0);

            var border = new Border
            {
                BackgroundColor = cardBg,
                Stroke = new SolidColorBrush(borderClr),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
                Padding = new Thickness(12),
                MaximumWidthRequest = 280,
            };
            Grid.SetColumn(border, 1);

            var label = new Label
            {
                Text = message,
                TextColor = textClr,
                FontSize = 13,
                LineBreakMode = LineBreakMode.WordWrap
            };

            border.Content = label;
            grid.Children.Add(avatar);
            grid.Children.Add(border);
            MessagesContainer.Children.Add(grid);
        }

        private static Color TryGetColor(ResourceDictionary? res, string key, string fallback)
        {
            if (res != null && res.TryGetValue(key, out var v) && v is Color c) return c;
            return Color.FromArgb(fallback);
        }

        private async void OnCloseClicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync(true);
        }

        private void OnClearChat(object sender, EventArgs e)
        {
            MessagesContainer.Children.Clear();
            _aiService.ClearHistory();

            // Add welcome message back
            AddAiMessage("👋 Yo! Gue Claw, AI assistant lo di sini. Tanya apa aja — soal kerjaan, progress, stok, atau mau ngobrol santai juga boleh! 😄");
        }

    }
}
