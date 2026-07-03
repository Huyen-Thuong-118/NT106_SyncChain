namespace SyncChain.Desktop;

public partial class CustomerShell : Shell
{
	public CustomerShell()
	{
		InitializeComponent();
		Routing.RegisterRoute(nameof(Views.Pages.OrderDetailPage), typeof(Views.Pages.OrderDetailPage));
		Routing.RegisterRoute(nameof(Views.Pages.OrderTrackingPage), typeof(Views.Pages.OrderTrackingPage));
		Routing.RegisterRoute(nameof(Views.Pages.PaymentPage), typeof(Views.Pages.PaymentPage));
		Navigated += (_, _) => FlyoutIsPresented = false;

		// Kết nối SignalR khi cổng khách hàng mở (đã đăng nhập, có token).
		if (!string.IsNullOrEmpty(Services.ApiClientProvider.Token))
			_ = App.SignalR.StartAsync(Services.ApiClientProvider.Token!);

		App.SignalR.OnNewNotification += OnNewNotification;
	}

	private void OnNewNotification(string title, string content)
	{
		MainThread.BeginInvokeOnMainThread(async () =>
		{
			try { await DisplayAlert($"🔔 {title}", content, "OK"); } catch { }
		});
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
