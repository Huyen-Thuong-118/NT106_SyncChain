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

	public string UserName { get; private set; } = "Khách hàng";
	public string UserEmail { get; private set; } = string.Empty;

	public IReadOnlyList<CustomerMetric> Metrics { get; private set; } =
	[
		new("Đơn đang xử lý", "0", "Đang tải dữ liệu", "ORD", Blue),
		new("Đã hoàn thành", "0", "Đang tải dữ liệu", "OK", Mist),
		new("Tổng đơn", "0", "Đang tải dữ liệu", "ALL", Ice),
		new("Sản phẩm đang bán", "0", "Đang tải dữ liệu", "SP", Sapphire)
	];

	public IReadOnlyList<CustomerOrderCard> RecentOrders { get; private set; } = Array.Empty<CustomerOrderCard>();
	public IReadOnlyList<ProductItem> SuggestedProducts { get; private set; } = Array.Empty<ProductItem>();

	public CustomerHomePage() : this(ApiClientProvider.Client)
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
			var profileTask = LoadProfileAsync();
			var productsTask = LoadProductsAsync();
			var ordersTask = LoadOrdersAsync();

			await Task.WhenAll(profileTask, productsTask, ordersTask);
			UpdateMetrics(ordersTask.Result, productsTask.Result);
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

	private async Task LoadProfileAsync()
	{
		try
		{
			var profile = await _http.GetFromJsonAsync<ProfileApi>("api/auth/profile");
			if (profile == null)
				return;

			UserName = string.IsNullOrWhiteSpace(profile.TenDangNhap) ? "Khách hàng" : profile.TenDangNhap;
			UserEmail = profile.Email ?? string.Empty;
			OnPropertyChanged(nameof(UserName));
			OnPropertyChanged(nameof(UserEmail));
		}
		catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound)
		{
			System.Diagnostics.Debug.WriteLine($"[CustomerHomePage] Profile skipped: {ex.Message}");
		}
	}

	private async Task<List<SanPhamApi>> LoadProductsAsync()
	{
		var products = await _http.GetFromJsonAsync<List<SanPhamApi>>("api/product") ?? [];
		var activeProducts = products
			.Where(x => x.TrangThai == "Hoat dong" && x.SoLuongTon > 0)
			.Take(8)
			.Select(sp => new ProductItem
			{
				Code = $"SP-{sp.MaSanPham:0000}",
				Name = sp.TenSanPham,
				Description = sp.TrangThai == "Hoat dong" ? "Đang bán" : "Ngừng bán",
				Price = $"{sp.GiaBan:N0} đ",
				Stock = sp.SoLuongTon.ToString("N0"),
				BadgeText = sp.SoLuongTon > 0 ? "Còn hàng" : "Hết hàng",
				BadgeColor = sp.SoLuongTon > 0 ? Colors.Green : Colors.Red,
				Initials = BuildInitials(sp.TenSanPham),
				HealthProgress = sp.SoLuongTon <= 0 ? 0.0 : Math.Min(1.0, (double)sp.SoLuongTon / 100)
			})
			.ToList();

		SuggestedProducts = activeProducts;
		OnPropertyChanged(nameof(SuggestedProducts));
		return products;
	}

	private async Task<List<DonHangApi>> LoadOrdersAsync()
	{
		var orders = await _http.GetFromJsonAsync<List<DonHangApi>>("api/order") ?? [];
		RecentOrders = orders
			.OrderByDescending(o => o.NgayTao)
			.Take(4)
			.Select(o => new CustomerOrderCard(
				$"#ORD-{o.MaDonHang:0000}",
				OrderStatusDisplay.Label(o.TrangThai),
				o.NgayTao.ToLocalTime().ToString("dd/MM/yyyy"),
				$"{o.TongTien:N0} đ",
				o.TrangThai == "done" ? Sapphire : Blue))
			.ToList();

		OnPropertyChanged(nameof(RecentOrders));
		return orders;
	}

	private void UpdateMetrics(IReadOnlyList<DonHangApi> orders, IReadOnlyList<SanPhamApi> products)
	{
		var processing = orders.Count(o => OrderStatusDisplay.IsActive(o.TrangThai));
		var completed = orders.Count(o => o.TrangThai == "done");
		var activeProducts = products.Count(x => x.TrangThai == "Hoat dong" && x.SoLuongTon > 0);

		Metrics =
		[
			new("Đơn đang xử lý", processing.ToString("N0"), "Đơn cần theo dõi", "ORD", Blue),
			new("Đã hoàn thành", completed.ToString("N0"), "Đơn đã nhận", "OK", Mist),
			new("Tổng đơn", orders.Count.ToString("N0"), "Tất cả đơn của bạn", "ALL", Ice),
			new("Sản phẩm đang bán", activeProducts.ToString("N0"), "Có thể mua ngay", "SP", Sapphire)
		];
		OnPropertyChanged(nameof(Metrics));
	}

	private static string BuildInitials(string name) =>
		string.Join("", name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
			.Take(2)
			.Select(w => char.ToUpperInvariant(w[0])));

	private async void OnTrackOrderClicked(object? sender, EventArgs e) =>
		await Shell.Current.GoToAsync("//orders");

	private async void OnShopClicked(object? sender, EventArgs e) =>
		await Shell.Current.GoToAsync("//products");

	private async void OnCartClicked(object? sender, EventArgs e) =>
		await Shell.Current.GoToAsync("//cart");

	private void OnLogoutClicked(object? sender, EventArgs e)
	{
		ApiClientProvider.ClearSession();
		App.ShowLogin();
	}
}

public sealed record CustomerMetric(string Title, string Value, string Subtitle, string Icon, Color Accent);

public sealed record CustomerOrderCard(string Code, string Status, string Date, string Total, Color Accent);

public sealed record CustomerAction(string Title, string Description, string Icon, Color Accent);

public sealed record CustomerPromotion(string Code, string Description, Color Accent);
