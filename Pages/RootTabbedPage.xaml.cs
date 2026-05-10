using Microsoft.Extensions.DependencyInjection;
using StokBarangMAUI.Models;
using StokBarangMAUI.Pages;
using StokBarangMAUI.Services;
#if ANDROID
using Android.Views;
using Google.Android.Material.BottomNavigation;
#endif

namespace StokBarangMAUI.Pages
{
    public partial class RootTabbedPage : TabbedPage
    {
        private readonly ProjectService       _projectService;
        private readonly GoogleSheetsService  _sheets;
        private readonly DatabaseService      _db;
        private readonly StockDatabaseService _stockDb;
        private readonly ProjectConfig        _project;
        private readonly AuthService          _auth;
        private readonly DraftService         _drafts;
        private readonly GoogleOAuthService   _gauth;
        private readonly UploadCoordinator    _upload;

        // Pages call this to open the drawer from their own hamburger button
        public static Func<Task>? OpenDrawer { get; private set; }

        public RootTabbedPage(DatabaseService db, GoogleSheetsService sheets,
                              StockDatabaseService stockDb, ProjectConfig project,
                              ProjectService projectService,
                              AuthService auth, DraftService drafts,
                              GoogleOAuthService gauth, UploadCoordinator upload)
        {
            InitializeComponent();
            _projectService = projectService;
            _sheets         = sheets;
            _db             = db;
            _stockDb        = stockDb;
            _project        = project;
            _auth           = auth;
            _drafts         = drafts;
            _gauth          = gauth;
            _upload         = upload;

            Children.Add(new NavigationPage(new SuratJalanPage(sheets))        { Title = "Surat Jalan" });
            Children.Add(new NavigationPage(new ProgressPage(sheets, project))  { Title = "Progress" });
            Children.Add(new NavigationPage(new StokAktualPage(sheets))         { Title = "Stok Diterima" });
            Children.Add(new NavigationPage(new StokGudangPage(sheets))         { Title = "Stok Gudang" });
            // Resolve services dari DI container
            var drive = ((App)Application.Current!).Handler!.MauiContext!.Services.GetRequiredService<DriveUploadService>();
            var approval = ((App)Application.Current!).Handler!.MauiContext!.Services.GetRequiredService<ApprovalService>();
            Children.Add(new NavigationPage(new InputHubPage(drafts, project, sheets, gauth, upload, drive, auth, approval)) { Title = "Input" });

            OpenDrawer = OpenDrawerAsync;

            HandlerChanged += OnHandlerChanged;

            // Inject project context into AI agent
            var aiService = ((App)Application.Current!).Handler!.MauiContext!.Services.GetRequiredService<AiChatService>();
            aiService.SetProjectContext(sheets, project);

            // Sinkronisasi nama project dari config sheet (background, silent)
            _ = SyncProjectNameAsync();
        }

        private void OnHandlerChanged(object? sender, EventArgs e)
        {
#if ANDROID
            if (Handler?.PlatformView is Android.Views.View rootView)
            {
                var nav = FindBottomNav(rootView);
                if (nav != null)
                    nav.Visibility = ViewStates.Gone;
            }
#endif
        }

#if ANDROID
        private static BottomNavigationView? FindBottomNav(Android.Views.View view)
        {
            if (view is BottomNavigationView bnv) return bnv;
            if (view is Android.Views.ViewGroup vg)
                for (int i = 0; i < vg.ChildCount; i++)
                {
                    var child = vg.GetChildAt(i);
                    if (child == null) continue;
                    var found = FindBottomNav(child);
                    if (found != null) return found;
                }
            return null;
        }
#endif

        private async Task SyncProjectNameAsync()
        {
            try
            {
                var (name, desc) = await _sheets.FetchProjectAlamatAsync();
                if (string.IsNullOrWhiteSpace(name)) return;

                bool changed = false;
                if (name != _project.Name) { _project.Name = name; changed = true; }
                if (!string.IsNullOrWhiteSpace(desc) && desc != _project.Description)
                { _project.Description = desc; changed = true; }
                if (!changed) return;

                await _projectService.SaveAsync(_project);
                ProjectNameUpdated?.Invoke(_project);
            }
            catch { /* silent — tidak ganggu UX */ }
        }

        // Pages may subscribe to refresh their displayed project title when sync completes.
        public static event Action<ProjectConfig>? ProjectNameUpdated;

        private async Task OpenDrawerAsync()
        {
            var drive = ((App)Application.Current!).Handler!.MauiContext!.Services.GetRequiredService<DriveUploadService>();
            var drawer = new DrawerMenuPage(this, _project, _projectService, _sheets, _db, _stockDb, _auth, _drafts, _gauth, _upload, drive);
            await Navigation.PushModalAsync(drawer, false);
        }

        protected override bool OnBackButtonPressed()
        {
            if (CurrentPage is NavigationPage navPage && navPage.Navigation.NavigationStack.Count > 1)
                return base.OnBackButtonPressed();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Application.Current!.Windows[0].Page =
                    new NavigationPage(new ProjectsHomePage(_projectService, _sheets, _db, _stockDb, _auth, _drafts, _gauth, _upload))
                    { BarBackgroundColor = App.Theme.GetNavBarColor() };
            });
            return true;
        }
    }
}
