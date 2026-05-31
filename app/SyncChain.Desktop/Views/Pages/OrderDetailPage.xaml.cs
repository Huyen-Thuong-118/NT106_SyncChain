using System.Net.Http.Json;
using Microsoft.Maui.Controls;
using SyncChain.Desktop.Models;

namespace SyncChain.Desktop.Views.Pages;

public partial class OrderDetailPage : ContentPage
{
	private readonly HttpClient _http;

	// Thông tin đơn hàng
	public string OrderCode { get; private set; } = "";
	public string OrderDate { get; private set; } = "";
	public string ProductCount { get; private set; } = "0 sản phẩm";
	public string OrderStatus { get; private set; } = "";
	public string OrderStatusColor { get; private set; } = "#5C647A";

	// Thông tin khách hàng
	public string CustomerName { get; private set; } = "";
	public string CustomerEmail { get; private set; } = "";
	public string CustomerPhone { get; private set; } = "";
	public string CustomerAddress { get; private set; } = "";
	public string CustomerInitials { get; private set; } = "";

	// Tài chính
	public string Subtotal { get; private set; } = "0 đ";
	public string Shipping { get; private set; } = "30,000 đ";
	public string Discount { get; private set; } = "-0 đ";
	public string TotalAmount { get; private set; } = "0 đ";

	// Chi tiết đơn hàng
	public IReadOnlyList<LineItem> Lines { get; private set; } = Array.Empty<LineItem>();
	public IReadOnlyList<TimelineItem> Timeline { get; private set; } = Array.Empty<TimelineItem>();

	public OrderDetailPage(HttpClient http)
	{
		_http = http;
		InitializeComponent();
		BindingContext = this;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await LoadOrderDetailAsync();
	}

	private async Task LoadOrderDetailAsync()
	{
		try
		{
			// Lấy danh sách đơn hàng trước
			var orders = await _http.GetFromJsonAsync<List<DonHangApi>>("api/order");
			if (orders == null || orders.Count == 0) return;

			var firstOrder = orders.First();

			// Gọi API lấy chi tiết đơn hàng
			var details = await _http.GetFromJsonAsync<List<ChiTietDonHangApi>>($"api/order/{firstOrder.MaDonHang}");
			if (details == null) return;

			// Cập nhật thông tin đơn hàng
			OrderCode = $"#ORD-{firstOrder.MaDonHang:0000}";
			OrderDate = $"Được tạo vào {firstOrder.NgayTao:dd/MM/yyyy} lúc {firstOrder.NgayTao:HH:mm}";
			ProductCount = $"{details.Count} sản phẩm";

			// Trạng thái
			var (statusText, statusColor) = firstOrder.TrangThai switch
			{
				"pending" => ("Chờ duyệt", "#dae2fd"),
				"processing" => ("Đang xử lý", "#dbeafe"),
				"done" => ("Hoàn tất", "#d3e5f1"),
				"cancel" => ("Đã hủy", "#ffdad6"),
				_ => (firstOrder.TrangThai, "#eef0f2")
			};
			OrderStatus = statusText;
			OrderStatusColor = statusColor;

			// Thông tin khách hàng
			CustomerName = $"Khách hàng #{firstOrder.MaNguoiDung}";
			CustomerEmail = $"kh{firstOrder.MaNguoiDung}@gmail.com";
			CustomerPhone = "Chưa cập nhật";
			CustomerAddress = "Chưa cập nhật";
			CustomerInitials = "KH";

			// Tài chính
			Subtotal = $"{firstOrder.TongTien:N0} đ";
			TotalAmount = $"{firstOrder.TongTien:N0} đ";

			// Chi tiết sản phẩm
			Lines = details.Select(item => new LineItem
			{
				Name = item.SanPham?.TenSanPham ?? $"SP-{item.MaSanPham}",
				Variant = $"SP-{item.MaSanPham:0000}",
				Quantity = item.SoLuong.ToString(),
				Price = $"{item.DonGia:N0} đ",
				Initials = string.Join("", (item.SanPham?.TenSanPham ?? "SP")
					.Split(' ', StringSplitOptions.RemoveEmptyEntries)
					.Take(2).Select(w => w[0])).ToUpperInvariant()
			}).ToList();

			// Timeline từ trạng thái đơn hàng
			Timeline = new List<TimelineItem>
			{
				new() { Title = "Đơn hàng đã tạo", Time = firstOrder.NgayTao.ToString("dd/MM/yyyy HH:mm"), State = "completed", Accent = Colors.Green },
				new() { Title = $"Trạng thái: {statusText}", Time = firstOrder.NgayTao.ToString("dd/MM/yyyy HH:mm"), State = "current", Accent = Colors.Blue },
				new() { Title = "Chờ giao hàng", Time = "Dự kiến", State = "pending", Accent = Colors.Gray }
			};

			// Thông báo UI cập nhật
			OnPropertyChanged(nameof(OrderCode));
			OnPropertyChanged(nameof(OrderDate));
			OnPropertyChanged(nameof(ProductCount));
			OnPropertyChanged(nameof(OrderStatus));
			OnPropertyChanged(nameof(OrderStatusColor));
			OnPropertyChanged(nameof(CustomerName));
			OnPropertyChanged(nameof(CustomerEmail));
			OnPropertyChanged(nameof(CustomerPhone));
			OnPropertyChanged(nameof(CustomerAddress));
			OnPropertyChanged(nameof(CustomerInitials));
			OnPropertyChanged(nameof(Subtotal));
			OnPropertyChanged(nameof(Shipping));
			OnPropertyChanged(nameof(Discount));
			OnPropertyChanged(nameof(TotalAmount));
			OnPropertyChanged(nameof(Lines));
			OnPropertyChanged(nameof(Timeline));
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[OrderDetailPage] Load error: {ex.Message}");
		}
	}

	private async void OnBackClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("..");
	}
}
