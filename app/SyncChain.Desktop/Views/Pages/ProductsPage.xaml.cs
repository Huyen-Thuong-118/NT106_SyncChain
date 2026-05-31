using System.Net.Http.Json;
using Microsoft.Maui.Controls;
using SyncChain.Desktop.Models;

namespace SyncChain.Desktop.Views.Pages;

public partial class ProductsPage : ContentPage
{
	private readonly HttpClient _http;

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

	private async Task LoadProductsAsync()
	{
		try
		{
			// Gọi API lấy danh sách sản phẩm
			var products = await _http.GetFromJsonAsync<List<SanPhamApi>>("api/product");
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
		await Shell.Current.GoToAsync(nameof(ProductDetailPage));
	}
}
