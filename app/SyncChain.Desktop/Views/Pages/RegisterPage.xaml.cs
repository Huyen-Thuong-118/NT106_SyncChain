using System.Net.Http.Json;
using System.Text.RegularExpressions;
using SyncChain.Desktop.Services;

namespace SyncChain.Desktop.Views.Pages;

public partial class RegisterPage : ContentPage
{
	public RegisterPage()
	{
		InitializeComponent();
		ApplyBackgroundImage();
		HoEntry.TextChanged += (_, _) => ValidateField(HoEntry, HoError, "Vui lòng nhập họ");
		TenEntry.TextChanged += (_, _) => ValidateField(TenEntry, TenError, "Vui lòng nhập tên");
		UsernameEntry.TextChanged += (_, _) => ValidateField(UsernameEntry, UsernameError, "Vui lòng nhập tên đăng nhập");
		PhoneEntry.TextChanged += (_, _) => ValidatePhone();
		EmailEntry.TextChanged += (_, _) => ValidateEmail();
		PasswordEntry.TextChanged += (_, _) => ValidatePassword();
		ConfirmPasswordEntry.TextChanged += (_, _) => ValidateConfirmPassword();
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

	private static void ValidateField(Entry entry, Label error, string msg)
	{
		error.Text = msg;
		error.IsVisible = string.IsNullOrWhiteSpace(entry.Text);
	}

	private void ValidatePhone()
	{
		var phone = PhoneEntry.Text?.Trim() ?? "";
		if (string.IsNullOrWhiteSpace(phone)) { PhoneError.Text = "Vui lòng nhập số điện thoại"; PhoneError.IsVisible = true; }
		else if (!Regex.IsMatch(phone, @"^\d{9,11}$")) { PhoneError.Text = "Số điện thoại không hợp lệ (9-11 chữ số)"; PhoneError.IsVisible = true; }
		else PhoneError.IsVisible = false;
	}

	private void ValidateEmail()
	{
		var email = EmailEntry.Text?.Trim() ?? "";
		if (string.IsNullOrWhiteSpace(email)) { EmailError.Text = "Vui lòng nhập email"; EmailError.IsVisible = true; }
		else if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$")) { EmailError.Text = "Email không đúng định dạng"; EmailError.IsVisible = true; }
		else EmailError.IsVisible = false;
	}

	private void ValidatePassword()
	{
		var pwd = PasswordEntry.Text ?? "";
		if (pwd.Length > 0 && pwd.Length < 8) { PasswordError.Text = "Mật khẩu phải ít nhất 8 ký tự"; PasswordError.IsVisible = true; }
		else PasswordError.IsVisible = false;
		ValidateConfirmPassword();
	}

	private void ValidateConfirmPassword()
	{
		if (!string.IsNullOrEmpty(ConfirmPasswordEntry.Text) && ConfirmPasswordEntry.Text != PasswordEntry.Text)
		{ ConfirmPasswordError.Text = "Mật khẩu xác nhận không khớp"; ConfirmPasswordError.IsVisible = true; }
		else ConfirmPasswordError.IsVisible = false;
	}

	private bool ValidateAll()
	{
		var valid = true;
		void Check(Entry e, Label err, string msg)
		{ if (string.IsNullOrWhiteSpace(e.Text)) { err.Text = msg; err.IsVisible = true; valid = false; } }

		Check(HoEntry, HoError, "Vui lòng nhập họ");
		Check(TenEntry, TenError, "Vui lòng nhập tên");
		Check(UsernameEntry, UsernameError, "Vui lòng nhập tên đăng nhập");
		ValidatePhone(); if (PhoneError.IsVisible) valid = false;
		ValidateEmail(); if (EmailError.IsVisible) valid = false;

		if (string.IsNullOrWhiteSpace(PasswordEntry.Text))
		{ PasswordError.Text = "Vui lòng nhập mật khẩu"; PasswordError.IsVisible = true; valid = false; }
		else if ((PasswordEntry.Text ?? "").Length < 8)
		{ PasswordError.Text = "Mật khẩu phải ít nhất 8 ký tự"; PasswordError.IsVisible = true; valid = false; }

		if (ConfirmPasswordEntry.Text != PasswordEntry.Text)
		{ ConfirmPasswordError.Text = "Mật khẩu xác nhận không khớp"; ConfirmPasswordError.IsVisible = true; valid = false; }

		if (!AgreeCheckBox.IsChecked)
		{ GeneralErrorLabel.Text = "Vui lòng đồng ý với điều khoản dịch vụ"; GeneralError.IsVisible = true; valid = false; }

		return valid;
	}

	private async void OnRegisterClicked(object? sender, EventArgs e)
	{
		GeneralError.IsVisible = false;
		if (!ValidateAll()) return;

		RegisterButton.IsEnabled = false;
		RegisterButton.Text = "Đang đăng ký...";

		try
		{
			var response = await AppHttpClient.Instance.PostAsJsonAsync("api/auth/register", new
			{
				Ho = HoEntry.Text!.Trim(),
				Ten = TenEntry.Text!.Trim(),
				TenDangNhap = UsernameEntry.Text!.Trim(),
				SoDienThoai = PhoneEntry.Text!.Trim(),
				Email = EmailEntry.Text!.Trim().ToLowerInvariant(),
				Password = PasswordEntry.Text,
				ConfirmPassword = ConfirmPasswordEntry.Text
			});

			if (!response.IsSuccessStatusCode)
			{
				var err = await response.Content.ReadFromJsonAsync<ErrorResponse>();
				GeneralErrorLabel.Text = err?.message ?? "Đăng ký thất bại";
				GeneralError.IsVisible = true;
				return;
			}

			await DisplayAlert("Thành công", "Tạo tài khoản thành công! Vui lòng đăng nhập.", "Đăng nhập");
			await Navigation.PopAsync();
		}
		catch (Exception ex)
		{
			GeneralErrorLabel.Text = $"Lỗi kết nối: {ex.Message}";
			GeneralError.IsVisible = true;
		}
		finally
		{
			RegisterButton.IsEnabled = true;
			RegisterButton.Text = "ĐĂNG KÝ";
		}
	}

	private async void OnBackClicked(object? sender, EventArgs e) => await Navigation.PopAsync();

	private sealed record ErrorResponse(string message);
}
