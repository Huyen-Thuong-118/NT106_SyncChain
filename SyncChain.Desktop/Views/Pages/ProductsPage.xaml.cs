using System.Collections.ObjectModel;
using System.Globalization;
using SyncChain.Desktop.Models;
using SyncChain.Desktop.Services;

namespace SyncChain.Desktop.Views.Pages;

public partial class ProductsPage : ContentPage
{
	private readonly List<ProductItem> _allProducts = new();

	public ObservableCollection<ProductItem> Products { get; } = new();

	public bool CanManageProducts => SyncChainApiClient.Instance.CanManageProducts;

	public ProductsPage()
	{
		InitializeComponent();
		BindingContext = this;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		ApplyPermissions();
		await LoadProductsAsync();
	}

	// Tải danh sách sản phẩm từ API và cập nhật giao diện.
	private async Task LoadProductsAsync()
	{
		try
		{
			var products = await SyncChainApiClient.Instance.GetProductsAsync();
			_allProducts.Clear();
			_allProducts.AddRange(products);
			ApplyFilter();
			UpdateSummary();
		}
		catch (Exception ex)
		{
			Products.Clear();
			_allProducts.Clear();
			UpdateSummary();
			await DisplayAlert("Không tải được sản phẩm", ex.Message, "OK");
		}
	}

	// Bật/tắt các nút quản lý theo quyền người dùng.
	private void ApplyPermissions()
	{
		var canManage = SyncChainApiClient.Instance.CanManageProducts;
		OnPropertyChanged(nameof(CanManageProducts));
		ShowCreateFormButton.IsVisible = canManage;
		PermissionLabel.Text = canManage
			? "Bạn có quyền thêm/sửa/xóa sản phẩm"
			: "Chỉ admin hoặc manager được thêm/sửa/xóa";
	}

	// Lọc sản phẩm theo từ khóa tìm kiếm.
	private void ApplyFilter()
	{
		var keyword = SearchEntry.Text?.Trim() ?? string.Empty;
		var filtered = string.IsNullOrWhiteSpace(keyword)
			? _allProducts
			: _allProducts
				.Where(x => x.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase)
					|| x.Code.Contains(keyword, StringComparison.OrdinalIgnoreCase))
				.ToList();

		Products.Clear();
		foreach (var product in filtered)
		{
			Products.Add(product);
		}

		EmptyLabel.IsVisible = Products.Count == 0;
	}

	// Tính các số liệu tổng quan ở đầu trang.
	private void UpdateSummary()
	{
		TotalProductsLabel.Text = _allProducts.Count.ToString(CultureInfo.InvariantCulture);
		ActiveProductsLabel.Text = _allProducts.Count(x => x.StockQuantity > 0).ToString(CultureInfo.InvariantCulture);
		LowStockProductsLabel.Text = _allProducts.Count(x => x.StockQuantity > 0 && x.StockQuantity <= 10).ToString(CultureInfo.InvariantCulture);
		OutOfStockProductsLabel.Text = _allProducts.Count(x => x.StockQuantity <= 0).ToString(CultureInfo.InvariantCulture);
		EmptyLabel.IsVisible = Products.Count == 0;
	}

	// Cập nhật bộ lọc khi người dùng nhập tìm kiếm.
	private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
	{
		ApplyFilter();
	}

	// Tải lại danh sách sản phẩm thủ công.
	private async void OnRefreshClicked(object? sender, EventArgs e)
	{
		await LoadProductsAsync();
	}

	// Mở màn hình tạo sản phẩm nếu đủ quyền.
	private async void OnShowCreateFormClicked(object? sender, EventArgs e)
	{
		if (!EnsureCanManageProducts())
			return;

		await Shell.Current.GoToAsync(nameof(CreateProductPage));
	}

	// Mở trang chi tiết ở chế độ chỉnh sửa sản phẩm.
	private async void OnEditProductClicked(object? sender, EventArgs e)
	{
		if (!EnsureCanManageProducts())
			return;

		if ((sender as Button)?.CommandParameter is not ProductItem product)
			return;

		await Shell.Current.GoToAsync($"{nameof(ProductDetailPage)}?productId={product.Id}");
	}

	// Xác nhận và xóa sản phẩm đã chọn.
	private async void OnDeleteProductClicked(object? sender, EventArgs e)
	{
		if (!EnsureCanManageProducts())
			return;

		if ((sender as Button)?.CommandParameter is not ProductItem product)
			return;

		var confirmed = await DisplayAlert("Xóa sản phẩm", $"Xóa {product.Name}?", "Xóa", "Hủy");
		if (!confirmed)
			return;

		try
		{
			await SyncChainApiClient.Instance.DeleteProductAsync(product.Id);
			await LoadProductsAsync();
			await DisplayAlert("Xóa sản phẩm", "Đã xóa sản phẩm.", "OK");
		}
		catch (Exception ex)
		{
			await DisplayAlert("Không xóa được sản phẩm", ex.Message, "OK");
		}
	}

	// Mở trang chi tiết sản phẩm.
	private async void OnOpenDetailClicked(object? sender, EventArgs e)
	{
		if ((sender as Button)?.CommandParameter is not ProductItem product)
			return;

		await Shell.Current.GoToAsync($"{nameof(ProductDetailPage)}?productId={product.Id}");
	}

	// Chặn thao tác quản lý khi người dùng không đủ quyền.
	private bool EnsureCanManageProducts()
	{
		if (SyncChainApiClient.Instance.CanManageProducts)
			return true;

		DisplayAlert("Không có quyền", "Chỉ admin hoặc manager được quản lý sản phẩm.", "OK");
		return false;
	}

}
