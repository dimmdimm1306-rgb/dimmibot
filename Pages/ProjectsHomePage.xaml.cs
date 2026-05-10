using StokBarangMAUI.Models;
using StokBarangMAUI.Services;

namespace StokBarangMAUI.Pages
{
    public partial class ProjectsHomePage : ContentPage
    {
        private readonly ProjectService      _projectService;
        private readonly GoogleSheetsService _sheets;
        private readonly DatabaseService     _db;
        private readonly StockDatabaseService _stockDb;
        private readonly AuthService         _auth;
        private readonly DraftService        _drafts;
        private readonly GoogleOAuthService  _gauth;
        private readonly UploadCoordinator   _upload;

        public ProjectsHomePage(ProjectService projectService, GoogleSheetsService sheets,
                                DatabaseService db, StockDatabaseService stockDb,
                                AuthService auth, DraftService drafts,
                                GoogleOAuthService gauth, UploadCoordinator upload)
        {
            InitializeComponent();
            _projectService = projectService;
            _sheets         = sheets;
            _db             = db;
            _stockDb        = stockDb;
            _auth           = auth;
            _drafts         = drafts;
            _gauth          = gauth;
            _upload         = upload;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            LblTheme.Text = App.Theme.ThemeIcon;
            
            // Auto-login AuthService jika Google sudah signed in (session restore)
            if (_gauth.IsSignedIn && !string.IsNullOrWhiteSpace(_gauth.AccountEmail))
            {
                if (!_auth.IsLoggedIn || _auth.CurrentEmail != _gauth.AccountEmail)
                    _auth.TryLogin(_gauth.AccountEmail);
            }
            
            UpdateAuthUI();

            bool isAdmin = _auth.CanEdit;
            ToolbarAdd.IsVisible = isAdmin;

            // Non-admin: sync config dari remote admin (background)
            if (!isAdmin)
                _ = _projectService.TrySyncFromRemoteAsync(_sheets);

            await BuildProjectCards();
        }
        
        private void UpdateAuthUI()
        {
            if (_gauth.IsSignedIn)
            {
                // User sudah sign in dengan Google
                BtnGoogleSignIn.IsVisible = false;
                BtnUserProfile.IsVisible = true;
                LblUserEmail.Text = _gauth.AccountEmail ?? "User";
                
                // Show tombol tambah project hanya jika email ada di whitelist
                ToolbarAdd.IsVisible = _auth.CanEdit;
            }
            else
            {
                // User belum sign in
                BtnGoogleSignIn.IsVisible = _gauth.IsConfigured;
                BtnUserProfile.IsVisible = false;
                
                // Hide tombol tambah project jika belum sign in
                ToolbarAdd.IsVisible = false;
            }
        }

        private async Task BuildProjectCards()
        {
            ProjectList.Children.Clear();
            var projects = await _projectService.GetAllAsync();
            var res      = Application.Current?.Resources;
            var textMuted = res != null && res.TryGetValue("TextMuted", out var tm) ? (Color)tm : Color.FromArgb("#6B7280");

            if (projects.Count == 0)
            {
                ProjectList.Children.Add(new VerticalStackLayout
                {
                    HorizontalOptions = LayoutOptions.Center,
                    Margin = new Thickness(0, 60),
                    Spacing = 12,
                    Children =
                    {
                        new Label { Text = "📭", FontSize = 48, HorizontalOptions = LayoutOptions.Center },
                        new Label { Text = "Belum ada project", TextColor = textMuted,
                                    FontSize = 15, HorizontalOptions = LayoutOptions.Center },
                        new Label { Text = "Tap '+ Tambah Project Baru' untuk mulai",
                                    TextColor = textMuted,
                                    FontSize = 12, HorizontalOptions = LayoutOptions.Center }
                    }
                });
                return;
            }

            foreach (var project in projects)
            {
                try { ProjectList.Children.Add(BuildCard(project)); }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[ProjectsHome] BuildCard: {ex.Message}"); }
            }
        }

        private View BuildCard(ProjectConfig project)
        {
            var accentColor = string.IsNullOrWhiteSpace(project.Color)
                ? Color.FromArgb("#3B82F6") : Color.FromArgb(project.Color);
            var res         = Application.Current?.Resources;
            var cardBg      = res != null && res.TryGetValue("CardBg",      out var r1) ? (Color)r1 : Color.FromArgb("#1E293B");
            var borderClr   = res != null && res.TryGetValue("BorderClr",   out var r2) ? (Color)r2 : Color.FromArgb("#334155");
            var textPrimary = res != null && res.TryGetValue("TextPrimary", out var r3) ? (Color)r3 : Colors.White;
            var textMuted   = res != null && res.TryGetValue("TextMuted",   out var r4) ? (Color)r4 : Colors.Gray;

            var card = new Border
            {
                BackgroundColor = cardBg,
                StrokeShape     = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 18 },
                Stroke          = new SolidColorBrush(borderClr),
                StrokeThickness = 1,
                Shadow          = new Shadow { Brush = Colors.Black, Offset = new Point(0, 4),
                                              Radius = 12, Opacity = 0.15f }
            };

            var inner = new Grid { ColumnDefinitions = new ColumnDefinitionCollection(
                new ColumnDefinition { Width = new GridLength(6) },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }) };

            // Left color accent strip
            inner.Add(new BoxView
            {
                Color        = accentColor,
                CornerRadius = new CornerRadius(18, 0, 18, 0)
            }, 0, 0);

            // Content
            var content = new VerticalStackLayout { Padding = new Thickness(14, 14), Spacing = 6 };

            // Icon + name row
            var nameRow = new Grid { ColumnDefinitions = new ColumnDefinitionCollection(
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star }) };
            nameRow.Add(new Label { Text = project.Icon, FontSize = 22,
                                    VerticalOptions = LayoutOptions.Center, Margin = new Thickness(0,0,10,0) }, 0, 0);
            nameRow.Add(new Label { Text = project.Name, FontSize = 16, FontAttributes = FontAttributes.Bold,
                                    TextColor = textPrimary, VerticalOptions = LayoutOptions.Center }, 1, 0);
            content.Add(nameRow);

            if (!string.IsNullOrWhiteSpace(project.Description))
                content.Add(new Label { Text = project.Description, FontSize = 12,
                                        TextColor = textMuted, LineBreakMode = LineBreakMode.TailTruncation });

            // Sheet info badge
            var cardBg2 = res != null && res.TryGetValue("CardBg2", out var r5) ? (Color)r5 : Color.FromArgb("#001530");
            var badge = new Border
            {
                BackgroundColor = cardBg2,
                StrokeShape     = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 6 },
                Stroke          = new SolidColorBrush(borderClr),
                Padding         = new Thickness(8, 4),
                HorizontalOptions = LayoutOptions.Start
            };
            var idPreview = project.SpreadsheetId.Length > 8
                ? project.SpreadsheetId[..8] + "…"
                : project.SpreadsheetId;
            badge.Content = new Label
            {
                Text      = $"🗂 {project.SegmentGids.Count} segment  •  {idPreview}",
                FontSize  = 10,
                TextColor = textMuted
            };
            content.Add(badge);

            inner.Add(content, 1, 0);

            // Edit / delete buttons
            var btnCol = new VerticalStackLayout
            {
                Padding = new Thickness(0, 12, 10, 12),
                Spacing = 6,
                VerticalOptions = LayoutOptions.Center
            };

            var btnEdit = new Border
            {
                BackgroundColor = cardBg2,
                StrokeShape     = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
                Stroke          = new SolidColorBrush(borderClr),
                Padding         = new Thickness(10, 7)
            };
            btnEdit.Content = new Label { Text = "✏", FontSize = 14 };
            btnEdit.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(async () => await EditProject(project))
            });

            var btnDel = new Border
            {
                BackgroundColor = Color.FromArgb("#450A0A"),
                StrokeShape     = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
                Stroke          = new SolidColorBrush(Color.FromArgb("#7F1D1D")),
                Padding         = new Thickness(10, 7)
            };
            btnDel.Content = new Label { Text = "🗑", FontSize = 14 };
            btnDel.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(async () => await DeleteProject(project))
            });

            btnCol.Add(btnEdit);
            btnCol.Add(btnDel);
            btnCol.IsVisible = _auth.CanEdit;
            inner.Add(btnCol, 2, 0);
            card.Content = inner;

            // Tap whole card → open project
            card.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(async () => await OpenProject(project))
            });

            return card;
        }

        private async Task OpenProject(ProjectConfig project)
        {
            _sheets.SetProject(project);
            Application.Current!.Windows[0].Page =
                new NavigationPage(new RootTabbedPage(_db, _sheets, _stockDb, project, _projectService, _auth, _drafts, _gauth, _upload));
        }

        protected override bool OnBackButtonPressed()
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                bool exit = await DisplayAlert("Keluar", "Keluar dari aplikasi?", "Keluar", "Batal");
                if (exit) Application.Current?.Quit();
            });
            return true;
        }

        private async Task EditProject(ProjectConfig project)
        {
            await Navigation.PushAsync(new AddProjectPage(_projectService, project));
        }

        private async Task DeleteProject(ProjectConfig project)
        {
            bool confirm = await DisplayAlert(
                "Hapus Project",
                $"Hapus \"{project.Name}\"? Data konfigurasi akan dihapus.",
                "Hapus", "Batal");
            if (!confirm) return;
            await _projectService.DeleteAsync(project.Id);
            await BuildProjectCards();
        }

        private void OnThemeToggle(object sender, TappedEventArgs e)
        {
            App.Theme.Toggle();
            LblTheme.Text = App.Theme.ThemeIcon;
            // Rebuild cards to pick up new theme colors
            _ = BuildProjectCards();
        }

        private async void OnAddProject(object sender, TappedEventArgs e) =>
            await Navigation.PushAsync(new AddProjectPage(_projectService, null));
        
        private async void OnGoogleSignIn(object sender, TappedEventArgs e)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            
            var (ok, msg) = await _gauth.SignInAsync();
            if (ok)
            {
                // Setelah sign in, cek apakah email ada di whitelist untuk auto-login AuthService
                var email = _gauth.AccountEmail;
                if (!string.IsNullOrWhiteSpace(email))
                {
                    _auth.TryLogin(email);
                }
                
                await DisplayAlert("Sign In Berhasil", $"Selamat datang, {email}!", "OK");
                UpdateAuthUI();
            }
            else
            {
                await DisplayAlert("Sign In Gagal", msg, "OK");
            }
        }
        
        private async void OnUserProfileTapped(object sender, TappedEventArgs e)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            
            var action = await DisplayActionSheet(
                $"Akun: {_gauth.AccountEmail}",
                "Batal",
                "Sign Out",
                _auth.CanEdit ? "✓ Admin Access" : "👁 View Only");
            
            if (action == "Sign Out")
            {
                bool confirm = await DisplayAlert(
                    "Sign Out",
                    "Anda akan keluar dari akun Google. Fitur input tidak akan bisa diakses.",
                    "Sign Out",
                    "Batal");
                
                if (confirm)
                {
                    await _gauth.SignOutAsync();
                    _auth.Logout();
                    UpdateAuthUI();
                    await DisplayAlert("Sign Out", "Anda telah keluar dari akun Google.", "OK");
                }
            }
        }
    }
}
