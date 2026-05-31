using System.Globalization;
using SyncChain.Desktop.Services;

namespace SyncChain.Desktop.Views.Pages;

public partial class CreateProductPage : ContentPage
{
	public CreateProductPage()
	{
		InitializeComponent();
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		if (!SyncChainApiClient.Instance.CanManageProducts)
		{
			await DisplayAlert("Không có quyền", "Chỉ admin hoặc manager được thêm sản phẩm.", "OK");
			await Shell.Current.GoToAsync("..");
		}
	}

	// Kiểm tra form, tải ảnh nếu cần và tạo sản phẩm mới.
	private async void OnSaveClicked(object? sender, EventArgs e)
	{
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

			await SyncChainApiClient.Instance.CreateProductAsync(name, price, importPrice, stockQuantity, imageUrl, description);
			await DisplayAlert("Sản phẩm", "Thêm sản phẩm thành công.", "OK");
			await Shell.Current.GoToAsync("..");
		}
		catch (Exception ex)
		{
			await DisplayAlert("Không lưu được sản phẩm", ex.Message, "OK");
		}
		finally
		{
			SaveButton.IsEnabled = true;
		}
	}

	// Chọn ảnh từ máy và hiển thị xem trước.
	private async void OnPickImageClicked(object? sender, EventArgs e)
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
			PreviewImage.Source = imageSource;
			PreviewImage.IsVisible = imageSource != null;
			PreviewLabel.IsVisible = imageSource == null;
		}
		catch (Exception ex)
		{
			await DisplayAlert("Không chọn được ảnh", ex.Message, "OK");
		}
	}

	// Tạo nguồn ảnh từ file cục bộ hoặc URL.
	private static ImageSource? CreateImageSource(string imageUrl)
	{
		if (string.IsNullOrWhiteSpace(imageUrl))
			return null;

		if (File.Exists(imageUrl))
			return ImageSource.FromStream(() => File.OpenRead(imageUrl));

		return Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https"
			? ImageSource.FromUri(uri)
			: null;
	}

	// Quay lại trang trước.
	private async void OnBackClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("..");
	}
}
