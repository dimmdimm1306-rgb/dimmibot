using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;
using StokBarangMAUI.Pages;
using StokBarangMAUI.Services;
using StokBarangMAUI.Controls;

namespace StokBarangMAUI;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            .ConfigureMauiHandlers(handlers =>
            {
#if ANDROID
                handlers.AddHandler<WebView, StokBarangMAUI.Platforms.Android.CustomWebViewHandler>();
#endif
            });

        builder.Services.AddSingleton<DatabaseService>();
        builder.Services.AddSingleton<GoogleSheetsService>();
        builder.Services.AddSingleton<StockDatabaseService>();
        builder.Services.AddSingleton<ThemeService>();
        builder.Services.AddSingleton<ProjectService>();
        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddSingleton<DraftService>();
        builder.Services.AddSingleton<GoogleOAuthService>();
        builder.Services.AddSingleton<SheetsWriteService>();
        builder.Services.AddSingleton<DriveUploadService>();
        builder.Services.AddSingleton<UploadCoordinator>();
        builder.Services.AddSingleton<ApprovalService>();
        builder.Services.AddSingleton<OpenClawBotService>();
        builder.Services.AddSingleton<GDriveReaderService>();
        builder.Services.AddSingleton<GDriveCommandHandler>();
        builder.Services.AddSingleton<AiChatService>();
        builder.Services.AddTransient<MainPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
