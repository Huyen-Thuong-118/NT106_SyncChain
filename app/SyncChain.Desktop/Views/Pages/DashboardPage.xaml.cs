using System.Net.Http.Json;
using Microsoft.Maui.Controls;
using SyncChain.Desktop.Models;

namespace SyncChain.Desktop.Views.Pages;

public partial class DashboardPage : ContentPage
{
	private readonly HttpClient _http;

	public IReadOnlyList<StatCard> Stats { get; private set; } = Array.Empty<StatCard>();
	public IReadOnlyList<AlertItem> Alerts { get; private set; } = Array.Empty<AlertItem>();
	public IReadOnlyList<ActivityItem> Activities { get; private set; } = Array.Empty<ActivityItem>();
	public IReadOnlyList<BridgeItem> Bridges { get; private set; } = Array.Empty<BridgeItem>();

	public DashboardPage(HttpClient http)
	{
		_http = http;
		InitializeComponent();
		BindingContext = this;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await LoadDashboardAsync();
	}

	private async Task LoadDashboardAsync()
	{
		try
		{
			// Lấy sản phẩm
			var productsResp = await _http.GetFromJsonAsync<ApiResponse<List<SanPhamApi>>>("api/sanpham");
			// Lấy đơn hàng
			var ordersResp = await _http.GetFromJsonAsync<ApiResponse<List<DonHangApi>>>("api/donhang");

			var products = productsResp?.data ?? new List<SanPhamApi>();
			var orders = ordersResp?.data ?? new List<DonHangApi>();

			var totalProducts = products.Count;
			var lowStockCount = products.Count(p => p.SoLuongTon > 0 && p.SoLuongTon <= p.MucTonThap);
			var totalOrders = orders.Count;
			var totalRevenue = orders.Sum(o => o.TongTien);

			// Tạo stat cards
			Stats = new List<StatCard>
			{
				new() { Title = "TỔNG SẢN PHẨM", Value = totalProducts.ToString(), Icon = "📦",
					Accent = Color.FromArgb("#213145") },
				new() { Title = "TỔNG ĐƠN HÀNG", Value = totalOrders.ToString(), Icon = "🛒",
					Accent = Color.FromArgb("#50616B") },
				new() { Title = "DOANH THU", Value = $"{totalRevenue:N0} đ", Icon = "💰",
					Accent = Color.FromArgb("#5C647A") },
				new() { Title = "TỒN KHO THẤP", Value = lowStockCount.ToString(), Icon = "⚠️",
					Accent = lowStockCount > 0 ? Color.FromArgb("#BA1A1A") : Color.FromArgb("#5C647A") },
				new() { Title = "ĐANG XỬ LÝ", Value = orders.Count(o => o.TrangThaiDon is "Da dat hang" or "Dang xu ly").ToString(),
					Icon = "⏳", Accent = Color.FromArgb("#B7C9D5") }
			};

			// Tạo cảnh báo tồn kho thấp
			Alerts = products
				.Where(p => p.SoLuongTon > 0 && p.SoLuongTon <= p.MucTonThap)
				.Take(3)
				.Select(p => new AlertItem
				{
					Name = p.TenSanPham,
					Code = $"SP-{p.MaSanPham:0000}",
					StockText = $"Còn {p.SoLuongTon}",
					Accent = p.SoLuongTon <= 5 ? Color.FromArgb("#BA1A1A") : Color.FromArgb("#50616B")
				})
				.ToList();

			// Tạo hoạt động gần đây từ đơn hàng
			Activities = orders
				.OrderByDescending(o => o.NgayTao)
				.Take(4)
				.Select(o => new ActivityItem
				{
					Title = $"Đơn #ORD-{o.MaDonHang:0000} - {MapStatus(o.TrangThaiDon)}",
					Time = o.NgayTao.ToString("dd/MM HH:mm"),
					Icon = "🛒",
					Accent = Color.FromArgb("#50616B")
				})
				.ToList();

			// Bridge items
			Bridges = new List<BridgeItem>
			{
				new() { Title = "API Backend", Description = $"Đã kết nối {totalProducts} sản phẩm, {totalOrders} đơn hàng",
					Status = "Đã kết nối", Accent = Color.FromArgb("#213145") }
			};

			OnPropertyChanged(nameof(Stats));
			OnPropertyChanged(nameof(Alerts));
			OnPropertyChanged(nameof(Activities));
			OnPropertyChanged(nameof(Bridges));
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[DashboardPage] Load error: {ex.Message}");
		}
	}

	private static string MapStatus(string status) => status switch
	{
		"Da dat hang" => "Chờ duyệt",
		"Dang xu ly" => "Đang xử lý",
		"Dang van chuyen" => "Đang vận chuyển",
		"Hoan tat" => "Hoàn tất",
		"Huy" => "Đã hủy",
		_ => status
	};
}
