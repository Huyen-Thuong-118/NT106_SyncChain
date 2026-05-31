using System.Net.Http.Json;
using Microsoft.Maui.Controls;
using SyncChain.Desktop.Models;

namespace SyncChain.Desktop.Views.Pages;

public partial class LogsPage : ContentPage
{
	private readonly HttpClient _http;

	public IReadOnlyList<LogItem> Logs { get; private set; } = Array.Empty<LogItem>();

	// Thống kê
	public string TodayActivities { get; private set; } = "0";
	public string DataChanges { get; private set; } = "0";
	public string Warnings { get; private set; } = "0";
	public string PaginationText { get; private set; } = "Đang tải...";

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
			// Gọi API lấy logs
			var logs = await _http.GetFromJsonAsync<List<LogApi>>("api/report/logs");
			if (logs != null)
			{
				Logs = logs.Select(MapToLogItem).ToList();

				// Thống kê
				var today = DateTime.Now.Date;
				TodayActivities = logs.Count(l => l.Time.Date == today).ToString();
				DataChanges = logs.Count(l => l.Tag == "Kho hàng").ToString();
				Warnings = logs.Count(l => l.Level == "danger" || l.Level == "warning").ToString();
				PaginationText = $"Hiển thị 1 - {logs.Count} trong số {logs.Count} hoạt động";

				OnPropertyChanged(nameof(Logs));
				OnPropertyChanged(nameof(TodayActivities));
				OnPropertyChanged(nameof(DataChanges));
				OnPropertyChanged(nameof(Warnings));
				OnPropertyChanged(nameof(PaginationText));
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[LogsPage] Load error: {ex.Message}");
		}
	}

	private static LogItem MapToLogItem(LogApi log)
	{
		var (icon, accent) = log.Level switch
		{
			"success" => ("✅", Color.FromArgb("#2E7D32")),
			"danger" => ("❌", Color.FromArgb("#C62828")),
			"warning" => ("⚠️", Color.FromArgb("#F57C00")),
			_ => (log.Icon, Color.FromArgb("#5C647A"))
		};

		return new LogItem
		{
			Title = log.Title,
			Description = log.Description,
			Time = log.Time.ToString("dd/MM/yyyy HH:mm"),
			Tag = log.Tag,
			Icon = icon,
			Accent = accent
		};
	}
}
