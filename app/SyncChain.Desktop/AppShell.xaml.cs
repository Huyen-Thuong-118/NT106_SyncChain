namespace SyncChain.Desktop;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute(nameof(Views.Pages.RegisterPage),        typeof(Views.Pages.RegisterPage));
		Routing.RegisterRoute(nameof(Views.Pages.ProductDetailPage),  typeof(Views.Pages.ProductDetailPage));
		Routing.RegisterRoute(nameof(Views.Pages.OrderDetailPage),    typeof(Views.Pages.OrderDetailPage));
		Routing.RegisterRoute(nameof(Views.Pages.ProfilePage),        typeof(Views.Pages.ProfilePage));
		Routing.RegisterRoute(nameof(Views.Pages.AddressPage),        typeof(Views.Pages.AddressPage));
		Routing.RegisterRoute(nameof(Views.Pages.CartPage),           typeof(Views.Pages.CartPage));
		Routing.RegisterRoute(nameof(Views.Pages.PaymentPage),        typeof(Views.Pages.PaymentPage));
		Routing.RegisterRoute(nameof(Views.Pages.OrderTrackingPage),  typeof(Views.Pages.OrderTrackingPage));
		Routing.RegisterRoute(nameof(Views.Pages.NotificationPage),   typeof(Views.Pages.NotificationPage));
		Navigated += (_, _) => FlyoutIsPresented = false;
	}

#if WINDOWS
	protected override void OnHandlerChanged()
	{
		base.OnHandlerChanged();
		AttachHoverFlyout();
	}

	private void AttachHoverFlyout()
	{
		if (Window?.Handler?.PlatformView is not Microsoft.UI.Xaml.Window nativeWindow ||
			nativeWindow.Content is not Microsoft.UI.Xaml.FrameworkElement root)
		{
			return;
		}

		root.PointerMoved -= OnRootPointerMoved;
		root.PointerMoved += OnRootPointerMoved;
		root.PointerExited -= OnRootPointerExited;
		root.PointerExited += OnRootPointerExited;
	}

	private void OnRootPointerMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
	{
		if (sender is not Microsoft.UI.Xaml.UIElement element)
		{
			return;
		}

		var point = e.GetCurrentPoint(element).Position;

		if (!FlyoutIsPresented && point.X <= 28)
		{
			FlyoutIsPresented = true;
			return;
		}

		if (FlyoutIsPresented && point.X > FlyoutWidth + 20)
		{
			FlyoutIsPresented = false;
		}
	}

	private void OnRootPointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
	{
		FlyoutIsPresented = false;
	}
#endif
}
