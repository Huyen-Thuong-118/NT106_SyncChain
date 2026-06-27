using System.Net.Http.Json;
using System.Globalization;
using SyncChain.Desktop.Models;
using SyncChain.Desktop.Services;

namespace SyncChain.Desktop.Views.Pages;

public partial class LoginPage : ContentPage
{
	private readonly HttpClient _http;

	public LoginPage() : this(ApiClientProvider.Client)
	{
	}

	public LoginPage(HttpClient http)
	{
		_http = http;
		InitializeComponent();
		ApplyBackgroundImage();
	}

	private void ApplyBackgroundImage()
	{
		var source = Services.SigninBackground.CreateSource();
		if (source is null)
			return;

		BackgroundPhoto.Source = source;
		BackgroundImageSource = Services.SigninBackground.CreateSource();
	}

	private async void OnLoginClicked(object? sender, EventArgs e)
	{
		await LoginAsync(customerShell: false);
	}

	private async void OnCustomerLoginClicked(object? sender, EventArgs e)
	{
		await LoginAsync(customerShell: true);
	}

	private async void OnRegisterClicked(object? sender, EventArgs e)
	{
		await Navigation.PushAsync(new RegisterPage());
	}

	private async Task LoginAsync(bool customerShell)
	{
		try
		{
			var email = EmailEntry.Text?.Trim() ?? string.Empty;
			var password = PasswordEntry.Text ?? string.Empty;

			if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
			{
				await DisplayAlertAsync("Thiếu thông tin", "Vui lòng nhập đầy đủ email và mật khẩu.", "OK");
				return;
			}

			var response = await _http.PostAsJsonAsync("api/Auth/login", new
			{
				email,
				password,
				device = $"{DeviceInfo.Current.Name} · {DeviceInfo.Current.Platform} {DeviceInfo.Current.VersionString}",
				location = $"{RegionInfo.CurrentRegion.DisplayName} · {TimeZoneInfo.Local.StandardName}"
			});
			if (!response.IsSuccessStatusCode)
			{
				var message = await response.Content.ReadAsStringAsync();
				await DisplayAlertAsync("Đăng nhập thất bại", string.IsNullOrWhiteSpace(message) ? "Sai thông tin đăng nhập." : message, "OK");
				return;
			}

			var login = await response.Content.ReadFromJsonAsync<LoginResponseApi>();
			if (string.IsNullOrWhiteSpace(login?.Token))
			{
				await DisplayAlertAsync("Đăng nhập thất bại", "Backend không trả token.", "OK");
				return;
			}

			var role = login.User?.Role?.Trim().ToLowerInvariant();
			if (customerShell && role != "customer")
			{
				await DisplayAlertAsync("Không đúng cổng đăng nhập", "Tài khoản quản trị vui lòng dùng nút Đăng nhập quản trị.", "OK");
				return;
			}

			if (!customerShell && role is not ("admin" or "manager" or "staff"))
			{
				await DisplayAlertAsync("Không có quyền truy cập", "Tài khoản này không có quyền vào trang quản trị.", "OK");
				return;
			}

			ApiClientProvider.SetSession(login.Token, login.User?.Role, login.User?.MaNguoiDung);
			if (customerShell)
				App.ShowCustomerShell();
			else
				App.ShowShell();
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Không kết nối được API", ex.Message, "OK");
		}
	}
}
