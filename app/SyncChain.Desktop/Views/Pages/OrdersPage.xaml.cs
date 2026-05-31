using System.Net.Http.Json;
using Microsoft.Maui.Controls;
using SyncChain.Desktop.Models;

namespace SyncChain.Desktop.Views.Pages;

public partial class OrdersPage : ContentPage
{
	private readonly HttpClient _http;

	public IReadOnlyList<OrderItem> Orders { get; private set; } = Array.Empty<OrderItem>();

	public OrdersPage(HttpClient http)
	{
		_http = http;
		InitializeComponent();
		BindingContext = this;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await LoadOrdersAsync();
	}

	private async Task LoadOrdersAsync()
	{
		try
		{
			var response = await _http.GetFromJsonAsync<ApiResponse<List<DonHangApi>>>("api/donhang");
			if (response?.success == true && response.data != null)
			{
				Orders = response.data.Select(MapToOrderItem).ToList();
				OnPropertyChanged(nameof(Orders));
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[OrdersPage] Load error: {ex.Message}");
		}
	}

	private static OrderItem MapToOrderItem(DonHangApi dh)
	{
		var (statusText, statusColor) = dh.TrangThaiDon switch
		{
			"Da dat hang" => ("Chờ duyệt", Color.FromArgb("#dae2fd")),
			"Dang xu ly" => ("Đang xử lý", Color.FromArgb("#dbeafe")),
			"Dang van chuyen" => ("Đang vận chuyển", Color.FromArgb("#dbeafe")),
			"Hoan tat" => ("Hoàn tất", Color.FromArgb("#d3e5f1")),
			"Huy" => ("Hủy", Color.FromArgb("#ffdad6")),
			_ => (dh.TrangThaiDon, Color.FromArgb("#eef0f2"))
		};

		return new OrderItem
		{
			Code = $"#ORD-{dh.MaDonHang:0000}",
			Customer = $"Khách hàng #{dh.MaKhachHang}",
			Email = $"kh{dh.MaKhachHang}@gmail.com",
			CreatedAt = dh.NgayTao.ToString("dd/MM/yyyy HH:mm"),
			Total = $"{dh.TongTien:N0} đ",
			Status = statusText,
			StatusColor = statusColor,
			Initials = $"KH"
		};
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
