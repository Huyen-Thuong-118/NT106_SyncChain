using System.Net.Http.Json;
using SyncChain.Desktop.Services;

namespace SyncChain.Desktop.Views.Pages;

public partial class LoginPage : ContentPage
{
	public LoginPage()
	{
		InitializeComponent();
		ApplyBackgroundImage();
	}

	private void ApplyBackgroundImage()
	{
		var source = SigninBackground.CreateSource();
		if (source != null)
		{
			BackgroundPhoto.Source = source;
			BackgroundImageSource = SigninBackground.CreateSource();
		}
	}

	private async void OnLoginClicked(object? sender, EventArgs e)
	{
		var identifier = IdentifierEntry.Text?.Trim() ?? "";
		var password = PasswordEntry.Text ?? "";

		IdentifierError.IsVisible = false;
		PasswordError.IsVisible = false;
		GeneralError.IsVisible = false;

		var valid = true;
		if (string.IsNullOrWhiteSpace(identifier))
		{
			IdentifierError.Text = "Vui lòng nhập thông tin đăng nhập";
			IdentifierError.IsVisible = true;
			valid = false;
		}
		if (string.IsNullOrWhiteSpace(password))
		{
			PasswordError.Text = "Vui lòng nhập mật khẩu";
			PasswordError.IsVisible = true;
			valid = false;
		}
		if (!valid) return;

		LoginButton.IsEnabled = false;
		LoginButton.Text = "Đang đăng nhập...";

		try
		{
			var http = AppHttpClient.Instance;
			var response = await http.PostAsJsonAsync("api/auth/login", new
			{
				Identifier = identifier,
				Password = password
			});

			if (!response.IsSuccessStatusCode)
			{
				var err = await response.Content.ReadFromJsonAsync<ErrorResponse>();
				GeneralErrorLabel.Text = err?.message ?? "Thông tin đăng nhập không đúng";
				GeneralError.IsVisible = true;
				return;
			}

			var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
			if (result == null) return;

			var displayName = string.IsNullOrWhiteSpace(result.user.HoTen)
				? result.user.TenDangNhap
				: result.user.HoTen;
			TokenStore.Save(result.token, result.user.MaNguoiDung, result.user.role,
				displayName, result.user.Email);

			// Kết nối SignalR sau khi đăng nhập thành công
			await App.SignalR.StartAsync(result.token);

			if (result.user.role == "customer")
				App.ShowCustomerShell();
			else
				App.ShowShell();
		}
		catch (Exception ex)
		{
			GeneralErrorLabel.Text = $"Lỗi kết nối máy chủ: {ex.Message}";
			GeneralError.IsVisible = true;
		}
		finally
		{
			LoginButton.IsEnabled = true;
			LoginButton.Text = "ĐĂNG NHẬP";
		}
	}

	private async void OnRegisterClicked(object? sender, EventArgs e)
	{
		await Navigation.PushAsync(new RegisterPage());
	}

	private async void OnForgotPasswordClicked(object? sender, EventArgs e)
	{
		await Navigation.PushAsync(new ForgotPasswordPage());
	}

	private sealed record ErrorResponse(string message);
	private sealed record LoginUserInfo(int MaNguoiDung, string TenDangNhap, string Email,
		string? HoTen, string role);
	private sealed record LoginResponse(string token, LoginUserInfo user);
}
