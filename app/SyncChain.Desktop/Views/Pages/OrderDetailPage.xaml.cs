using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using SyncChain.Desktop.Models;
using SyncChain.Desktop.Services;

namespace SyncChain.Desktop.Views.Pages;

public partial class OrderDetailPage : ContentPage, IQueryAttributable
{
	private readonly HttpClient _http = ApiClientProvider.Client;
	private int _orderId;
	private OrderDetailApi? _order;
	private DeliveryEstimateApi? _estimate;

	public string OrderCode { get; private set; } = string.Empty;
	public string OrderDate { get; private set; } = string.Empty;
	public string OrderStatus { get; private set; } = string.Empty;
	public Color OrderStatusColor { get; private set; } = Colors.LightGray;
	public string CustomerName { get; private set; } = string.Empty;
	public string CustomerEmail { get; private set; } = string.Empty;
	public string CustomerPhone { get; private set; } = string.Empty;
	public string CustomerAddress { get; private set; } = string.Empty;
	public string CustomerInitials { get; private set; } = "KH";
	public string ProductCount { get; private set; } = "0 sản phẩm";
	public string OrderNote { get; private set; } = "Không có ghi chú.";
	public string Subtotal { get; private set; } = "0 đ";
	public string ShippingFee { get; private set; } = "0 đ";
	public string TotalAmount { get; private set; } = "0 đ";
	public string Carrier { get; private set; } = "Chưa tạo vận chuyển";
	public string TrackingNumber { get; private set; } = "Chưa có";
	public string EstimatedDelivery { get; private set; } = "Chưa tính";
	public string EtaConfidence { get; private set; } = "—";
	public string EtaAnalysis { get; private set; } = "Cần có tỉnh/thành và phường/xã để ước tính.";
	public string PrimaryActionText { get; private set; } = string.Empty;
	public bool HasPrimaryAction { get; private set; }
	public bool CanCancel { get; private set; }
	public IReadOnlyList<LineItem> Lines { get; private set; } = Array.Empty<LineItem>();
	public IReadOnlyList<TimelineItem> Timeline { get; private set; } = Array.Empty<TimelineItem>();

	public OrderDetailPage()
	{
		InitializeComponent();
		BindingContext = this;
	}

	public void ApplyQueryAttributes(IDictionary<string, object> query)
	{
		if (query.TryGetValue("orderId", out var value))
			int.TryParse(Uri.UnescapeDataString(value?.ToString() ?? string.Empty), out _orderId);
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await LoadAsync();
	}

	private async Task LoadAsync()
	{
		if (_orderId <= 0) return;

		// Bọc toàn bộ luồng tải trong try/catch: OnAppearing là async void nên mọi
		// exception (401 hết phiên, 403, 500, mất mạng...) nếu không bắt sẽ thoát ra
		// và làm sập ứng dụng. 401 → điều hướng an toàn về trang đăng nhập.
		try
		{
			_order = await _http.GetFromJsonAsync<OrderDetailApi>($"api/order/{_orderId}");
			if (_order == null) return;

			await PopulateAsync();
		}
		catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
		{
			await this.HandleUnauthorizedAsync();
		}
		catch (Exception ex)
		{
			AppLog.Error("OrderDetail", $"Không tải được đơn #{_orderId}", ex);
			await DisplayAlertAsync("Không tải được đơn hàng",
				"Đã xảy ra lỗi khi tải chi tiết đơn. Vui lòng thử lại.", "OK");
		}
	}

	private async Task PopulateAsync()
	{
		if (_order == null) return;

		OrderCode = $"#ORD-{_order.MaDonHang:0000}";
		OrderDate = $"Đặt ngày {_order.NgayTao.ToLocalTime():dd/MM/yyyy HH:mm}";
		(OrderStatus, OrderStatusColor) = StatusDisplay(_order.TrangThai);
		CustomerName = string.IsNullOrWhiteSpace(_order.CustomerName) ? $"Khách hàng #{_order.MaNguoiDung}" : _order.CustomerName;
		CustomerEmail = _order.CustomerEmail;
		CustomerPhone = string.IsNullOrWhiteSpace(_order.SoDienThoai) ? "Chưa cập nhật số điện thoại" : _order.SoDienThoai;
		CustomerAddress = string.IsNullOrWhiteSpace(_order.DiaChiGiaoHang)
			? "Chưa cập nhật địa chỉ giao hàng"
			: _order.DiaChiGiaoHang;
		CustomerInitials = BuildInitials(CustomerName);
		OrderNote = string.IsNullOrWhiteSpace(_order.GhiChu) ? "Không có ghi chú." : _order.GhiChu;
		var subtotal = _order.Details.Sum(x => x.SoLuong * x.DonGia);
		var shippingFee = _order.Shipping?.ShippingFee ?? Math.Max(0, _order.TongTien - subtotal);
		var totalAmount = subtotal + shippingFee;
		Subtotal = $"{subtotal:N0} đ";
		ShippingFee = $"{shippingFee:N0} đ";
		TotalAmount = $"{totalAmount:N0} đ";
		Carrier = string.IsNullOrWhiteSpace(_order.Shipping?.Carrier) ? "Chưa tạo vận chuyển" : _order.Shipping.Carrier;
		TrackingNumber = string.IsNullOrWhiteSpace(_order.Shipping?.TrackingNumber) ? "Chưa có" : _order.Shipping.TrackingNumber;
		EstimatedDelivery = _order.Shipping?.EstimatedDeliveryAt?.ToLocalTime().ToString("dd/MM/yyyy") ?? "Chưa tính";
		Lines = _order.Details.Select(x => new LineItem
		{
			Name = x.SanPham?.TenSanPham ?? $"SP-{x.MaSanPham}",
			Variant = $"SP-{x.MaSanPham:0000}",
			Quantity = x.SoLuong.ToString("N0"),
			Price = $"{x.DonGia:N0} đ",
			Initials = BuildInitials(x.SanPham?.TenSanPham ?? "SP")
		}).ToList();
		ProductCount = $"{_order.Details.Sum(x => x.SoLuong):N0} sản phẩm";
		ConfigureActions();
		await LoadEstimateAsync();
		BuildTimeline();
		NotifyAll();
	}

	private void BuildTimeline()
	{
		if (_order == null) return;
		var items = new List<TimelineItem>
		{
			new()
			{
				Title = "Đơn hàng đã được tạo",
				Time = _order.NgayTao.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
				State = "completed",
				Accent = Colors.Green
			}
		};
		if (_order.TrangThai is "processing" or "shipping" or "done")
			items.Add(new TimelineItem { Title = "Đơn hàng đang được xử lý", Time = "Đã xác nhận", State = "completed", Accent = Colors.Green });
		if (_order.TrangThai is "shipping" or "done")
			items.Add(new TimelineItem
			{
				Title = $"Đang vận chuyển · {TrackingNumber}",
				Time = $"Dự kiến: {EstimatedDelivery}",
				State = _order.TrangThai == "shipping" ? "current" : "completed",
				Accent = _order.TrangThai == "shipping" ? Colors.Blue : Colors.Green
			});
		if (_order.TrangThai == "done")
			items.Add(new TimelineItem { Title = "Khách hàng đã nhận hàng", Time = "Hoàn thành", State = "completed", Accent = Colors.Green });
		else if (_order.TrangThai == "cancel")
			items.Add(new TimelineItem { Title = "Đơn hàng đã hủy", Time = "Kết thúc", State = "cancelled", Accent = Colors.Red });
		else
			items.Add(new TimelineItem { Title = "Chờ hoàn thành giao hàng", Time = EstimatedDelivery, State = "pending", Accent = Colors.Gray });
		if (items.Count > 0)
            items[^1].ShowConnector = false;
		Timeline = items;
	}

	private async Task LoadEstimateAsync()
	{
		if (_order == null || string.IsNullOrWhiteSpace(_order.TinhThanh)) return;
		var response = await _http.PostAsJsonAsync("api/shipping/estimate", new
		{
			orderDate = _order.NgayTao,
			province = _order.TinhThanh,
			ward = _order.PhuongXa,
			serviceType = _order.LoaiDichVu,
			weightKg = _order.TrongLuongKg,
			orderTotal = _order.Details.Sum(x => x.SoLuong * x.DonGia)
		});
		if (!response.IsSuccessStatusCode) return;
		_estimate = await response.Content.ReadFromJsonAsync<DeliveryEstimateApi>();
		if (_estimate == null) return;
		EstimatedDelivery = $"{_estimate.EarliestDelivery:dd/MM/yyyy} – {_estimate.LatestDelivery:dd/MM/yyyy}";
		EtaConfidence = $"{_estimate.ConfidencePercent}%";
		EtaAnalysis = $"{_estimate.EstimatedDistanceKm:N0} km · {_estimate.AreaType} · " +
			$"{_estimate.WarehouseProcessing} · {_estimate.TransitTime}. {_estimate.Factors} {_estimate.Assumption}";
	}

	private void ConfigureActions()
	{
		if (_order == null) return;

		// Các thao tác đẩy trạng thái/vận chuyển là của nhân sự nội bộ.
		var isInternal = ApiClientProvider.Role is "staff" or "manager" or "admin";

		(PrimaryActionText, HasPrimaryAction) = isInternal
			? _order.TrangThai switch
			{
				"pending" => ("CHUYỂN SANG ĐANG XỬ LÝ", true),
				"processing" when _order.Shipping != null
				    => ("CHUYỂN SANG CHỜ GIAO HÀNG", true),
				"processing" => ("TẠO VẬN CHUYỂN", true),
				"shipping" => ("XÁC NHẬN ĐÃ GIAO", true),
				_ => (string.Empty, false)
			}
			: (string.Empty, false);

		// Nhân sự hủy được ở nhiều trạng thái; khách chỉ hủy đơn đang chờ xử lý.
		CanCancel = isInternal
			? _order.TrangThai is "pending" or "processing" or "shipping"
			: _order.TrangThai == "pending";
	}

	private async void OnPrimaryActionClicked(object? sender, EventArgs e)
	{
		if (_order == null) return;
		if (_order.TrangThai == "pending")
			await UpdateOrderStatusAsync("processing");
		else if (_order.TrangThai == "processing")
		{
            if (_order.Shipping != null)
                await UpdateOrderStatusAsync("shipping");
            else
                await CreateShippingAsync();
     }
		else if (_order.TrangThai == "shipping")
			await CompleteShippingAsync();
	}

	private async Task CreateShippingAsync()
	{
		if (_order == null) return;
		var carrier = await DisplayPromptAsync("Đơn vị vận chuyển", "Nhập tên đơn vị vận chuyển:", "Tiếp tục", "Hủy");
		if (string.IsNullOrWhiteSpace(carrier)) return;
		var eta = _estimate?.LatestDelivery ?? DateTime.UtcNow.AddDays(5);
		var response = await _http.PostAsJsonAsync($"api/orders/{_orderId}/shipping", new
		{
			carrier,
			shippingFee = _estimate?.ShippingFee ?? 0,
			estimatedDeliveryAt = eta
		});
		if (!response.IsSuccessStatusCode)
		{
			await DisplayAlertAsync("Không tạo được vận chuyển", await ReadErrorAsync(response), "OK");
			return;
		}
		await LoadAsync();
	}

	private async Task CompleteShippingAsync()
	{
		if (_order?.Shipping == null) return;
		var response = await _http.PutAsJsonAsync($"api/orders/{_orderId}/shipping/status", new
		{
			status = "delivered",
			expectedStatus = _order.Shipping.ShippingStatus,
			concurrencyVersion = _order.Shipping.ConcurrencyVersion,
			note = "Khách hàng xác nhận đã nhận hàng"
		});
		if (!response.IsSuccessStatusCode)
		{
			await DisplayAlertAsync("Không hoàn thành được đơn", await ReadErrorAsync(response), "OK");
			return;
		}
		await LoadAsync();
	}

	private async Task UpdateOrderStatusAsync(string status)
	{
		if (_order == null) return;
		var response = await _http.PutAsJsonAsync($"api/order/{_orderId}/status", new
		{
			status,
			expectedStatus = _order.TrangThai,
			concurrencyVersion = _order.ConcurrencyVersion
		});
		if (!response.IsSuccessStatusCode)
		{
			await DisplayAlertAsync("Không cập nhật được trạng thái", await ReadErrorAsync(response), "OK");
			return;
		}
		await LoadAsync();
	}

	private async void OnCancelOrderClicked(object? sender, EventArgs e)
	{
		if (_order == null) return;
		if (!await DisplayAlertAsync("Hủy đơn hàng", "Bạn có chắc muốn hủy đơn này?", "Hủy đơn", "Không"))
			return;

		// Khách hàng dùng endpoint tự hủy; nhân sự nội bộ dùng luồng cập nhật trạng thái.
		if (string.Equals(ApiClientProvider.Role, "customer", StringComparison.OrdinalIgnoreCase))
		{
			var response = await _http.PutAsync($"api/order/{_orderId}/cancel", null);
			if (!response.IsSuccessStatusCode)
			{
				await DisplayAlertAsync("Không hủy được đơn", await ReadErrorAsync(response), "OK");
				return;
			}
			await LoadAsync();
		}
		else
		{
			await UpdateOrderStatusAsync("cancel");
		}
	}

	private async void OnPrintInvoiceClicked(object? sender, EventArgs e)
	{
		if (_order == null) return;
		var rows = string.Join("", _order.Details.Select(x =>
			$"<tr><td>{WebUtility.HtmlEncode(x.SanPham?.TenSanPham ?? $"SP-{x.MaSanPham}")}</td>" +
			$"<td>{x.SoLuong}</td><td>{x.DonGia:N0} đ</td><td>{x.SoLuong * x.DonGia:N0} đ</td></tr>"));
		var html = $$"""
			<!doctype html><html><head><meta charset="utf-8"><title>Hóa đơn {{OrderCode}}</title>
			<style>body{font-family:Arial;margin:40px;color:#17243a}table{width:100%;border-collapse:collapse}
			th,td{padding:10px;border-bottom:1px solid #ddd;text-align:left}h1{margin-bottom:4px}
			.total{text-align:right;font-size:20px;font-weight:bold;margin-top:24px}</style></head>
			<body><h1>SyncChain</h1><h2>HÓA ĐƠN {{OrderCode}}</h2><p>{{OrderDate}}</p>
			<p><b>Khách hàng:</b> {{WebUtility.HtmlEncode(CustomerName)}}<br>
			<b>Địa chỉ:</b> {{WebUtility.HtmlEncode(CustomerAddress)}}</p>
			<table><thead><tr><th>Sản phẩm</th><th>SL</th><th>Đơn giá</th><th>Thành tiền</th></tr></thead>
			<tbody>{{rows}}</tbody></table><div class="total">Tổng cộng: {{TotalAmount}}</div>
			<script>window.onload=()=>window.print()</script></body></html>
			""";
		var path = Path.Combine(FileSystem.CacheDirectory, $"invoice-{_orderId}.html");
		await File.WriteAllTextAsync(path, html, Encoding.UTF8);
		await Launcher.Default.OpenAsync(new OpenFileRequest("In hóa đơn", new ReadOnlyFile(path)));
	}

	private void NotifyAll()
	{
		foreach (var name in new[] { nameof(OrderCode), nameof(OrderDate), nameof(OrderStatus),
			nameof(OrderStatusColor), nameof(CustomerName), nameof(CustomerEmail), nameof(CustomerPhone),
			nameof(CustomerAddress), nameof(CustomerInitials), nameof(ProductCount), nameof(OrderNote),
			nameof(Subtotal), nameof(ShippingFee), nameof(TotalAmount),
			nameof(Carrier), nameof(TrackingNumber), nameof(EstimatedDelivery), nameof(EtaConfidence),
			nameof(EtaAnalysis), nameof(PrimaryActionText), nameof(HasPrimaryAction), nameof(CanCancel),
			nameof(Lines), nameof(Timeline) }) OnPropertyChanged(name);
	}

	private static string BuildInitials(string value)
	{
		var result = string.Join("", value.Split(' ', StringSplitOptions.RemoveEmptyEntries)
			.Take(2).Select(x => char.ToUpperInvariant(x[0])));
		return string.IsNullOrWhiteSpace(result) ? "KH" : result;
	}

	private static (string, Color) StatusDisplay(string status) => status switch
	{
		"pending" => ("Chờ duyệt", Color.FromArgb("#dae2fd")),
		"processing" => ("Đang xử lý", Color.FromArgb("#dbeafe")),
		"shipping" => ("Vận chuyển", Color.FromArgb("#fff3cd")),
		"done" => ("Hoàn thành", Color.FromArgb("#d3e5f1")),
		"cancel" => ("Đã hủy", Color.FromArgb("#ffdad6")),
		_ => (status, Colors.LightGray)
	};

	private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
	{
		var text = await response.Content.ReadAsStringAsync();
		try
		{
			using var json = JsonDocument.Parse(text);
			if (json.RootElement.TryGetProperty("message", out var message))
				return message.GetString() ?? text;
		}
		catch { }
		return text.Trim('"');
	}

	private async void OnBackClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");
}
