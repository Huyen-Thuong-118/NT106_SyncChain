using SyncChain.Desktop.Models;
using SyncChain.Desktop.Services;

namespace SyncChain.Desktop.Views.Pages;

public partial class LogsPage : ContentPage
{
	public IReadOnlyList<LogItem> Logs => DemoData.Logs;

	public LogsPage()
	{
		InitializeComponent();
		BindingContext = this;
	}
}
