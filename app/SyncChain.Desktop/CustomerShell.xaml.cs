namespace SyncChain.Desktop;

public partial class CustomerShell : Shell
{
	public CustomerShell()
	{
		InitializeComponent();
		Routing.RegisterRoute(nameof(Views.Pages.OrderDetailPage), typeof(Views.Pages.OrderDetailPage));
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
