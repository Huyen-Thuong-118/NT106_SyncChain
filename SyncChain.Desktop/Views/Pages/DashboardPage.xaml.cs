using System.Collections.ObjectModel;
using SyncChain.Desktop.Models;
using SyncChain.Desktop.Services;

namespace SyncChain.Desktop.Views.Pages;

public partial class DashboardPage : ContentPage
{
	public ObservableCollection<StatCard> Stats { get; } = new();
	public ObservableCollection<AlertItem> Alerts { get; } = new();
	public ObservableCollection<ActivityItem> Activities { get; } = new();
	public ObservableCollection<OrderTrendItem> OrderTrend { get; } = new();
	public ObservableCollection<TopProductItem> TopProducts { get; } = new();

	public DashboardPage()
	{
		InitializeComponent();
		BindingContext = this;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await LoadDashboardAsync();
	}

	// Tải dữ liệu dashboard từ API và cập nhật các vùng thống kê.
	private async Task LoadDashboardAsync()
	{
		try
		{
			var dashboard = await SyncChainApiClient.Instance.GetDashboardAsync();

			Replace(Stats, dashboard.Stats);
			Replace(Alerts, dashboard.LowStockAlerts);
			Replace(Activities, dashboard.Activities);
			Replace(OrderTrend, dashboard.OrderTrend);
			Replace(TopProducts, dashboard.TopProducts);

			InventoryPercentLabel.Text = dashboard.InventoryPercent;
			InventorySubtitleLabel.Text = dashboard.InventorySubtitle;
			EmptyAlertsLabel.IsVisible = Alerts.Count == 0;
			SubtitleLabel.Text = $"Cập nhật từ backend luc {DateTime.Now:HH:mm dd/MM/yyyy}.";
		}
		catch (Exception ex)
		{
			ClearAll();
			SubtitleLabel.Text = "Không tải được dashboard từ backend.";
			await DisplayAlert("Bảng điều khiển", ex.Message, "OK");
		}
	}

	// Làm mới dashboard theo thao tác người dùng.
	private async void OnRefreshClicked(object? sender, EventArgs e)
	{
		await LoadDashboardAsync();
	}

	// Xóa dữ liệu hiển thị khi không tải được dashboard.
	private void ClearAll()
	{
		Stats.Clear();
		Alerts.Clear();
		Activities.Clear();
		OrderTrend.Clear();
		TopProducts.Clear();
		EmptyAlertsLabel.IsVisible = true;
		InventoryPercentLabel.Text = "0%";
		InventorySubtitleLabel.Text = "Chưa có dữ liệu";
	}

	// Thay toàn bộ dữ liệu trong ObservableCollection để UI tự cập nhật.
	private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> items)
	{
		target.Clear();
		foreach (var item in items)
		{
			target.Add(item);
		}
	}
}
