using SyncChain.Desktop.Models;
using SyncChain.Desktop.Services;

namespace SyncChain.Desktop.Views.Pages;

public partial class ProductDetailPage : ContentPage
{
	public IReadOnlyList<InventoryEvent> Events => DemoData.InventoryEvents;

	public ProductDetailPage()
	{
		InitializeComponent();
		BindingContext = this;
	}

	private async void OnBackClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("..");
	}
}
