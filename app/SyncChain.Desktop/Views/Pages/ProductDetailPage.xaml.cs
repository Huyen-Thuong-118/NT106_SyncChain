using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SyncChain.Desktop.Models;
using SyncChain.Desktop.Services;

namespace SyncChain.Desktop.Views.Pages;

public partial class ProductDetailPage : ContentPage, IQueryAttributable
{
	private readonly HttpClient _http = ApiClientProvider.Client;
	private int _productId;
	private SanPhamApi? _product;

	public bool CanManageProducts =>
		ApiClientProvider.Role?.Trim().ToLowerInvariant() is "admin" or "manager";
	// Khách hàng: hiện khu mua hàng thay cho các thao tác quản trị.
	public bool IsCustomer =>
		ApiClientProvider.Role?.Trim().ToLowerInvariant() == "customer";
	// Chỉ cho thêm giỏ khi là khách, sản phẩm đang bán và còn hàng.
	public bool CanBuy =>
		IsCustomer && _product != null && _product.TrangThai != "Ngung ban" && _product.SoLuongTon > 0;
	public string ProductCode { get; private set; } = string.Empty;
	public string ProductName { get; private set; } = string.Empty;
	public string ProductInitials { get; private set; } = "SP";
	public string ProductDescription { get; private set; } = string.Empty;
	public string ProductStatus { get; private set; } = string.Empty;
	public string ProductStatusColor { get; private set; } = "#5C647A";
	public string StatusActionText { get; private set; } = "NGỪNG BÁN";
	public string Price { get; private set; } = "0 đ";
	public string ImportPrice { get; private set; } = "0 đ";
	public string Stock { get; private set; } = "0";
	public string SoldCount { get; private set; } = "0";
	public string Category { get; private set; } = "Chưa phân loại";
	public string PerformanceIcon { get; private set; } = "→";
	public string PerformanceText { get; private set; } = "0%";
	public Color PerformanceColor { get; private set; } = Colors.Gray;
	public IReadOnlyList<ProductImageItem> ProductImages { get; private set; } = Array.Empty<ProductImageItem>();
	public string SelectedImageUrl { get; private set; } = string.Empty;
	public string ImageCountText { get; private set; } = "Chưa có hình ảnh";
	public string RatingText { get; private set; } = "—";
	public string RatingStars { get; private set; } = "☆☆☆☆☆";
	public string ReviewCountText { get; private set; } = "0 đánh giá";
	public string ReviewEmptyText { get; private set; } = "Sản phẩm chưa có đánh giá.";

	public ProductDetailPage()
	{
		InitializeComponent();
		BindingContext = this;
	}

	public void ApplyQueryAttributes(IDictionary<string, object> query)
	{
		if (query.TryGetValue("productId", out var value))
			int.TryParse(Uri.UnescapeDataString(value?.ToString() ?? string.Empty), out _productId);
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await LoadProductDetailAsync();
	}

	private async Task LoadProductDetailAsync()
	{
		try
		{
			if (_productId <= 0)
				throw new InvalidOperationException("Thiếu mã sản phẩm.");

			// Khách hàng dùng endpoint public-detail (không lộ giá nhập/doanh thu);
			// nhân sự nội bộ (staff/manager/admin) mới được gọi /detail đầy đủ.
			// Trước đây khách gọi /detail (StaffOrAbove) nên luôn bị 403 → không xem
			// được sản phẩm và không thêm được vào giỏ.
			var role = ApiClientProvider.Role?.Trim().ToLowerInvariant();
			var isInternal = role is "staff" or "manager" or "admin";
			var endpoint = isInternal
				? $"api/product/{_productId}/detail"
				: $"api/product/{_productId}/public-detail";

			var detail = await _http.GetFromJsonAsync<ProductDetailApi>(endpoint);
			if (detail?.Product == null)
				throw new InvalidOperationException("API không trả dữ liệu sản phẩm.");

			_product = detail.Product;
			var sp = detail.Product;
			ProductCode = $"SP-{sp.MaSanPham:0000}";
			ProductName = sp.TenSanPham;
			ProductInitials = BuildInitials(sp.TenSanPham);
			ProductDescription = string.IsNullOrWhiteSpace(sp.MoTa) ? "Chưa có mô tả." : sp.MoTa;
			ProductImages = ParseImageUrls(sp.HinhAnhUrl)
				.Select(x => new ProductImageItem { Url = x })
				.ToList();
			if (ProductImages.Count == 0)
				ProductImages = [new ProductImageItem()];
			var firstImage = ProductImages.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.Url));
			SelectedImageUrl = firstImage?.Url ?? string.Empty;
			ImageCountText = ProductImages.All(x => string.IsNullOrWhiteSpace(x.Url))
				? "Chưa có hình ảnh"
				: $"{ProductImages.Count(x => !string.IsNullOrWhiteSpace(x.Url))} hình ảnh";

			(ProductStatus, ProductStatusColor) = sp.TrangThai switch
			{
				"Ngung ban" => ("NGỪNG BÁN", "#C62828"),
				_ when sp.SoLuongTon <= 0 => ("HẾT HÀNG", "#C62828"),
				_ when sp.SoLuongTon <= sp.MucTonThap => ("SẮP HẾT HÀNG", "#F57C00"),
				_ => ("ĐANG BÁN", "#2E7D32")
			};
			StatusActionText = sp.TrangThai == "Ngung ban" ? "MỞ BÁN" : "NGỪNG BÁN";
			Price = $"{sp.GiaBan:N0} đ";
			ImportPrice = $"{sp.GiaNhap:N0} đ";
			Stock = sp.SoLuongTon.ToString("N0");
			SoldCount = detail.SoldCount.ToString("N0");
			Category = sp.DanhMuc?.TenDanhMuc ?? "Chưa phân loại";

			var performance = detail.PerformancePercent;
			PerformanceIcon = performance > 0 ? "↑" : performance < 0 ? "↓" : "→";
			PerformanceText = $"{Math.Abs(performance):0.#}%";
			PerformanceColor = performance > 0 ? Colors.Green : performance < 0 ? Colors.Red : Colors.Gray;

			if (detail.ReviewCount > 0)
			{
				RatingText = detail.AverageRating.ToString("0.0");
				RatingStars = BuildStars(detail.AverageRating);
				ReviewCountText = $"{detail.ReviewCount:N0} đánh giá";
				ReviewEmptyText = string.Empty;
			}
			else
			{
				RatingText = "—";
				RatingStars = "☆☆☆☆☆";
				ReviewCountText = "0 đánh giá";
				ReviewEmptyText = "Sản phẩm chưa có đánh giá. Hệ thống hiện chưa lưu dữ liệu đánh giá khách hàng.";
			}
			NotifyAll();
			ProductImageGallery.SelectedItem = firstImage;
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Không tải được chi tiết", ex.Message, "OK");
		}
	}

	private void NotifyAll()
	{
		foreach (var property in new[]
		{
			nameof(CanManageProducts), nameof(ProductCode), nameof(ProductName), nameof(ProductInitials),
			nameof(ProductDescription), nameof(ProductStatus), nameof(ProductStatusColor),
			nameof(StatusActionText), nameof(Price), nameof(ImportPrice), nameof(Stock),
			nameof(SoldCount), nameof(Category), nameof(PerformanceIcon),
			nameof(PerformanceText), nameof(PerformanceColor), nameof(ProductImages),
			nameof(SelectedImageUrl), nameof(ImageCountText), nameof(RatingText), nameof(RatingStars),
			nameof(ReviewCountText), nameof(ReviewEmptyText),
			nameof(IsCustomer), nameof(CanBuy)
		})
			OnPropertyChanged(property);
	}

	private void OnImageSelected(object? sender, SelectionChangedEventArgs e)
	{
		if (e.CurrentSelection.FirstOrDefault() is not ProductImageItem image ||
			string.IsNullOrWhiteSpace(image.Url))
			return;

		SelectedImageUrl = image.Url;
		OnPropertyChanged(nameof(SelectedImageUrl));
	}

	private async void OnEditClicked(object? sender, EventArgs e)
	{
		if (!CanManageProducts)
			return;

		await Shell.Current.GoToAsync($"{nameof(ProductFormPage)}?productId={_productId}");
	}

	private async void OnImportStockClicked(object? sender, EventArgs e)
	{
		if (!CanManageProducts)
			return;

		var quantityText = await DisplayPromptAsync(
			"Nhập thêm hàng", "Nhập số lượng cần cộng vào tồn kho:",
			"Nhập", "Hủy", keyboard: Keyboard.Numeric);
		if (!int.TryParse(quantityText, out var quantity) || quantity <= 0)
			return;

		var note = await DisplayPromptAsync(
			"Ghi chú", "Nguồn nhập hoặc ghi chú:",
			"Tiếp tục", "Bỏ qua") ?? "Nhập kho nhanh từ trang sản phẩm";
		var response = await _http.PostAsJsonAsync(
			$"api/product/{_productId}/import",
			new { soLuong = quantity, ghiChu = note });
		if (!response.IsSuccessStatusCode)
		{
			await DisplayAlertAsync("Nhập kho thất bại", await ReadErrorAsync(response), "OK");
			return;
		}
		await LoadProductDetailAsync();
	}

	private async void OnToggleStatusClicked(object? sender, EventArgs e)
	{
		if (!CanManageProducts)
			return;

		if (_product == null) return;
		var newStatus = _product.TrangThai == "Ngung ban" ? "Hoat dong" : "Ngung ban";
		var response = await _http.PutAsync(
			$"api/product/{_productId}/status?status={Uri.EscapeDataString(newStatus)}", null);
		if (!response.IsSuccessStatusCode)
		{
			await DisplayAlertAsync("Không đổi được trạng thái", await ReadErrorAsync(response), "OK");
			return;
		}
		await LoadProductDetailAsync();
	}

	private async void OnDeleteClicked(object? sender, EventArgs e)
	{
		if (!CanManageProducts)
			return;

		if (!await DisplayAlertAsync("Xóa sản phẩm", $"Bạn có chắc muốn xóa {ProductName}?", "Xóa", "Hủy"))
			return;
		var response = await _http.DeleteAsync($"api/product/{_productId}");
		if (!response.IsSuccessStatusCode)
		{
			await DisplayAlertAsync("Không xóa được", await ReadErrorAsync(response), "OK");
			return;
		}
		await Shell.Current.GoToAsync("..");
	}

	private static IEnumerable<string> ParseImageUrls(string? value)
	{
		if (string.IsNullOrWhiteSpace(value)) yield break;
		foreach (var item in value.Split([',', ';', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries))
		{
			var url = item.Trim();
			yield return Uri.TryCreate(url, UriKind.Absolute, out _)
				? url
				: new Uri(ApiClientProvider.Client.BaseAddress!, url.TrimStart('/')).ToString();
		}
	}

	private static string BuildStars(decimal rating)
	{
		var full = Math.Clamp((int)Math.Round(rating), 0, 5);
		return new string('★', full) + new string('☆', 5 - full);
	}

	private static string BuildInitials(string name) =>
		string.Join("", name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
			.Take(2).Select(x => char.ToUpperInvariant(x[0])));

	private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
	{
		var text = await response.Content.ReadAsStringAsync();
		try
		{
			using var document = JsonDocument.Parse(text);
			if (document.RootElement.TryGetProperty("message", out var message))
				return message.GetString() ?? text;
		}
		catch { }
		return string.IsNullOrWhiteSpace(text) ? $"HTTP {(int)response.StatusCode}" : text.Trim('"');
	}

	// Khách thêm sản phẩm vào giỏ (POST api/cart/items).
	private async void OnAddToCartClicked(object? sender, EventArgs e)
	{
		if (!CanBuy) return;
		if (!int.TryParse(QtyEntry.Text?.Trim(), out var quantity) || quantity <= 0)
			quantity = 1;

		try
		{
			var response = await _http.PostAsJsonAsync("api/cart/items",
				new { MaSanPham = _productId, SoLuong = quantity });
			if (!response.IsSuccessStatusCode)
			{
				await DisplayAlertAsync("Không thêm được vào giỏ", await ReadErrorAsync(response), "OK");
				return;
			}

			var viewCart = await DisplayAlertAsync("Đã thêm vào giỏ",
				$"Đã thêm {quantity} sản phẩm vào giỏ hàng.", "Xem giỏ", "Tiếp tục mua");
			if (viewCart)
				await Shell.Current.GoToAsync("//cart");
		}
		catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
		{
			await this.HandleUnauthorizedAsync();
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Lỗi kết nối", ex.Message, "OK");
		}
	}

	private async void OnBackClicked(object? sender, EventArgs e) =>
		await Shell.Current.GoToAsync("..");
}
