using System.Collections.ObjectModel;
using SyncChain.Desktop.Models;
using SyncChain.Desktop.Services;

namespace SyncChain.Desktop.Views.Pages;

[QueryProperty(nameof(OrderId), "orderId")]
public partial class OrderDetailPage : ContentPage
{
	private int _orderId;
	private string _status = "pending";

	public ObservableCollection<OrderDetailLineItem> Lines { get; } = new();

	public string OrderId
	{
		set
		{
			// Nhận mã đơn hàng từ tham số điều hướng.
			if (int.TryParse(value, out var orderId))
			{
				_orderId = orderId;
			}
		}
	}

	public OrderDetailPage()
	{
		InitializeComponent();
		BindingContext = this;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await LoadOrderAsync();
	}

	// Tải thông tin đơn hàng, chi tiết dòng hàng và quyền cập nhật trạng thái.
	private async Task LoadOrderAsync()
	{
		if (_orderId <= 0)
		{
			await DisplayAlert("Đơn hàng", "Không tìm thấy mã đơn hàng.", "OK");
			await Shell.Current.GoToAsync("..");
			return;
		}

		try
		{
			var orders = await SyncChainApiClient.Instance.GetOrdersAsync();
			var order = orders.FirstOrDefault(x => x.Id == _orderId);

			if (order != null)
			{
				_status = order.Status;
				OrderTitleLabel.Text = $"ĐƠN HÀNG {order.Code}";
				OrderSubtitleLabel.Text = $"Tạo lúc {order.CreatedAt}";
				OrderCodeLabel.Text = order.Code;
				TotalLabel.Text = order.Total;
				StatusLabel.Text = order.Status;
				StatusBadge.BackgroundColor = order.StatusColor;
				StatusPicker.SelectedItem = order.Status;
			}

			var details = await SyncChainApiClient.Instance.GetOrderDetailsAsync(_orderId);
			Lines.Clear();

			foreach (var detail in details)
			{
				Lines.Add(detail);
			}

			LineCountLabel.Text = $"{Lines.Count} sản phẩm";
			EmptyLinesLabel.IsVisible = Lines.Count == 0;

			var canManage = SyncChainApiClient.Instance.CanManageOrders;
			StatusPicker.IsVisible = canManage;
			UpdateStatusButton.IsVisible = canManage;
		}
		catch (Exception ex)
		{
			await DisplayAlert("Không tải được chi tiết đơn", ex.Message, "OK");
		}
	}

	// Cập nhật trạng thái đơn hàng khi người dùng chọn trạng thái mới.
	private async void OnUpdateStatusClicked(object? sender, EventArgs e)
	{
		if (StatusPicker.SelectedItem is not string status)
		{
			await DisplayAlert("Trạng thái", "Vui lòng chọn trạng thái.", "OK");
			return;
		}

		if (status == _status)
		{
			await DisplayAlert("Trạng thái", "Đơn hàng đang ở trạng thái này.", "OK");
			return;
		}

		UpdateStatusButton.IsEnabled = false;

		try
		{
			await SyncChainApiClient.Instance.UpdateOrderStatusAsync(_orderId, status);
			_status = status;
			await DisplayAlert("Trạng thái", "Cập nhật trạng thái thành công.", "OK");
			await LoadOrderAsync();
		}
		catch (Exception ex)
		{
			await DisplayAlert("Không cập nhật được trạng thái", ex.Message, "OK");
		}
		finally
		{
			UpdateStatusButton.IsEnabled = true;
		}
	}

	// Quay lại danh sách đơn hàng.
	private async void OnBackClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("..");
	}
}
