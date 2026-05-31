using SyncChain.Desktop.Models;
using SyncChain.Desktop.Services;

namespace SyncChain.Desktop.Views.Pages;

public partial class OrderDetailPage : ContentPage
{
	public IReadOnlyList<LineItem> Lines => DemoData.OrderLines;
	public IReadOnlyList<TimelineItem> Timeline => DemoData.Timeline;

	public OrderDetailPage()
	{
		InitializeComponent();
		BindingContext = this;
	}

	private async void OnBackClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("..");
	}
}
