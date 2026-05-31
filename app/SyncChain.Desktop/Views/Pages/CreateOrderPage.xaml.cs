using System.Net.Http.Json;
using Microsoft.Maui.Controls;
using SyncChain.Desktop.Models;

namespace SyncChain.Desktop.Views.Pages;

public partial class CreateOrderPage : ContentPage
{
	private readonly HttpClient _http;
	private List<SanPhamApi> _allProducts = new();

	public List<LineItem> Lines { get; private set; } = new();
	public IReadOnlyList<PaymentOption> Payments { get; private set; } = new List<PaymentOption>
	{
		new() { Name = "Tiền mặt khi nhận hàng", IsSelected = true },
		new() { Name = "Chuyển khoản ngân hàng", IsSelected = false },
		new() { Name = "Ví điện tử MoMo", IsSelected = false }
	};

	public CreateOrderPage(HttpClient http)
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
				_allProducts = response.data.Where(p => p.SoLuongTon > 0).ToList();

				// Tạo LineItem từ 2 sản phẩm đầu tiên làm demo
				Lines = _allProducts.Take(2).Select(p => new LineItem
				{
					Name = p.TenSanPham,
					Variant = $"Tồn kho: {p.SoLuongTon}",
					Quantity = "1",
					Price = $"{p.GiaBan:N0} đ",
					Initials = string.Join("", p.TenSanPham.Split(' ', StringSplitOptions.RemoveEmptyEntries)
						.Take(2).Select(w => w[0])).ToUpperInvariant()
				}).ToList();

				OnPropertyChanged(nameof(Lines));
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[CreateOrderPage] Load error: {ex.Message}");
		}
	}

	private async void OnBackClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("//orders");
	}
}
