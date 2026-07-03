using System.Net.Http.Json;
using Microsoft.Maui.Controls;
using SyncChain.Desktop.Models;

namespace SyncChain.Desktop.Views.Pages;

[QueryProperty(nameof(ProductId), "productId")]
public partial class ProductDetailPage : ContentPage
{
	private readonly HttpClient _http;
	private int _productId;

	public int ProductId
	{
		get => _productId;
		set => _productId = value;
	}

	// Thông tin sản phẩm
	public string ProductCode { get; private set; } = "";
	public string ProductName { get; private set; } = "";
	public string ProductDescription { get; private set; } = "";
	public string ProductStatus { get; private set; } = "";
	public string ProductStatusColor { get; private set; } = "#5C647A";

	// Giá và tồn kho
	public string Price { get; private set; } = "0 đ";
	public string ImportPrice { get; private set; } = "0 đ";
	public string Stock { get; private set; } = "0";
	public string SoldCount { get; private set; } = "0";
	public string Revenue { get; private set; } = "0 đ";

	// Thông số kỹ thuật
	public string Category { get; private set; } = "";
	public string Unit { get; private set; } = "Cái";
	public string AlertThreshold { get; private set; } = "10";
	public string StatusText { get; private set; } = "";
	public string CreatedDate { get; private set; } = "";
	public string UpdatedDate { get; private set; } = "";

	// Lịch sử kho
	public IReadOnlyList<InventoryEvent> Events { get; private set; } = Array.Empty<InventoryEvent>();

	public ProductDetailPage(HttpClient http)
	{
		_http = http;
		InitializeComponent();
		BindingContext = this;
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
			// Xác định sản phẩm cần xem: ưu tiên productId từ navigation, nếu không có thì lấy sản phẩm đầu tiên
			var targetId = _productId;
			if (targetId <= 0)
			{
				var products = await _http.GetFromJsonAsync<List<SanPhamApi>>("api/product");
				if (products == null || products.Count == 0) return;
				targetId = products.First().MaSanPham;
			}

			// Gọi API lấy chi tiết sản phẩm
			var detail = await _http.GetFromJsonAsync<ProductDetailApi>($"api/product/{targetId}/detail");
			if (detail?.Product == null) return;

			var sp = detail.Product;

			// Cập nhật thông tin sản phẩm
			ProductCode = $"SP-{sp.MaSanPham:0000}";
			ProductName = sp.TenSanPham;
			ProductDescription = sp.MoTa ?? "Không có mô tả";

			// Trạng thái
			var (status, statusColor) = sp.TrangThai switch
			{
				"Hoat dong" when sp.SoLuongTon > sp.MucTonThap => ("ĐANG BÁN", "#2E7D32"),
				"Hoat dong" when sp.SoLuongTon > 0 => ("SẮP HẾT HÀNG", "#F57C00"),
				"Hoat dong" => ("HẾT HÀNG", "#C62828"),
				"Ngung ban" => ("NGỪNG BÁN", "#C62828"),
				_ => (sp.TrangThai, "#5C647A")
			};
			ProductStatus = status;
			ProductStatusColor = statusColor;

			// Giá và tồn kho
			Price = $"{sp.GiaBan:N0} đ";
			ImportPrice = $"{sp.GiaNhap:N0} đ";
			Stock = sp.SoLuongTon.ToString();
			SoldCount = detail.SoldCount.ToString();
			Revenue = $"{detail.Revenue:N0} đ";

			// Thông số kỹ thuật
			Category = sp.MoTa?.Contains("điện tử") == true ? "Điện tử" :
					   sp.MoTa?.Contains("thời trang") == true ? "Thời trang" : "Khác";
			StatusText = sp.TrangThai == "Hoat dong" ? "Đang bán" : "Ngừng bán";
			AlertThreshold = sp.MucTonThap.ToString();
			CreatedDate = DateTime.Now.AddDays(-30).ToString("dd/MM/yyyy");
			UpdatedDate = DateTime.Now.ToString("dd/MM/yyyy");

			// Lịch sử kho từ API
			Events = detail.StockHistory.Select(h => new InventoryEvent
			{
				Date = h.ThoiGian.ToString("dd/MM/yyyy HH:mm"),
				Type = h.Loai,
				Quantity = h.SoLuong > 0 ? $"+{h.SoLuong}" : h.SoLuong.ToString(),
				Actor = h.MaNguoiDung.HasValue ? $"User #{h.MaNguoiDung}" : "Hệ thống",
				Note = h.GhiChu ?? "",
				Accent = h.Loai switch
				{
					"Nhập kho" => Color.FromArgb("#2E7D32"),
					"Xuất kho" => Color.FromArgb("#C62828"),
					_ => Color.FromArgb("#5C647A")
				}
			}).ToList();

			// Thông báo UI cập nhật
			OnPropertyChanged(nameof(ProductCode));
			OnPropertyChanged(nameof(ProductName));
			OnPropertyChanged(nameof(ProductDescription));
			OnPropertyChanged(nameof(ProductStatus));
			OnPropertyChanged(nameof(ProductStatusColor));
			OnPropertyChanged(nameof(Price));
			OnPropertyChanged(nameof(ImportPrice));
			OnPropertyChanged(nameof(Stock));
			OnPropertyChanged(nameof(SoldCount));
			OnPropertyChanged(nameof(Revenue));
			OnPropertyChanged(nameof(Category));
			OnPropertyChanged(nameof(StatusText));
			OnPropertyChanged(nameof(AlertThreshold));
			OnPropertyChanged(nameof(CreatedDate));
			OnPropertyChanged(nameof(UpdatedDate));
			OnPropertyChanged(nameof(Events));
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[ProductDetailPage] Load error: {ex.Message}");
		}
	}

	private async void OnBackClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("..");
	}
}
