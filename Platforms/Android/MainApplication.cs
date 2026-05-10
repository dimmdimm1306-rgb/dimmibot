using Android.App;
using Android.Runtime;

namespace StokBarangMAUI;

[Application]
public class MainApplication : MauiApplication
{
	public MainApplication(IntPtr handle, JniHandleOwnership ownership)
		: base(handle, ownership)
	{
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

	public override void OnCreate()
	{
		base.OnCreate();

		// Global handler — show dialog instead of force close
		AndroidEnvironment.UnhandledExceptionRaiser += (_, args) =>
		{
			args.Handled = true;
			var msg = args.Exception?.Message ?? "Unknown error";
			System.Diagnostics.Debug.WriteLine($"[CRASH] {args.Exception}");
			MainThread.BeginInvokeOnMainThread(async () =>
			{
				try
				{
					var page = Microsoft.Maui.Controls.Application.Current?.MainPage;
					if (page != null)
						await page.DisplayAlert("Terjadi Kesalahan", msg, "OK");
				}
				catch { }
			});
		};

		AppDomain.CurrentDomain.UnhandledException += (_, args) =>
		{
			System.Diagnostics.Debug.WriteLine($"[APPDOMAIN] {args.ExceptionObject}");
		};
	}
}
