using SyncChain.Desktop.Services;

namespace SyncChain.Desktop;

public partial class App : Application
{
	// SignalR realtime cho module khách hàng (thông báo/thanh toán/trạng thái đơn).
	public static SignalRService SignalR { get; } = new();

	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new NavigationPage(new Views.Pages.LoginPage()));
	}

	public static void ShowShell()
	{
		if (Current?.Windows.Count > 0)
		{
			Current.Windows[0].Page = new AppShell();
		}
	}

	public static void ShowCustomerShell()
	{
		if (Current?.Windows.Count > 0)
		{
			Current.Windows[0].Page = new CustomerShell();
		}
	}

	public static void ShowLogin()
	{
		_ = SignalR.StopAsync();
		ApiClientProvider.ClearSession();
		if (Current?.Windows.Count > 0)
		{
			Current.Windows[0].Page = new NavigationPage(new Views.Pages.LoginPage());
		}
	}
}
