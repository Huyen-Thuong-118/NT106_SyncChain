using System.Net.Http.Json;
using SyncChain.Desktop.Services;

namespace SyncChain.Desktop.Views.Pages;

public partial class ForgotPasswordPage : ContentPage
{
	private string? _email;

	public ForgotPasswordPage()
	{
		InitializeComponent();
		var source = SigninBackground.CreateSource();
		if (source != null) BackgroundPhoto.Source = source;
	}

	private async void OnSendOtpClicked(object? sender, EventArgs e)
	{
		ErrorBanner.IsVisible = false;
		EmailError.IsVisible = false;

		var email = EmailEntry.Text?.Trim() ?? "";
		if (string.IsNullOrWhiteSpace(email))
		{
			EmailError.Text = "Vui lòng nhập email";
			EmailError.IsVisible = true;
			return;
		}

		SendOtpButton.IsEnabled = false;
		SendOtpButton.Text = "Đang gửi...";

		try
		{
			var response = await AppHttpClient.Instance.PostAsJsonAsync("api/auth/forgot-password", new { Email = email });
			if (!response.IsSuccessStatusCode)
			{
				var err = await response.Content.ReadFromJsonAsync<ErrorResponse>();
				ErrorLabel.Text = err?.message ?? "Không thể gửi OTP";
				ErrorBanner.IsVisible = true;
				return;
			}
			_email = email;
			Step1.IsVisible = false;
			Step2.IsVisible = true;
			SubtitleLabel.Text = $"OTP đã gửi đến {email}. Kiểm tra hộp thư (hoặc console server).";
		}
		catch (Exception ex)
		{
			ErrorLabel.Text = $"Lỗi kết nối: {ex.Message}";
			ErrorBanner.IsVisible = true;
		}
		finally
		{
			SendOtpButton.IsEnabled = true;
			SendOtpButton.Text = "GỬI MÃ OTP";
		}
	}

	private async void OnResetPasswordClicked(object? sender, EventArgs e)
	{
		ErrorBanner.IsVisible = false;
		OtpError.IsVisible = false;
		NewPasswordError.IsVisible = false;
		ConfirmPasswordError.IsVisible = false;

		var otp = OtpEntry.Text?.Trim() ?? "";
		var newPwd = NewPasswordEntry.Text ?? "";
		var confirm = ConfirmPasswordEntry.Text ?? "";
		var valid = true;

		if (string.IsNullOrWhiteSpace(otp)) { OtpError.Text = "Vui lòng nhập OTP"; OtpError.IsVisible = true; valid = false; }
		if (newPwd.Length < 8) { NewPasswordError.Text = "Mật khẩu phải ít nhất 8 ký tự"; NewPasswordError.IsVisible = true; valid = false; }
		if (newPwd != confirm) { ConfirmPasswordError.Text = "Mật khẩu xác nhận không khớp"; ConfirmPasswordError.IsVisible = true; valid = false; }
		if (!valid) return;

		ResetButton.IsEnabled = false;
		ResetButton.Text = "Đang xử lý...";

		try
		{
			var response = await AppHttpClient.Instance.PostAsJsonAsync("api/auth/reset-password", new
			{
				Email = _email,
				Otp = otp,
				NewPassword = newPwd,
				ConfirmPassword = confirm
			});

			if (!response.IsSuccessStatusCode)
			{
				var err = await response.Content.ReadFromJsonAsync<ErrorResponse>();
				ErrorLabel.Text = err?.message ?? "Đặt lại mật khẩu thất bại";
				ErrorBanner.IsVisible = true;
				return;
			}

			await DisplayAlert("Thành công", "Đặt lại mật khẩu thành công. Vui lòng đăng nhập lại.", "OK");
			await Navigation.PopAsync();
		}
		catch (Exception ex)
		{
			ErrorLabel.Text = $"Lỗi kết nối: {ex.Message}";
			ErrorBanner.IsVisible = true;
		}
		finally
		{
			ResetButton.IsEnabled = true;
			ResetButton.Text = "ĐẶT LẠI MẬT KHẨU";
		}
	}

	private async void OnBackClicked(object? sender, EventArgs e) => await Navigation.PopAsync();

	private sealed record ErrorResponse(string message);
}
