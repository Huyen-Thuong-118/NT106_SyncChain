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
		// Màn hình đăng ký hiện là mock UI: các Entry chưa được bind và chưa gọi
		// api/Auth/register nên KHÔNG hề có tài khoản hay JWT nào được tạo.
		// Trước đây code gọi App.ShowShell() để nhảy thẳng vào AppShell (khu quản trị)
		// mà không có token — mọi request có [Authorize] (vd GET api/order ở OrdersPage)
		// bị backend trả 401 và làm sập app. Vì đây vẫn là demo, ta đưa người dùng
		// quay lại trang Đăng nhập thay vì vào shell đã xác thực.
		await DisplayAlert("Đăng ký demo", "UI đã sẵn cho bước gửi email xác thực và tạo tài khoản. Hiện tại màn hình đang dùng mock frontend. Vui lòng đăng nhập để vào hệ thống.", "Tiếp tục");
		await Navigation.PopAsync();
	}

	private async void OnBackClicked(object? sender, EventArgs e)
	{
		await Navigation.PopAsync();
	}
}
