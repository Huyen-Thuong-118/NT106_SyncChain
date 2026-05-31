using System.Net.Http.Json;
using SyncChain.Desktop.Models;

namespace SyncChain.Desktop.Views.Pages;

public partial class CustomerHomePage : ContentPage
{
	private readonly HttpClient _http;

	private static readonly Color Sapphire = Color.FromArgb("#213145");
	private static readonly Color Blue = Color.FromArgb("#50616B");
	private static readonly Color Mist = Color.FromArgb("#5C647A");
	private static readonly Color Ice = Color.FromArgb("#B7C9D5");
	private static readonly Color Critical = Color.FromArgb("#BA1A1A");

	public IReadOnlyList<CustomerMetric> Metrics { get; } =
	[
		new("Đơn đang xử lý", "03", "1 đơn cần theo dõi", "ORD", Blue),
		new("Điểm tích lũy", "1,240", "Hạng bạc", "VIP", Mist),
		new("Voucher", "05", "2 mã sắp hết hạn", "VC", Ice),
		new("Hỗ trợ", "24/7", "Phản hồi nhanh", "CS", Sapphire)
	];

	public IReadOnlyList<CustomerOrderCard> RecentOrders { get; private set; } =
	[
		new("#ORD-0001", "Đang xử lý", DateTime.Now.ToString("dd/MM/yyyy"), "0 đ", Blue)
	];

	public IReadOnlyList<CustomerAction> QuickActions { get; } =
	[
		new("Theo dõi đơn", "Xem trạng thái xử lý và vận chuyển theo thời gian thực.", "01", Blue),
		new("Mua lại", "Tạo lại đơn từ sản phẩm đã mua gần đây.", "02", Sapphire),
		new("Hỗ trợ", "Gửi yêu cầu đổi trả, bảo hành hoặc cập nhật giao hàng.", "03", Mist)
	];

	public IReadOnlyList<CustomerPromotion> Promotions { get; } =
	[
		new("FREESHIP24", "Miễn phí vận chuyển cho đơn từ 500,000 đ", Blue),
		new("TVT10", "Giảm 10% cho phụ kiện trong tuần này", Critical)
	];

	public IReadOnlyList<ProductItem> SuggestedProducts { get; private set; } = Array.Empty<ProductItem>();

	public CustomerHomePage(HttpClient http)
	{
		_http = http;
		InitializeComponent();
		BindingContext = this;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await LoadDataAsync();
	}

	private async Task LoadDataAsync()
	{
		try
		{
			// Lấy sản phẩm gợi ý
			var productsResp = await _http.GetFromJsonAsync<ApiResponse<List<SanPhamApi>>>("api/sanpham");
			if (productsResp?.success == true && productsResp.data != null)
			{
				SuggestedProducts = productsResp.data.Take(4).Select(sp => new ProductItem
				{
					Code = $"SP-{sp.MaSanPham:0000}",
					Name = sp.TenSanPham,
					Description = sp.TrangThai == "Hoat dong" ? "Đang kinh doanh" : "Ngừng kinh doanh",
					Price = $"{sp.GiaBan:N0} đ",
					Stock = sp.SoLuongTon.ToString(),
					BadgeText = sp.SoLuongTon > 0 ? "Còn hàng" : "Hết hàng",
					BadgeColor = sp.SoLuongTon > 0 ? Colors.Green : Colors.Red,
					Initials = string.Join("", sp.TenSanPham.Split(' ', StringSplitOptions.RemoveEmptyEntries)
						.Take(2).Select(w => w[0])).ToUpperInvariant(),
					HealthProgress = sp.SoLuongTon <= 0 ? 0.0 : Math.Min(1.0, (double)sp.SoLuongTon / 100)
				}).ToList();
				OnPropertyChanged(nameof(SuggestedProducts));
			}

			// Lấy đơn hàng gần đây
			var ordersResp = await _http.GetFromJsonAsync<ApiResponse<List<DonHangApi>>>("api/donhang");
			if (ordersResp?.success == true && ordersResp.data != null)
			{
				RecentOrders = ordersResp.data.Take(3).Select(o => new CustomerOrderCard(
					$"#ORD-{o.MaDonHang:0000}",
					o.TrangThaiDon switch
					{
						"Da dat hang" => "Đang chuẩn bị hàng",
						"Dang xu ly" => "Đang xử lý",
						"Hoan tat" => "Hoàn tất",
						"Huy" => "Đã hủy",
						_ => o.TrangThaiDon
					},
					o.NgayTao.ToString("dd/MM/yyyy"),
					$"{o.TongTien:N0} đ",
					o.TrangThaiDon == "Hoan tat" ? Sapphire : Blue
				)).ToList();
				OnPropertyChanged(nameof(RecentOrders));
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[CustomerHomePage] Load error: {ex.Message}");
		}
	}

	private async void OnTrackOrderClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync(nameof(OrderDetailPage));
	}

	private void OnLogoutClicked(object? sender, EventArgs e)
	{
		App.ShowLogin();
	}
}

public sealed record CustomerMetric(string Title, string Value, string Subtitle, string Icon, Color Accent);

public sealed record CustomerOrderCard(string Code, string Status, string Date, string Total, Color Accent);

public sealed record CustomerAction(string Title, string Description, string Icon, Color Accent);

public sealed record CustomerPromotion(string Code, string Description, Color Accent);
