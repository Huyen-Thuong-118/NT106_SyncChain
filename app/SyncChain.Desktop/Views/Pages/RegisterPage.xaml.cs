using System.Globalization;
using System.Net.Http.Json;
using SyncChain.Desktop.Models;
using SyncChain.Desktop.Services;

namespace SyncChain.Desktop.Views.Pages;

public partial class RegisterPage : ContentPage
{
	private readonly HttpClient _http;

	public RegisterPage() : this(ApiClientProvider.Client)
	{
	}

	public RegisterPage(HttpClient http)
	{
		_http = http;
		InitializeComponent();
		ApplyBackgroundImage();
	}

	private void ApplyBackgroundImage()
	{
		var source = Services.SigninBackground.CreateSource();
		if (source is null)
		{
			return;
		}

		BackgroundPhoto.Source = source;
		BackgroundImageSource = Services.SigninBackground.CreateSource();
	}

	private async void OnRegisterClicked(object? sender, EventArgs e)
	{
		AppLog.Info("Register", "Nút đăng ký được bấm");

		var fullName = FullNameEntry.Text?.Trim() ?? string.Empty;
		var email = EmailEntry.Text?.Trim() ?? string.Empty;
		var username = UsernameEntry.Text?.Trim() ?? string.Empty;
		var password = PasswordEntry.Text ?? string.Empty;
		var confirm = ConfirmPasswordEntry.Text ?? string.Empty;

		// ── Kiểm tra dữ liệu ngay tại client để phản hồi nhanh (backend vẫn kiểm tra lại). ──
		if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
		{
			await DisplayAlert("Thiếu thông tin", "Vui lòng nhập email và mật khẩu.", "OK");
			return;
		}
		if (password.Length < 6)
		{
			await DisplayAlert("Mật khẩu quá ngắn", "Mật khẩu phải từ 6 ký tự trở lên.", "OK");
			return;
		}
		if (password != confirm)
		{
			await DisplayAlert("Mật khẩu không khớp", "Mật khẩu xác nhận chưa trùng khớp.", "OK");
			return;
		}
		if (!TermsCheckBox.IsChecked)
		{
			await DisplayAlert("Chưa đồng ý điều khoản", "Vui lòng đồng ý Điều khoản dịch vụ để tiếp tục.", "OK");
			return;
		}

		var (ho, ten) = SplitName(fullName);

		try
		{
			RegisterButton.IsEnabled = false;

			// ── 1) Gọi API tạo tài khoản thật (POST api/Auth/register). ──
			AppLog.Info("Register", $"Gọi POST api/Auth/register cho {email}");
			var registerResponse = await _http.PostAsJsonAsync("api/Auth/register", new
			{
				email,
				password,
				ho,
				ten,
				tenDangNhap = username,
				soDienThoai = string.Empty
			});

			if (!registerResponse.IsSuccessStatusCode)
			{
				var message = await registerResponse.Content.ReadAsStringAsync();
				AppLog.Warn("Register", $"Đăng ký thất bại ({(int)registerResponse.StatusCode}): {message}");
				await DisplayAlert("Đăng ký thất bại",
					string.IsNullOrWhiteSpace(message) ? "Không tạo được tài khoản." : message, "OK");
				return;
			}

			AppLog.Info("Register", "Tạo tài khoản thành công, tiến hành đăng nhập tự động");

			// ── 2) Tự đăng nhập để lấy JWT (register không trả token). ──
			var loginResponse = await _http.PostAsJsonAsync("api/Auth/login", new
			{
				email,
				password,
				device = $"{DeviceInfo.Current.Name} · {DeviceInfo.Current.Platform} {DeviceInfo.Current.VersionString}",
				location = $"{RegionInfo.CurrentRegion.DisplayName} · {TimeZoneInfo.Local.StandardName}"
			});

			// Nếu tự đăng nhập lỗi vì lý do nào đó, tài khoản vẫn đã được tạo →
			// đưa người dùng về trang Login để họ tự đăng nhập, không mất dữ liệu.
			if (!loginResponse.IsSuccessStatusCode)
			{
				AppLog.Warn("Register", $"Tự đăng nhập thất bại ({(int)loginResponse.StatusCode}), chuyển về trang Login");
				await DisplayAlert("Đăng ký thành công",
					"Tài khoản đã được tạo. Vui lòng đăng nhập để tiếp tục.", "OK");
				await Navigation.PopAsync();
				return;
			}

			var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponseApi>();
			if (string.IsNullOrWhiteSpace(login?.Token))
			{
				AppLog.Warn("Register", "Backend không trả token khi tự đăng nhập, chuyển về Login");
				await DisplayAlert("Đăng ký thành công",
					"Tài khoản đã được tạo. Vui lòng đăng nhập để tiếp tục.", "OK");
				await Navigation.PopAsync();
				return;
			}

			// ── 3) Lưu session + gắn Bearer token, rồi vào cổng khách hàng. ──
			ApiClientProvider.SetSession(login.Token, login.User?.Role, login.User?.MaNguoiDung);
			AppLog.Info("Register", $"Token đã lưu (role={login.User?.Role}), mở CustomerShell");

			await DisplayAlert("Đăng ký thành công", "Chào mừng bạn đến với SyncChain!", "Bắt đầu");
			// Register luôn tạo tài khoản vai trò "customer" → mở cổng khách hàng.
			App.ShowCustomerShell();
		}
		catch (Exception ex)
		{
			AppLog.Error("Register", "Lỗi khi gọi API đăng ký", ex);
			await DisplayAlert("Không kết nối được API", ex.Message, "OK");
		}
		finally
		{
			RegisterButton.IsEnabled = true;
		}
	}

	private async void OnBackClicked(object? sender, EventArgs e)
	{
		await Navigation.PopAsync();
	}

	// Tách "Nguyễn Văn A" thành Ho="Nguyễn Văn", Ten="A" (từ cuối là tên gọi).
	private static (string ho, string ten) SplitName(string fullName)
	{
		if (string.IsNullOrWhiteSpace(fullName))
			return (string.Empty, string.Empty);

		var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length == 1)
			return (string.Empty, parts[0]);

		return (string.Join(' ', parts[..^1]), parts[^1]);
	}
}
