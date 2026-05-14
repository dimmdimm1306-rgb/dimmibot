using Android.App;
using Android.Content;

namespace StokBarangMAUI.Platforms.Android.Notifications
{
    /// <summary>
    /// Re-pasang alarm setelah HP reboot atau app di-update.
    /// AlarmManager nge-reset semua pending alarm setelah reboot, jadi harus diset ulang.
    /// </summary>
    [BroadcastReceiver(Enabled = true, Exported = true)]
    [IntentFilter(new[] {
        Intent.ActionBootCompleted,
        Intent.ActionMyPackageReplaced,
        Intent.ActionPackageReplaced
    })]
    public class BootReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context? context, Intent? intent)
        {
            if (context == null) return;
            try
            {
                var scheduler = new AndroidNotificationScheduler();
                scheduler.ScheduleDailyReminders();
                global::Android.Util.Log.Info("BootReceiver", "Reminders rescheduled after boot/update");
            }
            catch (System.Exception ex)
            {
                global::Android.Util.Log.Error("BootReceiver", $"Failed to reschedule: {ex.Message}");
            }
        }
    }
}
