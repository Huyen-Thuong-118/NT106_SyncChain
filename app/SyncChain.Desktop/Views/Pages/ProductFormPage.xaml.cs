using System.Net.Http.Json;
using System.Text.Json;
using SyncChain.Desktop.Models;
using SyncChain.Desktop.Services;

namespace SyncChain.Desktop.Views.Pages;

public partial class ProductFormPage : ContentPage, IQueryAttributable
{
	private readonly HttpClient _http = ApiClientProvider.Client;
	private int? _productId;
	private SanPhamApi? _product;
	private readonly List<FileResult> _selectedImages = [];

	public string PageTitle { get; private set; } = "Thêm sản phẩm";
	public string SaveButtonText { get; private set; } = "TẠO SẢN PHẨM";
	public string StockHint { get; private set; } = "Tồn kho được ghi nhận khi tạo sản phẩm.";
	public string StatusMessage { get; private set; } = string.Empty;
	public IReadOnlyList<CategoryApi> Categories { get; private set; } = Array.Empty<CategoryApi>();
	public IReadOnlyList<ProductImageItem> ImagePreviews { get; private set; } = Array.Empty<ProductImageItem>();
	public string SelectedImagesText { get; private set; } = "Chưa chọn ảnh mới.";

	public ProductFormPage()
	{
		InitializeComponent();
		BindingContext = this;
	}

	public void ApplyQueryAttributes(IDictionary<string, object> query)
	{
		if (query.TryGetValue("productId", out var value) &&
			int.TryParse(Uri.UnescapeDataString(value?.ToString() ?? string.Empty), out var id))
		{
			_productId = id;
			PageTitle = "Chỉnh sửa sản phẩm";
			SaveButtonText = "LƯU THAY ĐỔI";
			StockHint = "Tồn kho không sửa trực tiếp tại đây. Dùng chức năng nhập thêm hàng.";
			OnPropertyChanged(nameof(PageTitle));
			OnPropertyChanged(nameof(SaveButtonText));
			OnPropertyChanged(nameof(StockHint));
		}
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await LoadAsync();
	}

	private async Task LoadAsync()
	{
		try
		{
			await LoadCategoriesAsync();

			if (!_productId.HasValue)
				return;

			_product = await _http.GetFromJsonAsync<SanPhamApi>($"api/product/{_productId.Value}");
			if (_product == null)
				return;

			NameEntry.Text = _product.TenSanPham;
			DescriptionEditor.Text = _product.MoTa;
			CostPriceEntry.Text = _product.GiaNhap.ToString("0.##");
			SalePriceEntry.Text = _product.GiaBan.ToString("0.##");
			StockEntry.Text = _product.SoLuongTon.ToString();
			StockEntry.IsReadOnly = true;
			ImageUrlEntry.Text = _product.HinhAnhUrl;
			ImagePreviews = ParseImageUrls(_product.HinhAnhUrl)
				.Select(x => new ProductImageItem { Url = x })
				.ToList();
			SelectedImagesText = ImagePreviews.Count == 0
				? "Sản phẩm chưa có ảnh."
				: $"{ImagePreviews.Count} ảnh hiện có.";
			OnPropertyChanged(nameof(ImagePreviews));
			OnPropertyChanged(nameof(SelectedImagesText));
			CategoryPicker.SelectedItem = Categories.FirstOrDefault(x => x.MaDanhMuc == _product.MaDanhMuc);
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Không tải được sản phẩm", ex.Message, "OK");
		}
	}

	private async void OnSaveClicked(object? sender, EventArgs e)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(NameEntry.Text))
				throw new InvalidOperationException("Tên sản phẩm không được để trống.");
			if (!decimal.TryParse(CostPriceEntry.Text, out var costPrice) || costPrice < 0)
				throw new InvalidOperationException("Giá nhập không hợp lệ.");
			if (!decimal.TryParse(SalePriceEntry.Text, out var salePrice) || salePrice < 0)
				throw new InvalidOperationException("Giá bán không hợp lệ.");
			if (!int.TryParse(StockEntry.Text, out var stock) || stock < 0)
				throw new InvalidOperationException("Tồn kho không hợp lệ.");

			var category = CategoryPicker.SelectedItem as CategoryApi;
			var imageUrls = ParseRawImageUrls(ImageUrlEntry.Text).ToList();
			foreach (var image in _selectedImages)
				imageUrls.Add(await UploadImageAsync(image));
			var imageUrl = string.Join(Environment.NewLine, imageUrls.Distinct(StringComparer.OrdinalIgnoreCase));

			var body = new
			{
				tenSanPham = NameEntry.Text.Trim(),
				giaBan = salePrice,
				giaNhap = costPrice,
				soLuongTon = stock,
				hinhAnhUrl = imageUrl,
				moTa = DescriptionEditor.Text?.Trim() ?? string.Empty,
				maDanhMuc = category?.MaDanhMuc,
				trangThai = _product?.TrangThai
			};

			var response = _productId.HasValue
				? await _http.PutAsJsonAsync($"api/product/{_productId.Value}", body)
				: await _http.PostAsJsonAsync("api/product", body);

			if (!response.IsSuccessStatusCode)
				throw new InvalidOperationException(await ReadErrorAsync(response));

			StatusMessage = _productId.HasValue
				? "Đã cập nhật sản phẩm."
				: "Đã tạo sản phẩm.";
			OnPropertyChanged(nameof(StatusMessage));
			await DisplayAlertAsync("Thành công", StatusMessage, "OK");
			await Shell.Current.GoToAsync("..");
		}
		catch (Exception ex)
		{
			StatusMessage = ex.Message;
			OnPropertyChanged(nameof(StatusMessage));
			await DisplayAlertAsync("Không lưu được", ex.Message, "OK");
		}
	}

	private async void OnPickImageClicked(object? sender, EventArgs e)
	{
		try
		{
			var files = await FilePicker.Default.PickMultipleAsync(new PickOptions
			{
				PickerTitle = "Chọn nhiều hình ảnh sản phẩm",
				FileTypes = FilePickerFileType.Images
			});
			var selected = files?
				.Where(x => x is not null)
				.Cast<FileResult>()
				.ToList() ?? [];
			if (selected.Count == 0)
				return;

			foreach (var image in selected)
			{
				if (_selectedImages.All(x =>
					!string.Equals(x.FullPath, image.FullPath, StringComparison.OrdinalIgnoreCase)))
				{
					_selectedImages.Add(image);
				}
			}
			var existing = ParseImageUrls(ImageUrlEntry.Text)
				.Select(x => new ProductImageItem { Url = x });
			var local = _selectedImages.Select(x => new ProductImageItem { Url = x.FullPath });
			ImagePreviews = existing.Concat(local).ToList();
			SelectedImagesText = $"Đã chọn {_selectedImages.Count} ảnh mới. Các ảnh sẽ được tải lên khi lưu.";
			OnPropertyChanged(nameof(ImagePreviews));
			OnPropertyChanged(nameof(SelectedImagesText));
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Không chọn được ảnh", ex.Message, "OK");
		}
	}

	private async void OnAddCategoryClicked(object? sender, EventArgs e)
	{
		var name = await DisplayPromptAsync(
			"Thêm danh mục",
			"Nhập tên danh mục mới:",
			"Thêm",
			"Hủy",
			maxLength: 100);
		if (string.IsNullOrWhiteSpace(name))
			return;

		var description = await DisplayPromptAsync(
			"Mô tả danh mục",
			"Nhập mô tả (không bắt buộc):",
			"Lưu",
			"Bỏ qua") ?? string.Empty;

		var response = await _http.PostAsJsonAsync("api/category", new
		{
			tenDanhMuc = name.Trim(),
			moTa = description.Trim()
		});
		if (!response.IsSuccessStatusCode)
		{
			await DisplayAlertAsync("Không thêm được danh mục", await ReadErrorAsync(response), "OK");
			return;
		}

		var created = await response.Content.ReadFromJsonAsync<CategoryApi>();
		await LoadCategoriesAsync();
		CategoryPicker.SelectedItem = Categories.FirstOrDefault(x =>
			x.MaDanhMuc == created?.MaDanhMuc ||
			string.Equals(x.TenDanhMuc, name.Trim(), StringComparison.OrdinalIgnoreCase));
	}

	private async Task LoadCategoriesAsync()
	{
		Categories = (await _http.GetFromJsonAsync<List<CategoryApi>>("api/category") ?? [])
			.Where(x => x.IsActive)
			.ToList();
		OnPropertyChanged(nameof(Categories));
	}

	private async Task<string> UploadImageAsync(FileResult file)
	{
		await using var stream = await file.OpenReadAsync();
		using var content = new MultipartFormDataContent();
		using var fileContent = new StreamContent(stream);
		if (!string.IsNullOrWhiteSpace(file.ContentType))
			fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
		content.Add(fileContent, "file", file.FileName);

		var response = await _http.PostAsync("api/product/upload-image", content);
		if (!response.IsSuccessStatusCode)
			throw new InvalidOperationException(await ReadErrorAsync(response));

		var result = await response.Content.ReadFromJsonAsync<ImageUploadResponse>();
		return result?.ImageUrl ?? throw new InvalidOperationException("API không trả đường dẫn ảnh.");
	}

	private static IEnumerable<string> ParseImageUrls(string? value)
	{
		foreach (var url in ParseRawImageUrls(value))
			yield return Uri.TryCreate(url, UriKind.Absolute, out _)
				? url
				: new Uri(ApiClientProvider.Client.BaseAddress!, url.TrimStart('/')).ToString();
	}

	private static IEnumerable<string> ParseRawImageUrls(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
			yield break;

		foreach (var item in value.Split([',', ';', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries))
		{
			var url = item.Trim();
			if (!string.IsNullOrWhiteSpace(url))
				yield return url;
		}
	}

	private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
	{
		var text = await response.Content.ReadAsStringAsync();
		try
		{
			using var document = JsonDocument.Parse(text);
			if (document.RootElement.TryGetProperty("message", out var message))
				return message.GetString() ?? text;
		}
		catch
		{
		}
		return string.IsNullOrWhiteSpace(text) ? $"HTTP {(int)response.StatusCode}" : text.Trim('"');
	}

	private async void OnBackClicked(object? sender, EventArgs e) =>
		await Shell.Current.GoToAsync("..");

	private sealed class ImageUploadResponse
	{
		public string ImageUrl { get; set; } = string.Empty;
	}
}
