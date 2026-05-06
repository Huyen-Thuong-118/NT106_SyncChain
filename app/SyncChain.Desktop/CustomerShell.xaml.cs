namespace SyncChain.Desktop;

public partial class CustomerShell : Shell
{
	public CustomerShell()
	{
		InitializeComponent();
		Routing.RegisterRoute(nameof(Views.Pages.OrderDetailPage), typeof(Views.Pages.OrderDetailPage));
	}
}
