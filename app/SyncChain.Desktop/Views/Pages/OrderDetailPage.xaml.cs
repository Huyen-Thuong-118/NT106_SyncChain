using System.Net.Http.Json;
using Microsoft.Maui.Controls;
using SyncChain.Desktop.Models;

namespace SyncChain.Desktop.Views.Pages;

public partial class OrderDetailPage : ContentPage
{
	private readonly HttpClient _http;

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
			// Lấy danh sách đơn hàng trước, rồi lấy chi tiết đơn đầu tiên
			var ordersResp = await _http.GetFromJsonAsync<ApiResponse<List<DonHangApi>>>("api/donhang");
			if (ordersResp?.success == true && ordersResp.data?.Any() == true)
			{
				var firstOrder = ordersResp.data.First();
				var detail = await _http.GetFromJsonAsync<ApiResponse<DonHangDetailApi>>($"api/donhang/{firstOrder.MaDonHang}");

				if (detail?.success == true && detail.data != null)
				{
					Lines = detail.data.items.Select(item => new LineItem
					{
						Name = item.TenSanPham,
						Variant = $"SP-{item.MaSanPham:0000}",
						Quantity = item.SoLuong.ToString(),
						Price = $"{item.DonGia:N0} đ",
						Initials = string.Join("", item.TenSanPham.Split(' ', StringSplitOptions.RemoveEmptyEntries)
							.Take(2).Select(w => w[0])).ToUpperInvariant()
					}).ToList();

					// Tạo timeline từ trạng thái đơn hàng
					Timeline = new List<TimelineItem>
					{
						new() { Title = "Đơn hàng đã tạo", Time = detail.data.NgayTao.ToString("dd/MM/yyyy HH:mm"), State = "completed", Accent = Colors.Green },
						new() { Title = $"Trạng thái: {detail.data.TrangThaiDon}", Time = detail.data.NgayTao.ToString("dd/MM/yyyy HH:mm"), State = "current", Accent = Colors.Blue },
						new() { Title = "Chờ giao hàng", Time = "Dự kiến", State = "pending", Accent = Colors.Gray }
					};

					OnPropertyChanged(nameof(Lines));
					OnPropertyChanged(nameof(Timeline));
				}
			}
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
