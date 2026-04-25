namespace SyncChain.Desktop;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute(nameof(Views.Pages.RegisterPage), typeof(Views.Pages.RegisterPage));
		Routing.RegisterRoute(nameof(Views.Pages.ProductDetailPage), typeof(Views.Pages.ProductDetailPage));
		Routing.RegisterRoute(nameof(Views.Pages.OrderDetailPage), typeof(Views.Pages.OrderDetailPage));
	}
}
