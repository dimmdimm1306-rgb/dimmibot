#if ANDROID
using Android.Content;
using Android.OS;

namespace StokBarangMAUI.Platforms.Android
{
    public static class WhatsAppShare
    {
        // Authority harus match dengan AndroidManifest.xml provider declaration.
        private const string Authority = "com.companyname.stokbarangmaui.shareprovider";

        // Share 1+ foto plus caption ke WhatsApp dalam SATU chat send.
        // Target WhatsApp langsung supaya caption auto-fill di kotak pesan.
        // Fallback ke share sheet kalau WhatsApp tidak terinstall.
        public static void ShareImagesWithText(IList<string> imagePaths, string caption)
        {
            if (imagePaths == null || imagePaths.Count == 0) return;
            var ctx = global::Android.App.Application.Context;
            var pm  = ctx.PackageManager!;

            // Build URI list
            var uris = new List<IParcelable>();
            foreach (var p in imagePaths)
            {
                if (string.IsNullOrWhiteSpace(p) || !global::System.IO.File.Exists(p)) continue;
                var uri = global::AndroidX.Core.Content.FileProvider.GetUriForFile(
                    ctx, Authority, new global::Java.IO.File(p));
                uris.Add(uri);
            }
            if (uris.Count == 0) return;

            // Detect WhatsApp / WhatsApp Business
            string? wa = null;
            foreach (var pkg in new[] { "com.whatsapp", "com.whatsapp.w4b" })
            {
                try { pm.GetPackageInfo(pkg, 0); wa = pkg; break; } catch { }
            }

            Intent BuildIntent()
            {
                Intent i;
                if (uris.Count == 1)
                {
                    i = new Intent(Intent.ActionSend);
                    i.PutExtra(Intent.ExtraStream, uris[0]);
                }
                else
                {
                    i = new Intent(Intent.ActionSendMultiple);
                    i.PutParcelableArrayListExtra(Intent.ExtraStream, uris);
                }
                i.SetType("image/*");
                if (!string.IsNullOrWhiteSpace(caption))
                {
                    i.PutExtra(Intent.ExtraText,    caption);
                    i.PutExtra(Intent.ExtraSubject, caption);
                }
                i.AddFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.NewTask);
                return i;
            }

            // Try WhatsApp directly first
            if (wa != null)
            {
                try
                {
                    var direct = BuildIntent();
                    direct.SetPackage(wa);
                    ctx.StartActivity(direct);
                    return;
                }
                catch { /* fall through to chooser */ }
            }

            // Fallback: system share sheet
            var fallback = BuildIntent();
            var chooser  = Intent.CreateChooser(fallback, "Bagikan ke...");
            chooser!.AddFlags(ActivityFlags.NewTask);
            ctx.StartActivity(chooser);
        }
    }
}
#endif
