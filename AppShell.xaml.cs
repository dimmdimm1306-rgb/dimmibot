namespace StokBarangMAUI;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		
		// Register routes
		Routing.RegisterRoute("openclawbot", typeof(Pages.OpenClawBotPage));
	}
}
