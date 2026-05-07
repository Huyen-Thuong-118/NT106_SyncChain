using SyncChain.Desktop.Services;

namespace SyncChain.Desktop.Views.Pages;

public partial class LoginPage : ContentPage
{
	public LoginPage()
	{
		InitializeComponent();
	}

	// Kiểm tra thông tin đăng nhập rồi chuyển vào Shell chính.
	private async void OnLoginClicked(object? sender, EventArgs e)
	{
		var email = EmailEntry.Text?.Trim() ?? string.Empty;
		var password = PasswordEntry.Text ?? string.Empty;

		if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
		{
			await DisplayAlert("Đăng nhập", "Vui lòng nhập email và mật khẩu.", "OK");
			return;
		}

		LoginButton.IsEnabled = false;

		try
		{
			await SyncChainApiClient.Instance.LoginAsync(email, password);
			App.ShowShell();
		}
		catch (Exception ex)
		{
			await DisplayAlert("Không thể đăng nhập", ex.Message, "OK");
		}
		finally
		{
			LoginButton.IsEnabled = true;
		}
	}

	// Mở màn hình đăng ký tài khoản mới.
	private async void OnRegisterClicked(object? sender, EventArgs e)
	{
		await Navigation.PushAsync(new RegisterPage());
	}
}
