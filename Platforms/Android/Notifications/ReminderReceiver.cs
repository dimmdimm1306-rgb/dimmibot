using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using StokBarangMAUI.Services.Notifications;

namespace StokBarangMAUI.Platforms.Android.Notifications
{
    /// <summary>
    /// BroadcastReceiver yang dipanggil AlarmManager pada jam yang dijadwalkan.
    /// Build & post notifikasi, lalu reschedule untuk hari berikutnya supaya repeating.
    /// </summary>
    [BroadcastReceiver(Enabled = true, Exported = false)]
    public class ReminderReceiver : BroadcastReceiver
    {
        public const string ACTION_REMINDER = "com.companyname.stokbarangmaui.REMINDER";
        public const string EXTRA_REQUEST_CODE = "request_code";
        public const string EXTRA_TITLE = "title";
        public const string EXTRA_BODY = "body";

        public override void OnReceive(Context? context, Intent? intent)
        {
            if (context == null || intent == null) return;

            int requestCode = intent.GetIntExtra(EXTRA_REQUEST_CODE, -1);
            string title = intent.GetStringExtra(EXTRA_TITLE) ?? "Pengingat";
            string body  = intent.GetStringExtra(EXTRA_BODY) ?? "";

            try
            {
                NotificationHelper.EnsureChannel(context);
                NotificationHelper.PostReminder(context, requestCode, title, body);

                // Reschedule untuk besok di jam yang sama
                AndroidNotificationScheduler.RescheduleNext(context, requestCode);
            }
            catch (System.Exception ex)
            {
                global::Android.Util.Log.Error("ReminderReceiver", $"OnReceive failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Helper untuk channel + posting notifikasi (centralised).
    /// </summary>
    internal static class NotificationHelper
    {
        public static void EnsureChannel(Context ctx)
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;

            var nm = (NotificationManager?)ctx.GetSystemService(Context.NotificationService);
            if (nm == null) return;

            var existing = nm.GetNotificationChannel(ReminderSchedule.CHANNEL_ID);
            if (existing != null) return;

            var channel = new NotificationChannel(
                ReminderSchedule.CHANNEL_ID,
                ReminderSchedule.CHANNEL_NAME,
                NotificationImportance.Default)
            {
                Description = "Pengingat harian: progres pagi & malam"
            };
            channel.EnableVibration(true);
            nm.CreateNotificationChannel(channel);
        }

        public static void PostReminder(Context ctx, int requestCode, string title, string body)
        {
            // Build intent yang launch MainActivity dengan extra nav_target
            var openIntent = new Intent(ctx, typeof(MainActivity));
            openIntent.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);
            openIntent.PutExtra(ReminderSchedule.EXTRA_NAV_TARGET, ReminderSchedule.NAV_TARGET_PROGRESS);
            openIntent.SetAction($"reminder_{requestCode}_{System.DateTime.UtcNow.Ticks}");

            var pi = PendingIntent.GetActivity(
                ctx, requestCode, openIntent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

            // Cari icon yg available (Maui auto-generate appicon)
            int iconRes = ctx.Resources?.GetIdentifier("appicon", "mipmap", ctx.PackageName) ?? 0;
            if (iconRes == 0)
                iconRes = global::Android.Resource.Drawable.IcDialogInfo;

            var builder = new NotificationCompat.Builder(ctx, ReminderSchedule.CHANNEL_ID)
                .SetSmallIcon(iconRes)
                .SetContentTitle(title)
                .SetContentText(body)
                .SetStyle(new NotificationCompat.BigTextStyle().BigText(body))
                .SetPriority(NotificationCompat.PriorityDefault)
                .SetAutoCancel(true)
                .SetContentIntent(pi);

            var nm = NotificationManagerCompat.From(ctx);
            // Skip kalau notif dibatasi (Android 13+ no permission)
            if (!nm.AreNotificationsEnabled()) return;

            nm.Notify(requestCode, builder.Build());
        }
    }
}
