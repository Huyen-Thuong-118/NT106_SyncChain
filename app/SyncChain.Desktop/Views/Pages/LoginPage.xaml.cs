namespace SyncChain.Desktop.Views.Pages;

public partial class LoginPage : ContentPage
{
	public LoginPage()
	{
		InitializeComponent();
	}

	private void OnLoginClicked(object? sender, EventArgs e)
	{
		App.ShowShell();
	}

	private void OnCustomerLoginClicked(object? sender, EventArgs e)
	{
		App.ShowCustomerShell();
	}

	private async void OnRegisterClicked(object? sender, EventArgs e)
	{
		await Navigation.PushAsync(new RegisterPage());
	}
}
