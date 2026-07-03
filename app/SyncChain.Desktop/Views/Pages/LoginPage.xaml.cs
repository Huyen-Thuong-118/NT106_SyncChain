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

	// Part 8 - Health check: kiểm tra backend trước khi người dùng thao tác.
	// Nếu backend/DB không sẵn sàng thì báo thân thiện chứ không để app lỗi khó hiểu.
	protected override async void OnAppearing()
	{
		base.OnAppearing();
		AppLog.Info("Login", $"Kiểm tra backend tại {ApiClientProvider.ApiBaseUrl}");
		var healthy = await ApiClientProvider.IsBackendHealthyAsync();
		if (!healthy)
		{
			AppLog.Warn("Login", "Backend /health không phản hồi");
			await DisplayAlertAsync("Máy chủ chưa sẵn sàng",
				$"Không kết nối được tới máy chủ ({ApiClientProvider.ApiBaseUrl}).\n\n" +
				"Hãy chạy backend (scripts/run-backend.ps1 hoặc run-all.ps1) rồi thử lại.", "OK");
		}
		else
		{
			AppLog.Info("Login", "Backend OK");
		}
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
			AppLog.Info("Login", $"Nút đăng nhập được bấm (cổng={(customerShell ? "khách hàng" : "quản trị")})");

			if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
			{
				await DisplayAlertAsync("Thiếu thông tin", "Vui lòng nhập đầy đủ email và mật khẩu.", "OK");
				return;
			}

			AppLog.Info("Login", $"Gọi POST api/Auth/login cho {email}");
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
				AppLog.Warn("Login", $"Đăng nhập thất bại ({(int)response.StatusCode}): {message}");
				await DisplayAlertAsync("Đăng nhập thất bại", string.IsNullOrWhiteSpace(message) ? "Sai thông tin đăng nhập." : message, "OK");
				return;
			}

			var login = await response.Content.ReadFromJsonAsync<LoginResponseApi>();
			if (string.IsNullOrWhiteSpace(login?.Token))
			{
				AppLog.Warn("Login", "Backend không trả token");
				await DisplayAlertAsync("Đăng nhập thất bại", "Backend không trả token.", "OK");
				return;
			}
			AppLog.Info("Login", $"Đăng nhập OK (role={login.User?.Role}), token đã nhận");

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
			AppLog.Info("Login", $"Token đã lưu + gắn Authorization header, mở {(customerShell ? "CustomerShell" : "AppShell")}");
			if (customerShell)
				App.ShowCustomerShell();
			else
				App.ShowShell();
		}
		catch (Exception ex)
		{
			AppLog.Error("Login", "Lỗi khi gọi API đăng nhập", ex);
			await DisplayAlertAsync("Không kết nối được API", ex.Message, "OK");
		}
	}
}
