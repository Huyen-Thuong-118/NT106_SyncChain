using SyncChain.Desktop.Models;
using SyncChain.Desktop.Services;

namespace SyncChain.Desktop.Views.Pages;

public partial class ProductsPage : ContentPage
{
	public IReadOnlyList<ProductItem> Products => DemoData.Products;

	public ProductsPage()
	{
		InitializeComponent();
		BindingContext = this;
	}

	private async void OnOpenDetailClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync(nameof(ProductDetailPage));
	}
}
