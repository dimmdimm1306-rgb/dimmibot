namespace StokBarangMAUI.Services
{
    public class ThemeService
    {
        public bool IsDark { get; private set; } = true;
        public string ThemeIcon => IsDark ? "☀" : "🌙";
        
        // Event untuk notify UI saat theme berubah
        public event Action? ThemeChanged;

        public ThemeService()
        {
            // Only read preference here — Application.Current is NOT ready yet
            var saved = Preferences.Get("app_theme", "dark");
            IsDark = saved != "light";
            
            System.Diagnostics.Debug.WriteLine($"[ThemeService] Initialized with theme: {(IsDark ? "Dark" : "Light")}");
        }

        /// <summary>Call from App.xaml.cs AFTER InitializeComponent().</summary>
        public void Initialize()
        {
            Apply();
            System.Diagnostics.Debug.WriteLine("[ThemeService] Theme applied on initialization");
        }

        public void Toggle()
        {
            IsDark = !IsDark;
            Preferences.Set("app_theme", IsDark ? "dark" : "light");
            System.Diagnostics.Debug.WriteLine($"[ThemeService] Theme toggled to: {(IsDark ? "Dark" : "Light")}");
            Apply();
            
            // Notify subscribers
            ThemeChanged?.Invoke();
        }

        /// <summary>Get the current NavigationPage bar background color.</summary>
        public Color GetNavBarColor()
        {
            return IsDark
                ? Color.FromArgb("#001224")
                : Color.FromArgb("#1D4ED8");
        }

        private void Apply()
        {
            if (Application.Current?.Resources is null)
            {
                System.Diagnostics.Debug.WriteLine("[ThemeService] Cannot apply theme - Application.Current.Resources is null");
                return;
            }

            var res = Application.Current.Resources;
            
            try
            {
                if (IsDark)
                {
                    // ── Dark mode ─ Deep navy blue ──────────────────────────
                    res["PageBg"]      = Color.FromArgb("#001224");
                    res["CardBg"]      = Color.FromArgb("#001e40");
                    res["CardBg2"]     = Color.FromArgb("#001530");
                    res["TextPrimary"] = Color.FromArgb("#d5e3ff");
                    res["TextSecond"]  = Color.FromArgb("#a7c8ff");
                    res["TextMuted"]   = Color.FromArgb("#799dd6");
                    res["BorderClr"]   = Color.FromArgb("#1f477b");
                    res["DividerClr"]  = Color.FromArgb("#001e40");
                    res["SearchBg"]    = Color.FromArgb("#001e40");
                    res["HeaderBg"]    = Color.FromArgb("#001224");
                    res["SubHeaderBg"] = Color.FromArgb("#001224");

                    // ── Accent colors (dark) ────────────────────────────────
                    res["AccentBlueBg"]      = Color.FromArgb("#1E3A6E");
                    res["AccentBlueBorder"]  = Color.FromArgb("#2563EB");
                    res["AccentBlue"]        = Color.FromArgb("#1D4ED8");
                    res["AccentGreenBg"]     = Color.FromArgb("#064E3B");
                    res["AccentGreenBorder"] = Color.FromArgb("#059669");
                    res["AccentRedBg"]       = Color.FromArgb("#7F1D1D");
                    res["AccentRedBorder"]   = Color.FromArgb("#DC2626");

                    // ── Stat card text (dark) ── readable glow ─────────────────
                    res["StatBlueText"]   = Color.FromArgb("#93C5FD");  // soft bright blue
                    res["StatGreenText"]  = Color.FromArgb("#6EE7B7");  // soft bright green
                    res["StatRedText"]    = Color.FromArgb("#FCA5A5");  // soft bright red
                    res["StatBlueLabel"]  = Color.FromArgb("#60A5FA");  // subtle blue label
                    res["StatGreenLabel"] = Color.FromArgb("#34D399");  // subtle green label
                    res["StatRedLabel"]   = Color.FromArgb("#F87171");  // subtle red label

                    // ── Drawer / menu (dark) ────────────────────────────────
                    res["DrawerBg"]         = Color.FromArgb("#0D1117");
                    res["DrawerHeaderBg"]   = Color.FromArgb("#161B22");
                    res["DrawerItemBg"]     = Color.FromArgb("#161B22");
                    res["DrawerItemBorder"] = Color.FromArgb("#21262D");
                    res["DrawerBottomBg"]   = Color.FromArgb("#161B22");

                    // ── Console / Bot (dark) ────────────────────────────────
                    res["ConsoleBg"]     = Color.FromArgb("#0F172A");
                    res["ConsoleBorder"] = Color.FromArgb("#1E293B");
                    res["ConsoleText"]   = Color.FromArgb("#94A3B8");

                    // ── Header button tints (dark) ──────────────────────────
                    res["HeaderBtnBg"]     = Color.FromArgb("#1E293B");
                    res["HeaderBtnBorder"] = Color.FromArgb("#334155");
                    res["HeaderSubtitle"]  = Color.FromArgb("#93C5FD");

                    System.Diagnostics.Debug.WriteLine("[ThemeService] Dark mode colors applied");
                }
                else
                {
                    // ── Light mode ─ Clean white / soft grey ────────────────
                    res["PageBg"]      = Color.FromArgb("#F0F2F5");
                    res["CardBg"]      = Color.FromArgb("#FFFFFF");
                    res["CardBg2"]     = Color.FromArgb("#F3F4F6");
                    res["TextPrimary"] = Color.FromArgb("#111827");
                    res["TextSecond"]  = Color.FromArgb("#374151");
                    res["TextMuted"]   = Color.FromArgb("#6B7280");
                    res["BorderClr"]   = Color.FromArgb("#D1D5DB");
                    res["DividerClr"]  = Color.FromArgb("#E5E7EB");
                    res["SearchBg"]    = Color.FromArgb("#FFFFFF");
                    res["HeaderBg"]    = Color.FromArgb("#1D4ED8");
                    res["SubHeaderBg"] = Color.FromArgb("#2563EB");

                    // ── Accent colors (light) ───────────────────────────────
                    res["AccentBlueBg"]      = Color.FromArgb("#DBEAFE");
                    res["AccentBlueBorder"]  = Color.FromArgb("#3B82F6");
                    res["AccentBlue"]        = Color.FromArgb("#2563EB");
                    res["AccentGreenBg"]     = Color.FromArgb("#D1FAE5");
                    res["AccentGreenBorder"] = Color.FromArgb("#10B981");
                    res["AccentRedBg"]       = Color.FromArgb("#FEE2E2");
                    res["AccentRedBorder"]   = Color.FromArgb("#EF4444");

                    // ── Stat card text (light) ── darker than bg ───────────────
                    res["StatBlueText"]   = Color.FromArgb("#1E40AF");  // deep blue number
                    res["StatGreenText"]  = Color.FromArgb("#047857");  // deep green number
                    res["StatRedText"]    = Color.FromArgb("#B91C1C");  // deep red number
                    res["StatBlueLabel"]  = Color.FromArgb("#2563EB");  // medium blue label
                    res["StatGreenLabel"] = Color.FromArgb("#059669");  // medium green label
                    res["StatRedLabel"]   = Color.FromArgb("#DC2626");  // medium red label

                    // ── Drawer / menu (light) ───────────────────────────────
                    res["DrawerBg"]         = Color.FromArgb("#F9FAFB");
                    res["DrawerHeaderBg"]   = Color.FromArgb("#1D4ED8");
                    res["DrawerItemBg"]     = Color.FromArgb("#FFFFFF");
                    res["DrawerItemBorder"] = Color.FromArgb("#E5E7EB");
                    res["DrawerBottomBg"]   = Color.FromArgb("#F3F4F6");

                    // ── Console / Bot (light) ───────────────────────────────
                    res["ConsoleBg"]     = Color.FromArgb("#F9FAFB");
                    res["ConsoleBorder"] = Color.FromArgb("#D1D5DB");
                    res["ConsoleText"]   = Color.FromArgb("#374151");

                    // ── Header button tints (light) ─────────────────────────
                    res["HeaderBtnBg"]     = Color.FromArgb("#3B82F6");
                    res["HeaderBtnBorder"] = Color.FromArgb("#60A5FA");
                    res["HeaderSubtitle"]  = Color.FromArgb("#BFDBFE");

                    System.Diagnostics.Debug.WriteLine("[ThemeService] Light mode colors applied");
                }

                // Update NavigationPage bar color if available
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        if (Application.Current?.Windows?.Count > 0)
                        {
                            var window = Application.Current.Windows[0];
                            if (window?.Page is NavigationPage navPage)
                            {
                                navPage.BarBackgroundColor = GetNavBarColor();
                            }
                        }
                        System.Diagnostics.Debug.WriteLine("[ThemeService] Triggering UI refresh");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ThemeService] UI refresh error: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ThemeService] Apply error: {ex.Message}");
            }
        }
    }
}
