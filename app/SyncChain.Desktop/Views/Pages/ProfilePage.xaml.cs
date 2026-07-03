using System.Net.Http.Json;
using SyncChain.Desktop.Services;

namespace SyncChain.Desktop.Views.Pages;

public partial class ProfilePage : ContentPage
{
	private readonly HttpClient _http;

	public ProfilePage(HttpClient http)
	{
		_http = http;
		InitializeComponent();
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await LoadProfileAsync();
	}

	private async Task LoadProfileAsync()
	{
		try
		{
			var profile = await _http.GetFromJsonAsync<ProfileApiEx>("api/auth/profile");
			if (profile == null) return;
			HoEntry.Text = profile.Ho ?? "";
			TenEntry.Text = profile.Ten ?? "";
			UsernameEntry.Text = profile.TenDangNhap;
			PhoneEntry.Text = profile.SoDienThoai ?? "";
			EmailEntry.Text = profile.Email;
		}
		catch (Exception ex)
		{
			ErrorLabel.Text = $"Không thể tải hồ sơ: {ex.Message}";
			ErrorBanner.IsVisible = true;
		}
	}

	private async void OnSaveClicked(object? sender, EventArgs e)
	{
		ErrorBanner.IsVisible = false;
		SuccessBanner.IsVisible = false;
		SaveButton.IsEnabled = false;
		SaveButton.Text = "Đang lưu...";

		try
		{
			var response = await _http.PutAsJsonAsync("api/auth/profile", new
			{
				Ho = HoEntry.Text?.Trim() ?? "",
				Ten = TenEntry.Text?.Trim() ?? "",
				TenDangNhap = UsernameEntry.Text?.Trim().ToLowerInvariant() ?? "",
				SoDienThoai = PhoneEntry.Text?.Trim() ?? "",
				Email = EmailEntry.Text?.Trim().ToLowerInvariant() ?? ""
			});

			if (!response.IsSuccessStatusCode)
			{
				var err = await response.Content.ReadFromJsonAsync<ErrorResponse>();
				ErrorLabel.Text = err?.message ?? "Cập nhật thất bại";
				ErrorBanner.IsVisible = true;
				return;
			}

			SuccessLabel.Text = "Cập nhật hồ sơ thành công";
			SuccessBanner.IsVisible = true;
		}
		catch (Exception ex)
		{
			ErrorLabel.Text = $"Lỗi kết nối: {ex.Message}";
			ErrorBanner.IsVisible = true;
		}
		finally
		{
			SaveButton.IsEnabled = true;
			SaveButton.Text = "LƯU THAY ĐỔI";
		}
	}

	private async void OnChangePasswordClicked(object? sender, EventArgs e)
	{
		ErrorBanner.IsVisible = false;
		SuccessBanner.IsVisible = false;

		var current = CurrentPasswordEntry.Text ?? "";
		var next = NewPasswordEntry.Text ?? "";

		if (string.IsNullOrWhiteSpace(current) || string.IsNullOrWhiteSpace(next))
		{
			ErrorLabel.Text = "Vui lòng nhập đầy đủ mật khẩu";
			ErrorBanner.IsVisible = true;
			return;
		}
		if (next.Length < 8)
		{
			ErrorLabel.Text = "Mật khẩu mới phải ít nhất 8 ký tự";
			ErrorBanner.IsVisible = true;
			return;
		}

		ChangePassButton.IsEnabled = false;
		ChangePassButton.Text = "Đang đổi...";

		try
		{
			var response = await _http.PutAsJsonAsync("api/auth/change-password", new
			{
				CurrentPassword = current,
				NewPassword = next
			});

			if (!response.IsSuccessStatusCode)
			{
				var err = await response.Content.ReadFromJsonAsync<ErrorResponse>();
				ErrorLabel.Text = err?.message ?? "Đổi mật khẩu thất bại";
				ErrorBanner.IsVisible = true;
				return;
			}

			CurrentPasswordEntry.Text = "";
			NewPasswordEntry.Text = "";
			SuccessLabel.Text = "Đổi mật khẩu thành công";
			SuccessBanner.IsVisible = true;
		}
		catch (Exception ex)
		{
			ErrorLabel.Text = $"Lỗi kết nối: {ex.Message}";
			ErrorBanner.IsVisible = true;
		}
		finally
		{
			ChangePassButton.IsEnabled = true;
			ChangePassButton.Text = "ĐỔI MẬT KHẨU";
		}
	}

	private sealed record ProfileApiEx(int MaNguoiDung, string? Ho, string? Ten,
		string TenDangNhap, string Email, string? SoDienThoai, string role, bool IsActive);
	private sealed record ErrorResponse(string message);
}
