using System.Net.Http.Json;
using SyncChain.Desktop.Models;

namespace SyncChain.Desktop.Views.Pages;

public partial class OrdersPage : ContentPage
{
	private readonly HttpClient _http;
	private List<DonHangApi> _allOrders = [];
	private List<DonHangApi> _filteredOrders = [];
	private int _currentPage = 1;
	private int _pageSize = 20;

	public IReadOnlyList<OrderItem> Orders { get; private set; } = Array.Empty<OrderItem>();
	public IReadOnlyList<PageButtonItem> PageButtons { get; private set; } = Array.Empty<PageButtonItem>();
	public string PeriodOrderCount { get; private set; } = "0";
	public string PendingOrders { get; private set; } = "0";
	public string CompletedOrders { get; private set; } = "0";
	public string TotalRevenue { get; private set; } = "0 đ";
	public string OrdersPerDay { get; private set; } = "0 đơn/ngày";
	public string CompletionRate { get; private set; } = "0%";
	public string PeriodDescription { get; private set; } = "Đơn hàng trong tháng hiện tại";
	public string PaginationText { get; private set; } = string.Empty;
	public bool CanGoPrevious => _currentPage > 1;
	public bool CanGoNext => _currentPage < TotalPages;
	private int TotalPages => Math.Max(1, (int)Math.Ceiling(_filteredOrders.Count / (double)_pageSize));

	public OrdersPage() : this(Services.ApiClientProvider.Client) { }

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
		_allOrders = await _http.GetFromJsonAsync<List<DonHangApi>>("api/order") ?? [];
		ApplyFilters();
	}

	private void ApplyFilters()
	{
		var status = StatusPicker.SelectedIndex switch
		{
			1 => "pending", 2 => "processing", 3 => "shipping",
			4 => "done", 5 => "cancel", _ => string.Empty
		};
		_filteredOrders = string.IsNullOrEmpty(status)
			? _allOrders.ToList()
			: _allOrders.Where(x => x.TrangThai == status).ToList();
		_currentPage = Math.Min(_currentPage, TotalPages);
		ApplyPagination();
		UpdateSummary();
	}

	private void UpdateSummary()
	{
		var now = DateTime.Now;
		var period = PeriodPicker.SelectedIndex;
		var periodOrders = _allOrders.Where(x => period switch
		{
			0 => x.NgayTao.ToLocalTime().Date == now.Date,
			2 => x.NgayTao.ToLocalTime().Year == now.Year,
			_ => x.NgayTao.ToLocalTime().Year == now.Year && x.NgayTao.ToLocalTime().Month == now.Month
		}).ToList();
		var days = period switch { 0 => 1, 2 => Math.Max(1, now.DayOfYear), _ => Math.Max(1, now.Day) };
		PeriodOrderCount = periodOrders.Count.ToString("N0");
		PendingOrders = periodOrders.Count(x => x.TrangThai == "pending").ToString("N0");
		CompletedOrders = periodOrders.Count(x => x.TrangThai == "done").ToString("N0");
		TotalRevenue = $"{periodOrders.Where(x => x.TrangThai == "done").Sum(x => x.TongTien):N0} đ";
		OrdersPerDay = $"{periodOrders.Count / (double)days:0.#} đơn/ngày";
		CompletionRate = periodOrders.Count == 0
			? "0%"
			: $"{periodOrders.Count(x => x.TrangThai == "done") * 100.0 / periodOrders.Count:0.#}%";
		PeriodDescription = period switch
		{
			0 => "Đơn hàng trong ngày hôm nay",
			2 => "Đơn hàng trong năm hiện tại",
			_ => "Đơn hàng trong tháng hiện tại"
		};
		foreach (var name in new[] { nameof(PeriodOrderCount), nameof(PendingOrders), nameof(CompletedOrders),
			nameof(TotalRevenue), nameof(OrdersPerDay), nameof(CompletionRate), nameof(PeriodDescription) })
			OnPropertyChanged(name);
	}
	private void ApplyPagination()
	{
		var start = (_currentPage - 1) * _pageSize;
		Orders = _filteredOrders.Skip(start).Take(_pageSize).Select(MapOrder).ToList();
		var first = _filteredOrders.Count == 0 ? 0 : start + 1;
		var last = Math.Min(start + _pageSize, _filteredOrders.Count);
		PaginationText = $"Hiển thị {first} - {last} trong số {_filteredOrders.Count} đơn hàng";
		PageButtons = BuildPageButtons();
		foreach (var name in new[] { nameof(Orders), nameof(PaginationText), nameof(PageButtons),
			nameof(CanGoPrevious), nameof(CanGoNext) }) OnPropertyChanged(name);
	}

	private IReadOnlyList<PageButtonItem> BuildPageButtons()
	{
		var total = TotalPages;
		var pages = total <= 7
			? Enumerable.Range(1, total).ToList()
			: new[] { 1, 2, Math.Max(3, _currentPage - 1), _currentPage,
				Math.Min(total - 2, _currentPage + 1), total - 1, total }.Distinct().Order().ToList();
		var result = new List<PageButtonItem>();
		var previous = 0;
		foreach (var page in pages.Where(x => x > 0 && x <= total))
		{
			if (previous > 0 && page - previous > 1)
				result.Add(new PageButtonItem { Text = "…", PageNumber = 0 });
			result.Add(new PageButtonItem { Text = page.ToString(), PageNumber = page, IsCurrent = page == _currentPage });
			previous = page;
		}
		return result;
	}

	private static OrderItem MapOrder(DonHangApi order)
	{
		var (text, color) = order.TrangThai switch
		{
			"pending" => ("Chờ duyệt", Color.FromArgb("#dae2fd")),
			"processing" => ("Đang xử lý", Color.FromArgb("#dbeafe")),
			"shipping" => ("Vận chuyển", Color.FromArgb("#fff3cd")),
			"done" => ("Hoàn thành", Color.FromArgb("#d3e5f1")),
			"cancel" => ("Đã hủy", Color.FromArgb("#ffdad6")),
			_ => (order.TrangThai, Colors.LightGray)
		};
		return new OrderItem
		{
			Id = order.MaDonHang,
			Code = $"#ORD-{order.MaDonHang:0000}",
			Customer = string.IsNullOrWhiteSpace(order.CustomerName) ? $"Khách hàng #{order.MaNguoiDung}" : order.CustomerName,
			Email = order.CustomerEmail,
			CreatedAt = order.NgayTao.ToLocalTime().ToString("dd/MM/yyyy"),
			ProductSummary = order.ProductNames.Count == 0 ? $"{order.ProductCount} sản phẩm" :
				string.Join(", ", order.ProductNames),
			Total = $"{order.TongTien:N0} đ",
			Status = text,
			StatusColor = color,
			ConcurrencyVersion = order.ConcurrencyVersion
		};
	}

	private void OnPeriodChanged(object? sender, EventArgs e) => UpdateSummary();
	private void OnStatusChanged(object? sender, EventArgs e) { _currentPage = 1; ApplyFilters(); }
	
	private void OnPreviousPageClicked(object? sender, EventArgs e) { if (_currentPage > 1) { _currentPage--; ApplyPagination(); } }
	private void OnNextPageClicked(object? sender, EventArgs e) { if (_currentPage < TotalPages) { _currentPage++; ApplyPagination(); } }
	private void OnPageClicked(object? sender, EventArgs e)
	{
		if (sender is Button b && int.TryParse(b.CommandParameter?.ToString(), out var page) && page > 0)
		{ _currentPage = page; ApplyPagination(); }
	}
	private async void OnCreateOrderClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("//create-order");
	private async void OnOpenDetailClicked(object? sender, EventArgs e)
	{
		if (sender is Button b && int.TryParse(b.CommandParameter?.ToString(), out var id))
			await Shell.Current.GoToAsync($"{nameof(OrderDetailPage)}?orderId={id}");
	}
}
