using System.Collections.ObjectModel;
using SyncChain.Desktop.Models;
using SyncChain.Desktop.Services;

namespace SyncChain.Desktop.Views.Pages;

public partial class UserAccessPage : ContentPage
{
	private string _selectedRole = "manager";

	public ObservableCollection<InternalUserItem> Users { get; } = new();

	public UserAccessPage()
	{
		InitializeComponent();
		BindingContext = this;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		ApplyPermissions();
		await LoadUsersAsync();
	}

	// Tải danh sách tài khoản nội bộ khi người dùng là admin.
	private async Task LoadUsersAsync()
	{
		if (!SyncChainApiClient.Instance.CanManageUsers)
			return;

		try
		{
			var users = await SyncChainApiClient.Instance.GetInternalUsersAsync();
			Users.Clear();
			foreach (var user in users)
			{
				Users.Add(user);
			}

			UpdateSummary();
		}
		catch (Exception ex)
		{
			Users.Clear();
			UpdateSummary();
			await DisplayAlert("Người dùng", ex.Message, "OK");
		}
	}

	// Cập nhật thông tin quyền và ẩn/hiện khu vực quản trị.
	private void ApplyPermissions()
	{
		var currentUser = SyncChainApiClient.Instance.CurrentUser;
		var canManage = SyncChainApiClient.Instance.CanManageUsers;

		RoleHintLabel.Text = currentUser == null
			? "Chưa đăng nhập."
			: $"Đang đăng nhập: {currentUser.Email} - role {currentUser.Role}. Admin có toàn quyền quản lý tài khoản nội bộ.";
		CreatePanel.IsVisible = canManage;
		PermissionLabel.Text = canManage ? "Admin: tạo/sửa/khóa/reset" : "Chi admin được quản lý";
	}

	// Tính tổng số tài khoản theo role và trạng thái khóa.
	private void UpdateSummary()
	{
		TotalUsersLabel.Text = Users.Count.ToString();
		ManagerUsersLabel.Text = Users.Count(x => x.Role == "manager").ToString();
		StaffUsersLabel.Text = Users.Count(x => x.Role == "staff").ToString();
		LockedUsersLabel.Text = Users.Count(x => !x.IsActive).ToString();
		EmptyLabel.IsVisible = Users.Count == 0;
	}

	// Tải lại danh sách người dùng.
	private async void OnRefreshClicked(object? sender, EventArgs e)
	{
		await LoadUsersAsync();
	}

	// Chọn role manager cho tài khoản sắp tạo.
	private void OnManagerRoleClicked(object? sender, EventArgs e)
	{
		_selectedRole = "manager";
		ManagerRoleButton.Style = (Style)Application.Current!.Resources["PrimaryButtonStyle"];
		StaffRoleButton.Style = (Style)Application.Current!.Resources["SecondaryButtonStyle"];
	}

	// Chọn role staff cho tài khoản sắp tạo.
	private void OnStaffRoleClicked(object? sender, EventArgs e)
	{
		_selectedRole = "staff";
		ManagerRoleButton.Style = (Style)Application.Current!.Resources["SecondaryButtonStyle"];
		StaffRoleButton.Style = (Style)Application.Current!.Resources["PrimaryButtonStyle"];
	}

	// Tạo tài khoản nội bộ mới theo role đã chọn.
	private async void OnCreateUserClicked(object? sender, EventArgs e)
	{
		var email = EmailEntry.Text?.Trim() ?? string.Empty;
		var password = PasswordEntry.Text?.Trim() ?? string.Empty;
		var username = UsernameEntry.Text?.Trim() ?? string.Empty;

		if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
		{
			await DisplayAlert("Tài khoản", "Vui lòng nhập email và mật khẩu.", "OK");
			return;
		}

		CreateUserButton.IsEnabled = false;

		try
		{
			await SyncChainApiClient.Instance.CreateInternalUserAsync(email, password, username, _selectedRole);
			EmailEntry.Text = string.Empty;
			UsernameEntry.Text = string.Empty;
			PasswordEntry.Text = "123456";
			await LoadUsersAsync();
			await DisplayAlert("Tài khoản", "Tạo tài khoản thành công.", "OK");
		}
		catch (Exception ex)
		{
			await DisplayAlert("Không tạo được tài khoản", ex.Message, "OK");
		}
		finally
		{
			CreateUserButton.IsEnabled = true;
		}
	}

	// Chuyển đổi role giữa manager và staff cho tài khoản.
	private async void OnChangeRoleClicked(object? sender, EventArgs e)
	{
		if ((sender as Button)?.CommandParameter is not InternalUserItem user)
			return;

		var newRole = user.Role == "manager" ? "staff" : "manager";
		var confirmed = await DisplayAlert("Đổi role", $"Chuyển {user.Email} sang {newRole}?", "Đồng ý", "Hủy");
		if (!confirmed)
			return;

		try
		{
			await SyncChainApiClient.Instance.UpdateInternalUserAsync(user.Id, newRole, user.IsActive);
			await LoadUsersAsync();
		}
		catch (Exception ex)
		{
			await DisplayAlert("Không đổi được role", ex.Message, "OK");
		}
	}

	// Khóa hoặc mở khóa tài khoản nội bộ.
	private async void OnToggleActiveClicked(object? sender, EventArgs e)
	{
		if ((sender as Button)?.CommandParameter is not InternalUserItem user)
			return;

		var newState = !user.IsActive;
		var action = newState ? "mở khóa" : "khóa";
		var confirmed = await DisplayAlert("Trạng thái tài khoản", $"{action} {user.Email}?", "Đồng ý", "Hủy");
		if (!confirmed)
			return;

		try
		{
			await SyncChainApiClient.Instance.SetInternalUserActiveAsync(user.Id, newState);
			await LoadUsersAsync();
		}
		catch (Exception ex)
		{
			await DisplayAlert("Không cập nhật được tài khoản", ex.Message, "OK");
		}
	}

	// Đặt lại mật khẩu cho tài khoản nội bộ.
	private async void OnResetPasswordClicked(object? sender, EventArgs e)
	{
		if ((sender as Button)?.CommandParameter is not InternalUserItem user)
			return;

		var password = await DisplayPromptAsync("Reset mật khẩu", $"Mật khẩu mới cho {user.Email}:", "Cập nhật", "Hủy", "Nhập mật khẩu mới", 64, Keyboard.Text, "123456");
		if (string.IsNullOrWhiteSpace(password))
			return;

		try
		{
			await SyncChainApiClient.Instance.ResetInternalUserPasswordAsync(user.Id, password.Trim());
			await DisplayAlert("Mật khẩu", "Đã reset mật khẩu.", "OK");
		}
		catch (Exception ex)
		{
			await DisplayAlert("Không reset được mật khẩu", ex.Message, "OK");
		}
	}

	// Đăng xuất và quay về màn hình đăng nhập.
	private void OnLogoutClicked(object? sender, EventArgs e)
	{
		SyncChainApiClient.Instance.Logout();
		App.ShowLogin();
	}
}
