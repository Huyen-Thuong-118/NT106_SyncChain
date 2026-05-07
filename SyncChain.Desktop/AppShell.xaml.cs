namespace SyncChain.Desktop;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute(nameof(Views.Pages.RegisterPage), typeof(Views.Pages.RegisterPage));
		Routing.RegisterRoute(nameof(Views.Pages.CreateProductPage), typeof(Views.Pages.CreateProductPage));
		Routing.RegisterRoute(nameof(Views.Pages.ProductDetailPage), typeof(Views.Pages.ProductDetailPage));
		Routing.RegisterRoute(nameof(Views.Pages.OrderDetailPage), typeof(Views.Pages.OrderDetailPage));
		ApplyRoleNavigation();
	}

	public void RefreshUserFooter()
	{
		ApplyRoleNavigation();
	}

	// Cập nhật menu và footer theo role người dùng hiện tại.
	private void ApplyRoleNavigation()
	{
		var api = Services.SyncChainApiClient.Instance;
		var role = api.CurrentUser?.Role ?? "guest";
		var user = api.CurrentUser;

		AvatarLabel.Text = user?.Initials ?? "ND";
		DisplayNameLabel.Text = user?.DisplayName ?? "Người dùng";
		FooterRoleLabel.Text = user?.RoleLabel ?? role.ToUpperInvariant();

		DashboardItem.IsVisible = role is "admin" or "manager";
		ProductsItem.IsVisible = true;
		OrdersItem.IsVisible = true;
		CreateOrderItem.IsVisible = role is "customer" or "staff" or "manager" or "admin";
		ImportsItem.IsVisible = api.CanManageProducts;
		LogsItem.IsVisible = api.IsInternalUser;
		ChatItem.IsVisible = api.IsInternalUser;
		AccessItem.IsVisible = api.CanManageUsers;
	}

	// Mở trang cài đặt tài khoản khi bấm footer.
	private void OnAccountFooterTapped(object? sender, TappedEventArgs e)
	{
		FlyoutIsPresented = false;
		CurrentItem = AccountSettingsItem;
	}
}
