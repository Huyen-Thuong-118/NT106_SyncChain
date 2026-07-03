using System.Net.Http.Json;
using Microsoft.Maui.Controls;
using SyncChain.Desktop.Models;
using SyncChain.Desktop.Services;

namespace SyncChain.Desktop.Views.Pages;

public partial class ProductsPage : ContentPage
{
	private readonly HttpClient _http;

	// Khách hàng thấy nút "Thêm vào giỏ / Chi tiết"; nhân sự nội bộ thấy thanh sức khỏe tồn kho.
	public bool IsCustomer => TokenStore.Role == "customer";
	public bool IsStaff => !IsCustomer;

	public IReadOnlyList<ProductItem> Products { get; private set; } = Array.Empty<ProductItem>();

	// Thống kê từ dashboard
	public string TotalProducts { get; private set; } = "0";
	public string ActiveProducts { get; private set; } = "0";
	public string LowStockProducts { get; private set; } = "0";
	public string PaginationText { get; private set; } = "Đang tải...";

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

	private string _searchQuery = "";
	private string _sortMode = "";
	private bool _inStockOnly = false;

	public async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
	{
		_searchQuery = e.NewTextValue?.Trim() ?? "";
		await LoadProductsAsync();
	}

	public async void OnInStockToggled(object sender, ToggledEventArgs e)
	{
		_inStockOnly = e.Value;
		await LoadProductsAsync();
	}

	private async Task LoadProductsAsync()
	{
		try
		{
			var query = $"api/product?search={Uri.EscapeDataString(_searchQuery)}" +
				(_inStockOnly ? "&inStockOnly=true" : "") +
				(string.IsNullOrEmpty(_sortMode) ? "" : $"&sort={_sortMode}");
			var products = await _http.GetFromJsonAsync<List<SanPhamApi>>(query);
			if (products != null)
			{
				Products = products.Select(MapToProductItem).ToList();
				PaginationText = $"Hiển thị 1 - {products.Count} trong số {products.Count} sản phẩm";
				OnPropertyChanged(nameof(Products));
				OnPropertyChanged(nameof(PaginationText));
			}

			// Gọi API dashboard để lấy thống kê
			try
			{
				var dashboard = await _http.GetFromJsonAsync<DashboardApi>("api/report/dashboard");
				if (dashboard != null)
				{
					TotalProducts = dashboard.TotalProducts.ToString("N0");
					ActiveProducts = dashboard.ActiveProducts.ToString("N0");
					LowStockProducts = dashboard.LowStockProducts.ToString();
					OnPropertyChanged(nameof(TotalProducts));
					OnPropertyChanged(nameof(ActiveProducts));
					OnPropertyChanged(nameof(LowStockProducts));
				}
			}
			catch
			{
				// Nếu không có quyền gọi dashboard, dùng dữ liệu từ sản phẩm
				if (products != null)
				{
					TotalProducts = products.Count.ToString("N0");
					ActiveProducts = products.Count(p => p.TrangThai == "Hoat dong" && p.SoLuongTon > 0).ToString("N0");
					LowStockProducts = products.Count(p => p.SoLuongTon > 0 && p.SoLuongTon <= p.MucTonThap).ToString();
					OnPropertyChanged(nameof(TotalProducts));
					OnPropertyChanged(nameof(ActiveProducts));
					OnPropertyChanged(nameof(LowStockProducts));
				}
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[ProductsPage] Load error: {ex.Message}");
		}
	}

	private static ProductItem MapToProductItem(SanPhamApi sp)
	{
		var initials = string.Join("", sp.TenSanPham.Split(' ', StringSplitOptions.RemoveEmptyEntries)
			.Take(2).Select(w => w[0])).ToUpperInvariant();

		var (badgeText, badgeColor) = sp.TrangThai switch
		{
			"Hoat dong" when sp.SoLuongTon > sp.MucTonThap => ("Còn hàng", Colors.Green),
			"Hoat dong" => ("Sắp hết", Colors.Orange),
			"Ngung ban" => ("Ngừng bán", Colors.Red),
			_ => ("Không rõ", Colors.Gray)
		};

		var health = sp.SoLuongTon <= 0 ? 0.0 :
			Math.Min(1.0, (double)sp.SoLuongTon / Math.Max(1, sp.SoLuongTon + 50));

		return new ProductItem
		{
			MaSanPham = sp.MaSanPham,
			Code = $"SP-{sp.MaSanPham:0000}",
			Name = sp.TenSanPham,
			Description = sp.TrangThai == "Hoat dong" ? "Đang kinh doanh" : "Ngừng kinh doanh",
			Price = $"{sp.GiaBan:N0} đ",
			Stock = sp.SoLuongTon.ToString(),
			BadgeText = badgeText,
			BadgeColor = badgeColor,
			Initials = initials,
			HealthProgress = health
		};
	}

	private async void OnOpenDetailClicked(object? sender, EventArgs e)
	{
		if (sender is Button btn && btn.CommandParameter is int maSp)
			await Shell.Current.GoToAsync($"{nameof(ProductDetailPage)}?productId={maSp}");
		else
			await Shell.Current.GoToAsync(nameof(ProductDetailPage));
	}

	public async void OnAddToCartClicked(object? sender, EventArgs e)
	{
		if (sender is Button btn && btn.CommandParameter is int maSp)
		{
			try
			{
				var response = await _http.PostAsJsonAsync("api/cart/items", new { MaSanPham = maSp, SoLuong = 1 });
				if (response.IsSuccessStatusCode)
					await Shell.Current.DisplayAlert("Thành công", "Đã thêm vào giỏ hàng", "OK");
				else
				{
					var err = await response.Content.ReadFromJsonAsync<ErrorResponse>();
					await Shell.Current.DisplayAlert("Lỗi", err?.message ?? "Không thể thêm vào giỏ", "OK");
				}
			}
			catch (Exception ex)
			{
				await Shell.Current.DisplayAlert("Lỗi", ex.Message, "OK");
			}
		}
	}

	private sealed record ErrorResponse(string message);
}
