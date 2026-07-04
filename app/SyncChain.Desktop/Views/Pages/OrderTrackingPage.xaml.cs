using System.Net;
using System.Net.Http.Json;
using SyncChain.Desktop.Models;
using SyncChain.Desktop.Services;

namespace SyncChain.Desktop.Views.Pages;

[QueryProperty(nameof(OrderId), "orderId")]
public partial class OrderTrackingPage : ContentPage
{
	private readonly HttpClient _http;
	private int _orderId;
	private decimal _orderTotal;
	private bool _subscribedToSignalR;

	public int OrderId { get => _orderId; set => _orderId = value; }

	public OrderTrackingPage() : this(ApiClientProvider.Client)
	{
	}

	public OrderTrackingPage(HttpClient http)
	{
		_http = http;
		InitializeComponent();
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		if (_orderId > 0)
			await LoadAsync();

		App.SignalR.OnOrderStatusUpdated -= OnStatusUpdated;
		App.SignalR.OnOrderStatusUpdated += OnStatusUpdated;
		_subscribedToSignalR = true;
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		if (_subscribedToSignalR)
		{
			App.SignalR.OnOrderStatusUpdated -= OnStatusUpdated;
			_subscribedToSignalR = false;
		}
	}

	private async void OnStatusUpdated(int orderId, string status, string timestamp)
	{
		if (orderId == _orderId)
			await LoadAsync();
	}

	private async Task LoadAsync()
	{
		try
		{
			var data = await _http.GetFromJsonAsync<OrderTrackingResponse>($"api/order/{_orderId}/tracking");
			if (data?.Order == null)
				return;

			var order = data.Order;
			_orderTotal = order.TongTien;

			OrderCodeLabel.Text = $"#ORD-{order.MaDonHang:0000}";
			OrderDateLabel.Text = order.NgayTao.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
			(StatusLabel.Text, StatusBadge.BackgroundColor) = OrderStatusDisplay.Badge(order.TrangThai);

			var paymentMethod = string.IsNullOrWhiteSpace(order.PhuongThucThanhToan)
				? "-"
				: order.PhuongThucThanhToan.ToUpperInvariant();
			PaymentLabel.Text = data.Payment != null
				? $"{paymentMethod} - {data.Payment.TrangThaiThanhToan}"
				: $"{paymentMethod} - Chưa thanh toán";

			PayButton.IsVisible = order.TrangThai == "pending" &&
				(data.Payment == null || data.Payment.TrangThaiThanhToan != "Completed");

			RecipientLabel.Text = string.IsNullOrWhiteSpace(order.NguoiNhan)
				? "Chưa có thông tin"
				: order.NguoiNhan;
			AddressLabel.Text = string.IsNullOrWhiteSpace(order.DiaChiGiao)
				? "Chưa có địa chỉ"
				: order.DiaChiGiao;

			RenderTimeline(data.Timeline);
			RenderProducts(data.ChiTiet);
			TotalLabel.Text = $"{order.TongTien:N0} đ";
		}
		catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
		{
			await this.HandleUnauthorizedAsync();
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[Tracking] {ex.Message}");
		}
	}

	private void RenderTimeline(IEnumerable<TrackingTimelineStep> steps)
	{
		TimelineStack.Children.Clear();
		foreach (var step in steps)
		{
			var row = new Grid
			{
				ColumnDefinitions =
				{
					new ColumnDefinition { Width = 32 },
					new ColumnDefinition { Width = GridLength.Star }
				},
				ColumnSpacing = 16,
				Padding = new Thickness(0, 10)
			};

			var dot = new Border
			{
				WidthRequest = 16,
				HeightRequest = 16,
				BackgroundColor = step.Color,
				StrokeThickness = 0,
				StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
				HorizontalOptions = LayoutOptions.Center
			};
			row.Add(dot, 0, 0);
			row.Add(new Label
			{
				Text = step.Label,
				FontFamily = "Inter",
				FontSize = 15,
				FontAttributes = step.TrangThai == "hienTai" ? FontAttributes.Bold : FontAttributes.None,
				TextColor = step.Color,
				VerticalTextAlignment = TextAlignment.Center
			}, 1, 0);

			TimelineStack.Children.Add(row);
		}
	}

	private void RenderProducts(IEnumerable<ChiTietDonHangApi> items)
	{
		ProductStack.Children.Clear();
		foreach (var item in items)
		{
			var productName = item.SanPham?.TenSanPham ?? $"SP-{item.MaSanPham}";
			var row = new Grid
			{
				ColumnDefinitions =
				{
					new ColumnDefinition { Width = GridLength.Star },
					new ColumnDefinition { Width = GridLength.Auto },
					new ColumnDefinition { Width = GridLength.Auto }
				},
				ColumnSpacing = 16
			};

			row.Add(new Label
			{
				Text = productName,
				FontFamily = "Inter",
				FontAttributes = FontAttributes.Bold,
				TextColor = Color.FromArgb("#112033"),
				VerticalTextAlignment = TextAlignment.Center
			}, 0, 0);
			row.Add(new Label
			{
				Text = $"x{item.SoLuong}",
				TextColor = Color.FromArgb("#6B7280"),
				VerticalTextAlignment = TextAlignment.Center
			}, 1, 0);
			row.Add(new Label
			{
				Text = $"{item.DonGia * item.SoLuong:N0} đ",
				FontFamily = "Inter",
				FontAttributes = FontAttributes.Bold,
				TextColor = Color.FromArgb("#112033"),
				VerticalTextAlignment = TextAlignment.Center,
				HorizontalTextAlignment = TextAlignment.End
			}, 2, 0);

			ProductStack.Children.Add(row);
		}
	}

	private async void OnPayClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync(
			$"{nameof(PaymentPage)}?orderId={_orderId}&amount={_orderTotal}&orderCode=ORD-{_orderId:0000}");
	}

	private async void OnBackClicked(object? sender, EventArgs e) =>
		await Shell.Current.GoToAsync("..");

	private async void OnHomeClicked(object? sender, EventArgs e) =>
		await Shell.Current.GoToAsync("//customer-home");

	private void OnMenuClicked(object? sender, EventArgs e) =>
		Shell.Current.FlyoutIsPresented = true;
}
