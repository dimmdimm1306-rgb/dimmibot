namespace StokBarangMAUI.Services.Notifications
{
    /// <summary>
    /// Cross-platform contract untuk scheduler notifikasi pengingat harian.
    /// Implementasi platform-specific di Platforms/Android/Notifications/.
    /// </summary>
    public interface INotificationScheduler
    {
        /// <summary>Pasang/ubah jadwal notif harian. Idempotent (aman dipanggil berulang).</summary>
        void ScheduleDailyReminders();

        /// <summary>Batalkan semua notif terjadwal (untuk debug/disable).</summary>
        void CancelAll();

        /// <summary>Minta permission POST_NOTIFICATIONS (Android 13+).</summary>
        Task RequestPermissionIfNeededAsync();
    }

    /// <summary>
    /// Daftar reminder default. Jam dalam WIB (server pakai local time HP).
    /// </summary>
    public static class ReminderSchedule
    {
        // Pagi: cek site yang belum dikerjakan
        public const int MORNING_HOUR = 6;
        public const int MORNING_MINUTE = 0;
        public const string MORNING_TITLE = "🌅 Selamat Pagi";
        public const string MORNING_BODY  = "Cek site yang belum dikerjakan ya.";
        public const int MORNING_REQUEST_CODE = 1001;

        // Malam: pengingat input progres harian
        public const int EVENING_HOUR = 20;
        public const int EVENING_MINUTE = 0;
        public const string EVENING_TITLE = "🌙 Pengingat Progres";
        public const string EVENING_BODY  = "Progres harian jangan lupa di-cek!";
        public const int EVENING_REQUEST_CODE = 1002;

        // Channel ID
        public const string CHANNEL_ID = "ftth_reminders";
        public const string CHANNEL_NAME = "Pengingat Harian";

        // Intent extras (dipakai MainActivity untuk routing)
        public const string EXTRA_NAV_TARGET = "ftth_nav_target";
        public const string NAV_TARGET_PROGRESS = "progress";
        public const string PREF_NAV_TARGET = "pending_nav_target";
    }
}
