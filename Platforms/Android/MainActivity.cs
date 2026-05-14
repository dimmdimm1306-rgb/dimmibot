using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using StokBarangMAUI.Services.Notifications;

namespace StokBarangMAUI;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        CaptureNavTarget(Intent);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        // App sudah jalan dan user tap notif lagi → update Intent supaya OnAppearing pickup
        if (intent != null) Intent = intent;
        CaptureNavTarget(intent);
    }

    /// <summary>
    /// Kalau notification dibuka, intent berisi extra ftth_nav_target.
    /// Simpan ke Preferences supaya RootTabbedPage bisa baca dan switch tab.
    /// </summary>
    private static void CaptureNavTarget(Intent? intent)
    {
        if (intent == null) return;
        var target = intent.GetStringExtra(ReminderSchedule.EXTRA_NAV_TARGET);
        if (string.IsNullOrEmpty(target)) return;

        try
        {
            Preferences.Set(ReminderSchedule.PREF_NAV_TARGET, target);
        }
        catch { /* preferences not ready yet */ }
    }
}
