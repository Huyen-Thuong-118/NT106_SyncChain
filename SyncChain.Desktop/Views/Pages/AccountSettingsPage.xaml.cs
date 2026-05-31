using SyncChain.Desktop.Services;

namespace SyncChain.Desktop.Views.Pages;

public partial class AccountSettingsPage : ContentPage
{
	public AccountSettingsPage()
	{
		InitializeComponent();
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await LoadProfileAsync();
	}

	// Tải hồ sơ tài khoản, nếu lỗi thì dùng dữ liệu đang lưu tạm.
	private async Task LoadProfileAsync()
	{
		try
		{
			var user = await SyncChainApiClient.Instance.GetProfileAsync();
			ApplyUser(user);
		}
		catch (Exception ex)
		{
			var user = SyncChainApiClient.Instance.CurrentUser;
			if (user != null)
			{
				ApplyUser(user);
			}

			await DisplayAlert("Tài khoản", ex.Message, "OK");
		}
	}

	// Đổ thông tin người dùng lên form tài khoản.
	private void ApplyUser(SyncChainApiClient.ApiUser user)
	{
		AvatarLabel.Text = user.Initials;
		DisplayNameEntry.Text = user.DisplayName;
		EmailEntry.Text = user.Email;
		RoleLabel.Text = user.RoleLabel;
	}

	// Lưu thay đổi tên hiển thị của tài khoản.
	private async void OnSaveProfileClicked(object? sender, EventArgs e)
	{
		var displayName = DisplayNameEntry.Text?.Trim() ?? string.Empty;
		if (string.IsNullOrWhiteSpace(displayName))
		{
			await DisplayAlert("Thông tin cá nhân", "Vui lòng nhập tên hiển thị.", "OK");
			return;
		}

		SaveProfileButton.IsEnabled = false;

		try
		{
			var user = await SyncChainApiClient.Instance.UpdateProfileAsync(displayName);
			ApplyUser(user);
			(Shell.Current as AppShell)?.RefreshUserFooter();
			await DisplayAlert("Thông tin cá nhân", "Đã cập nhật thông tin tài khoản.", "OK");
		}
		catch (Exception ex)
		{
			await DisplayAlert("Không cập nhật được", ex.Message, "OK");
		}
		finally
		{
			SaveProfileButton.IsEnabled = true;
		}
	}

	// Kiểm tra và gửi yêu cầu đổi mật khẩu.
	private async void OnChangePasswordClicked(object? sender, EventArgs e)
	{
		var currentPassword = CurrentPasswordEntry.Text ?? string.Empty;
		var newPassword = NewPasswordEntry.Text ?? string.Empty;
		var confirmPassword = ConfirmPasswordEntry.Text ?? string.Empty;

		if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
		{
			await DisplayAlert("Đổi mật khẩu", "Vui lòng nhập đầy đủ mật khẩu hiện tại và mật khẩu mới.", "OK");
			return;
		}

		if (newPassword != confirmPassword)
		{
			await DisplayAlert("Đổi mật khẩu", "Mật khẩu mới nhập lại không khớp.", "OK");
			return;
		}

		ChangePasswordButton.IsEnabled = false;

		try
		{
			await SyncChainApiClient.Instance.ChangePasswordAsync(currentPassword, newPassword);
			CurrentPasswordEntry.Text = string.Empty;
			NewPasswordEntry.Text = string.Empty;
			ConfirmPasswordEntry.Text = string.Empty;
			await DisplayAlert("Đổi mật khẩu", "Đã đổi mật khẩu.", "OK");
		}
		catch (Exception ex)
		{
			await DisplayAlert("Không đổi được mật khẩu", ex.Message, "OK");
		}
		finally
		{
			ChangePasswordButton.IsEnabled = true;
		}
	}

	// Đăng xuất và quay về màn hình đăng nhập.
	private void OnLogoutClicked(object? sender, EventArgs e)
	{
		SyncChainApiClient.Instance.Logout();
		App.ShowLogin();
	}
}
