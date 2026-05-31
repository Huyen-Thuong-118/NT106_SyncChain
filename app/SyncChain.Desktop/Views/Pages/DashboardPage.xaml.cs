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

	// Dữ liệu cho biểu đồ xu hướng (7 ngày)
	public IReadOnlyList<TrendApi> TrendData { get; private set; } = Array.Empty<TrendApi>();

	// Dữ liệu cho phân bổ tồn kho
	public IReadOnlyList<TopProductApi> TopProducts { get; private set; } = Array.Empty<TopProductApi>();

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
			// Gọi API dashboard tổng hợp
			var dashboard = await _http.GetFromJsonAsync<DashboardApi>("api/report/dashboard");
			if (dashboard == null) return;

			// Tạo stat cards từ dữ liệu dashboard
			Stats = new List<StatCard>
			{
				new() { Title = "TỔNG SẢN PHẨM", Value = dashboard.TotalProducts.ToString(), Icon = "📦",
					Accent = Color.FromArgb("#213145") },
				new() { Title = "SẢN PHẨM HOẠT ĐỘNG", Value = dashboard.ActiveProducts.ToString(), Icon = "✅",
					Accent = Color.FromArgb("#50616B") },
				new() { Title = "TỔNG ĐƠN HÀNG", Value = dashboard.TotalOrders.ToString(), Icon = "🛒",
					Accent = Color.FromArgb("#5C647A") },
				new() { Title = "DOANH THU", Value = $"{dashboard.TotalRevenue:N0} đ", Icon = "💰",
					Accent = Color.FromArgb("#213145") },
				new() { Title = "TỒN KHO THẤP", Value = dashboard.LowStockProducts.ToString(), Icon = "⚠️",
					Accent = dashboard.LowStockProducts > 0 ? Color.FromArgb("#BA1A1A") : Color.FromArgb("#5C647A") },
				new() { Title = "ĐANG XỬ LÝ", Value = dashboard.PendingOrders.ToString(),
					Icon = "⏳", Accent = Color.FromArgb("#B7C9D5") }
			};

			// Tạo cảnh báo tồn kho thấp từ API
			Alerts = dashboard.LowStock.Select(p => new AlertItem
			{
				Name = p.TenSanPham,
				Code = $"SP-{p.MaSanPham:0000}",
				StockText = $"Còn {p.SoLuongTon}",
				Accent = p.SoLuongTon <= 5 ? Color.FromArgb("#BA1A1A") : Color.FromArgb("#50616B")
			}).ToList();

			// Tạo hoạt động gần đây từ API
			Activities = dashboard.RecentActivities.Select(a => new ActivityItem
			{
				Title = a.Title,
				Time = a.Time.ToString("dd/MM HH:mm"),
				Icon = a.Type == "order" ? "🛒" : "📦",
				Accent = a.Type == "order" ? Color.FromArgb("#50616B") : Color.FromArgb("#5C647A")
			}).ToList();

			// Dữ liệu xu hướng 7 ngày
			TrendData = dashboard.Trend;

			// Top sản phẩm bán chạy
			TopProducts = dashboard.TopProducts;

			// Bridge items
			Bridges = new List<BridgeItem>
			{
				new() { Title = "API Backend", Description = $"Đã kết nối {dashboard.TotalProducts} sản phẩm, {dashboard.TotalOrders} đơn hàng",
					Status = "Đã kết nối", Accent = Color.FromArgb("#213145") }
			};

			OnPropertyChanged(nameof(Stats));
			OnPropertyChanged(nameof(Alerts));
			OnPropertyChanged(nameof(Activities));
			OnPropertyChanged(nameof(Bridges));
			OnPropertyChanged(nameof(TrendData));
			OnPropertyChanged(nameof(TopProducts));
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[DashboardPage] Load error: {ex.Message}");
		}
	}
}
