using System.Net.Http.Json;

namespace SyncChain.Desktop.Views.Pages;

public partial class AddressPage : ContentPage
{
	private readonly HttpClient _http;

	public IReadOnlyList<AddressItem> Addresses { get; private set; } = Array.Empty<AddressItem>();

	public AddressPage(HttpClient http)
	{
		_http = http;
		InitializeComponent();
		BindingContext = this;
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
			var list = await _http.GetFromJsonAsync<List<AddressItem>>("api/address");
			Addresses = list ?? new List<AddressItem>();
			OnPropertyChanged(nameof(Addresses));
		}
		catch (Exception ex)
		{
			await DisplayAlert("Lỗi", ex.Message, "OK");
		}
	}

	private async void OnAddClicked(object? sender, EventArgs e)
	{
		FormError.IsVisible = false;
		var ten = TenNguoiNhanEntry.Text?.Trim() ?? "";
		var sdt = SdtEntry.Text?.Trim() ?? "";
		var tinh = TinhEntry.Text?.Trim() ?? "";
		var quan = QuanEntry.Text?.Trim() ?? "";
		var phuong = PhuongEntry.Text?.Trim() ?? "";
		var chiTiet = ChiTietEntry.Text?.Trim() ?? "";

		if (new[] { ten, sdt, tinh, quan, phuong, chiTiet }.Any(string.IsNullOrWhiteSpace))
		{
			FormError.Text = "Vui lòng điền đầy đủ thông tin địa chỉ";
			FormError.IsVisible = true;
			return;
		}

		AddButton.IsEnabled = false;
		try
		{
			var response = await _http.PostAsJsonAsync("api/address", new
			{
				TenNguoiNhan = ten, SoDienThoai = sdt,
				TinhThanh = tinh, QuanHuyen = quan, PhuongXa = phuong,
				DiaChiChiTiet = chiTiet, LaMacDinh = MacDinhCheck.IsChecked
			});
			if (!response.IsSuccessStatusCode)
			{
				var err = await response.Content.ReadFromJsonAsync<ErrorResponse>();
				FormError.Text = err?.message ?? "Thêm địa chỉ thất bại";
				FormError.IsVisible = true;
				return;
			}
			TenNguoiNhanEntry.Text = SdtEntry.Text = TinhEntry.Text =
				QuanEntry.Text = PhuongEntry.Text = ChiTietEntry.Text = "";
			MacDinhCheck.IsChecked = false;
			await LoadAsync();
		}
		catch (Exception ex) { FormError.Text = ex.Message; FormError.IsVisible = true; }
		finally { AddButton.IsEnabled = true; }
	}

	private async void OnSetDefaultClicked(object? sender, EventArgs e)
	{
		if (sender is Button btn && btn.CommandParameter is int id)
		{
			try
			{
				await _http.PutAsync($"api/address/{id}/default", null);
				await LoadAsync();
			}
			catch (Exception ex) { await DisplayAlert("Lỗi", ex.Message, "OK"); }
		}
	}

	private async void OnDeleteClicked(object? sender, EventArgs e)
	{
		if (sender is Button btn && btn.CommandParameter is int id)
		{
			var confirm = await DisplayAlert("Xác nhận", "Xóa địa chỉ này?", "Xóa", "Hủy");
			if (!confirm) return;
			try
			{
				await _http.DeleteAsync($"api/address/{id}");
				await LoadAsync();
			}
			catch (Exception ex) { await DisplayAlert("Lỗi", ex.Message, "OK"); }
		}
	}

	public sealed record AddressItem(int MaDiaChi, string TenNguoiNhan, string SoDienThoai,
		string TinhThanh, string QuanHuyen, string PhuongXa, string DiaChiChiTiet,
		bool LaMacDinh, string DiaChi);

	private sealed record ErrorResponse(string message);
}
