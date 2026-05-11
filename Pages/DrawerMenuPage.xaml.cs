using StokBarangMAUI.Models;
using StokBarangMAUI.Services;

namespace StokBarangMAUI.Pages
{
    public partial class DrawerMenuPage : ContentPage
    {
        private readonly RootTabbedPage    _tabbed;
        private readonly ProjectConfig     _project;
        private readonly ProjectService    _projectService;
        private readonly GoogleSheetsService _sheets;
        private readonly DatabaseService   _db;
        private readonly StockDatabaseService _stockDb;
        private readonly AuthService       _auth;
        private readonly DraftService      _drafts;
        private readonly GoogleOAuthService _gauth;
        private readonly UploadCoordinator  _upload;
        private readonly DriveUploadService _drive;

        public DrawerMenuPage(RootTabbedPage tabbed, ProjectConfig project,
                              ProjectService projectService, GoogleSheetsService sheets,
                              DatabaseService db, StockDatabaseService stockDb,
                              AuthService auth, DraftService drafts,
                              GoogleOAuthService gauth, UploadCoordinator upload,
                              DriveUploadService drive)
        {
            InitializeComponent();
            _tabbed         = tabbed;
            _project        = project;
            _projectService = projectService;
            _sheets         = sheets;
            _db             = db;
            _stockDb        = stockDb;
            _auth           = auth;
            _drafts         = drafts;
            _gauth          = gauth;
            _upload         = upload;
            _drive          = drive;

            LblProjectName.Text = project.Name;
            LblProjectDesc.Text = string.IsNullOrWhiteSpace(project.Description)
                ? $"{project.SegmentGids.Count} segment dikonfigurasi"
                : project.Description;

            HighlightActiveTab();
        }

        private void HighlightActiveTab()
        {
            var activeBg    = Color.FromArgb("#1D4ED820");
            var activeBorder= Color.FromArgb("#1D4ED8");
            var borders     = new[] { NavSJ, NavProg, NavStokA, NavStokG, NavInput };
            int active      = _tabbed.Children.IndexOf(_tabbed.CurrentPage);

            for (int i = 0; i < borders.Length; i++)
            {
                if (i == active)
                {
                    borders[i].BackgroundColor = activeBg;
                    borders[i].Stroke          = new SolidColorBrush(activeBorder);
                }
            }
        }

        private async Task SelectTab(int idx)
        {
            if (idx >= 0 && idx < _tabbed.Children.Count)
                _tabbed.CurrentPage = _tabbed.Children[idx];
            await Navigation.PopModalAsync(false);
        }

        private async void OnClose(object s, TappedEventArgs e)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            await Navigation.PopModalAsync(false);
        }

        private async void OnNavSuratJalan(object s, TappedEventArgs e)
        { HapticFeedback.Default.Perform(HapticFeedbackType.Click); await SelectTab(0); }
        private async void OnNavProgress(object s, TappedEventArgs e)
        { HapticFeedback.Default.Perform(HapticFeedbackType.Click); await SelectTab(1); }
        private async void OnNavStokAktual(object s, TappedEventArgs e)
        { HapticFeedback.Default.Perform(HapticFeedbackType.Click); await SelectTab(2); }
        private async void OnNavStokGudang(object s, TappedEventArgs e)
        { HapticFeedback.Default.Perform(HapticFeedbackType.Click); await SelectTab(3); }
        private async void OnNavInput(object s, TappedEventArgs e)
        { HapticFeedback.Default.Perform(HapticFeedbackType.Click); await SelectTab(4); }

        private async void OnNavAbsensi(object s, TappedEventArgs e)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            
            // Cek apakah user sudah sign in dengan Google
            if (!_gauth.IsSignedIn)
            {
                await DisplayAlert("Sign In Diperlukan", 
                    "Fitur Kegiatan/Absensi memerlukan akses Google Drive.\n\nSilakan sign in terlebih dahulu dari halaman Input.", 
                    "OK");
                return;
            }
            
            await Navigation.PopModalAsync(false);
            await _tabbed.Navigation.PushAsync(new AbsensiFeedPage(_drafts, _project, _gauth, _drive, _sheets));
        }

        private async void OnBackToProjects(object s, TappedEventArgs e)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            await Navigation.PopModalAsync(false);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Application.Current!.Windows[0].Page =
                    new NavigationPage(new ProjectsHomePage(_projectService, _sheets, _db, _stockDb, _auth, _drafts, _gauth, _upload))
                    { BarBackgroundColor = App.Theme.GetNavBarColor() };
            });
        }

        private async void OnNavAbout(object s, TappedEventArgs e)
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            await Navigation.PopModalAsync(false);
            await _tabbed.Navigation.PushAsync(new AboutPage());
        }

    }
}
