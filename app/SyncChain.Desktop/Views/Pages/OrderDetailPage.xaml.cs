using System.Net.Http.Json;
using Microsoft.Maui.Controls;
using SyncChain.Desktop.Models;
using SyncChain.Desktop.Services;

namespace SyncChain.Desktop.Views.Pages;

[QueryProperty(nameof(OrderId), "orderId")]
public partial class OrderDetailPage : ContentPage
{
	private readonly HttpClient _http;
	private int _orderId;

	public int OrderId
	{
		get => _orderId;
		set => _orderId = value;
	}

	public string OrderCode { get; private set; } = "Đang tải...";
	public string OrderDate { get; private set; } = "";
	public string ProductCount { get; private set; } = "0 sản phẩm";
	public string OrderStatus { get; private set; } = "";
	public string OrderStatusColor { get; private set; } = "#5C647A";

	public string CustomerName { get; private set; } = "";
	public string CustomerEmail { get; private set; } = "";
	public string CustomerPhone { get; private set; } = "";
	public string CustomerAddress { get; private set; } = "";
	public string CustomerInitials { get; private set; } = "KH";

	public string Subtotal { get; private set; } = "0 đ";
	public string Shipping { get; private set; } = "Miễn phí";
	public string Discount { get; private set; } = "0 đ";
	public string TotalAmount { get; private set; } = "0 đ";

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
		if (_orderId > 0)
			await LoadOrderDetailAsync();
	}

	private async Task LoadOrderDetailAsync()
	{
		try
		{
			// Lấy chi tiết sản phẩm trong đơn
			var details = await _http.GetFromJsonAsync<List<ChiTietDonHangApi>>($"api/order/{_orderId}");
			if (details == null) return;

			// Lấy header đơn hàng từ list (cùng 1 call đã có trong cache browser)
			var orders = await _http.GetFromJsonAsync<List<DonHangApi>>("api/order");
			var order = orders?.FirstOrDefault(o => o.MaDonHang == _orderId);
			if (order == null) return;

			// Thông tin đơn hàng
			OrderCode = $"#ORD-{order.MaDonHang:0000}";
			OrderDate = $"Được tạo vào {order.NgayTao:dd/MM/yyyy} lúc {order.NgayTao:HH:mm}";
			ProductCount = $"{details.Count} sản phẩm";

			var (statusText, statusColor) = order.TrangThai switch
			{
				"Draft"      => ("Chờ duyệt",  "#dae2fd"),
				"Approved"   => ("Đã duyệt",   "#dae2fd"),
				"Processing" => ("Đang xử lý", "#dbeafe"),
				"Done"       => ("Hoàn tất",   "#d3e5f1"),
				"Cancelled"  => ("Đã hủy",     "#ffdad6"),
				_ => (order.TrangThai, "#eef0f2")
			};
			OrderStatus = statusText;
			OrderStatusColor = statusColor;

			// Thông tin giao hàng
			CustomerName = order.NguoiNhan ?? $"Khách hàng #{order.MaNguoiDung}";
			CustomerEmail = TokenStore.Email ?? "";
			CustomerPhone = order.SoDienThoaiNhan ?? "Chưa cập nhật";
			CustomerAddress = order.DiaChiGiao ?? "Chưa cập nhật";
			CustomerInitials = string.IsNullOrEmpty(order.NguoiNhan) ? "KH"
				: string.Concat(order.NguoiNhan.Split(' ', StringSplitOptions.RemoveEmptyEntries)
					.TakeLast(2).Select(w => char.ToUpperInvariant(w[0])));

			// Tài chính
			Subtotal = $"{order.TongTien:N0} đ";
			TotalAmount = $"{order.TongTien:N0} đ";

			// Sản phẩm trong đơn
			Lines = details.Select(item => new LineItem
			{
				Name = item.SanPham?.TenSanPham ?? $"SP-{item.MaSanPham}",
				Variant = $"SP-{item.MaSanPham:0000}",
				Quantity = item.SoLuong.ToString(),
				Price = $"{item.DonGia:N0} đ",
				Initials = string.Join("", (item.SanPham?.TenSanPham ?? "SP")
					.Split(' ', StringSplitOptions.RemoveEmptyEntries)
					.Take(2).Select(w => char.ToUpperInvariant(w[0])))
			}).ToList();

			// Timeline phản ánh đúng trạng thái thật của đơn: Draft → Approved → Processing → Done
			Timeline = BuildTimeline(order.TrangThai, order.NgayTao);

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

	// Dựng timeline theo tiến trình trạng thái thực tế của đơn hàng.
	private static IReadOnlyList<TimelineItem> BuildTimeline(string trangThai, DateTime ngayTao)
	{
		var steps = new[]
		{
			("Draft",      "Đơn hàng đã tạo"),
			("Approved",   "Đã duyệt"),
			("Processing", "Đang xử lý / đóng gói"),
			("Done",       "Hoàn tất giao hàng")
		};

		if (trangThai == "Cancelled")
		{
			return new List<TimelineItem>
			{
				new() { Title = "Đơn hàng đã tạo", Time = ngayTao.ToString("dd/MM/yyyy HH:mm"), State = "completed", Accent = Colors.Green },
				new() { Title = "Đơn hàng đã hủy", Time = "", State = "cancelled", Accent = Colors.Red }
			};
		}

		var currentIdx = Array.FindIndex(steps, s => s.Item1 == trangThai);
		if (currentIdx < 0) currentIdx = 0;

		return steps.Select((s, i) => new TimelineItem
		{
			Title = s.Item2,
			Time = i == 0 ? ngayTao.ToString("dd/MM/yyyy HH:mm")
				 : i <= currentIdx ? "Đã cập nhật" : "Dự kiến",
			State = i < currentIdx ? "completed" : i == currentIdx ? "current" : "pending",
			Accent = i < currentIdx ? Colors.Green : i == currentIdx ? Colors.Blue : Colors.Gray
		}).ToList();
	}

	private async void OnBackClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("..");
	}
}
