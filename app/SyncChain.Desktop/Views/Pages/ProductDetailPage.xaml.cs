using System.Net.Http.Json;
using Microsoft.Maui.Controls;
using SyncChain.Desktop.Models;

namespace SyncChain.Desktop.Views.Pages;

public partial class ProductDetailPage : ContentPage
{
	private readonly HttpClient _http;

	public IReadOnlyList<InventoryEvent> Events { get; private set; } = Array.Empty<InventoryEvent>();

	public ProductDetailPage(HttpClient http)
	{
		_http = http;
		InitializeComponent();
		BindingContext = this;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await LoadProductDetailAsync();
	}

	private async Task LoadProductDetailAsync()
	{
		try
		{
			var response = await _http.GetFromJsonAsync<ApiResponse<List<SanPhamApi>>>("api/sanpham");
			if (response?.success == true && response.data?.Any() == true)
			{
				var product = response.data.First();

				Events = new List<InventoryEvent>
				{
					new() { Date = DateTime.Now.AddDays(-7).ToString("dd/MM/yyyy"), Type = "Nhập kho",
						Quantity = $"+{product.SoLuongTon}", Actor = "Quản lý", Note = "Nhập hàng đợt đầu",
						Accent = Color.FromArgb("#5C647A") },
					new() { Date = DateTime.Now.AddDays(-3).ToString("dd/MM/yyyy"), Type = "Kiểm kê",
						Quantity = product.SoLuongTon.ToString(), Actor = "Nhân viên kho",
						Note = "Đối chiếu tồn kho thực tế", Accent = Color.FromArgb("#50616B") },
					new() { Date = DateTime.Now.ToString("dd/MM/yyyy"), Type = "Cập nhật",
						Quantity = product.SoLuongTon.ToString(), Actor = "Hệ thống",
						Note = $"Trạng thái: {product.TrangThai}", Accent = Color.FromArgb("#213145") }
				};

				OnPropertyChanged(nameof(Events));
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[ProductDetailPage] Load error: {ex.Message}");
		}
	}

	private async void OnBackClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("..");
	}
}
