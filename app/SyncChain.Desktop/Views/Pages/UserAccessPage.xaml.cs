using SyncChain.Desktop.Models;

namespace SyncChain.Desktop.Views.Pages;

public partial class UserAccessPage : ContentPage
{
	public IReadOnlyList<RoleOption> Roles { get; } =
	[
		new() { Name = "Admin", Description = "Quản trị hệ thống, toàn quyền truy cập", IsSelected = true },
		new() { Name = "Manager", Description = "Quản lý kho hàng và đơn hàng", IsSelected = false },
		new() { Name = "Staff", Description = "Nhân viên xử lý đơn và kho", IsSelected = false }
	];

	public UserAccessPage()
	{
		InitializeComponent();
		BindingContext = this;
	}

	private void OnLogoutClicked(object? sender, EventArgs e)
	{
		App.ShowLogin();
	}
}
