using StokBarangMAUI.Services;
using Microsoft.Maui.Controls.Shapes;

namespace StokBarangMAUI.Pages
{
    public partial class AiChatPopup : ContentPage
    {
        private readonly AiChatService _aiService;
        private readonly AuthService _authService;
        private System.Timers.Timer? _authCheckTimer;

        // Bubble width adaptif: HP narrow ~80% layar, tablet bisa s/d 1100px
        private static double MessageMaxWidth
        {
            get
            {
                try
                {
                    var w = DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density;
                    if (w <= 0) return 680;
                    var pct = w * 0.85;
                    return Math.Min(Math.Max(pct, 280), 1100);
                }
                catch { return 680; }
            }
        }

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

        // Disable hardware back button  user must tap the X button
        protected override bool OnBackButtonPressed()
        {
            return true; // true = handled, don't navigate back
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Auto-refresh data context tiap kali popup dibuka  user gak perlu ketik "refresh data"
            _aiService.InvalidateContext();
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
                AuthStatusLabel.Text = " Admin Mode";
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
                Console.WriteLine($"[Chat] USER: {message}");

                // Get AI response
                var response = await _aiService.SendMessageAsync(message);

                // Log response summary so we can see what bot shows on device
                var preview = response.Length > 500 ? response.Substring(0, 500) + "...[truncated]" : response;
                Console.WriteLine($"[Chat] BOT ({response.Length} chars): {preview}");

                // Add AI response
                AddAiMessage(response);
                
                // Update auth status (in case user authenticated via chat)
                UpdateAuthStatus();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Chat] ERROR: {ex}");
                AddAiMessage($" Error: {ex.Message}");
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
                MaximumWidthRequest = MessageMaxWidth,
            };

            var selectable = BuildSelectableText(message, textClr);
            border.Content = selectable;
            MessagesContainer.Children.Add(border);
        }

        private void AddAiMessage(string message)
        {
            var res = Application.Current?.Resources;
            var cardBg = TryGetColor(res, "CardBg", "#FFFFFF");
            var borderClr = TryGetColor(res, "BorderClr", "#C4C5D6");
            var textClr = TryGetColor(res, "TextPrimary", "#1A1B23");
            var aiBotBg = TryGetColor(res, "AiBotBg", "#6514D6");
            var aiBotBorder = TryGetColor(res, "AiBotBorder", "#7E3DEF");

            // Grid with AI avatar badge + message bubble
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

            var avatar = new Border
            {
                BackgroundColor = aiBotBg,
                Stroke = new SolidColorBrush(aiBotBorder),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 14 },
                WidthRequest = 32,
                HeightRequest = 32,
                VerticalOptions = LayoutOptions.Start,
                Margin = new Thickness(0, 4, 0, 0),
                Content = new Label
                {
                    Text = "AI",
                    FontSize = 11,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.White,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                }
            };
            Grid.SetColumn(avatar, 0);

            var border = new Border
            {
                BackgroundColor = cardBg,
                Stroke = new SolidColorBrush(borderClr),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
                Padding = new Thickness(12),
                MaximumWidthRequest = MessageMaxWidth,
            };
            Grid.SetColumn(border, 1);

            border.Content = BuildSelectableText(message, textClr);
            grid.Children.Add(avatar);
            grid.Children.Add(border);
            MessagesContainer.Children.Add(grid);
        }

        /// <summary>
        /// Build a selectable text view. Pakai custom SelectableLabel yang di Android
        /// ngaktifin setTextIsSelectable(true) di underlying TextView.
        /// - Long-press → context menu (Copy, Select All, Share) 
        /// - Double-tap → select word
        /// - Drag handle → expand selection
        /// </summary>
        private View BuildSelectableText(string message, Color textColor)
        {
            return new StokBarangMAUI.Controls.SelectableLabel
            {
                Text = message,
                TextColor = textColor,
                FontSize = 13,
                LineBreakMode = LineBreakMode.WordWrap,
            };
        }

        private static readonly System.Text.RegularExpressions.Regex UrlRegex =
            new(@"https?://[^\s]+", System.Text.RegularExpressions.RegexOptions.Compiled);

        private static FormattedString BuildFormattedMessage(string message, Color textColor, Color linkColor)
        {
            var fs = new FormattedString();
            int lastEnd = 0;

            foreach (System.Text.RegularExpressions.Match m in UrlRegex.Matches(message))
            {
                if (m.Index > lastEnd)
                {
                    fs.Spans.Add(new Span
                    {
                        Text = message.Substring(lastEnd, m.Index - lastEnd),
                        TextColor = textColor,
                        FontSize = 13
                    });
                }

                // Trim trailing punctuation that's usually NOT part of the URL
                var url = m.Value;
                int trimLen = 0;
                while (trimLen < url.Length && ".,;:!?)]}".IndexOf(url[url.Length - 1 - trimLen]) >= 0)
                    trimLen++;
                var cleanUrl = trimLen > 0 ? url.Substring(0, url.Length - trimLen) : url;
                var tail = trimLen > 0 ? url.Substring(url.Length - trimLen) : "";

                var linkSpan = new Span
                {
                    Text = cleanUrl,
                    TextColor = linkColor,
                    TextDecorations = TextDecorations.Underline,
                    FontSize = 13
                };
                var capturedUrl = cleanUrl;
                var tap = new TapGestureRecognizer();
                tap.Tapped += async (_, _) =>
                {
                    try { await Microsoft.Maui.ApplicationModel.Launcher.OpenAsync(new Uri(capturedUrl)); }
                    catch { /* invalid URL  ignore */ }
                };
                linkSpan.GestureRecognizers.Add(tap);
                fs.Spans.Add(linkSpan);

                if (tail.Length > 0)
                    fs.Spans.Add(new Span { Text = tail, TextColor = textColor, FontSize = 13 });

                lastEnd = m.Index + m.Length;
            }

            if (lastEnd < message.Length)
            {
                fs.Spans.Add(new Span
                {
                    Text = message.Substring(lastEnd),
                    TextColor = textColor,
                    FontSize = 13
                });
            }

            // Empty message guard  at least one empty span so Label has a FormattedString
            if (fs.Spans.Count == 0)
                fs.Spans.Add(new Span { Text = message, TextColor = textColor, FontSize = 13 });

            return fs;
        }

        private void AttachLongPressCopy(View target, string textToCopy)
        {
            System.Threading.CancellationTokenSource? cts = null;
            var ptr = new PointerGestureRecognizer();

            ptr.PointerPressed += (_, _) =>
            {
                cts?.Cancel();
                cts = new System.Threading.CancellationTokenSource();
                var token = cts.Token;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(500, token);
                        if (token.IsCancellationRequested) return;
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            await Clipboard.SetTextAsync(textToCopy);
                            await DisplayAlert("", "Teks disalin ke clipboard", "OK");
                        });
                    }
                    catch (TaskCanceledException) { /* released before threshold */ }
                });
            };
            ptr.PointerReleased += (_, _) => cts?.Cancel();
            ptr.PointerExited   += (_, _) => cts?.Cancel();

            target.GestureRecognizers.Add(ptr);
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
            AddAiMessage(" Halo! Saya AI assistant kamu. Tanya apa aja  soal pekerjaan, progress, stok, atau mau ngobrol santai juga boleh!");
        }

    }
}
