using SyncChain.Desktop.Services;

namespace SyncChain.Desktop.Views.Pages;

public partial class RegisterPage : ContentPage
{
	public RegisterPage()
	{
		InitializeComponent();
	}

	// Kiểm tra dữ liệu đăng ký và gửi yêu cầu tạo tài khoản.
	private async void OnRegisterClicked(object? sender, EventArgs e)
	{
		var email = EmailEntry.Text?.Trim() ?? string.Empty;
		var password = PasswordEntry.Text ?? string.Empty;
		var confirmPassword = ConfirmPasswordEntry.Text ?? string.Empty;

		if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
		{
			await DisplayAlert("Đăng ký", "Vui lòng nhập email và mật khẩu.", "OK");
			return;
		}

		if (password != confirmPassword)
		{
			await DisplayAlert("Đăng ký", "Mật khẩu xác nhận không khớp.", "OK");
			return;
		}

		RegisterButton.IsEnabled = false;

		try
		{
			await SyncChainApiClient.Instance.RegisterAsync(email, password);
			await DisplayAlert("Đăng ký", "Tạo tài khoản thành công. Hãy đăng nhập để vào hệ thống.", "OK");
			await Navigation.PopAsync();
		}
		catch (Exception ex)
		{
			await DisplayAlert("Không thể đăng ký", ex.Message, "OK");
		}
		finally
		{
			RegisterButton.IsEnabled = true;
		}
	}

	// Quay lại màn hình đăng nhập.
	private async void OnBackClicked(object? sender, EventArgs e)
	{
		await Navigation.PopAsync();
	}
}
