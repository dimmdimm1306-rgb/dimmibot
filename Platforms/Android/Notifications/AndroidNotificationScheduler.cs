using Android.App;
using Android.Content;
using Android.OS;
using StokBarangMAUI.Services.Notifications;

namespace StokBarangMAUI.Platforms.Android.Notifications
{
    /// <summary>
    /// Implementation Android pakai AlarmManager (set + reschedule manual after fire).
    /// Lebih ringan dari WorkManager karena cuma 2 alarm/hari, gak butuh persistence kompleks.
    /// </summary>
    public class AndroidNotificationScheduler : INotificationScheduler
    {
        public void ScheduleDailyReminders()
        {
            var ctx = global::Android.App.Application.Context;
            NotificationHelper.EnsureChannel(ctx);

            // Pagi
            ScheduleAt(ctx,
                ReminderSchedule.MORNING_REQUEST_CODE,
                ReminderSchedule.MORNING_HOUR,
                ReminderSchedule.MORNING_MINUTE,
                ReminderSchedule.MORNING_TITLE,
                ReminderSchedule.MORNING_BODY);

            // Malam
            ScheduleAt(ctx,
                ReminderSchedule.EVENING_REQUEST_CODE,
                ReminderSchedule.EVENING_HOUR,
                ReminderSchedule.EVENING_MINUTE,
                ReminderSchedule.EVENING_TITLE,
                ReminderSchedule.EVENING_BODY);
        }

        public void CancelAll()
        {
            var ctx = global::Android.App.Application.Context;
            CancelOne(ctx, ReminderSchedule.MORNING_REQUEST_CODE);
            CancelOne(ctx, ReminderSchedule.EVENING_REQUEST_CODE);
        }

        public async Task RequestPermissionIfNeededAsync()
        {
            // Android 13+ butuh runtime permission POST_NOTIFICATIONS.
            // Manifest sudah declare; OS akan auto-prompt saat NotificationCompat.From().Notify()
            // dipanggil pertama kali. Tapi biar lebih aman, kita explicit minta lewat
            // ActivityCompat.RequestPermissions kalau belum granted.
            try
            {
                if (Build.VERSION.SdkInt < BuildVersionCodes.Tiramisu)
                    return; // Android < 13: granted by manifest

                var ctx = global::Android.App.Application.Context;
                var status = AndroidX.Core.Content.ContextCompat.CheckSelfPermission(
                    ctx, global::Android.Manifest.Permission.PostNotifications);

                if (status == global::Android.Content.PM.Permission.Granted) return;

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
                    if (activity != null)
                    {
                        AndroidX.Core.App.ActivityCompat.RequestPermissions(activity,
                            new[] { global::Android.Manifest.Permission.PostNotifications }, 1100);
                    }
                });
            }
            catch { /* permission request best-effort */ }
            await Task.CompletedTask;
        }

        // ── Internal: dipanggil dari ReminderReceiver buat reschedule besok ───
        internal static void RescheduleNext(Context ctx, int requestCode)
        {
            (int hour, int minute, string title, string body) = requestCode switch
            {
                ReminderSchedule.MORNING_REQUEST_CODE => (
                    ReminderSchedule.MORNING_HOUR, ReminderSchedule.MORNING_MINUTE,
                    ReminderSchedule.MORNING_TITLE, ReminderSchedule.MORNING_BODY),
                ReminderSchedule.EVENING_REQUEST_CODE => (
                    ReminderSchedule.EVENING_HOUR, ReminderSchedule.EVENING_MINUTE,
                    ReminderSchedule.EVENING_TITLE, ReminderSchedule.EVENING_BODY),
                _ => (0, 0, "", "")
            };
            if (hour == 0 && minute == 0 && title == "") return;

            ScheduleAt(ctx, requestCode, hour, minute, title, body);
        }

        // ── Internal: pasang 1 alarm ke jam tertentu (next occurrence) ───────
        internal static void ScheduleAt(Context ctx, int requestCode, int hour, int minute,
            string title, string body)
        {
            var am = (AlarmManager?)ctx.GetSystemService(Context.AlarmService);
            if (am == null) return;

            var intent = new Intent(ctx, typeof(ReminderReceiver));
            intent.SetAction(ReminderReceiver.ACTION_REMINDER);
            intent.PutExtra(ReminderReceiver.EXTRA_REQUEST_CODE, requestCode);
            intent.PutExtra(ReminderReceiver.EXTRA_TITLE, title);
            intent.PutExtra(ReminderReceiver.EXTRA_BODY, body);

            var pi = PendingIntent.GetBroadcast(ctx, requestCode, intent,
                PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
            if (pi == null) return;

            // Hitung trigger time — next occurrence (kalau jam udah lewat hari ini, ke besok)
            var now = Java.Util.Calendar.Instance!;
            var target = (Java.Util.Calendar)now.Clone()!;
            target.Set(Java.Util.CalendarField.HourOfDay, hour);
            target.Set(Java.Util.CalendarField.Minute, minute);
            target.Set(Java.Util.CalendarField.Second, 0);
            target.Set(Java.Util.CalendarField.Millisecond, 0);

            if (target.TimeInMillis <= now.TimeInMillis)
                target.Add(Java.Util.CalendarField.DayOfYear, 1);

            long trigger = target.TimeInMillis;

            // Pakai SetAndAllowWhileIdle untuk Doze mode (lebih reliable di Android 6+)
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                try
                {
                    am.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, trigger, pi);
                }
                catch
                {
                    // Some OEMs block SetExact — fallback inexact
                    am.SetAndAllowWhileIdle(AlarmType.RtcWakeup, trigger, pi);
                }
            }
            else
            {
                am.SetExact(AlarmType.RtcWakeup, trigger, pi);
            }
        }

        private static void CancelOne(Context ctx, int requestCode)
        {
            var am = (AlarmManager?)ctx.GetSystemService(Context.AlarmService);
            if (am == null) return;

            var intent = new Intent(ctx, typeof(ReminderReceiver));
            intent.SetAction(ReminderReceiver.ACTION_REMINDER);

            var pi = PendingIntent.GetBroadcast(ctx, requestCode, intent,
                PendingIntentFlags.NoCreate | PendingIntentFlags.Immutable);
            if (pi != null) am.Cancel(pi);
        }
    }
}
