using System.Net.Http.Json;
using SyncChain.Desktop.Models;

namespace SyncChain.Desktop.Views.Pages;

public partial class ProductsPage : ContentPage
{
	private readonly HttpClient _http;
	private static readonly Uri ApiBaseUri = Services.ApiClientProvider.Client.BaseAddress
		?? new Uri(Services.ApiClientProvider.ApiBaseUrl);
	private List<SanPhamApi> _allProducts = [];
	private ProductCategoryFilterItem? _selectedCategoryFilter;
	private int _currentPage = 1;
	private int PageSize = 20;

	public IReadOnlyList<ProductItem> Products { get; private set; } = Array.Empty<ProductItem>();
	public IReadOnlyList<ProductCategoryFilterItem> CategoryFilters { get; private set; } = Array.Empty<ProductCategoryFilterItem>();
	public IReadOnlyList<PageButtonItem> PageButtons { get; private set; } = Array.Empty<PageButtonItem>();
	public string TotalProducts { get; private set; } = "0";
	public string ActiveProducts { get; private set; } = "0";
	public string LowStockProducts { get; private set; } = "0";
	public string PaginationText { get; private set; } = "Đang tải...";
	public string EmptyProductsText { get; private set; } = string.Empty;
	public bool CanManageProducts =>
		Services.ApiClientProvider.Role?.Trim().ToLowerInvariant() is "admin" or "manager";
	public bool CanGoPrevious => _currentPage > 1;
	public bool CanGoNext => _currentPage < TotalPages;
	private IReadOnlyList<SanPhamApi> FilteredProducts =>
		_selectedCategoryFilter == null || _selectedCategoryFilter.IsAll
			? _allProducts
			: _selectedCategoryFilter.CategoryId.HasValue
				? _allProducts.Where(x => x.MaDanhMuc == _selectedCategoryFilter.CategoryId.Value).ToList()
				: _allProducts.Where(x => x.MaDanhMuc == null).ToList();
	private int TotalPages => Math.Max(1, (int)Math.Ceiling(FilteredProducts.Count / (double)PageSize));

	public ProductsPage() : this(Services.ApiClientProvider.Client)
	{
	}

	public ProductsPage(HttpClient http)
	{
		_http = http;
		InitializeComponent();
		BindingContext = this;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await LoadProductsAsync();
	}

	private async Task LoadProductsAsync()
	{
		try
		{
			_allProducts = await _http.GetFromJsonAsync<List<SanPhamApi>>("api/product") ?? [];
			TotalProducts = _allProducts.Count.ToString("N0");
			ActiveProducts = _allProducts.Count(x => x.TrangThai == "Hoat dong" && x.SoLuongTon > 0).ToString("N0");
			LowStockProducts = _allProducts.Count(x => x.SoLuongTon > 0 && x.SoLuongTon <= x.MucTonThap).ToString("N0");
			BuildCategoryFilters();
			_currentPage = Math.Min(_currentPage, TotalPages);
			ApplyPagination();
			NotifySummary();
		}
		catch (Exception ex)
		{
			_allProducts = [];
			Products = Array.Empty<ProductItem>();
			CategoryFilters = Array.Empty<ProductCategoryFilterItem>();
			PageButtons = Array.Empty<PageButtonItem>();
			PaginationText = "Không tải được danh sách sản phẩm";
			EmptyProductsText = $"Vui lòng kiểm tra API hoặc quyền truy cập. {ex.Message}";
			NotifyPagination();
		}
	}

	private void ApplyPagination()
	{
		var filteredProducts = FilteredProducts;
		var start = (_currentPage - 1) * PageSize;
		Products = filteredProducts
			.Skip(start)
			.Take(PageSize)
			.Select(MapToProductItem)
			.ToList();

		var firstItem = filteredProducts.Count == 0 ? 0 : start + 1;
		var lastItem = Math.Min(start + PageSize, filteredProducts.Count);
		PaginationText = $"Hiển thị {firstItem} - {lastItem} trong số {_allProducts.Count} sản phẩm";
		EmptyProductsText = _allProducts.Count == 0 ? "Chưa có sản phẩm nào." : string.Empty;
		PaginationText = $"Hiển thị {firstItem} - {lastItem} trong số {filteredProducts.Count} sản phẩm";
		EmptyProductsText = filteredProducts.Count == 0 ? "Không có sản phẩm trong danh mục này." : string.Empty;
		PageButtons = BuildPageButtons();
		NotifyPagination();
	}

	private IReadOnlyList<PageButtonItem> BuildPageButtons()
	{
		var total = TotalPages;
		if (total <= 7)
			return Enumerable.Range(1, total).Select(CreatePageButton).ToList();

		var pages = new SortedSet<int> { 1, 2, total - 1, total };
		for (var page = Math.Max(3, _currentPage - 1); page <= Math.Min(total - 2, _currentPage + 1); page++)
			pages.Add(page);

		var result = new List<PageButtonItem>();
		var previous = 0;
		foreach (var page in pages)
		{
			if (previous > 0 && page - previous > 1)
				result.Add(new PageButtonItem { PageNumber = 0, Text = "…" });
			result.Add(CreatePageButton(page));
			previous = page;
		}
		return result;
	}

	private void BuildCategoryFilters()
	{
		var filters = new List<ProductCategoryFilterItem>
		{
			new()
			{
				CategoryId = null,
				IsAll = true,
				Name = "Tất cả",
				DisplayText = $"Tất cả ({_allProducts.Count:N0})"
			}
		};

		filters.AddRange(_allProducts
			.GroupBy(x => new
			{
				x.MaDanhMuc,
				Name = x.DanhMuc?.TenDanhMuc ?? "Chưa phân loại"
			})
			.OrderBy(group => group.Key.Name)
			.Select(group => new ProductCategoryFilterItem
			{
				CategoryId = group.Key.MaDanhMuc,
				Name = group.Key.Name,
				DisplayText = $"{group.Key.Name} ({group.Count():N0})"
			}));

		CategoryFilters = filters;
		OnPropertyChanged(nameof(CategoryFilters));
	}

	private PageButtonItem CreatePageButton(int page) => new()
	{
		PageNumber = page,
		Text = page.ToString(),
		IsCurrent = page == _currentPage
	};

	private static ProductItem MapToProductItem(SanPhamApi sp)
	{
		var initials = string.Join("", sp.TenSanPham
			.Split(' ', StringSplitOptions.RemoveEmptyEntries)
			.Take(2)
			.Select(x => x[0])).ToUpperInvariant();
		var (status, statusColor) = sp.TrangThai switch
		{
			"Ngung ban" => ("Ngừng bán", Colors.Red),
			_ when sp.SoLuongTon <= 0 => ("Hết hàng", Colors.Red),
			_ when sp.SoLuongTon <= sp.MucTonThap => ("Sắp hết", Colors.Orange),
			_ => ("Đang bán", Colors.Green)
		};
		var performance = sp.HieuSuatPhanTram;
		var performanceIcon = performance > 0 ? "↑" : performance < 0 ? "↓" : "→";
		var performanceColor = performance > 0 ? Colors.Green : performance < 0 ? Colors.Red : Colors.Gray;
		var imageUrl = ParseImageUrls(sp.HinhAnhUrl).FirstOrDefault() ?? string.Empty;

		return new ProductItem
		{
			Id = sp.MaSanPham,
			Code = $"SP-{sp.MaSanPham:0000}",
			Name = sp.TenSanPham,
			ImageUrl = imageUrl,
			Category = sp.DanhMuc?.TenDanhMuc ?? "Chưa phân loại",
			Price = $"{sp.GiaBan:N0} đ",
			Stock = sp.SoLuongTon.ToString("N0"),
			BadgeText = status,
			BadgeColor = statusColor,
			Initials = initials,
			PerformanceIcon = performanceIcon,
			PerformanceText = $"{Math.Abs(performance):0.#}%",
			PerformanceColor = performanceColor
		};
	}

	private static IEnumerable<string> ParseImageUrls(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
			yield break;

		foreach (var item in value.Split([',', ';', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries))
		{
			var url = item.Trim();
			if (Uri.TryCreate(url, UriKind.Absolute, out _))
				yield return url;
			else
				yield return new Uri(ApiBaseUri, url.TrimStart('/')).ToString();
		}
	}

	private void NotifySummary()
	{
		OnPropertyChanged(nameof(TotalProducts));
		OnPropertyChanged(nameof(ActiveProducts));
		OnPropertyChanged(nameof(LowStockProducts));
	}

	private void NotifyPagination()
	{
		OnPropertyChanged(nameof(Products));
		OnPropertyChanged(nameof(CategoryFilters));
		OnPropertyChanged(nameof(PageButtons));
		OnPropertyChanged(nameof(PaginationText));
		OnPropertyChanged(nameof(EmptyProductsText));
		OnPropertyChanged(nameof(CanGoPrevious));
		OnPropertyChanged(nameof(CanGoNext));
	}


	private void OnCategoryFilterChanged(object? sender, EventArgs e)
	{
		_selectedCategoryFilter = CategoryFilterPicker.SelectedItem as ProductCategoryFilterItem;
		_currentPage = 1;
		ApplyPagination();
	}

	private void OnPreviousPageClicked(object? sender, EventArgs e)
	{
		if (_currentPage <= 1) return;
		_currentPage--;
		ApplyPagination();
	}

	private void OnNextPageClicked(object? sender, EventArgs e)
	{
		if (_currentPage >= TotalPages) return;
		_currentPage++;
		ApplyPagination();
	}

	private void OnPageClicked(object? sender, EventArgs e)
	{
		if (sender is not Button button ||
			!int.TryParse(button.CommandParameter?.ToString(), out var page) ||
			page <= 0 || page == _currentPage)
			return;
		_currentPage = page;
		ApplyPagination();
	}

	private async void OnOpenDetailClicked(object? sender, EventArgs e)
	{
		if (sender is Button button && int.TryParse(button.CommandParameter?.ToString(), out var productId))
			await Shell.Current.GoToAsync($"{nameof(ProductDetailPage)}?productId={productId}");
	}

	private async void OnAddProductClicked(object? sender, EventArgs e) =>
		await Shell.Current.GoToAsync(nameof(ProductFormPage));
}
