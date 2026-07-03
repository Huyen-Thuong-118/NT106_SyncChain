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

	// Thông tin người dùng
	public string UserName { get; private set; } = "Khách hàng";
	public string UserEmail { get; private set; } = "";
	public string UserPhone { get; private set; } = "Chưa cập nhật";

	public IReadOnlyList<CustomerMetric> Metrics { get; private set; } =
	[
		new("Đơn đang xử lý", "0", "Đang tải...", "ORD", Blue),
		new("Tổng đơn", "0", "Tất cả đơn của bạn", "ALL", Mist),
		new("Đã hoàn tất", "0", "Giao thành công", "OK", Ice),
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

	// Chưa có API khuyến mãi — không hiển thị mã ưu đãi giả.
	public IReadOnlyList<CustomerPromotion> Promotions { get; } = [];

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
			// Lấy thông tin profile
			try
			{
				var profile = await _http.GetFromJsonAsync<ProfileApi>("api/auth/profile");
				if (profile != null)
				{
					UserName = string.IsNullOrWhiteSpace(profile.HoTen) ? profile.TenDangNhap : profile.HoTen;
						UserPhone = string.IsNullOrWhiteSpace(profile.SoDienThoai) ? "Chưa cập nhật" : profile.SoDienThoai;
					UserEmail = profile.Email ?? "";
					OnPropertyChanged(nameof(UserName));
					OnPropertyChanged(nameof(UserEmail));
					OnPropertyChanged(nameof(UserPhone));
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

			// Lấy đơn hàng gần đây
			var orders = await _http.GetFromJsonAsync<List<DonHangApi>>("api/order");
			if (orders != null)
			{
				RecentOrders = orders.Take(3).Select(o => new CustomerOrderCard(
					$"#ORD-{o.MaDonHang:0000}",
					o.TrangThai switch
					{
						"Draft"      => "Chờ duyệt",
						"Approved"   => "Đã duyệt",
						"Processing" => "Đang xử lý",
						"Done"       => "Hoàn tất",
						"Cancelled"  => "Đã hủy",
						_ => o.TrangThai
					},
					o.NgayTao.ToString("dd/MM/yyyy"),
					$"{o.TongTien:N0} đ",
					o.TrangThai == "Done" ? Sapphire : Blue
				)).ToList();

				// Cập nhật metrics
				Metrics =
				[
					new("Đơn đang xử lý", orders.Count(o => o.TrangThai is "Draft" or "Approved" or "Processing").ToString(), "Đơn cần theo dõi", "ORD", Blue),
					new("Tổng đơn", orders.Count.ToString(), "Tất cả đơn của bạn", "ALL", Mist),
					new("Đã hoàn tất", orders.Count(o => o.TrangThai == "Done").ToString(), "Giao thành công", "OK", Ice),
					new("Hỗ trợ", "24/7", "Phản hồi nhanh", "CS", Sapphire)
				];

				OnPropertyChanged(nameof(RecentOrders));
				OnPropertyChanged(nameof(Metrics));
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[CustomerHomePage] Load error: {ex.Message}");
		}
	}

	private async void OnTrackOrderClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("//orders");
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
