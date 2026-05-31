using System.Collections.ObjectModel;
using System.Globalization;
using SyncChain.Desktop.Models;
using SyncChain.Desktop.Services;

namespace SyncChain.Desktop.Views.Pages;

public partial class ImportsPage : ContentPage
{
	public ObservableCollection<ImportItem> Imports { get; } = new();
	public ObservableCollection<ProductItem> Products { get; } = new();

	public ImportsPage()
	{
		InitializeComponent();
		BindingContext = this;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await LoadDataAsync();
	}

	// Tải sản phẩm và lịch sử nhập kho từ backend.
	private async Task LoadDataAsync()
	{
		try
		{
			CreateImportButton.IsEnabled = false;
			ImportListSubtitleLabel.Text = "Đang tải dữ liệu...";

			var api = SyncChainApiClient.Instance;
			var products = await api.GetProductsAsync();
			var imports = await api.GetImportHistoryAsync();

			Replace(Products, products.OrderBy(x => x.Name));
			Replace(Imports, imports);

			UpdateSummary();
			ImportListSubtitleLabel.Text = Imports.Count == 0
				? "Chưa có giao dịch nhập kho."
				: $"Hiển thị {Imports.Count} giao dịch nhập kho gần đây.";
		}
		catch (Exception ex)
		{
			ImportListSubtitleLabel.Text = "Không tải được dữ liệu nhập hàng.";
			await DisplayAlert("Nhập hàng", ex.Message, "OK");
		}
		finally
		{
			CreateImportButton.IsEnabled = true;
		}
	}

	// Cập nhật các thẻ thống kê nhanh.
	private void UpdateSummary()
	{
		TotalImportsLabel.Text = Imports.Count.ToString(CultureInfo.InvariantCulture);

		var todayCount = Imports.Count(x =>
			DateTime.TryParseExact(x.Date, "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
			&& date.Date == DateTime.Today);
		TodayImportsLabel.Text = todayCount.ToString(CultureInfo.InvariantCulture);

		var totalQuantity = Imports.Sum(x =>
		{
			var text = x.ProductCount.Replace("+", string.Empty).Replace("sản phẩm", string.Empty).Trim();
			return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var quantity) ? quantity : 0;
		});
		TotalQuantityLabel.Text = totalQuantity.ToString("N0", CultureInfo.InvariantCulture);

		var totalAmount = Imports.Sum(x =>
		{
			var text = x.Amount.Replace("VND", string.Empty).Replace(",", string.Empty).Trim();
			return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) ? amount : 0;
		});
		ImportValueLabel.Text = totalAmount.ToString("N0", CultureInfo.InvariantCulture) + " VND";
	}

	// Làm mới dữ liệu từ backend.
	private async void OnRefreshClicked(object? sender, EventArgs e)
	{
		await LoadDataAsync();
	}

	// Gửi yêu cầu nhập kho cho sản phẩm đang chọn.
	private async void OnCreateImportClicked(object? sender, EventArgs e)
	{
		if (ProductPicker.SelectedItem is not ProductItem product)
		{
			await DisplayAlert("Nhập hàng", "Vui lòng chọn sản phẩm cần nhập.", "OK");
			return;
		}

		if (!int.TryParse(QuantityEntry.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var quantity) || quantity <= 0)
		{
			await DisplayAlert("Nhập hàng", "Số lượng nhập phải lớn hơn 0.", "OK");
			return;
		}

		try
		{
			CreateImportButton.IsEnabled = false;
			var note = string.IsNullOrWhiteSpace(NoteEditor.Text) ? "Nhập hàng từ trang Nhập hàng" : NoteEditor.Text.Trim();
			await SyncChainApiClient.Instance.ImportProductStockAsync(product.Id, quantity, note);

			QuantityEntry.Text = string.Empty;
			NoteEditor.Text = string.Empty;
			await LoadDataAsync();
			await DisplayAlert("Nhập hàng", "Đã nhập kho thành công.", "OK");
		}
		catch (Exception ex)
		{
			await DisplayAlert("Nhập hàng", ex.Message, "OK");
		}
		finally
		{
			CreateImportButton.IsEnabled = true;
		}
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
