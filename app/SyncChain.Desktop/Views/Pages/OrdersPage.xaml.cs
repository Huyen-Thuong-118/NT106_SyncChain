using System.Net.Http.Json;
using Microsoft.Maui.Controls;
using SyncChain.Desktop.Models;

namespace SyncChain.Desktop.Views.Pages;

public partial class OrdersPage : ContentPage
{
	private readonly HttpClient _http;

	public IReadOnlyList<OrderItem> Orders { get; private set; } = Array.Empty<OrderItem>();

	// Thống kê từ dashboard
	public string TotalOrders { get; private set; } = "0";
	public string PendingOrders { get; private set; } = "0";
	public string CompletedOrders { get; private set; } = "0";
	public string TotalRevenue { get; private set; } = "0 đ";
	public string PaginationText { get; private set; } = "Đang tải...";

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
			// Gọi API lấy danh sách đơn hàng
			var orders = await _http.GetFromJsonAsync<List<DonHangApi>>("api/order");
			if (orders != null)
			{
				Orders = orders.Select(MapToOrderItem).ToList();
				PaginationText = $"Hiển thị 1 - {orders.Count} trong số {orders.Count} đơn hàng";
				OnPropertyChanged(nameof(Orders));
				OnPropertyChanged(nameof(PaginationText));
			}

			// Gọi API dashboard để lấy thống kê
			try
			{
				var dashboard = await _http.GetFromJsonAsync<DashboardApi>("api/report/dashboard");
				if (dashboard != null)
				{
					TotalOrders = dashboard.TotalOrders.ToString("N0");
					PendingOrders = dashboard.PendingOrders.ToString();
					CompletedOrders = dashboard.CompletedOrders.ToString();
					TotalRevenue = $"{dashboard.TotalRevenue:N0} đ";
					OnPropertyChanged(nameof(TotalOrders));
					OnPropertyChanged(nameof(PendingOrders));
					OnPropertyChanged(nameof(CompletedOrders));
					OnPropertyChanged(nameof(TotalRevenue));
				}
			}
			catch
			{
				// Nếu không có quyền gọi dashboard, dùng dữ liệu từ đơn hàng
				if (orders != null)
				{
					TotalOrders = orders.Count.ToString("N0");
					PendingOrders = orders.Count(o => o.TrangThai is "pending" or "processing").ToString();
					CompletedOrders = orders.Count(o => o.TrangThai == "done").ToString();
					TotalRevenue = $"{orders.Sum(o => o.TongTien):N0} đ";
					OnPropertyChanged(nameof(TotalOrders));
					OnPropertyChanged(nameof(PendingOrders));
					OnPropertyChanged(nameof(CompletedOrders));
					OnPropertyChanged(nameof(TotalRevenue));
				}
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[OrdersPage] Load error: {ex.Message}");
		}
	}

	private static OrderItem MapToOrderItem(DonHangApi dh)
	{
		var (statusText, statusColor) = dh.TrangThai switch
		{
			"pending" => ("Chờ duyệt", Color.FromArgb("#dae2fd")),
			"processing" => ("Đang xử lý", Color.FromArgb("#dbeafe")),
			"done" => ("Hoàn tất", Color.FromArgb("#d3e5f1")),
			"cancel" => ("Đã hủy", Color.FromArgb("#ffdad6")),
			_ => (dh.TrangThai, Color.FromArgb("#eef0f2"))
		};

		return new OrderItem
		{
			Code = $"#ORD-{dh.MaDonHang:0000}",
			Customer = $"Khách hàng #{dh.MaNguoiDung}",
			Email = $"kh{dh.MaNguoiDung}@gmail.com",
			CreatedAt = dh.NgayTao.ToString("dd/MM/yyyy HH:mm"),
			Total = $"{dh.TongTien:N0} đ",
			Status = statusText,
			StatusColor = statusColor,
			Initials = "KH"
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
