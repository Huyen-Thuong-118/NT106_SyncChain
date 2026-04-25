using SyncChain.Desktop.Models;
using SyncChain.Desktop.Services;

namespace SyncChain.Desktop.Views.Pages;

public partial class OrdersPage : ContentPage
{
	public IReadOnlyList<OrderItem> Orders => DemoData.Orders;

	public OrdersPage()
	{
		InitializeComponent();
		BindingContext = this;
	}

	private async void OnCreateOrderClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("//create-order");
	}

	private async void OnOpenDetailClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync(nameof(OrderDetailPage));
	}
}
