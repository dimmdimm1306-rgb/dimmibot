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
                ? Color.FromArgb("#0B1024")   // very dark navy (Stitch Titan dark)
                : Color.FromArgb("#3D5DDC");  // primary-container blue (Stitch Titan light)
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
                    // ── Dark mode ─ Stitch Titan adapted ─────────────────────
                    // Surface family (deep navy, low-blue tint)
                    res["PageBg"]      = Color.FromArgb("#0B1024");  // surface
                    res["CardBg"]      = Color.FromArgb("#141936");  // surface-container
                    res["CardBg2"]     = Color.FromArgb("#10142E");  // surface-container-low
                    res["TextPrimary"] = Color.FromArgb("#E6E7FF");  // on-primary-container
                    res["TextSecond"]  = Color.FromArgb("#B8C3FF");  // inverse-primary
                    res["TextMuted"]   = Color.FromArgb("#8B91B5");  // muted text
                    res["BorderClr"]   = Color.FromArgb("#2A3160");  // outline
                    res["DividerClr"]  = Color.FromArgb("#1A1F40");  // outline-variant
                    res["SearchBg"]    = Color.FromArgb("#141936");
                    res["HeaderBg"]    = Color.FromArgb("#0B1024");  // header darker than light
                    res["SubHeaderBg"] = Color.FromArgb("#0F1430");  // sub-header

                    // ── Accent colors (dark) — keyed to Stitch Titan primary ──
                    res["AccentBlueBg"]      = Color.FromArgb("#1F2A6B");  // primary-fixed-dim subtle
                    res["AccentBlueBorder"]  = Color.FromArgb("#3D5DDC");  // primary-container
                    res["AccentBlue"]        = Color.FromArgb("#1D42C3");  // primary
                    res["AccentGreenBg"]     = Color.FromArgb("#064E3B");  // status-ok bg
                    res["AccentGreenBorder"] = Color.FromArgb("#10B981");  // status-ok
                    res["AccentRedBg"]       = Color.FromArgb("#5B1717");  // status-nok bg
                    res["AccentRedBorder"]   = Color.FromArgb("#EF4444");  // status-nok

                    // ── Stat card text (dark) — readable on tinted bg ────────
                    res["StatBlueText"]   = Color.FromArgb("#B8C3FF");
                    res["StatGreenText"]  = Color.FromArgb("#6EE7B7");
                    res["StatRedText"]    = Color.FromArgb("#FCA5A5");
                    res["StatBlueLabel"]  = Color.FromArgb("#8AA0FF");
                    res["StatGreenLabel"] = Color.FromArgb("#34D399");
                    res["StatRedLabel"]   = Color.FromArgb("#F87171");

                    // ── Drawer / menu (dark) ────────────────────────────────
                    res["DrawerBg"]         = Color.FromArgb("#0B1024");
                    res["DrawerHeaderBg"]   = Color.FromArgb("#1D42C3");  // primary
                    res["DrawerItemBg"]     = Color.FromArgb("#141936");
                    res["DrawerItemBorder"] = Color.FromArgb("#2A3160");
                    res["DrawerBottomBg"]   = Color.FromArgb("#0F1430");

                    // ── Console / Bot (dark) ────────────────────────────────
                    res["ConsoleBg"]     = Color.FromArgb("#0F1430");
                    res["ConsoleBorder"] = Color.FromArgb("#2A3160");
                    res["ConsoleText"]   = Color.FromArgb("#B8C3FF");

                    // ── Header button tints (dark) ──────────────────────────
                    res["HeaderBtnBg"]     = Color.FromArgb("#1F2A6B");
                    res["HeaderBtnBorder"] = Color.FromArgb("#3D5DDC");
                    res["HeaderSubtitle"]  = Color.FromArgb("#B8C3FF");

                    // ── AI bot chip (dark) ──────────────────────────────────
                    res["AiBotBg"]     = Color.FromArgb("#6514D6");  // tertiary
                    res["AiBotBorder"] = Color.FromArgb("#7E3DEF");  // tertiary-container

                    // ── Tab bar (dark) ──────────────────────────────────────
                    res["TabBarBg"]              = Color.FromArgb("#0B1024");
                    res["TabBarText"]            = Color.FromArgb("#FFFFFF");
                    res["TabBarTextUnselected"]  = Color.FromArgb("#8B91B5");
                    res["TabBarSelected"]        = Color.FromArgb("#B8C3FF");

                    System.Diagnostics.Debug.WriteLine("[ThemeService] Dark mode colors applied");
                }
                else
                {
                    // ── Light mode ─ Stitch Titan ───────────────────────────
                    // Surface family (clean off-white with violet undertone)
                    res["PageBg"]      = Color.FromArgb("#FBF8FF");  // surface
                    res["CardBg"]      = Color.FromArgb("#FFFFFF");  // surface-container-lowest
                    res["CardBg2"]     = Color.FromArgb("#F4F2FE");  // surface-container-low
                    res["TextPrimary"] = Color.FromArgb("#1A1B23");  // on-surface
                    res["TextSecond"]  = Color.FromArgb("#444654");  // on-surface-variant
                    res["TextMuted"]   = Color.FromArgb("#6B7280");  // text-muted
                    res["BorderClr"]   = Color.FromArgb("#C4C5D6");  // outline-variant
                    res["DividerClr"]  = Color.FromArgb("#E2E1EC");  // surface-variant
                    res["SearchBg"]    = Color.FromArgb("#FFFFFF");
                    res["HeaderBg"]    = Color.FromArgb("#3D5DDC");  // background-header
                    res["SubHeaderBg"] = Color.FromArgb("#3D5DDC");

                    // ── Accent colors (light) — Stitch Titan primary ────────
                    res["AccentBlueBg"]      = Color.FromArgb("#DDE1FF");  // primary-fixed
                    res["AccentBlueBorder"]  = Color.FromArgb("#3D5DDC");  // primary-container
                    res["AccentBlue"]        = Color.FromArgb("#1D42C3");  // primary
                    res["AccentGreenBg"]     = Color.FromArgb("#D1FAE5");
                    res["AccentGreenBorder"] = Color.FromArgb("#10B981");  // status-ok
                    res["AccentRedBg"]       = Color.FromArgb("#FEE2E2");
                    res["AccentRedBorder"]   = Color.FromArgb("#EF4444");  // status-nok

                    // ── Stat card text (light) ── darker than bg ───────────
                    res["StatBlueText"]   = Color.FromArgb("#0737B9");  // on-primary-fixed-variant
                    res["StatGreenText"]  = Color.FromArgb("#047857");
                    res["StatRedText"]    = Color.FromArgb("#B91C1C");
                    res["StatBlueLabel"]  = Color.FromArgb("#1D42C3");
                    res["StatGreenLabel"] = Color.FromArgb("#10B981");
                    res["StatRedLabel"]   = Color.FromArgb("#EF4444");

                    // ── Drawer / menu (light) ───────────────────────────────
                    res["DrawerBg"]         = Color.FromArgb("#FBF8FF");
                    res["DrawerHeaderBg"]   = Color.FromArgb("#3D5DDC");
                    res["DrawerItemBg"]     = Color.FromArgb("#FFFFFF");
                    res["DrawerItemBorder"] = Color.FromArgb("#E2E1EC");
                    res["DrawerBottomBg"]   = Color.FromArgb("#F4F2FE");

                    // ── Console / Bot (light) ───────────────────────────────
                    res["ConsoleBg"]     = Color.FromArgb("#F4F2FE");
                    res["ConsoleBorder"] = Color.FromArgb("#C4C5D6");
                    res["ConsoleText"]   = Color.FromArgb("#444654");

                    // ── Header button tints (light) ─────────────────────────
                    res["HeaderBtnBg"]     = Color.FromArgb("#5470E0");  // lighter than HeaderBg
                    res["HeaderBtnBorder"] = Color.FromArgb("#B8C3FF");
                    res["HeaderSubtitle"]  = Color.FromArgb("#DDE1FF");

                    // ── AI bot chip (light) ─────────────────────────────────
                    res["AiBotBg"]     = Color.FromArgb("#6514D6");  // tertiary (violet)
                    res["AiBotBorder"] = Color.FromArgb("#7E3DEF");

                    // ── Tab bar (light) ─────────────────────────────────────
                    res["TabBarBg"]              = Color.FromArgb("#1A1F3D");  // background-tab
                    res["TabBarText"]            = Color.FromArgb("#FFFFFF");
                    res["TabBarTextUnselected"]  = Color.FromArgb("#B0B5D0");
                    res["TabBarSelected"]        = Color.FromArgb("#B8C3FF");

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
