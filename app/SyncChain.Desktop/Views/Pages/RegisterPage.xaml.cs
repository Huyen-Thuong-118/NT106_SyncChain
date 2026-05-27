namespace SyncChain.Desktop.Views.Pages;

public partial class RegisterPage : ContentPage
{
	public RegisterPage()
	{
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
		await DisplayAlert("Đăng ký demo", "UI đã sẵn cho bước gửi email xác thực và tạo tài khoản. Hiện tại màn hình đang dùng mock frontend.", "Tiếp tục");
		App.ShowShell();
	}

	private async void OnBackClicked(object? sender, EventArgs e)
	{
		await Navigation.PopAsync();
	}
}
