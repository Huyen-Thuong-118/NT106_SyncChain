using System.Net;
using System.Net.Http.Json;
using SyncChain.Desktop.Models;
using SyncChain.Desktop.Services;

namespace SyncChain.Desktop.Views.Pages;

public partial class CustomerHomePage : ContentPage
{
	private readonly HttpClient _http;

	private static readonly Color Sapphire = Color.FromArgb("#213145");
	private static readonly Color Blue = Color.FromArgb("#50616B");
	private static readonly Color Mist = Color.FromArgb("#5C647A");
	private static readonly Color Ice = Color.FromArgb("#B7C9D5");
	private static readonly Color Critical = Color.FromArgb("#BA1A1A");

	// Thông tin người dùng
	public string UserName { get; private set; } = "Khách hàng";
	public string UserEmail { get; private set; } = "";

	public IReadOnlyList<CustomerMetric> Metrics { get; private set; } =
	[
		new("Đơn đang xử lý", "0", "Đang tải...", "ORD", Blue),
		new("Đã hoàn thành", "0", "Đang tải...", "OK", Mist),
		new("Tổng đơn", "0", "Đang tải...", "ALL", Ice),
		new("Hỗ trợ", "24/7", "Phản hồi nhanh", "CS", Sapphire)
	];

	public IReadOnlyList<CustomerOrderCard> RecentOrders { get; private set; } =
	[
		new("#ORD-0001", "Chờ duyệt", DateTime.Now.ToString("dd/MM/yyyy"), "0 đ", Blue)
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

	public CustomerHomePage() : this(Services.ApiClientProvider.Client)
	{
	}

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
			// Lấy thông tin profile
			try
			{
				var profile = await _http.GetFromJsonAsync<ProfileApi>("api/auth/profile");
				if (profile != null)
				{
					UserName = profile.TenDangNhap ?? "Khách hàng";
					UserEmail = profile.Email ?? "";
					OnPropertyChanged(nameof(UserName));
					OnPropertyChanged(nameof(UserEmail));
				}
			}
			catch
			{
				// Không có quyền lấy profile
			}

			// Lấy sản phẩm gợi ý
			var products = await _http.GetFromJsonAsync<List<SanPhamApi>>("api/product");
			if (products != null)
			{
				SuggestedProducts = products.Take(4).Select(sp => new ProductItem
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

			// Lấy đơn hàng gần đây (dùng nguồn hiển thị trạng thái dùng chung —
			// khớp bộ trạng thái lowercase của backend).
			var orders = await _http.GetFromJsonAsync<List<DonHangApi>>("api/order");
			if (orders != null)
			{
				RecentOrders = orders
					.OrderByDescending(o => o.NgayTao)
					.Take(3)
					.Select(o => new CustomerOrderCard(
						$"#ORD-{o.MaDonHang:0000}",
						OrderStatusDisplay.Label(o.TrangThai),
						o.NgayTao.ToLocalTime().ToString("dd/MM/yyyy"),
						$"{o.TongTien:N0} đ",
						o.TrangThai == "done" ? Sapphire : Blue))
					.ToList();

				// Metrics trung thực từ dữ liệu đơn thật (bỏ số liệu bịa trước đây).
				var dangXuLy = orders.Count(o => OrderStatusDisplay.IsActive(o.TrangThai));
				var hoanThanh = orders.Count(o => o.TrangThai == "done");
				Metrics =
				[
					new("Đơn đang xử lý", dangXuLy.ToString("N0"), "Đơn cần theo dõi", "ORD", Blue),
					new("Đã hoàn thành", hoanThanh.ToString("N0"), "Đơn đã nhận", "OK", Mist),
					new("Tổng đơn", orders.Count.ToString("N0"), "Tất cả đơn của bạn", "ALL", Ice),
					new("Hỗ trợ", "24/7", "Phản hồi nhanh", "CS", Sapphire)
				];

				OnPropertyChanged(nameof(RecentOrders));
				OnPropertyChanged(nameof(Metrics));
			}
		}
		catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
		{
			await this.HandleUnauthorizedAsync();
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[CustomerHomePage] Load error: {ex.Message}");
		}
	}

	private async void OnTrackOrderClicked(object? sender, EventArgs e)
	{
		// Mở tab "Đơn hàng của tôi" để khách chọn đơn cần theo dõi.
		await Shell.Current.GoToAsync("//orders");
	}

	private void OnLogoutClicked(object? sender, EventArgs e)
	{
		Services.ApiClientProvider.ClearSession();
		App.ShowLogin();
	}
}

public sealed record CustomerMetric(string Title, string Value, string Subtitle, string Icon, Color Accent);

public sealed record CustomerOrderCard(string Code, string Status, string Date, string Total, Color Accent);

public sealed record CustomerAction(string Title, string Description, string Icon, Color Accent);

public sealed record CustomerPromotion(string Code, string Description, Color Accent);
