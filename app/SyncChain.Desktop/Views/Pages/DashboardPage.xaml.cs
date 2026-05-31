using SyncChain.Desktop.Models;
using SyncChain.Desktop.Services;

namespace SyncChain.Desktop.Views.Pages;

public partial class DashboardPage : ContentPage
{
	public IReadOnlyList<StatCard> Stats => DemoData.DashboardStats;
	public IReadOnlyList<AlertItem> Alerts => DemoData.LowStockAlerts;
	public IReadOnlyList<ActivityItem> Activities => DemoData.Activities;
	public IReadOnlyList<BridgeItem> Bridges => DemoData.Bridges;

	public DashboardPage()
	{
		InitializeComponent();
		BindingContext = this;
	}
}
