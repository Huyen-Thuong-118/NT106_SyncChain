using System.Net.Http.Json;
using System.Collections.ObjectModel;
using System.Text.Json;
using Microsoft.Maui.Controls;
using SyncChain.Desktop.Models;

namespace SyncChain.Desktop.Views.Pages;

public partial class CreateOrderPage : ContentPage
{
	private static readonly Uri ApiBaseUri = Services.ApiClientProvider.Client.BaseAddress
		?? new Uri(Services.ApiClientProvider.ApiBaseUrl);

	private static readonly HttpClient AddressHttp = new()
	{
		BaseAddress = new Uri("https://provinces.open-api.vn/api/v2/"),
		Timeout = TimeSpan.FromSeconds(20)
	};

	private readonly HttpClient _http;
	private List<SanPhamApi> _allProducts = new();
	private bool _productsLoaded;
	private bool _addressesLoaded;
	private bool _isSubmitting;
	private bool _isLoadingWards;
	private bool _isAdjustingService;
	private int _salesChannelIndex;
	private int _estimateRequestVersion;
	private DeliveryEstimateApi? _estimate;
	private bool _isProductPickerOpen;
	private string _productSearchText = string.Empty;
	private readonly string _paymentCodeSeed = DateTime.Now.ToString("yyyyMMddHHmmss");

	public ObservableCollection<CreateOrderLine> Lines { get; } = new();
	public ObservableCollection<ProvinceApi> Provinces { get; } = new();
	public ObservableCollection<WardApi> Wards { get; } = new();
	public ObservableCollection<ProductSelectionItem> ProductChoices { get; } = new();
	public bool IsProductPickerOpen
	{
		get => _isProductPickerOpen;
		private set
		{
			if (_isProductPickerOpen == value) return;
			_isProductPickerOpen = value;
			OnPropertyChanged();
		}
	}
	public bool IsCartEmpty => Lines.Count == 0;
	public int SalesChannelIndex
	{
		get => _salesChannelIndex;
		set
		{
			if (_salesChannelIndex == value) return;
			_salesChannelIndex = value;
			OnPropertyChanged();
			OnPropertyChanged(nameof(SalesChannel));
			OnPropertyChanged(nameof(RequiresShipping));
			OnPropertyChanged(nameof(CanCreateOrder));
		}
	}
	public string SalesChannel => SalesChannelIndex == 1 ? "Facebook" : "Cửa hàng trực tiếp";
	public bool RequiresShipping => SalesChannel == "Facebook";
	public bool CanCreateOrder =>
		Lines.Count > 0 &&
		!_isSubmitting &&
		SalesChannel is "Cửa hàng trực tiếp" or "Facebook";
	public bool CanSelectWard => ProvincePicker?.SelectedItem != null && !_isLoadingWards;
	public bool CanUseExpress =>
		ProvincePicker?.SelectedItem is ProvinceApi province &&
		province.Name.Contains("Hồ Chí Minh", StringComparison.OrdinalIgnoreCase) &&
		_estimate?.AreaType == "Nội thành đô thị lớn";
	public decimal Subtotal => Lines.Sum(x => x.UnitPrice * x.Quantity);
	public decimal ShippingFee =>
		!RequiresShipping || Subtotal >= 500_000m ? 0 : _estimate?.ShippingFee ?? 0;
	public string SubtotalText => $"{Subtotal:N0} đ";
	public string ShippingFeeText => !RequiresShipping
		? "0 đ"
		: Subtotal >= 500_000m
		? "Miễn phí"
		: _estimate == null
			? "Chọn địa chỉ"
			: $"{ShippingFee:N0} đ";
	public string DeliveryEstimateText => _estimate == null
		? ""
		: $"{_estimate.DeliveryDays:N0} ngày · dự kiến {_estimate.EarliestDelivery:dd/MM}–{_estimate.LatestDelivery:dd/MM}";
	public string TotalText => $"{Subtotal + ShippingFee:N0} đ";
	public string SelectedPaymentMethod
	{
		get
		{
			var selectedIndex = Payments
				.Select((payment, index) => new { payment, index })
				.FirstOrDefault(x => x.payment.IsSelected)?.index ?? 0;
			return selectedIndex switch
			{
				1 => "bank",
				2 => "momo",
				_ => "cod"
			};
		}
	}
	public bool ShowPaymentCode => SelectedPaymentMethod is "bank" or "momo";
	public string PaymentCodeTitle => SelectedPaymentMethod switch
	{
		"bank" => "Ma chuyen khoan",
		"momo" => "Ma thanh toan MoMo",
		_ => string.Empty
	};
	public string PaymentCode => SelectedPaymentMethod switch
	{
		"bank" => $"SC-{_paymentCodeSeed}",
		"momo" => $"MOMO-{_paymentCodeSeed}",
		_ => string.Empty
	};
	public string PaymentQrImageUrl
	{
		get
		{
			if (!ShowPaymentCode)
				return string.Empty;

			var payload = $"SYNCCHAIN|METHOD={SelectedPaymentMethod}|CODE={PaymentCode}|AMOUNT={Subtotal + ShippingFee:0}";
			return $"https://api.qrserver.com/v1/create-qr-code/?size=180x180&data={Uri.EscapeDataString(payload)}";
		}
	}
	public string PaymentAmountText => TotalText;
	public IReadOnlyList<PaymentOption> Payments { get; private set; } = new List<PaymentOption>
	{
		new() { Name = "Tiền mặt khi nhận hàng", IsSelected = true },
		new() { Name = "Chuyển khoản ngân hàng", IsSelected = false },
		new() { Name = "Ví điện tử MoMo", IsSelected = false }
	};

	public CreateOrderPage() : this(Services.ApiClientProvider.Client)
	{
	}

	public CreateOrderPage(HttpClient http)
	{
		_http = http;
		InitializeComponent();
		BindingContext = this;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		var tasks = new List<Task>();
		if (!_productsLoaded) tasks.Add(LoadProductsAsync());
		if (!_addressesLoaded) tasks.Add(LoadProvincesAsync());
		await Task.WhenAll(tasks);
	}

	private async Task LoadProductsAsync()
	{
		try
		{
			var products = await _http.GetFromJsonAsync<List<SanPhamApi>>("api/product");
			_allProducts = (products ?? [])
				.Where(p => p.SoLuongTon > 0 && p.TrangThai != "Ngung ban")
				.OrderBy(p => p.TenSanPham)
				.ToList();
			_productsLoaded = true;
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Không tải được sản phẩm", ex.Message, "OK");
		}
	}

	private async Task LoadProvincesAsync()
	{
		try
		{
			var provinces = await AddressHttp.GetFromJsonAsync<List<ProvinceApi>>("p/");
			Provinces.Clear();
			foreach (var province in (provinces ?? []).OrderBy(x => x.Name))
				Provinces.Add(province);
			_addressesLoaded = true;
			OnPropertyChanged(nameof(Provinces));
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync(
				"Không tải được địa chỉ",
				$"Không thể tải danh sách tỉnh/thành phố. {ex.Message}",
				"OK");
		}
	}

	private async void OnProvinceChanged(object? sender, EventArgs e)
	{
		Wards.Clear();
		WardPicker.SelectedItem = null;
		_estimate = null;
		_isLoadingWards = ProvincePicker.SelectedItem is ProvinceApi;
		NotifyShippingChanged();

		if (ProvincePicker.SelectedItem is not ProvinceApi province)
			return;

		try
		{
			var detail = await AddressHttp.GetFromJsonAsync<ProvinceApi>($"p/{province.Code}?depth=2");
			foreach (var ward in (detail?.Wards ?? []).OrderBy(x => x.Name))
				Wards.Add(ward);
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync(
				"Không tải được phường/xã",
				$"Không thể tải danh sách phường/xã của {province.Name}. {ex.Message}",
				"OK");
		}
		finally
		{
			_isLoadingWards = false;
			NotifyShippingChanged();
		}
	}

	private async void OnWardChanged(object? sender, EventArgs e) =>
		await RefreshShippingEstimateAsync();

	private async void OnSalesChannelChanged(object? sender, EventArgs e)
	{
		var requiresShipping = RequiresShipping;

		if (!requiresShipping)
		{
			ProvincePicker.SelectedItem = null;
			WardPicker.SelectedItem = null;
			Wards.Clear();
			AddressEditor.Text = string.Empty;
			ServicePicker.SelectedIndex = 2;
			_estimate = null;
		}

		NotifyCartChanged();
		NotifyShippingChanged();
		await RefreshShippingEstimateAsync();
	}

	private async void OnShippingInputChanged(object? sender, EventArgs e)
	{
		if (_isAdjustingService)
			return;
		await RefreshShippingEstimateAsync();
	}

	private void OnPaymentMethodTapped(object? sender, TappedEventArgs e)
	{
		if ((sender as BindableObject)?.BindingContext is not PaymentOption selected)
			return;

		foreach (var payment in Payments)
			payment.IsSelected = payment == selected;

		OnPropertyChanged(nameof(SelectedPaymentMethod));
		OnPropertyChanged(nameof(ShowPaymentCode));
		OnPropertyChanged(nameof(PaymentCodeTitle));
		OnPropertyChanged(nameof(PaymentCode));
		OnPropertyChanged(nameof(PaymentQrImageUrl));
		OnPropertyChanged(nameof(PaymentAmountText));
	}

	private async Task RefreshShippingEstimateAsync()
	{
		var requestVersion = ++_estimateRequestVersion;
		if (!RequiresShipping ||
			ProvincePicker.SelectedItem is not ProvinceApi province ||
			WardPicker.SelectedItem is not WardApi ward ||
			Lines.Count == 0)
		{
			_estimate = null;
			NotifyShippingChanged();
			return;
		}

		try
		{
			var response = await _http.PostAsJsonAsync("api/shipping/estimate", new
			{
				orderDate = DateTime.UtcNow,
				province = province.Name,
				ward = ward.Name,
				serviceType = ServicePicker.SelectedItem?.ToString() ?? "Tiêu chuẩn",
				weightKg = 1,
				orderTotal = Subtotal
			});
			if (!response.IsSuccessStatusCode || requestVersion != _estimateRequestVersion)
				return;

			_estimate = await response.Content.ReadFromJsonAsync<DeliveryEstimateApi>();
			if (requestVersion != _estimateRequestVersion)
				return;

			if (ServicePicker.SelectedItem?.ToString() == "Hỏa tốc" && !CanUseExpress)
			{
				_isAdjustingService = true;
				ServicePicker.SelectedItem = "Nhanh";
				_isAdjustingService = false;
				await DisplayAlertAsync(
					"Không hỗ trợ hỏa tốc",
					"Hỏa tốc chỉ áp dụng cho khu vực nội thành TP. Hồ Chí Minh.",
					"OK");
				await RefreshShippingEstimateAsync();
				return;
			}

			NotifyShippingChanged();
		}
		catch
		{
			if (requestVersion == _estimateRequestVersion)
			{
				_estimate = null;
				NotifyShippingChanged();
			}
		}
	}

	private async void OnAddProductClicked(object? sender, EventArgs e)
	{
		if (!_productsLoaded)
			await LoadProductsAsync();

		RefreshProductChoices();
		if (ProductChoices.Count == 0)
		{
			await DisplayAlertAsync("Sản phẩm", "Không còn sản phẩm khả dụng để thêm.", "OK");
			return;
		}

		IsProductPickerOpen = true;
	}

	private void OnProductSearchTextChanged(object? sender, TextChangedEventArgs e)
	{
		_productSearchText = e.NewTextValue?.Trim() ?? string.Empty;
		RefreshProductChoices();
	}

	private void OnCloseProductPickerClicked(object? sender, EventArgs e)
	{
		IsProductPickerOpen = false;
	}

	private async void OnSelectProductClicked(object? sender, EventArgs e)
	{
		if (sender is not Button button ||
			!int.TryParse(button.CommandParameter?.ToString(), out var productId))
			return;

		var product = _allProducts.FirstOrDefault(x => x.MaSanPham == productId);
		if (product == null || Lines.Any(x => x.ProductId == productId))
			return;

		Lines.Add(new CreateOrderLine
		{
			ProductId = product.MaSanPham,
			Name = product.TenSanPham,
			Stock = product.SoLuongTon,
			UnitPrice = product.GiaBan,
			Initials = BuildInitials(product.TenSanPham)
		});
		NotifyCartChanged();
		SyncProductChoice(productId);
		await RefreshShippingEstimateAsync();
	}

	private void OnPickerIncreaseQuantityClicked(object? sender, EventArgs e)
	{
		var line = FindLine(sender);
		if (line == null) return;
		if (line.Quantity >= line.Stock)
		{
			_ = DisplayAlertAsync(
				"Không đủ tồn kho",
				$"Sản phẩm chỉ còn {line.Stock:N0}.",
				"OK");
			return;
		}

		line.Quantity++;
		NotifyCartChanged();
		SyncProductChoice(line.ProductId);
		_ = RefreshShippingEstimateAsync();
	}

	private void OnPickerDecreaseQuantityClicked(object? sender, EventArgs e)
	{
		var line = FindLine(sender);
		if (line == null) return;

		if (line.Quantity <= 1)
			Lines.Remove(line);
		else
			line.Quantity--;

		NotifyCartChanged();
		SyncProductChoice(line.ProductId);
		_ = RefreshShippingEstimateAsync();
	}

	private void RefreshProductChoices()
	{
		var query = _allProducts.AsEnumerable();

		if (!string.IsNullOrWhiteSpace(_productSearchText))
		{
			query = query.Where(product =>
				product.TenSanPham.Contains(
					_productSearchText,
					StringComparison.CurrentCultureIgnoreCase));
		}

		ProductChoices.Clear();
		foreach (var product in query.OrderBy(x => x.TenSanPham))
		{
			ProductChoices.Add(new ProductSelectionItem
			{
				ProductId = product.MaSanPham,
				Name = product.TenSanPham,
				ImageUrl = ParseImageUrls(product.HinhAnhUrl).FirstOrDefault() ?? string.Empty,
				Initials = BuildInitials(product.TenSanPham),
				UnitPrice = product.GiaBan,
				Stock = product.SoLuongTon,
				SelectedQuantity = Lines
					.FirstOrDefault(line => line.ProductId == product.MaSanPham)
					?.Quantity ?? 0
			});
		}
		OnPropertyChanged(nameof(ProductChoices));
	}

	private void SyncProductChoice(int productId)
	{
		var choice = ProductChoices.FirstOrDefault(x => x.ProductId == productId);
		if (choice == null)
			return;

		choice.SelectedQuantity = Lines
			.FirstOrDefault(line => line.ProductId == productId)
			?.Quantity ?? 0;
	}

	private void OnIncreaseQuantityClicked(object? sender, EventArgs e)
	{
		var line = FindLine(sender);
		if (line == null) return;
		if (line.Quantity >= line.Stock)
		{
			DisplayAlertAsync("Không đủ tồn kho", $"Sản phẩm chỉ còn {line.Stock:N0}.", "OK");
			return;
		}
		line.Quantity++;
		NotifyCartChanged();
		SyncProductChoice(line.ProductId);
		_ = RefreshShippingEstimateAsync();
	}

	private void OnDecreaseQuantityClicked(object? sender, EventArgs e)
	{
		var line = FindLine(sender);
		if (line == null) return;
		if (line.Quantity > 1)
		{
			line.Quantity--;
			NotifyCartChanged();
			SyncProductChoice(line.ProductId);
			_ = RefreshShippingEstimateAsync();
		}
	}

	private void OnCartQuantityEntryCompleted(object? sender, EventArgs e)
	{
		UpdateCartQuantityFromEntry(sender);
	}

	private void OnCartQuantityEntryUnfocused(object? sender, FocusEventArgs e)
	{
		UpdateCartQuantityFromEntry(sender);
	}

	private void OnPickerQuantityEntryCompleted(object? sender, EventArgs e)
	{
		UpdatePickerQuantityFromEntry(sender);
	}

	private void OnPickerQuantityEntryUnfocused(object? sender, FocusEventArgs e)
	{
		UpdatePickerQuantityFromEntry(sender);
	}

	private void UpdateCartQuantityFromEntry(object? sender)
	{
		if (sender is not Entry entry ||
			entry.BindingContext is not CreateOrderLine line)
			return;

		SetLineQuantity(line, entry.Text);
		NotifyCartChanged();
		SyncProductChoice(line.ProductId);
		_ = RefreshShippingEstimateAsync();
	}

	private void UpdatePickerQuantityFromEntry(object? sender)
{
	if (sender is not Entry entry ||
		entry.BindingContext is not ProductSelectionItem item)
		return;

	var line = Lines.FirstOrDefault(x => x.ProductId == item.ProductId);
	if (line == null)
		return;

	SetLineQuantity(line, entry.Text);
	NotifyCartChanged();
	SyncProductChoice(line.ProductId);
	_ = RefreshShippingEstimateAsync();
}

	private void SetLineQuantity(CreateOrderLine line, string? text)
	{
		if (!int.TryParse(text, out var quantity))
			quantity = line.Quantity;

		quantity = Math.Clamp(quantity, 1, line.Stock);
		line.Quantity = quantity;
	}

	private void OnRemoveProductClicked(object? sender, EventArgs e)
	{
		var line = FindLine(sender);
		if (line == null) return;
		Lines.Remove(line);
		NotifyCartChanged();
		SyncProductChoice(line.ProductId);
		_ = RefreshShippingEstimateAsync();
	}

	private async void OnCreateOrderClicked(object? sender, EventArgs e)
	{
		try
		{
			if (Lines.Count == 0)
				throw new InvalidOperationException("Vui lòng chọn ít nhất một sản phẩm.");
			if (string.IsNullOrWhiteSpace(CustomerNameEntry.Text))
				throw new InvalidOperationException("Vui lòng nhập tên khách hàng.");

			var salesChannel = SalesChannel;
			if (salesChannel == "Online")
				throw new InvalidOperationException("Đơn Online phải do khách hàng tự đặt.");
			if (salesChannel is not "Cửa hàng trực tiếp" and not "Facebook")
				throw new InvalidOperationException("Vui lòng chọn kênh bán hàng hợp lệ.");

			var requiresShipping = salesChannel == "Facebook";
			ProvinceApi? province = null;
			WardApi? ward = null;
			if (requiresShipping)
			{
				if (string.IsNullOrWhiteSpace(AddressEditor.Text))
					throw new InvalidOperationException("Vui lòng nhập địa chỉ giao hàng.");
				province = ProvincePicker.SelectedItem as ProvinceApi
					?? throw new InvalidOperationException("Vui lòng chọn tỉnh/thành phố.");
				ward = WardPicker.SelectedItem as WardApi
					?? throw new InvalidOperationException("Vui lòng chọn phường/xã.");
				if (ServicePicker.SelectedItem?.ToString() == "Hỏa tốc" && !CanUseExpress)
				{
					throw new InvalidOperationException(
						"Hỏa tốc chỉ áp dụng cho khu vực nội thành TP. Hồ Chí Minh.");
				}
			}

			if (!string.IsNullOrWhiteSpace(EmailEntry.Text) &&
				!System.Net.Mail.MailAddress.TryCreate(EmailEntry.Text.Trim(), out _))
				throw new InvalidOperationException("Địa chỉ email không hợp lệ.");

			_isSubmitting = true;
			NotifyCartChanged();
			var request = new HttpRequestMessage(HttpMethod.Post, "api/order")
			{
				Content = JsonContent.Create(new
				{
					items = Lines.Select(x => new { maSanPham = x.ProductId, soLuong = x.Quantity }).ToList(),
					idempotencyKey = Guid.NewGuid().ToString("N"),
					tenNguoiNhan = CustomerNameEntry.Text.Trim(),
					soDienThoai = PhoneEntry.Text?.Trim() ?? string.Empty,
					emailNguoiNhan = EmailEntry.Text?.Trim() ?? string.Empty,
					salesChannel,
					diaChiGiaoHang = requiresShipping ? AddressEditor.Text.Trim() : string.Empty,
					tinhThanh = requiresShipping ? province!.Name : string.Empty,
					phuongXa = requiresShipping ? ward!.Name : string.Empty,
					loaiDichVu = requiresShipping
						? ServicePicker.SelectedItem?.ToString() ?? "Tiêu chuẩn"
						: "Tiêu chuẩn",
					trongLuongKg = 1,
					shippingFee = requiresShipping ? ShippingFee : 0,
					ghiChu = NoteEditor.Text?.Trim() ?? string.Empty
				})
			};
			var response = await _http.SendAsync(request);
			if (!response.IsSuccessStatusCode)
				throw new InvalidOperationException(await ReadErrorAsync(response));

			var result = await response.Content.ReadFromJsonAsync<CreateOrderResponse>();
			await DisplayAlertAsync(
				"Thành công",
				result == null
					? "Đã tạo đơn hàng."
					: $"Đã tạo đơn #{result.MaDonHang:0000}, tổng tiền {result.TongTien:N0} đ.",
				"OK");
			await Shell.Current.GoToAsync("//orders");
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Không tạo được đơn hàng", ex.Message, "OK");
		}
		finally
		{
			_isSubmitting = false;
			NotifyCartChanged();
		}
	}

	private async void OnResetClicked(object? sender, EventArgs e)
	{
		if (Lines.Count > 0 &&
			!await DisplayAlertAsync("Làm mới", "Xóa toàn bộ thông tin đang nhập?", "Xóa", "Giữ lại"))
			return;

		Lines.Clear();
		SalesChannelIndex = 0;
		CustomerNameEntry.Text = string.Empty;
		PhoneEntry.Text = string.Empty;
		EmailEntry.Text = string.Empty;
		ProvincePicker.SelectedItem = null;
		WardPicker.SelectedItem = null;
		Wards.Clear();
		AddressEditor.Text = string.Empty;
		ServicePicker.SelectedIndex = 2;
		NoteEditor.Text = string.Empty;
		_estimate = null;
		NotifyCartChanged();
	}

	private CreateOrderLine? FindLine(object? sender)
	{
		if (sender is not Button button ||
			!int.TryParse(button.CommandParameter?.ToString(), out var productId))
			return null;
		return Lines.FirstOrDefault(x => x.ProductId == productId);
	}

	private void RefreshLines()
	{
		var snapshot = Lines.ToList();
		Lines.Clear();
		foreach (var line in snapshot)
			Lines.Add(line);
		NotifyCartChanged();
	}

	private void NotifyCartChanged()
	{
		foreach (var property in new[]
		{
			nameof(Lines), nameof(IsCartEmpty), nameof(CanCreateOrder),
			nameof(SalesChannel), nameof(RequiresShipping),
			nameof(Subtotal), nameof(SubtotalText), nameof(ShippingFee),
			nameof(ShippingFeeText), nameof(DeliveryEstimateText), nameof(TotalText),
			nameof(PaymentAmountText), nameof(PaymentQrImageUrl)
		})
			OnPropertyChanged(property);
	}

	private void NotifyShippingChanged()
	{
		foreach (var property in new[]
		{
			nameof(Wards), nameof(CanSelectWard), nameof(ShippingFee),
			nameof(CanUseExpress), nameof(ShippingFeeText),
			nameof(DeliveryEstimateText), nameof(TotalText),
			nameof(PaymentAmountText), nameof(PaymentQrImageUrl)
		})
			OnPropertyChanged(property);
	}

	private static string BuildInitials(string name)
	{
		var initials = string.Join("", name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
			.Take(2).Select(word => char.ToUpperInvariant(word[0])));
		return string.IsNullOrWhiteSpace(initials) ? "SP" : initials;
	}

	private static IEnumerable<string> ParseImageUrls(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
			yield break;

		foreach (var item in value.Split([',', ';', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries))
		{
			var url = item.Trim();
			if (Uri.TryCreate(url, UriKind.Absolute, out _))
				yield return url;
			else
				yield return new Uri(ApiBaseUri, url.TrimStart('/')).ToString();
		}
	}

	private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
	{
		var text = await response.Content.ReadAsStringAsync();
		try
		{
			using var json = JsonDocument.Parse(text);
			if (json.RootElement.TryGetProperty("message", out var message))
				return message.GetString() ?? text;
		}
		catch
		{
		}
		return text.Trim('"');
	}

	private async void OnBackClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("//orders");
	}
}
