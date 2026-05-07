using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using SyncChain.Desktop.Models;
using SyncChain.Desktop.Services;

namespace SyncChain.Desktop.Views.Pages;

public partial class LogsPage : ContentPage
{
	private readonly List<LogItem> _allLogs = new();

	public ObservableCollection<LogItem> Logs { get; } = new();

	public LogsPage()
	{
		InitializeComponent();
		BindingContext = this;
		TagPicker.SelectedIndex = 0;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await LoadLogsAsync();
	}

	// Tải nhật ký hoạt động từ backend.
	private async Task LoadLogsAsync()
	{
		try
		{
			LogsSubtitleLabel.Text = "Đang tải nhật ký...";
			var logs = await SyncChainApiClient.Instance.GetActivityLogsAsync();
			_allLogs.Clear();
			_allLogs.AddRange(logs);
			ApplyFilter();
		}
		catch (Exception ex)
		{
			LogsSubtitleLabel.Text = "Không tải được nhật ký.";
			await DisplayAlert("Nhật ký", ex.Message, "OK");
		}
	}

	// Lọc nhật ký theo từ khóa và loại hoạt động.
	private void ApplyFilter()
	{
		var keyword = SearchEntry.Text?.Trim() ?? string.Empty;
		var selectedTag = TagPicker.SelectedItem as string;

		var filtered = _allLogs.AsEnumerable();
		if (!string.IsNullOrWhiteSpace(keyword))
		{
			filtered = filtered.Where(x =>
				x.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase)
				|| x.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase)
				|| x.Tag.Contains(keyword, StringComparison.OrdinalIgnoreCase));
		}

		if (!string.IsNullOrWhiteSpace(selectedTag) && selectedTag != "Tất cả")
		{
			filtered = filtered.Where(x => x.Tag == selectedTag);
		}

		Replace(Logs, filtered);
		UpdateSummary();
		LogsSubtitleLabel.Text = Logs.Count == 0
			? "Không có nhật ký phù hợp."
			: $"Hiển thị {Logs.Count} hoạt động gần đây.";
	}

	// Cập nhật số liệu nhanh của trang nhật ký.
	private void UpdateSummary()
	{
		TotalLogsLabel.Text = _allLogs.Count.ToString(CultureInfo.InvariantCulture);
		FilteredLogsLabel.Text = Logs.Count.ToString(CultureInfo.InvariantCulture);

		var todayCount = _allLogs.Count(x =>
			DateTime.TryParseExact(x.Time, "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
			&& date.Date == DateTime.Today);
		TodayLogsLabel.Text = todayCount.ToString(CultureInfo.InvariantCulture);
	}

	// Làm mới nhật ký từ backend.
	private async void OnRefreshClicked(object? sender, EventArgs e)
	{
		await LoadLogsAsync();
	}

	// Lọc lại khi người dùng nhập từ khóa.
	private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
	{
		ApplyFilter();
	}

	// Lọc lại khi đổi loại hoạt động.
	private void OnTagChanged(object? sender, EventArgs e)
	{
		ApplyFilter();
	}

	// Xuất nhật ký đang hiển thị ra file CSV trong thư mục cache.
	private async void OnExportClicked(object? sender, EventArgs e)
	{
		try
		{
			var filePath = Path.Combine(FileSystem.CacheDirectory, $"syncchain-logs-{DateTime.Now:yyyyMMdd-HHmmss}.csv");
			var builder = new StringBuilder();
			builder.AppendLine("Time,Tag,Title,Description");

			foreach (var log in Logs)
			{
				builder.AppendLine(string.Join(",",
					EscapeCsv(log.Time),
					EscapeCsv(log.Tag),
					EscapeCsv(log.Title),
					EscapeCsv(log.Description)));
			}

			await File.WriteAllTextAsync(filePath, builder.ToString(), Encoding.UTF8);
			await DisplayAlert("Nhật ký", $"Đã xuất file CSV:\n{filePath}", "OK");
		}
		catch (Exception ex)
		{
			await DisplayAlert("Nhật ký", ex.Message, "OK");
		}
	}

	// Escape nội dung CSV để tránh vỡ cột.
	private static string EscapeCsv(string value)
	{
		return "\"" + value.Replace("\"", "\"\"") + "\"";
	}

	// Thay dữ liệu trong ObservableCollection để UI tự cập nhật.
	private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> items)
	{
		target.Clear();
		foreach (var item in items)
		{
			target.Add(item);
		}
	}
}
