using System.Net;
using System.Net.Http.Json;
using SyncChain.Desktop.Services;

namespace SyncChain.Desktop.Views.Pages;

public partial class CartPage : ContentPage
{
	private readonly HttpClient _http;

	public IReadOnlyList<CartItemView> Items { get; private set; } = Array.Empty<CartItemView>();
	public decimal TongTien { get; private set; }
	public int SoLuong { get; private set; }

	public CartPage() : this(Services.ApiClientProvider.Client) { }

	public CartPage(HttpClient http)
	{
		_http = http;
		InitializeComponent();
		BindingContext = this;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await LoadCartAsync();
	}

	private async void OnBackClicked(object? sender, EventArgs e) =>
		await Shell.Current.GoToAsync("//products");

	private async Task LoadCartAsync()
	{
		try
		{
			var cart = await _http.GetFromJsonAsync<CartResponse>("api/cart");
			if (cart == null) return;
			Items = cart.Items.ToList();
			TongTien = cart.TongTien;
			SoLuong = cart.SoLuong;
			OnPropertyChanged(nameof(Items));
			OnPropertyChanged(nameof(TongTien));
			OnPropertyChanged(nameof(SoLuong));
		}
		catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
		{
			await this.HandleUnauthorizedAsync();
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"[CartPage] {ex.Message}");
		}
	}

	private async void OnDecreaseClicked(object? sender, EventArgs e)
	{
		if (sender is Button btn && btn.CommandParameter is int maSp)
		{
			var item = Items.FirstOrDefault(x => x.MaSanPham == maSp);
			if (item == null) return;
			if (item.SoLuong <= 1)
			{
				await _http.DeleteAsync($"api/cart/items/{maSp}");
			}
			else
			{
				await _http.PutAsync($"api/cart/items/{maSp}?soLuong={item.SoLuong - 1}", null);
			}
			await LoadCartAsync();
		}
	}

	private async void OnRemoveClicked(object? sender, EventArgs e)
	{
		if (sender is Button btn && btn.CommandParameter is int maSp)
		{
			await _http.DeleteAsync($"api/cart/items/{maSp}");
			await LoadCartAsync();
		}
	}

	private async void OnCheckoutClicked(object? sender, EventArgs e)
	{
		if (!Items.Any())
		{
			await DisplayAlert("Giỏ hàng trống", "Thêm sản phẩm trước khi đặt hàng.", "OK");
			return;
		}

		// Bắt buộc chọn địa chỉ giao hàng trước khi đặt (server nạp thông tin
		// người nhận từ mã địa chỉ này).
		int? selectedAddressId = await PickAddressAsync();
		if (selectedAddressId == null || selectedAddressId <= 0)
			return; // user hủy hoặc chưa có địa chỉ

		CheckoutButton.IsEnabled = false;
		try
		{
			var response = await _http.PostAsJsonAsync("api/order", new
			{
				Items = Items.Select(x => new { x.MaSanPham, x.SoLuong }).ToList(),
				MaDiaChi = selectedAddressId
			});

			if (!response.IsSuccessStatusCode)
			{
				var err = await response.Content.ReadFromJsonAsync<ErrorResponse>();
				await DisplayAlert("Lỗi", err?.message ?? "Đặt hàng thất bại", "OK");
				return;
			}

			// Đơn đã tạo (trạng thái pending) → sang bước thanh toán để chốt phương thức
			// (COD/VNPay/MoMo). Không xoá giỏ ở đây: server đã gỡ các sản phẩm vừa đặt,
			// giỏ sẽ tự làm mới khi quay lại tab.
			var result = await response.Content.ReadFromJsonAsync<OrderCreationResult>();
			if (result == null || result.MaDonHang <= 0)
			{
				await DisplayAlert("Đặt hàng thành công", "Đơn đã được tạo.", "OK");
				await LoadCartAsync();
				return;
			}

			await Shell.Current.GoToAsync(
				$"{nameof(PaymentPage)}?orderId={result.MaDonHang}&amount={result.TongTien}&orderCode=ORD-{result.MaDonHang:0000}");
		}
		catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
		{
			await this.HandleUnauthorizedAsync();
		}
		catch (Exception ex)
		{
			await DisplayAlert("Lỗi kết nối", ex.Message, "OK");
		}
		finally
		{
			CheckoutButton.IsEnabled = true;
		}
	}

	// Trả về MaDiaChi đã chọn, hoặc null nếu user hủy / chưa có địa chỉ.
	private async Task<int?> PickAddressAsync()
	{
		List<AddressItem>? addresses = null;
		try
		{
			addresses = await _http.GetFromJsonAsync<List<AddressItem>>("api/address");
		}
		catch { }

		if (addresses == null || addresses.Count == 0)
		{
			var goToAddress = await DisplayAlert(
				"Chưa có địa chỉ giao hàng",
				"Bạn cần thêm địa chỉ giao hàng trước khi đặt đơn.",
				"Thêm địa chỉ", "Để sau");
			if (goToAddress)
				await Shell.Current.GoToAsync("//address");
			return null;
		}

		// Đặt địa chỉ mặc định ở đầu danh sách gợi ý
		var options = addresses
			.OrderByDescending(a => a.LaMacDinh)
			.Select(a => a.DiaChi)
			.ToArray();

		var choice = await DisplayActionSheet("Chọn địa chỉ giao hàng", "Hủy", null, options);
		if (choice == null || choice == "Hủy") return null;

		var selected = addresses.FirstOrDefault(a => a.DiaChi == choice);
		return selected?.MaDiaChi;
	}

	public sealed record CartItemView(int MaChiTietGio, int MaSanPham, string TenSanPham,
		decimal GiaBan, string HinhAnhUrl, int SoLuongTon, string TrangThai,
		int SoLuong, decimal ThanhTien);

	private sealed record CartResponse(int MaGioHang, List<CartItemView> Items,
		decimal TongTien, int SoLuong);

	private sealed record AddressItem(int MaDiaChi, string TenNguoiNhan, string SoDienThoai,
		string TinhThanh, string QuanHuyen, string PhuongXa, string DiaChiChiTiet,
		bool LaMacDinh, string DiaChi);

	private sealed record ErrorResponse(string message);

	private sealed record OrderCreationResult(int MaDonHang, decimal TongTien);
}
