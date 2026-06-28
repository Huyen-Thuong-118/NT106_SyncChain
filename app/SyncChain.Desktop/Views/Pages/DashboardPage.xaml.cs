using System.Net.Http.Json;
using SyncChain.Desktop.Models;
using SyncChain.Desktop.Services;
using SyncChain.Desktop.Views.Charts;
using System.Text.Json;
using System.Text;

namespace SyncChain.Desktop.Views.Pages;

public partial class DashboardPage : ContentPage
{
	private readonly HttpClient _http;

	public IReadOnlyList<StatCard> Stats { get; private set; } = Array.Empty<StatCard>();
	public IReadOnlyList<AlertItem> Alerts { get; private set; } = Array.Empty<AlertItem>();
	public IReadOnlyList<ActivityItem> Activities { get; private set; } = Array.Empty<ActivityItem>();
	public IReadOnlyList<BridgeItem> Bridges { get; private set; } = Array.Empty<BridgeItem>();
	public IReadOnlyList<TrendApi> TrendData { get; private set; } = Array.Empty<TrendApi>();
	public IReadOnlyList<TopProductApi> TopProducts { get; private set; } = Array.Empty<TopProductApi>();
	public IReadOnlyList<InventoryDistributionItem> InventoryDistribution { get; private set; } = Array.Empty<InventoryDistributionItem>();
	public IReadOnlyList<OrderTrendPoint> OrderTrend { get; private set; } = Array.Empty<OrderTrendPoint>();
	public IReadOnlyList<InventorySlice> InventorySlices { get; private set; } = Array.Empty<InventorySlice>();
	public IReadOnlyList<QuickActionItem> QuickActions { get; } =
	[
		new() { Title = "Sản phẩm", Icon = "SP", Route = "//products" },
		new() { Title = "Đơn hàng", Icon = "DH", Route = "//orders" },
		new() { Title = "Nhập hàng", Icon = "NK", Route = "//imports" },
		new() { Title = "Tin nhắn", Icon = "TN", Route = "//chat" }
	];
	public string PrimaryInventoryCategory { get; private set; } = "Chưa có";
	public string PrimaryInventoryPercent { get; private set; } = "0%";

	public DashboardPage() : this(ApiClientProvider.Client)
	{
	}

	public DashboardPage(HttpClient http)
	{
		_http = http;
		InitializeComponent();
		BindingContext = this;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		if (ApiClientProvider.Role?.Trim().ToLowerInvariant() is not ("admin" or "manager"))
		{
			await Shell.Current.GoToAsync("//orders");
			return;
		}

		await LoadDashboardAsync();
	}

	private async Task LoadDashboardAsync()
	{
		try
		{
			var dashboard = await _http.GetFromJsonAsync<DashboardApi>("api/reports/dashboard");
			if (dashboard == null)
				return;

			Stats =
			[
				new()
				{
					Title = "Tổng sản phẩm",
					Value = dashboard.TotalProducts.ToString("N0"),
					Subtitle = $"{dashboard.ActiveProducts:N0} đang hoạt động",
					IconGlyph = "inventory_2",
					Accent = Color.FromArgb("#2F80ED")
				},
				new()
				{
					Title = "Đơn hàng",
					Value = dashboard.TotalOrders.ToString("N0"),
					Subtitle = $"{dashboard.CompletedOrders:N0} đã hoàn thành",
					IconGlyph = "shopping_cart",
					Accent = Color.FromArgb("#7C3AED")
				},
				new()
				{
					Title = "Doanh thu",
					Value = $"{dashboard.TotalRevenue:N0} đ",
					Subtitle = "Doanh thu thuần",
					IconGlyph = "\ue227",
					Accent = Color.FromArgb("#F43F5E")
				},
				new()
				{
					Title = "Đang xử lý",
					Value = dashboard.PendingOrders.ToString("N0"),
					Subtitle = "Đơn chờ xử lý",
					IconGlyph = "pending_actions",
					Accent = Color.FromArgb("#22C55E")
				},
				new()
				{
					Title = "Tồn kho thấp",
					Value = dashboard.LowStockProducts.ToString("N0"),
					Subtitle = "Cần bổ sung",
					IconGlyph = "warning_amber",
					Accent = Color.FromArgb("#F59E0B")
				},
				new()
				{
					Title = "Đã hủy",
					Value = dashboard.CancelledOrders.ToString("N0"),
					Subtitle = "Đơn không thành công",
					IconGlyph = "cancel",
					Accent = Color.FromArgb("#EF4444")
				}
			];

			Alerts = await LoadOrDefaultAsync(LoadLowStockAlertsAsync);
			Activities = ApiClientProvider.Role?.Trim().ToLowerInvariant() == "admin"
				? await LoadOrDefaultAsync(LoadRecentActivitiesAsync)
				: [];
			TopProducts = await LoadOrDefaultAsync(LoadTopProductsAsync);
			InventoryDistribution = await LoadOrDefaultAsync(LoadInventoryDistributionAsync);
			TrendData = dashboard.Trend;
			OrderTrend = await LoadOrDefaultAsync(LoadOrderTrendAsync);
			InventorySlices = BuildInventorySlices(InventoryDistribution);
			UpdateInventoryShare();
			ApplyCharts();

			Bridges =
			[
				new()
				{
					Title = "API Backend",
					Description = $"Đã kết nối {dashboard.TotalProducts:N0} sản phẩm, {dashboard.TotalOrders:N0} đơn hàng",
					Status = "Đã kết nối",
					Accent = Color.FromArgb("#213145")
				}
			];

			foreach (var name in new[]
			{
				nameof(Stats), nameof(Alerts), nameof(Activities), nameof(Bridges), nameof(TrendData),
				nameof(TopProducts), nameof(InventoryDistribution), nameof(OrderTrend), nameof(InventorySlices),
				nameof(PrimaryInventoryCategory), nameof(PrimaryInventoryPercent)
			})
				OnPropertyChanged(name);
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[DashboardPage] Load error: {ex.Message}");
		}
	}

	private static async Task<IReadOnlyList<T>> LoadOrDefaultAsync<T>(Func<Task<IReadOnlyList<T>>> load)
	{
		try
		{
			return await load();
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[DashboardPage] Optional load error: {ex.Message}");
			return [];
		}
	}

	private async Task<IReadOnlyList<AlertItem>> LoadLowStockAlertsAsync()
	{
		var inventory = await _http.GetFromJsonAsync<InventoryReportApi>("api/reports/inventory?lowStockThreshold=10");
		return inventory?.LowStockProducts.Take(5).Select(p => new AlertItem
		{
			Name = p.ProductName,
			Code = $"SP-{p.ProductId:0000}",
			StockText = $"Còn {p.Quantity}",
			Accent = p.Quantity <= 5 ? Color.FromArgb("#BA1A1A") : Color.FromArgb("#50616B")
		}).ToList() ?? [];
	}

	private async Task<IReadOnlyList<ActivityItem>> LoadRecentActivitiesAsync()
	{
		var page = await _http.GetFromJsonAsync<AuditLogPageApi>("api/audit-logs?page=1&pageSize=20");

		var hiddenActions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"LOGIN",
			"LOGIN_FAILED",
			"LOGOUT"
		};

		return page?.Items
			.Where(a => !hiddenActions.Contains(a.Action))
			.Take(6)
			.Select(a => new ActivityItem
			{
				Title = BuildActivityTitle(a),
				Time = a.Timestamp.ToLocalTime().ToString("dd/MM HH:mm"),
				Icon = Initials(a.EntityType),
				Accent = a.Result == "FAILED" ? Color.FromArgb("#BA1A1A") : Color.FromArgb("#5C647A")
			}).ToList() ?? [];
	}

	private async Task<IReadOnlyList<TopProductApi>> LoadTopProductsAsync()
	{
		var top = await _http.GetFromJsonAsync<TopProductReportApi>("api/reports/top-products?take=5&sortBy=quantity");
		return top?.Items.Select(x => new TopProductApi
		{
			MaSanPham = x.ProductId,
			TenSanPham = x.ProductName,
			SoLuongBan = x.SoldQuantity,
			DoanhThu = x.Revenue
		}).ToList() ?? [];
	}

	private async Task<IReadOnlyList<InventoryDistributionItem>> LoadInventoryDistributionAsync()
	{
		var categories = await _http.GetFromJsonAsync<CategoryReportPageApi>("api/reports/categories");
		return categories?.Items
			.Where(x => x.StockQuantity > 0)
			.OrderByDescending(x => x.StockQuantity)
			.Take(6)
			.Select(x => new InventoryDistributionItem
			{
				CategoryName = string.IsNullOrWhiteSpace(x.CategoryName) ? "Chưa phân loại" : x.CategoryName,
				StockQuantity = x.StockQuantity
			})
			.ToList() ?? [];
	}

	private async Task<IReadOnlyList<OrderTrendPoint>> LoadOrderTrendAsync()
	{
		var to = DateTime.UtcNow;
		var from = to.Date.AddDays(-6);
		var url = $"api/reports/orders?from={Uri.EscapeDataString(from.ToString("O"))}&to={Uri.EscapeDataString(to.ToString("O"))}";
		var report = await _http.GetFromJsonAsync<OrderReportApi>(url);
		var byDate = (report?.ByDay ?? [])
			.ToDictionary(x => x.Date.ToLocalTime().Date, x => x);

		return Enumerable.Range(0, 7)
			.Select(offset =>
			{
				var date = from.AddDays(offset).ToLocalTime().Date;
				byDate.TryGetValue(date, out var item);
				return new OrderTrendPoint
				{
					Label = BuildDayLabel(date),
					Completed = item?.Done ?? 0,
					Processing = item == null ? 0 : item.Pending + item.Processing
				};
			})
			.ToList();
	}

	private void UpdateInventoryShare()
	{
		var primary = InventorySlices.FirstOrDefault();
		PrimaryInventoryCategory = primary?.Label ?? "Chưa có";
		PrimaryInventoryPercent = primary == null ? "0%" : primary.PercentText;
	}

	private void ApplyCharts()
	{
		OrderTrendChart.Drawable = new OrderTrendChartDrawable { Points = OrderTrend };
		InventoryDonutChart.Drawable = new InventoryDonutDrawable
		{
			Slices = InventorySlices,
			CenterColor = Color.FromArgb("#213145")
		};
		OrderTrendChart.Invalidate();
		InventoryDonutChart.Invalidate();
	}

	private static IReadOnlyList<OrderTrendPoint> BuildOrderTrend(IReadOnlyList<TrendApi> trend)
	{
		return trend.TakeLast(7).Select(x => new OrderTrendPoint
		{
			Label = BuildTrendLabel(x),
			Completed = x.CompletedOrders,
			Processing = x.ProcessingOrders
		}).ToList();
	}

	private static string BuildDayLabel(DateTime date)
	{
		return date.DayOfWeek switch
		{
			DayOfWeek.Monday => "THỨ 2",
			DayOfWeek.Tuesday => "THỨ 3",
			DayOfWeek.Wednesday => "THỨ 4",
			DayOfWeek.Thursday => "THỨ 5",
			DayOfWeek.Friday => "THỨ 6",
			DayOfWeek.Saturday => "THỨ 7",
			_ => "CN"
		};
	}

	private static string BuildTrendLabel(TrendApi trend)
	{
		if (!string.IsNullOrWhiteSpace(trend.Label))
			return trend.Label;

		return trend.Date.ToLocalTime().DayOfWeek switch
		{
			DayOfWeek.Monday => "THỨ 2",
			DayOfWeek.Tuesday => "THỨ 3",
			DayOfWeek.Wednesday => "THỨ 4",
			DayOfWeek.Thursday => "THỨ 5",
			DayOfWeek.Friday => "THỨ 6",
			DayOfWeek.Saturday => "THỨ 7",
			_ => "CN"
		};
	}

	private static IReadOnlyList<InventorySlice> BuildInventorySlices(IReadOnlyList<InventoryDistributionItem> items)
	{
		var colors = new[]
		{
			Color.FromArgb("#FFFFFF"),
			Color.FromArgb("#B7D8FF"),
			Color.FromArgb("#7C3AED"),
			Color.FromArgb("#22C55E"),
			Color.FromArgb("#F59E0B"),
			Color.FromArgb("#EF4444")
		};
		var total = items.Sum(x => x.StockQuantity);
		if (total == 0)
			return [];

		return items.Select((x, index) => new InventorySlice
		{
			Label = x.CategoryName,
			Quantity = x.StockQuantity,
			Percent = x.StockQuantity * 100d / total,
			Color = colors[index % colors.Length]
		}).ToList();
	}

	private static string MapAuditAction(string value) => value switch
	{
		"CREATE" => "Tạo",
		"UPDATE" => "Cập nhật",
		"DELETE" => "Xóa",
		"STATUS_CHANGE" => "Đổi trạng thái",
		"INVENTORY_ADJUSTMENT" => "Điều chỉnh kho",
		_ => value
	};

	private static string MapEntity(string value) => value switch
	{
		"DonHang" => "đơn hàng",
		"SanPham" => "sản phẩm",
		"PhieuNhapKho" => "phiếu nhập",
		"PhieuXuatKho" => "phiếu xuất",
		_ => value
	};

	private static string Initials(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return "HT";
		var letters = new string(value.Where(char.IsUpper).Take(2).ToArray());
		return string.IsNullOrWhiteSpace(letters) ? value[..Math.Min(2, value.Length)].ToUpper() : letters;
	}
  
    private async void OnExportReportClicked(object? sender, EventArgs e)
	{
		try
		{
			var now = DateTime.Now;
			var fileName = $"dashboard-report-{now:yyyyMMdd-HHmmss}.csv";
			var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
			var path = Path.Combine(desktop, fileName);

			var csv = new StringBuilder();

			csv.AppendLine("BÁO CÁO DASHBOARD");
			csv.AppendLine($"Thời gian xuất,{now:dd/MM/yyyy HH:mm}");
			csv.AppendLine();

			csv.AppendLine("TỔNG QUAN");
			csv.AppendLine("Chỉ số,Giá trị,Ghi chú");
			foreach (var stat in Stats)
				csv.AppendLine($"{Csv(stat.Title)},{Csv(stat.Value)},{Csv(stat.Subtitle)}");

			csv.AppendLine();
			csv.AppendLine("SẢN PHẨM BÁN CHẠY");
			csv.AppendLine("Mã sản phẩm,Tên sản phẩm,Số lượng bán,Doanh thu");
			foreach (var product in TopProducts)
				csv.AppendLine($"{product.MaSanPham},{Csv(product.TenSanPham)},{product.SoLuongBan},{product.DoanhThu}");

			csv.AppendLine();
			csv.AppendLine("PHÂN BỔ TỒN KHO");
			csv.AppendLine("Danh mục,Số lượng tồn");
			foreach (var item in InventoryDistribution)
				csv.AppendLine($"{Csv(item.CategoryName)},{item.StockQuantity}");

			csv.AppendLine();
			csv.AppendLine("CẢNH BÁO TỒN KHO THẤP");
			csv.AppendLine("Sản phẩm,Mã,Số lượng");
			foreach (var alert in Alerts)
				csv.AppendLine($"{Csv(alert.Name)},{Csv(alert.Code)},{Csv(alert.StockText)}");

			await File.WriteAllTextAsync(path, csv.ToString(), new UTF8Encoding(true));

			await DisplayAlertAsync("Xuất báo cáo", $"Đã lưu báo cáo tại Desktop:\n{fileName}", "OK");
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Không xuất được báo cáo", ex.Message, "OK");
		}
	}

	private static string Csv(string? value)
	{
		value ??= string.Empty;
		return $"\"{value.Replace("\"", "\"\"")}\"";
	}

	private async void OnQuickActionTapped(object? sender, TappedEventArgs e)
	{
		if (e.Parameter is string route && !string.IsNullOrWhiteSpace(route))
			await Shell.Current.GoToAsync(route);
	}

	private static string BuildActivityTitle(AuditLogApi log)
	{
		var entityName = ResolveEntityName(log);
		return $"{MapAuditAction(log.Action)} {MapEntity(log.EntityType)} {entityName}";
	}

	private static string ResolveEntityName(AuditLogApi log)
	{
		if (log.EntityType == "SanPham")
		{
			var name = ReadAuditString(log.After, "TenSanPham")
				?? ReadAuditString(log.Before, "TenSanPham");

			if (!string.IsNullOrWhiteSpace(name))
				return name;
		}

		return log.EntityId ?? string.Empty;
	}

	private static string? ReadAuditString(string json, string propertyName)
	{
		if (string.IsNullOrWhiteSpace(json) || json == "{}")
			return null;

		try
		{
			using var doc = JsonDocument.Parse(json);
			return doc.RootElement.TryGetProperty(propertyName, out var value)
				? value.GetString()
				: null;
		}
		catch
		{
			return null;
		}
	}
}
