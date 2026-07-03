using System.Net;
using System.Net.Http.Json;
using SyncChain.Desktop.Models;
using SyncChain.Desktop.Services;

namespace SyncChain.Desktop.Views.Pages;

// Trang đơn hàng dành riêng cho KHÁCH HÀNG (thay cho trang "Quản lý Đơn hàng"
// của nhân sự trước đây bị dùng nhầm ở CustomerShell). Chỉ hiển thị đơn của
// chính khách, không có thao tác quản trị/tạo đơn nội bộ.
public partial class CustomerOrdersPage : ContentPage
{
	private readonly HttpClient _http;

	public IReadOnlyList<CustomerOrderRow> Orders { get; private set; } = Array.Empty<CustomerOrderRow>();

	public CustomerOrdersPage() : this(ApiClientProvider.Client) { }

	public CustomerOrdersPage(HttpClient http)
	{
		_http = http;
		InitializeComponent();
		BindingContext = this;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await LoadAsync();
	}

	private async Task LoadAsync()
	{
		try
		{
			var orders = await _http.GetFromJsonAsync<List<DonHangApi>>("api/order") ?? [];
			Orders = orders
				.OrderByDescending(o => o.NgayTao)
				.Select(o =>
				{
					var (text, color) = OrderStatusDisplay.Badge(o.TrangThai);
					return new CustomerOrderRow(
						o.MaDonHang,
						$"#ORD-{o.MaDonHang:0000}",
						$"Đặt ngày {o.NgayTao.ToLocalTime():dd/MM/yyyy HH:mm}",
						$"{o.TongTien:N0} đ",
						text,
						color);
				})
				.ToList();

			OnPropertyChanged(nameof(Orders));
			EmptyLabel.IsVisible = Orders.Count == 0;
		}
		catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
		{
			await this.HandleUnauthorizedAsync();
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[CustomerOrders] {ex.Message}");
		}
	}

	private async void OnRefreshClicked(object? sender, EventArgs e) => await LoadAsync();

	private async void OnDetailClicked(object? sender, EventArgs e)
	{
		if (sender is Button b && int.TryParse(b.CommandParameter?.ToString(), out var id))
			await Shell.Current.GoToAsync($"{nameof(OrderDetailPage)}?orderId={id}");
	}

	private async void OnTrackClicked(object? sender, EventArgs e)
	{
		if (sender is Button b && int.TryParse(b.CommandParameter?.ToString(), out var id))
			await Shell.Current.GoToAsync($"{nameof(OrderTrackingPage)}?orderId={id}");
	}
}

public sealed record CustomerOrderRow(int Id, string Code, string Date, string Total, string StatusText, Color StatusColor);
