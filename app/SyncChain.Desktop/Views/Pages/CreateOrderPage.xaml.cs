using SyncChain.Desktop.Models;
using SyncChain.Desktop.Services;

namespace SyncChain.Desktop.Views.Pages;

public partial class CreateOrderPage : ContentPage
{
	public IReadOnlyList<LineItem> Lines => DemoData.OrderLines.Take(2).ToList();
	public IReadOnlyList<PaymentOption> Payments => DemoData.Payments;

	public CreateOrderPage()
	{
		InitializeComponent();
		BindingContext = this;
	}

	private async void OnBackClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("//orders");
	}
}
