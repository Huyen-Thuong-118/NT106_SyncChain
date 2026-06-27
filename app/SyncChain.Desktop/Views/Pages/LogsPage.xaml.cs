using System.Net.Http.Json;
using System.Text.Json;
using SyncChain.Desktop.Models;

namespace SyncChain.Desktop.Views.Pages;

public partial class LogsPage : ContentPage
{
	private readonly HttpClient _http;
	private List<AuditLogApi> _currentItems = [];
	private int _currentPage = 1;
	private int _totalPages = 1;
	private readonly int _pageSize = 20;
	private bool _isDetailOpen;
	private CancellationTokenSource? _searchDelay;

	public IReadOnlyList<LogItem> Logs { get; private set; } = [];
	public string TodayActivities { get; private set; } = "0";
	public string DataChanges { get; private set; } = "0";
	public string Warnings { get; private set; } = "0";
	public string PaginationText { get; private set; } = string.Empty;
	public string CurrentPageText => $"{_currentPage}/{_totalPages}";
	public bool CanGoPrevious => _currentPage > 1;
	public bool CanGoNext => _currentPage < _totalPages;
	public bool IsEmpty => Logs.Count == 0;
	public bool IsDetailOpen
	{
		get => _isDetailOpen;
		private set
		{
			if (_isDetailOpen == value) return;
			_isDetailOpen = value;
			OnPropertyChanged();
		}
	}
	public string DetailSummary { get; private set; } = string.Empty;
	public string DetailUser { get; private set; } = string.Empty;
	public string DetailDevice { get; private set; } = string.Empty;
	public string DetailTrace { get; private set; } = string.Empty;
	public string DetailTime { get; private set; } = string.Empty;
	public string DetailBefore { get; private set; } = "{}";
	public string DetailAfter { get; private set; } = "{}";
	public string DetailMetadata { get; private set; } = "{}";

	public LogsPage() : this(Services.ApiClientProvider.Client) { }

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
			var query = BuildQuery();
			var page = await _http.GetFromJsonAsync<AuditLogPageApi>($"api/audit-logs?{query}")
				?? new AuditLogPageApi();
			_currentItems = page.Items;
			_totalPages = Math.Max(1, page.TotalPages);
			_currentPage = Math.Min(Math.Max(1, page.Page), _totalPages);
			Logs = page.Items
				.Where(MatchesSearch)
				.Select(MapToLogItem)
				.ToList();
			PaginationText = page.TotalItems == 0
				? "Không có hoạt động"
				: $"Trang {_currentPage:N0}/{_totalPages:N0} · {page.TotalItems:N0} hoạt động";
			await LoadStatisticsAsync();
			NotifyAll();
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Không tải được nhật ký", ex.Message, "OK");
		}
	}

	private async Task LoadStatisticsAsync()
	{
		var today = DateTime.UtcNow.Date.ToString("O");
		var todayPage = await _http.GetFromJsonAsync<AuditLogPageApi>(
			$"api/audit-logs?from={Uri.EscapeDataString(today)}&page=1&pageSize=200");
		var items = todayPage?.Items ?? [];
		TodayActivities = (todayPage?.TotalItems ?? 0).ToString("N0");
		DataChanges = items.Count(x =>
			x.Action is "CREATE" or "UPDATE" or "DELETE" or "STATUS_CHANGE" or
			"INVENTORY_ADJUSTMENT" or "ORDER_STATUS_CHANGE" or "SHIPPING_STATUS_CHANGE")
			.ToString("N0");
		Warnings = items.Count(x => x.Result == "FAILED").ToString("N0");
	}

	private string BuildQuery()
	{
		var parameters = new List<string>
		{
			$"page={_currentPage}",
			$"pageSize={_pageSize}"
		};
		var role = RolePicker.SelectedIndex switch
		{
			1 => "admin", 2 => "manager", 3 => "staff", 4 => "customer", _ => string.Empty
		};
		var action = ActionPicker.SelectedIndex switch
		{
			1 => "LOGIN", 2 => "CREATE", 3 => "UPDATE", 4 => "STATUS_CHANGE",
			5 => "ROLE_CHANGE", 6 => "PASSWORD_CHANGE", 7 => "INVENTORY_ADJUSTMENT",
			_ => string.Empty
		};
		var result = ResultPicker.SelectedIndex switch
		{
			1 => "SUCCESS", 2 => "FAILED", _ => string.Empty
		};
		if (role != string.Empty) parameters.Add($"role={Uri.EscapeDataString(role)}");
		if (action != string.Empty) parameters.Add($"action={Uri.EscapeDataString(action)}");
		if (result != string.Empty) parameters.Add($"result={Uri.EscapeDataString(result)}");
		return string.Join("&", parameters);
	}

	private bool MatchesSearch(AuditLogApi item)
	{
		var search = SearchEntry.Text?.Trim();
		if (string.IsNullOrWhiteSpace(search)) return true;
		return item.Username.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
			item.Action.Contains(search, StringComparison.OrdinalIgnoreCase) ||
			item.EntityType.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
			(item.EntityId?.Contains(search, StringComparison.CurrentCultureIgnoreCase) ?? false);
	}

	private async void OnFilterChanged(object? sender, EventArgs e)
	{
		_currentPage = 1;
		await LoadLogsAsync();
	}

	private async void OnSearchChanged(object? sender, TextChangedEventArgs e)
	{
		_searchDelay?.Cancel();
		_searchDelay = new CancellationTokenSource();
		try
		{
			await Task.Delay(300, _searchDelay.Token);
			_currentPage = 1;
			await LoadLogsAsync();
		}
		catch (TaskCanceledException) { }
	}

	private async void OnClearFiltersClicked(object? sender, EventArgs e)
	{
		SearchEntry.Text = string.Empty;
		RolePicker.SelectedIndex = 0;
		ActionPicker.SelectedIndex = 0;
		ResultPicker.SelectedIndex = 0;
		_currentPage = 1;
		await LoadLogsAsync();
	}

	private async void OnReloadClicked(object? sender, EventArgs e) => await LoadLogsAsync();
	private async void OnPreviousClicked(object? sender, EventArgs e)
	{
		if (_currentPage <= 1) return;
		_currentPage--;
		await LoadLogsAsync();
	}
	private async void OnNextClicked(object? sender, EventArgs e)
	{
		if (_currentPage >= _totalPages) return;
		_currentPage++;
		await LoadLogsAsync();
	}

	private void OnViewDetailClicked(object? sender, EventArgs e)
	{
		if (sender is not Button button ||
			!long.TryParse(button.CommandParameter?.ToString(), out var id))
			return;
		var item = _currentItems.FirstOrDefault(x => x.Id == id);
		if (item == null) return;

		DetailSummary = $"{ActionText(item.Action)} · {item.EntityType} {item.EntityId}".Trim();
		DetailUser = $"{DisplayUsername(item.Username)} · {RoleText(item.Role)}";
		DetailDevice = $"{EmptyFallback(item.IpAddress)} · {EmptyFallback(item.UserAgent)}";
		DetailTrace = EmptyFallback(item.TraceId);
		DetailTime = item.Timestamp.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");
		DetailBefore = PrettyJson(item.Before);
		DetailAfter = PrettyJson(item.After);
		DetailMetadata = PrettyJson(item.Metadata);
		foreach (var property in new[] { nameof(DetailSummary), nameof(DetailUser),
			nameof(DetailDevice), nameof(DetailTrace), nameof(DetailTime),
			nameof(DetailBefore), nameof(DetailAfter), nameof(DetailMetadata) })
			OnPropertyChanged(property);
		IsDetailOpen = true;
	}

	private void OnCloseDetailClicked(object? sender, EventArgs e) => IsDetailOpen = false;

	private static LogItem MapToLogItem(AuditLogApi log)
	{
		var success = log.Result == "SUCCESS";
		return new LogItem
		{
			Id = log.Id,
			Title = ActionText(log.Action),
			Description = $"{DisplayUsername(log.Username)} · {RoleText(log.Role)}",
			Time = log.Timestamp.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
			Tag = $"{log.EntityType}{(string.IsNullOrWhiteSpace(log.EntityId) ? "" : $" #{log.EntityId}")}",
			Icon = success ? "✓" : "!",
			Accent = success ? Colors.Green : Colors.Red,
			ResultText = success ? "Thành công" : "Thất bại"
		};
	}

	private static string ActionText(string action) => action switch
	{
		"CREATE" => "Tạo mới", "UPDATE" => "Cập nhật", "DELETE" => "Xóa",
		"STATUS_CHANGE" => "Đổi trạng thái", "APPROVE" => "Phê duyệt",
		"REJECT" => "Từ chối", "LOGIN" => "Đăng nhập",
		"LOGIN_FAILED" => "Đăng nhập thất bại", "LOGOUT" => "Đăng xuất",
		"PASSWORD_CHANGE" => "Đổi mật khẩu", "ROLE_CHANGE" => "Đổi vai trò",
		"INVENTORY_ADJUSTMENT" => "Điều chỉnh kho",
		"ORDER_STATUS_CHANGE" => "Đổi trạng thái đơn",
		"SHIPPING_STATUS_CHANGE" => "Đổi trạng thái vận chuyển",
		_ => action
	};

	private static string RoleText(string role) => role switch
	{
		"admin" => "Admin", "manager" => "Manager", "staff" => "Staff",
		"customer" => "Khách hàng", _ => string.IsNullOrWhiteSpace(role) ? "Không xác định" : role
	};
	private static string DisplayUsername(string value) =>
		string.IsNullOrWhiteSpace(value) ? "Hệ thống" : value;
	private static string EmptyFallback(string value) =>
		string.IsNullOrWhiteSpace(value) ? "Không có" : value;
	private static string PrettyJson(string value)
	{
		try
		{
			using var document = JsonDocument.Parse(value);
			return JsonSerializer.Serialize(document.RootElement, new JsonSerializerOptions { WriteIndented = true });
		}
		catch { return value; }
	}

	private void NotifyAll()
	{
		foreach (var name in new[] { nameof(Logs), nameof(TodayActivities), nameof(DataChanges),
			nameof(Warnings), nameof(PaginationText), nameof(CurrentPageText),
			nameof(CanGoPrevious), nameof(CanGoNext), nameof(IsEmpty) })
			OnPropertyChanged(name);
	}
}
