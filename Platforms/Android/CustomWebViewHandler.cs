using Microsoft.Maui.Handlers;

namespace StokBarangMAUI.Platforms.Android
{
    public class CustomWebViewHandler : WebViewHandler
    {
        protected override global::Android.Webkit.WebView CreatePlatformView()
        {
            var view = base.CreatePlatformView();
            view.Settings.JavaScriptEnabled        = true;
            view.Settings.DomStorageEnabled        = true;
            view.Settings.LoadsImagesAutomatically = true;
            view.Settings.MixedContentMode         = global::Android.Webkit.MixedContentHandling.AlwaysAllow;
            // Chrome UA so Google Drive preview/embed loads without login prompt
            view.Settings.UserAgentString =
                "Mozilla/5.0 (Linux; Android 10; Mobile) AppleWebKit/537.36 " +
                "(KHTML, like Gecko) Chrome/124.0.0.0 Mobile Safari/537.36";
            return view;
        }
    }
}
