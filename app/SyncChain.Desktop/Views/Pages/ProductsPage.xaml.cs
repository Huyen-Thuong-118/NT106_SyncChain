using System.Net.Http.Json;
using Microsoft.Maui.Controls;
using SyncChain.Desktop.Models;

namespace SyncChain.Desktop.Views.Pages;

public partial class ProductsPage : ContentPage
{
	private readonly HttpClient _http;

	public IReadOnlyList<ProductItem> Products { get; private set; } = Array.Empty<ProductItem>();

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
			var response = await _http.GetFromJsonAsync<ApiResponse<List<SanPhamApi>>>("api/sanpham");
			if (response?.success == true && response.data != null)
			{
				Products = response.data.Select(MapToProductItem).ToList();
				OnPropertyChanged(nameof(Products));
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
