using System.Globalization;
using SyncChain.Desktop.Models;
using SyncChain.Desktop.Services;

namespace SyncChain.Desktop.Views.Pages;

[QueryProperty(nameof(ProductId), "productId")]
public partial class ProductDetailPage : ContentPage
{
	private int _productId;
	private ProductItem? _product;
	private ProductDetailData? _detail;

	public string ProductId
	{
		set
		{
			// Nhận mã sản phẩm từ tham số điều hướng.
			if (int.TryParse(value, out var productId))
			{
				_productId = productId;
			}
		}
	}

	public ProductDetailPage()
	{
		InitializeComponent();
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		ApplyPermissions();
		await LoadProductAsync();
	}

	// Tải dữ liệu chi tiết sản phẩm và lịch sử kho.
	private async Task LoadProductAsync()
	{
		if (_productId <= 0)
		{
			await DisplayAlert("Sản phẩm", "Không tìm thấy mã sản phẩm.", "OK");
			await Shell.Current.GoToAsync("..");
			return;
		}

		try
		{
			_detail = await SyncChainApiClient.Instance.GetProductDetailAsync(_productId);
			_product = _detail.Product;
			RenderProduct(_detail);
		}
		catch (Exception ex)
		{
			await DisplayAlert("Không tải được sản phẩm", ex.Message, "OK");
		}
	}

	// Đổ dữ liệu sản phẩm lên các nhãn, ảnh, số liệu và form sửa.
	private void RenderProduct(ProductDetailData detail)
	{
		var product = detail.Product;
		var soldCount = detail.SoldCount;
		var revenue = detail.Revenue;
		var consumption = product.StockQuantity <= 0 ? 1 : Math.Min(1, Math.Max(0.05, soldCount / 100d));

		CodeLabel.Text = product.Code;
		NameLabel.Text = product.Name;
		DescriptionLabel.Text = product.Description;
		PriceLabel.Text = product.Price;
		ImportPriceLabel.Text = product.ImportPrice.ToString("N0", CultureInfo.InvariantCulture) + " VND";
		StockLabel.Text = product.StockQuantity.ToString("00", CultureInfo.InvariantCulture);
		StockLabel.TextColor = product.StockQuantity <= product.LowStockThreshold ? Colors.Firebrick : Colors.Black;
		LowStockLabel.Text = product.LowStockThreshold.ToString(CultureInfo.InvariantCulture);
		SoldLabel.Text = soldCount.ToString(CultureInfo.InvariantCulture);
		SoldMetricLabel.Text = soldCount.ToString(CultureInfo.InvariantCulture);
		RevenueMetricLabel.Text = revenue.ToString("N0", CultureInfo.InvariantCulture) + " VND";
		ConsumptionProgress.Progress = consumption;
		PerformanceNoteLabel.Text = product.StockQuantity <= 0
			? "Sản phẩm đã hết hàng và được chuyển sang trạng thái Ngừng bán."
			: product.StockQuantity <= product.LowStockThreshold
				? "Sản phẩm đang bán tốt nhưng tồn kho thấp. Nên tạo phiếu nhập mới."
				: "Tồn kho hiện tại đang ổn định.";
		InventoryStateLabel.Text = product.BadgeText;
		RenderHistory(detail.StockHistory);

		StatusLabel.Text = product.BadgeText;
		StatusLabel.TextColor = Colors.White;
		StatusBadge.BackgroundColor = product.BadgeColor;
		ImageBadge.BackgroundColor = product.BadgeColor;
		ImageBadgeLabel.Text = product.BadgeText;

		InitialsLabel.Text = product.Initials;
		var imageSource = CreateImageSource(product.ImageUrl);
		ProductImage.Source = imageSource;
		ProductImage.IsVisible = imageSource != null;
		InitialsLabel.IsVisible = imageSource == null;

		NameEntry.Text = product.Name;
		PriceEntry.Text = product.UnitPrice.ToString(CultureInfo.InvariantCulture);
		ImportPriceEntry.Text = product.ImportPrice.ToString(CultureInfo.InvariantCulture);
		StockEntry.Text = product.StockQuantity.ToString(CultureInfo.InvariantCulture);
		ImageEntry.Text = product.ImageUrl;
		DescriptionEditor.Text = product.Description;
	}

	// Vẽ lại sản phẩm sau khi cập nhật mà vẫn giữ số liệu chi tiết hiện có.
	private void RenderProduct(ProductItem product)
	{
		RenderProduct(new ProductDetailData
		{
			Product = product,
			SoldCount = _detail?.SoldCount ?? 0,
			Revenue = _detail?.Revenue ?? 0,
			StockHistory = _detail?.StockHistory ?? Array.Empty<StockHistoryItem>()
		});
	}

	// Hiển thị lịch sử nhập/xuất kho mới nhất.
	private void RenderHistory(IReadOnlyList<StockHistoryItem> history)
	{
		HistoryList.Children.Clear();

		if (history.Count == 0)
		{
			HistoryList.Children.Add(new Label
			{
				Text = "Chưa có lịch sử nhập/xuất kho.",
				TextColor = Colors.Gray,
				FontSize = 12
			});
			return;
		}

		foreach (var item in history.Take(8))
		{
			var row = new Grid
			{
				ColumnDefinitions =
				{
					new ColumnDefinition { Width = GridLength.Star },
					new ColumnDefinition { Width = GridLength.Star },
					new ColumnDefinition { Width = GridLength.Star },
					new ColumnDefinition { Width = GridLength.Star }
				}
			};

			row.Add(new Label { Text = item.Time, FontSize = 12 }, 0);
			row.Add(new Label { Text = item.Type, FontAttributes = FontAttributes.Bold, FontSize = 12 }, 1);
			row.Add(new Label { Text = item.Quantity, FontSize = 12 }, 2);
			row.Add(new Label { Text = item.Note, FontSize = 12 }, 3);
			HistoryList.Children.Add(row);
		}
	}

	// Bật/tắt các thao tác quản lý theo quyền người dùng.
	private void ApplyPermissions()
	{
		var canManage = SyncChainApiClient.Instance.CanManageProducts;
		EditToggleButton.IsVisible = canManage;
		ImportButton.IsVisible = canManage;
		ManageActions.IsVisible = canManage;
	}

	// Tạo nguồn ảnh từ file cục bộ, URL tương đối backend hoặc URL tuyệt đối.
	private static ImageSource? CreateImageSource(string imageUrl)
{
    if (string.IsNullOrWhiteSpace(imageUrl))
        return null;

    try
    {
        // FILE LOCAL
        if (File.Exists(imageUrl))
            return ImageSource.FromStream(() => File.OpenRead(imageUrl));

        // URL TƯƠNG ĐỐI TỪ BACKEND
        if (imageUrl.StartsWith("/"))
        {
            imageUrl = $"http://localhost:5292{imageUrl}";
        }

        // URL HTTP/HTTPS
        if (Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri))
        {
            if (uri.IsFile && File.Exists(uri.LocalPath))
                return ImageSource.FromStream(() => File.OpenRead(uri.LocalPath));

            if (uri.Scheme is "http" or "https")
                return ImageSource.FromUri(uri);
        }
    }
    catch
    {
        return null;
    }

	return null;
}

	// Kiểm tra form và lưu thay đổi sản phẩm.
	private async void OnSaveClicked(object? sender, EventArgs e)
	{
		if (_product == null)
			return;

		var name = NameEntry.Text?.Trim() ?? string.Empty;
		var imageUrl = ImageEntry.Text?.Trim() ?? string.Empty;
		var description = DescriptionEditor.Text?.Trim() ?? string.Empty;

		if (string.IsNullOrWhiteSpace(name))
		{
			await DisplayAlert("Sản phẩm", "Vui lòng nhập tên sản phẩm.", "OK");
			return;
		}

		if (!decimal.TryParse(PriceEntry.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var price) || price < 0)
		{
			await DisplayAlert("Sản phẩm", "Giá bán không hợp lệ.", "OK");
			return;
		}

		if (!decimal.TryParse(ImportPriceEntry.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var importPrice) || importPrice < 0)
		{
			await DisplayAlert("Sản phẩm", "Giá nhập không hợp lệ.", "OK");
			return;
		}

		if (!int.TryParse(StockEntry.Text, out var stockQuantity) || stockQuantity < 0)
		{
			await DisplayAlert("Sản phẩm", "Tồn kho không hợp lệ.", "OK");
			return;
		}

		SaveButton.IsEnabled = false;

		try
		{
			if (File.Exists(imageUrl))
			{
				imageUrl = await SyncChainApiClient.Instance.UploadProductImageAsync(imageUrl);
			}

			_product = await SyncChainApiClient.Instance.UpdateProductAsync(_product.Id, name, price, importPrice, stockQuantity, imageUrl, description);
			_detail = await SyncChainApiClient.Instance.GetProductDetailAsync(_product.Id);
			_product = _detail.Product;
			RenderProduct(_detail);
			EditForm.IsVisible = false;
			await DisplayAlert("Sản phẩm", "Cập nhật sản phẩm thành công.", "OK");
		}
		catch (Exception ex)
		{
			await DisplayAlert("Không cập nhật được sản phẩm", ex.Message, "OK");
		}
		finally
		{
			SaveButton.IsEnabled = true;
		}
	}

	// Chọn ảnh mới và xem trước trên trang chi tiết.
	private async void OnPickDetailImageClicked(object? sender, EventArgs e)
	{
		try
		{
			var file = await FilePicker.Default.PickAsync(new PickOptions
			{
				PickerTitle = "Chọn hình ảnh sản phẩm",
				FileTypes = FilePickerFileType.Images
			});

			if (file == null)
				return;

			ImageEntry.Text = file.FullPath ?? file.FileName;
			var imageSource = CreateImageSource(ImageEntry.Text);
			ProductImage.Source = imageSource;
			ProductImage.IsVisible = imageSource != null;
			InitialsLabel.IsVisible = imageSource == null;
		}
		catch (Exception ex)
		{
			await DisplayAlert("Không chọn được ảnh", ex.Message, "OK");
		}
	}

	// Ẩn/hiện form chỉnh sửa.
	private void OnToggleEditClicked(object? sender, EventArgs e)
	{
		EditForm.IsVisible = !EditForm.IsVisible;
	}

	// Nhập thêm số lượng tồn kho cho sản phẩm.
	private async void OnImportClicked(object? sender, EventArgs e)
	{
		if (_product == null)
			return;

		var amountText = await DisplayPromptAsync("Nhập thêm hàng", "Số lượng nhập thêm:", "Cập nhật", "Hủy", keyboard: Keyboard.Numeric);
		if (!int.TryParse(amountText, out var amount) || amount <= 0)
			return;

		try
		{
			_product = await SyncChainApiClient.Instance.ImportProductStockAsync(_product.Id, amount, "Nhập thêm hàng từ Desktop");
			_detail = await SyncChainApiClient.Instance.GetProductDetailAsync(_product.Id);
			_product = _detail.Product;
			RenderProduct(_detail);
		}
		catch (Exception ex)
		{
			await DisplayAlert("Không cập nhật được tồn kho", ex.Message, "OK");
		}
	}

	// Chuyển sản phẩm sang trạng thái ngừng bán.
	private async void OnStopSellingClicked(object? sender, EventArgs e)
	{
		if (_product == null)
			return;

		var confirmed = await DisplayAlert("Ngừng bán", $"Chuyển {_product.Name} sang trạng thái Ngừng bán?", "Đồng ý", "Hủy");
		if (!confirmed)
			return;

		try
		{
			_product = await SyncChainApiClient.Instance.UpdateProductStatusAsync(_product.Id, "Ngung ban");
			_detail = await SyncChainApiClient.Instance.GetProductDetailAsync(_product.Id);
			_product = _detail.Product;
			RenderProduct(_detail);
		}
		catch (Exception ex)
		{
			await DisplayAlert("Không ngừng bán được", ex.Message, "OK");
		}
	}

	// Quay lại trang trước.
	private async void OnBackClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("..");
	}
}
