namespace StokBarangMAUI.Services
{
    public class AuthService
    {
        // Whitelist email yang boleh edit. Tambah/edit di sini.
        private static readonly HashSet<string> Whitelist = new(StringComparer.OrdinalIgnoreCase)
        {
            "dimmdimm1306@gmail.com",
        };

        private const string PrefKey = "auth.email";

        public string? CurrentEmail { get; private set; }
        public bool    IsLoggedIn   => !string.IsNullOrWhiteSpace(CurrentEmail);
        public bool    CanEdit      => IsLoggedIn && Whitelist.Contains(CurrentEmail!);

        public AuthService()
        {
            var saved = Preferences.Get(PrefKey, "");
            if (!string.IsNullOrWhiteSpace(saved)) CurrentEmail = saved;
        }

        // Returns (ok, message). Semua email valid bisa login.
        // CanEdit hanya true untuk email whitelist (admin).
        public (bool Ok, string Msg) TryLogin(string email)
        {
            email = (email ?? "").Trim();
            if (string.IsNullOrWhiteSpace(email))             return (false, "Email tidak boleh kosong.");
            if (!email.Contains('@') || !email.Contains('.')) return (false, "Format email tidak valid.");

            CurrentEmail = email;
            Preferences.Set(PrefKey, email);
            return (true, "Login berhasil.");
        }

        public void Logout()
        {
            CurrentEmail = null;
            Preferences.Remove(PrefKey);
        }
        
        // Check if email is in whitelist (admin)
        public bool IsWhitelisted(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            return Whitelist.Contains(email.Trim());
        }

        public static IReadOnlyCollection<string> GetWhitelistSnapshot() => Whitelist;
    }
}
