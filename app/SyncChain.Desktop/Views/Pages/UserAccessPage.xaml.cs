using SyncChain.Desktop.Models;
using SyncChain.Desktop.Services;

namespace SyncChain.Desktop.Views.Pages;

public partial class UserAccessPage : ContentPage
{
	public IReadOnlyList<RoleOption> Roles => DemoData.Roles;

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
