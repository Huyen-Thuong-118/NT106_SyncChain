using System.Net.Http.Json;
using Microsoft.Maui.Controls;
using SyncChain.Desktop.Models;

namespace SyncChain.Desktop.Views.Pages;

public partial class LogsPage : ContentPage
{
	private readonly HttpClient _http;

	public IReadOnlyList<LogItem> Logs { get; private set; } = Array.Empty<LogItem>();

	public LogsPage(HttpClient http)
	{
		_http = http;
		InitializeComponent();
		BindingContext = this;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await LoadLogsAsync();
	}

	private async Task LoadLogsAsync()
	{
		try
		{
			var ordersResp = await _http.GetFromJsonAsync<ApiResponse<List<DonHangApi>>>("api/donhang");
			var productsResp = await _http.GetFromJsonAsync<ApiResponse<List<SanPhamApi>>>("api/sanpham");

			var orders = ordersResp?.data ?? new List<DonHangApi>();
			var products = productsResp?.data ?? new List<SanPhamApi>();

			var logs = new List<LogItem>();

			// Log từ đơn hàng
			foreach (var o in orders.OrderByDescending(x => x.NgayTao).Take(10))
			{
				var (tag, icon, accent) = o.TrangThaiDon switch
				{
					"Hoan tat" => ("Đơn hàng", "✅", Color.FromArgb("#5C647A")),
					"Huy" => ("Đơn hàng", "❌", Color.FromArgb("#BA1A1A")),
					_ => ("Đơn hàng", "🛒", Color.FromArgb("#50616B"))
				};

				logs.Add(new LogItem
				{
					Title = $"Đơn hàng #ORD-{o.MaDonHang:0000}",
					Description = $"Trạng thái: {o.TrangThaiDon}, tổng tiền {o.TongTien:N0} VND",
					Time = o.NgayTao.ToString("dd/MM/yyyy HH:mm"),
					Tag = tag,
					Icon = icon,
					Accent = accent
				});
			}

			// Log từ sản phẩm
			foreach (var p in products.Take(5))
			{
				logs.Add(new LogItem
				{
					Title = $"Sản phẩm SP-{p.MaSanPham:0000}",
					Description = $"{p.TenSanPham}: tồn kho {p.SoLuongTon}, giá {p.GiaBan:N0} VND",
					Time = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
					Tag = "Kho hàng",
					Icon = "📦",
					Accent = p.SoLuongTon <= p.MucTonThap ? Color.FromArgb("#BA1A1A") : Color.FromArgb("#5C647A")
				});
			}

			Logs = logs.OrderByDescending(l => l.Time).ToList();
			OnPropertyChanged(nameof(Logs));
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[LogsPage] Load error: {ex.Message}");
		}
	}
}
