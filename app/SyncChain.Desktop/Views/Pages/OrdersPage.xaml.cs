using System.Net.Http.Json;
using Microsoft.Maui.Controls;
using SyncChain.Desktop.Models;
using SyncChain.Desktop.Services;

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
					PendingOrders = orders.Count(o => o.TrangThai is "Draft" or "Approved" or "Processing").ToString();
					CompletedOrders = orders.Count(o => o.TrangThai == "Done").ToString();
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
			"Draft"      => ("Chờ duyệt",  Color.FromArgb("#dae2fd")),
			"Approved"   => ("Đã duyệt",   Color.FromArgb("#dae2fd")),
			"Processing" => ("Đang xử lý", Color.FromArgb("#dbeafe")),
			"Done"       => ("Hoàn tất",   Color.FromArgb("#d3e5f1")),
			"Cancelled"  => ("Đã hủy",     Color.FromArgb("#ffdad6")),
			_ => (dh.TrangThai, Color.FromArgb("#eef0f2"))
		};

		var recipientName = dh.NguoiNhan ?? $"Khách hàng #{dh.MaNguoiDung}";

		return new OrderItem
		{
			MaDonHang = dh.MaDonHang,
			Code = $"#ORD-{dh.MaDonHang:0000}",
			Customer = recipientName,
			Email = dh.DiaChiGiao ?? "Chưa có địa chỉ",
			CreatedAt = dh.NgayTao.ToString("dd/MM/yyyy HH:mm"),
			Total = $"{dh.TongTien:N0} đ",
			Status = statusText,
			StatusColor = statusColor,
			Initials = string.Concat(recipientName.Split(' ').TakeLast(2).Select(w => w[0])).ToUpperInvariant()
		};
	}

	private async void OnCreateOrderClicked(object? sender, EventArgs e)
	{
		// Khách hàng đặt hàng qua giỏ hàng → điều hướng tới trang sản phẩm để chọn mua.
		// Route "//create-order" chỉ tồn tại trong AppShell (nhân sự nội bộ).
		if (TokenStore.Role == "customer")
			await Shell.Current.GoToAsync("//products");
		else
			await Shell.Current.GoToAsync("//create-order");
	}

	private async void OnOpenDetailClicked(object? sender, EventArgs e)
	{
		if (sender is Button btn && btn.CommandParameter is int orderId)
			await Shell.Current.GoToAsync($"{nameof(OrderDetailPage)}?orderId={orderId}");
	}
}
