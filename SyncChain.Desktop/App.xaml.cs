namespace SyncChain.Desktop;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new NavigationPage(new Views.Pages.LoginPage()));
	}

	// Chuyển ứng dụng sang Shell sau khi đăng nhập.
	public static void ShowShell()
	{
		if (Current?.Windows.Count > 0)
		{
			Current.Windows[0].Page = new AppShell();
		}
	}

	// Đưa ứng dụng về màn hình đăng nhập.
	public static void ShowLogin()
	{
		if (Current?.Windows.Count > 0)
		{
			Current.Windows[0].Page = new NavigationPage(new Views.Pages.LoginPage());
		}
	}
}
