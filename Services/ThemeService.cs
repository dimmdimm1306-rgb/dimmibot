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
                ? Color.FromArgb("#07111F")
                : Color.FromArgb("#0EA5E9");
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
                    res["PageBg"]      = Color.FromArgb("#07111F");
                    res["CardBg"]      = Color.FromArgb("#0E1A2B");
                    res["CardBg2"]     = Color.FromArgb("#10243A");
                    res["TextPrimary"] = Color.FromArgb("#F6FBFF");
                    res["TextSecond"]  = Color.FromArgb("#B8D7EA");
                    res["TextMuted"]   = Color.FromArgb("#7FA1B9");
                    res["BorderClr"]   = Color.FromArgb("#1C3950");
                    res["DividerClr"]  = Color.FromArgb("#123047");
                    res["SearchBg"]    = Color.FromArgb("#0B1B2D");
                    res["HeaderBg"]    = Color.FromArgb("#07111F");
                    res["SubHeaderBg"] = Color.FromArgb("#0B1B2D");

                    // ── Accent colors (dark) — keyed to Stitch Titan primary ──
                    res["AccentBlueBg"]      = Color.FromArgb("#0B2A4A");
                    res["AccentBlueBorder"]  = Color.FromArgb("#0EA5E9");
                    res["AccentBlue"]        = Color.FromArgb("#38BDF8");
                    res["AccentGreenBg"]     = Color.FromArgb("#073B31");
                    res["AccentGreenBorder"] = Color.FromArgb("#10B981");  // status-ok
                    res["AccentRedBg"]       = Color.FromArgb("#4C151D");
                    res["AccentRedBorder"]   = Color.FromArgb("#F43F5E");

                    // ── Stat card text (dark) — readable on tinted bg ────────
                    res["StatBlueText"]   = Color.FromArgb("#A5F3FC");
                    res["StatGreenText"]  = Color.FromArgb("#6EE7B7");
                    res["StatRedText"]    = Color.FromArgb("#FCA5A5");
                    res["StatBlueLabel"]  = Color.FromArgb("#67E8F9");
                    res["StatGreenLabel"] = Color.FromArgb("#34D399");
                    res["StatRedLabel"]   = Color.FromArgb("#F87171");

                    // ── Stat value colors (dark: bright on dark bg) ──────────
                    res["StatBlueValue"]   = Color.FromArgb("#22D3EE");
                    res["StatOrangeValue"] = Color.FromArgb("#FBBF24");  // amber-400
                    res["StatPurpleValue"] = Color.FromArgb("#C084FC");  // purple-400
                    res["StatGreenValue"]  = Color.FromArgb("#4ADE80");  // green-400

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
                    res["HeaderBtnBg"]     = Color.FromArgb("#0B2A4A");
                    res["HeaderBtnBorder"] = Color.FromArgb("#0EA5E9");
                    res["HeaderSubtitle"]  = Color.FromArgb("#A5F3FC");
                    res["SurfaceRaised"]   = Color.FromArgb("#14243A");
                    res["FiberCyan"]       = Color.FromArgb("#22D3EE");
                    res["FiberBlue"]       = Color.FromArgb("#38BDF8");
                    res["FiberGreen"]      = Color.FromArgb("#34D399");
                    res["SuccessBg"]       = Color.FromArgb("#073B31");
                    res["SuccessText"]     = Color.FromArgb("#6EE7B7");
                    res["WarningBg"]       = Color.FromArgb("#422006");
                    res["WarningText"]     = Color.FromArgb("#FCD34D");
                    res["DangerBg"]        = Color.FromArgb("#4C151D");
                    res["DangerText"]      = Color.FromArgb("#FDA4AF");
                    res["ChipBg"]          = Color.FromArgb("#10243A");
                    res["ChipSelectedBg"]  = Color.FromArgb("#0E7490");

                    // ── AI bot chip (dark) ──────────────────────────────────
                    res["AiBotBg"]     = Color.FromArgb("#6514D6");  // tertiary
                    res["AiBotBorder"] = Color.FromArgb("#7E3DEF");  // tertiary-container

                    // ── Tab bar (dark) ──────────────────────────────────────
                    res["TabBarBg"]              = Color.FromArgb("#07111F");
                    res["TabBarText"]            = Color.FromArgb("#FFFFFF");
                    res["TabBarTextUnselected"]  = Color.FromArgb("#8B91B5");
                    res["TabBarSelected"]        = Color.FromArgb("#22D3EE");

                    System.Diagnostics.Debug.WriteLine("[ThemeService] Dark mode colors applied");
                }
                else
                {
                    // ── Light mode ─ Stitch Titan ───────────────────────────
                    // Surface family (clean off-white with violet undertone)
                    res["PageBg"]      = Color.FromArgb("#F6FAFD");
                    res["CardBg"]      = Color.FromArgb("#FFFFFF");
                    res["CardBg2"]     = Color.FromArgb("#EEF7FB");
                    res["TextPrimary"] = Color.FromArgb("#06121F");
                    res["TextSecond"]  = Color.FromArgb("#27445A");
                    res["TextMuted"]   = Color.FromArgb("#64798A");
                    res["BorderClr"]   = Color.FromArgb("#C8DDE8");
                    res["DividerClr"]  = Color.FromArgb("#DCEAF1");
                    res["SearchBg"]    = Color.FromArgb("#FFFFFF");
                    res["HeaderBg"]    = Color.FromArgb("#0EA5E9");
                    res["SubHeaderBg"] = Color.FromArgb("#E0F7FF");

                    // ── Accent colors (light) — Stitch Titan primary ────────
                    res["AccentBlueBg"]      = Color.FromArgb("#E0F7FF");
                    res["AccentBlueBorder"]  = Color.FromArgb("#0EA5E9");
                    res["AccentBlue"]        = Color.FromArgb("#0369A1");
                    res["AccentGreenBg"]     = Color.FromArgb("#D1FAE5");
                    res["AccentGreenBorder"] = Color.FromArgb("#10B981");  // status-ok
                    res["AccentRedBg"]       = Color.FromArgb("#FEE2E2");
                    res["AccentRedBorder"]   = Color.FromArgb("#F43F5E");

                    // ── Stat card text (light) ── darker than bg ───────────
                    res["StatBlueText"]   = Color.FromArgb("#0369A1");
                    res["StatGreenText"]  = Color.FromArgb("#047857");
                    res["StatRedText"]    = Color.FromArgb("#B91C1C");
                    res["StatBlueLabel"]  = Color.FromArgb("#0EA5E9");
                    res["StatGreenLabel"] = Color.FromArgb("#10B981");
                    res["StatRedLabel"]   = Color.FromArgb("#EF4444");

                    // ── Stat value colors (light: darker/saturated for contrast on white bg) ──
                    res["StatBlueValue"]   = Color.FromArgb("#0891B2");
                    res["StatOrangeValue"] = Color.FromArgb("#C2410C");  // orange-700
                    res["StatPurpleValue"] = Color.FromArgb("#7E22CE");  // purple-700
                    res["StatGreenValue"]  = Color.FromArgb("#15803D");  // green-700

                    // ── Drawer / menu (light) ───────────────────────────────
                    res["DrawerBg"]         = Color.FromArgb("#F6FAFD");
                    res["DrawerHeaderBg"]   = Color.FromArgb("#0EA5E9");
                    res["DrawerItemBg"]     = Color.FromArgb("#FFFFFF");
                    res["DrawerItemBorder"] = Color.FromArgb("#DCEAF1");
                    res["DrawerBottomBg"]   = Color.FromArgb("#EEF7FB");

                    // ── Console / Bot (light) ───────────────────────────────
                    res["ConsoleBg"]     = Color.FromArgb("#EEF7FB");
                    res["ConsoleBorder"] = Color.FromArgb("#C8DDE8");
                    res["ConsoleText"]   = Color.FromArgb("#27445A");

                    // ── Header button tints (light) ─────────────────────────
                    res["HeaderBtnBg"]     = Color.FromArgb("#0369A1");
                    res["HeaderBtnBorder"] = Color.FromArgb("#A5F3FC");
                    res["HeaderSubtitle"]  = Color.FromArgb("#E0F7FF");
                    res["SurfaceRaised"]   = Color.FromArgb("#EAF6FB");
                    res["FiberCyan"]       = Color.FromArgb("#0891B2");
                    res["FiberBlue"]       = Color.FromArgb("#0284C7");
                    res["FiberGreen"]      = Color.FromArgb("#059669");
                    res["SuccessBg"]       = Color.FromArgb("#D1FAE5");
                    res["SuccessText"]     = Color.FromArgb("#047857");
                    res["WarningBg"]       = Color.FromArgb("#FEF3C7");
                    res["WarningText"]     = Color.FromArgb("#B45309");
                    res["DangerBg"]        = Color.FromArgb("#FFE4E6");
                    res["DangerText"]      = Color.FromArgb("#BE123C");
                    res["ChipBg"]          = Color.FromArgb("#EAF6FB");
                    res["ChipSelectedBg"]  = Color.FromArgb("#0EA5E9");

                    // ── AI bot chip (light) ─────────────────────────────────
                    res["AiBotBg"]     = Color.FromArgb("#6514D6");  // tertiary (violet)
                    res["AiBotBorder"] = Color.FromArgb("#7E3DEF");

                    // ── Tab bar (light) ─────────────────────────────────────
                    res["TabBarBg"]              = Color.FromArgb("#082F49");
                    res["TabBarText"]            = Color.FromArgb("#FFFFFF");
                    res["TabBarTextUnselected"]  = Color.FromArgb("#B0B5D0");
                    res["TabBarSelected"]        = Color.FromArgb("#67E8F9");

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
