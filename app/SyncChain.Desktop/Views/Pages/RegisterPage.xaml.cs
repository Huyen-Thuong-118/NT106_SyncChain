namespace SyncChain.Desktop.Views.Pages;

public partial class RegisterPage : ContentPage
{
	public RegisterPage()
	{
		InitializeComponent();
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
