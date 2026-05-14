#if ANDROID
using Android.Widget;
using Android.Text;
using Microsoft.Maui.Handlers;
using StokBarangMAUI.Controls;

namespace StokBarangMAUI.Platforms.Android
{
    /// <summary>
    /// Mapper yang aktifkan native Android text selection di TextView.
    /// Pakai LabelHandler (biar tetep pakai Label rendering), tambahin
    /// setTextIsSelectable(true) setelah handler connected.
    /// </summary>
    public static class SelectableLabelHandler
    {
        public static void Configure(IMauiHandlersCollection handlers)
        {
            handlers.AddHandler<SelectableLabel, Microsoft.Maui.Handlers.LabelHandler>();

            Microsoft.Maui.Handlers.LabelHandler.Mapper.AppendToMapping(
                "SelectableLabel.TextIsSelectable",
                (handler, view) =>
                {
                    if (view is SelectableLabel)
                    {
                        try
                        {
                            var tv = handler.PlatformView; // Android.Widget.TextView
                            tv.SetTextIsSelectable(true);
                            tv.LongClickable = true;
                            tv.Focusable = true;
                            tv.FocusableInTouchMode = true;
                            // Enable context menu with copy/select all
                            tv.CustomSelectionActionModeCallback = null; // use default Android menu
                        }
                        catch { }
                    }
                });
        }
    }
}
#endif
