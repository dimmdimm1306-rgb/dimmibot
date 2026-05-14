using StokBarangMAUI.Pages;
using StokBarangMAUI.Services;
using StokBarangMAUI.Services.Notifications;

namespace StokBarangMAUI;

public partial class App : Application
{
    public static ThemeService Theme { get; private set; } = null!;

    private readonly DatabaseService      _db;
    private readonly GoogleSheetsService  _sheets;
    private readonly StockDatabaseService _stockDb;
    private readonly ProjectService       _projectService;
    private readonly AuthService          _auth;
    private readonly DraftService         _drafts;
    private readonly GoogleOAuthService   _gauth;
    private readonly UploadCoordinator    _upload;

    public App(DatabaseService db, GoogleSheetsService sheets,
               StockDatabaseService stockDb, ThemeService theme,
               ProjectService projectService, AuthService auth, DraftService drafts,
               GoogleOAuthService gauth, UploadCoordinator upload)
    {
        InitializeComponent();
        Theme           = theme;
        theme.Initialize();
        _db             = db;
        _sheets         = sheets;
        _stockDb        = stockDb;
        _projectService = projectService;
        _auth           = auth;
        _drafts         = drafts;
        _gauth          = gauth;
        _upload         = upload;

        // cleanup temporary files on startup (fire-and-forget)
        _ = CleanupAsync();

        // Pasang notif harian (idempotent — aman dipanggil tiap launch)
        _ = ScheduleRemindersAsync();
    }

    private async Task ScheduleRemindersAsync()
    {
        try
        {
            var sp = this.Handler?.MauiContext?.Services
                  ?? Application.Current?.Handler?.MauiContext?.Services;
            var scheduler = sp?.GetService(typeof(INotificationScheduler)) as INotificationScheduler;
            if (scheduler == null) return;

            await scheduler.RequestPermissionIfNeededAsync();
            scheduler.ScheduleDailyReminders();
        }
        catch { /* don't crash app on scheduler failure */ }
    }

    private async Task CleanupAsync()
    {
        try
        {
            await _drafts.CleanupOrphanedPhotosAsync();
            await _projectService.CleanupOrphanedCaches();
        }
        catch { /* ignore cleanup errors */ }
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var home = new ProjectsHomePage(_projectService, _sheets, _db, _stockDb, _auth, _drafts, _gauth, _upload);
        return new Window(new NavigationPage(home) { BarBackgroundColor = Theme.GetNavBarColor() });
    }
}
