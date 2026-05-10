using Android.App;
using Android.Content;
using Microsoft.Maui.Authentication;

namespace StokBarangMAUI.Platforms.Android
{
    // Receives the OAuth redirect from Google (custom scheme com.companyname.stokbarangmaui://oauth2redirect)
    // and forwards it back to MAUI's WebAuthenticator.
    // Google Android OAuth redirect URI format: "scheme:/path" (single slash, opaque URI).
    // Android intent system can only match by DataScheme for opaque URIs — host/path are null.
    [Activity(NoHistory = true, LaunchMode = global::Android.Content.PM.LaunchMode.SingleTop, Exported = true)]
    [IntentFilter(new[] { Intent.ActionView },
        Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
        DataScheme = "com.googleusercontent.apps.1046374458759-qud9n6fqcibk2372lneg6vjdj0ae7ure")]
    public class OAuthCallbackActivity : WebAuthenticatorCallbackActivity
    {
    }
}
