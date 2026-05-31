using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using SyncChain.Desktop.Models;

namespace SyncChain.Desktop.Services;

public sealed class SyncChainApiClient
{
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
	private static readonly Uri ApiBaseAddress = new("http://localhost:5292/");
	private readonly HttpClient _httpClient;

	public static SyncChainApiClient Instance { get; } = new();

	public string? Token { get; private set; }
	public ApiUser? CurrentUser { get; private set; }

	private SyncChainApiClient()
	{
		_httpClient = new HttpClient
		{
			BaseAddress = ApiBaseAddress
		};
	}

	// Gửi thông tin đăng nhập và lưu token cho các request sau.
	public async Task<ApiUser> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.PostAsJsonAsync("api/Auth/login", new
		{
			Email = email,
			Password = password
		}, JsonOptions, cancellationToken);

		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
		}

		var result = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions, cancellationToken)
			?? throw new InvalidOperationException("Backend không trả về dữ liệu đăng nhập.");

		Token = result.Token;
		CurrentUser = result.User;
		_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);

		return CurrentUser;
	}

	// Xóa phiên đăng nhập hiện tại khỏi client.
	public void Logout()
	{
		Token = null;
		CurrentUser = null;
		_httpClient.DefaultRequestHeaders.Authorization = null;
	}

	// Lấy hồ sơ người dùng đang đăng nhập.
	public async Task<ApiUser> GetProfileAsync(CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.GetAsync("api/Auth/profile", cancellationToken);
		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
		}

		CurrentUser = await response.Content.ReadFromJsonAsync<ApiUser>(JsonOptions, cancellationToken)
			?? throw new InvalidOperationException("Backend không trả về thông tin tài khoản.");

		return CurrentUser;
	}

	// Cập nhật tên hiển thị của tài khoản.
	public async Task<ApiUser> UpdateProfileAsync(string username, CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.PutAsJsonAsync("api/Auth/profile", new
		{
			Username = username
		}, JsonOptions, cancellationToken);

		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
		}

		CurrentUser = await response.Content.ReadFromJsonAsync<ApiUser>(JsonOptions, cancellationToken)
			?? throw new InvalidOperationException("Backend không trả về thông tin tài khoản.");

		return CurrentUser;
	}

	// Đổi mật khẩu tài khoản hiện tại.
	public async Task ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.PutAsJsonAsync("api/Auth/change-password", new
		{
			CurrentPassword = currentPassword,
			NewPassword = newPassword
		}, JsonOptions, cancellationToken);

		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
		}
	}

	// Đăng ký tài khoản khách hàng mới.
	public async Task RegisterAsync(string email, string password, CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.PostAsJsonAsync("api/Auth/register", new
		{
			Email = email,
			Password = password
		}, JsonOptions, cancellationToken);

		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
		}
	}

	// Lấy danh sách sản phẩm và map sang model hiển thị.
	public async Task<IReadOnlyList<ProductItem>> GetProductsAsync(CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.GetAsync("api/Product", cancellationToken);
		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
		}

		var products = await response.Content.ReadFromJsonAsync<List<ApiProduct>>(JsonOptions, cancellationToken)
			?? new List<ApiProduct>();

		return products.Select(MapProduct).ToList();
	}

	// Lấy một sản phẩm theo mã.
	public async Task<ProductItem> GetProductAsync(int productId, CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.GetAsync($"api/Product/{productId}", cancellationToken);
		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
		}

		var product = await response.Content.ReadFromJsonAsync<ApiProduct>(JsonOptions, cancellationToken)
			?? throw new InvalidOperationException("Backend không trả về sản phẩm.");

		return MapProduct(product);
	}

	// Lấy thông tin chi tiết sản phẩm kèm lịch sử kho.
	public async Task<ProductDetailData> GetProductDetailAsync(int productId, CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.GetAsync($"api/Product/{productId}/detail", cancellationToken);
		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
		}

		var detail = await response.Content.ReadFromJsonAsync<ApiProductDetail>(JsonOptions, cancellationToken)
			?? throw new InvalidOperationException("Backend không trả về chi tiết sản phẩm.");

		return new ProductDetailData
		{
			Product = MapProduct(detail.Product),
			SoldCount = detail.SoldCount,
			Revenue = detail.Revenue,
			StockHistory = detail.StockHistory
				.OrderByDescending(x => x.ThoiGian)
				.Select(MapStockHistory)
				.ToList()
		};
	}

	// Tạo sản phẩm mới trên backend.
	public async Task<ProductItem> CreateProductAsync(string name, decimal price, decimal importPrice, int stockQuantity, string imageUrl, string description, CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.PostAsJsonAsync("api/Product", new
		{
			TenSanPham = name,
			GiaBan = price,
			GiaNhap = importPrice,
			SoLuongTon = stockQuantity,
			HinhAnhUrl = ToStoredImageUrl(imageUrl),
			MoTa = description
		}, JsonOptions, cancellationToken);

		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
		}

		var product = await response.Content.ReadFromJsonAsync<ApiProduct>(JsonOptions, cancellationToken)
			?? throw new InvalidOperationException("Backend không trả về sản phẩm vừa tạo.");

		return MapProduct(product);
	}

	// Tải ảnh sản phẩm lên backend và nhận đường dẫn lưu trữ.
	public async Task<string> UploadProductImageAsync(string filePath, CancellationToken cancellationToken = default)
	{
		if (!File.Exists(filePath))
			return filePath;

		await using var stream = File.OpenRead(filePath);
		using var content = new MultipartFormDataContent();
		using var fileContent = new StreamContent(stream);
		fileContent.Headers.ContentType = new MediaTypeHeaderValue(GetImageContentType(filePath));
		content.Add(fileContent, "file", Path.GetFileName(filePath));

		var response = await _httpClient.PostAsync("api/Product/upload-image", content, cancellationToken);
		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
		}

		var result = await response.Content.ReadFromJsonAsync<UploadImageResponse>(JsonOptions, cancellationToken)
			?? throw new InvalidOperationException("Backend không trả về đường dẫn ảnh.");

		return result.ImageUrl;
	}

	// Cập nhật thông tin sản phẩm.
	public async Task<ProductItem> UpdateProductAsync(int productId, string name, decimal price, decimal importPrice, int stockQuantity, string imageUrl, string description, CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.PutAsJsonAsync($"api/Product/{productId}", new
		{
			TenSanPham = name,
			GiaBan = price,
			GiaNhap = importPrice,
			SoLuongTon = stockQuantity,
			HinhAnhUrl = ToStoredImageUrl(imageUrl),
			MoTa = description
		}, JsonOptions, cancellationToken);

		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
		}

		var product = await response.Content.ReadFromJsonAsync<ApiProduct>(JsonOptions, cancellationToken)
			?? throw new InvalidOperationException("Backend không trả về sản phẩm vừa cập nhật.");

		return MapProduct(product);
	}

	// Xóa sản phẩm theo mã.
	public async Task DeleteProductAsync(int productId, CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.DeleteAsync($"api/Product/{productId}", cancellationToken);
		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
		}
	}

	// Nhập thêm tồn kho cho sản phẩm.
	public async Task<ProductItem> ImportProductStockAsync(int productId, int quantity, string note, CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.PostAsJsonAsync($"api/Product/{productId}/import", new
		{
			SoLuong = quantity,
			GhiChu = note
		}, JsonOptions, cancellationToken);

		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
		}

		var product = await response.Content.ReadFromJsonAsync<ApiProduct>(JsonOptions, cancellationToken)
			?? throw new InvalidOperationException("Backend không trả về sản phẩm.");

		return MapProduct(product);
	}

	// Cập nhật trạng thái bán/ngừng bán của sản phẩm.
	// Lấy lịch sử nhập kho gần đây để hiển thị ở trang nhập hàng.
	public async Task<IReadOnlyList<ImportItem>> GetImportHistoryAsync(CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.GetAsync("api/Product/imports", cancellationToken);
		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
		}

		var imports = await response.Content.ReadFromJsonAsync<List<ApiImportHistory>>(JsonOptions, cancellationToken)
			?? new List<ApiImportHistory>();

		return imports.Select(MapImportHistory).ToList();
	}

	public async Task<ProductItem> UpdateProductStatusAsync(int productId, string status, CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.PutAsync($"api/Product/{productId}/status?status={Uri.EscapeDataString(status)}", null, cancellationToken);
		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
		}

		var product = await response.Content.ReadFromJsonAsync<ApiProduct>(JsonOptions, cancellationToken)
			?? throw new InvalidOperationException("Backend không trả về sản phẩm.");

		return MapProduct(product);
	}

	// Lấy danh sách đơn hàng theo quyền người dùng.
	public async Task<IReadOnlyList<OrderItem>> GetOrdersAsync(CancellationToken cancellationToken = default)
	{
		var endpoint = IsInternalUser ? "api/Order/full" : "api/Order";
		var response = await _httpClient.GetAsync(endpoint, cancellationToken);
		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
		}

		var orders = await response.Content.ReadFromJsonAsync<List<ApiOrder>>(JsonOptions, cancellationToken)
			?? new List<ApiOrder>();

		return orders.Select(MapOrder).ToList();
	}

	// Lấy các dòng sản phẩm trong một đơn hàng.
	public async Task<IReadOnlyList<OrderDetailLineItem>> GetOrderDetailsAsync(int orderId, CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.GetAsync($"api/Order/{orderId}", cancellationToken);
		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
		}

		var details = await response.Content.ReadFromJsonAsync<List<ApiOrderDetail>>(JsonOptions, cancellationToken)
			?? new List<ApiOrderDetail>();

		return details.Select(MapOrderDetail).ToList();
	}

	// Cập nhật trạng thái xử lý của đơn hàng.
	public async Task UpdateOrderStatusAsync(int orderId, string status, CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.PutAsync($"api/Order/{orderId}/status?status={Uri.EscapeDataString(status)}", null, cancellationToken);
		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
		}
	}

	// Tạo đơn hàng mới từ các dòng sản phẩm đã chọn.
	public async Task<CreateOrderResult> CreateOrderAsync(IEnumerable<CreateOrderLineRequest> items, CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.PostAsJsonAsync("api/Order", new
		{
			Items = items
		}, JsonOptions, cancellationToken);

		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
		}

		return await response.Content.ReadFromJsonAsync<CreateOrderResult>(JsonOptions, cancellationToken)
			?? new CreateOrderResult();
	}

	// Lấy dữ liệu tổng hợp cho dashboard.
	public async Task<DashboardSnapshot> GetDashboardAsync(CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.GetAsync("api/Report/dashboard", cancellationToken);
		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
		}

		var dashboard = await response.Content.ReadFromJsonAsync<ApiDashboard>(JsonOptions, cancellationToken)
			?? throw new InvalidOperationException("Backend không trả về dashboard.");

		return MapDashboard(dashboard);
	}

	// Lấy danh sách tài khoản nội bộ cho admin.
	// Lấy nhật ký hoạt động gần đây từ backend.
	public async Task<IReadOnlyList<LogItem>> GetActivityLogsAsync(CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.GetAsync("api/Report/logs", cancellationToken);
		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
		}

		var logs = await response.Content.ReadFromJsonAsync<List<ApiActivityLog>>(JsonOptions, cancellationToken)
			?? new List<ApiActivityLog>();

		return logs.Select(MapActivityLog).ToList();
	}

	public async Task<IReadOnlyList<InternalUserItem>> GetInternalUsersAsync(CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.GetAsync("api/admin/users", cancellationToken);
		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
		}

		var users = await response.Content.ReadFromJsonAsync<List<ApiInternalUser>>(JsonOptions, cancellationToken)
			?? new List<ApiInternalUser>();

		return users.Select(MapInternalUser).ToList();
	}

	// Tạo tài khoản nội bộ manager/staff.
	public async Task CreateInternalUserAsync(string email, string password, string username, string role, CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.PostAsJsonAsync("api/admin/create-user", new
		{
			Email = email,
			Password = password,
			Username = username,
			Role = role
		}, JsonOptions, cancellationToken);

		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
		}
	}

	// Cập nhật role hoặc trạng thái hoạt động của tài khoản nội bộ.
	public async Task UpdateInternalUserAsync(int userId, string role, bool isActive, CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.PutAsJsonAsync($"api/admin/users/{userId}", new
		{
			Role = role,
			IsActive = isActive
		}, JsonOptions, cancellationToken);

		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
		}
	}

	// Khóa hoặc mở khóa tài khoản nội bộ.
	public async Task SetInternalUserActiveAsync(int userId, bool isActive, CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.PutAsync($"api/admin/users/{userId}/active?isActive={isActive}", null, cancellationToken);
		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
		}
	}

	// Đặt lại mật khẩu cho tài khoản nội bộ.
	public async Task ResetInternalUserPasswordAsync(int userId, string password, CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.PutAsJsonAsync($"api/admin/users/{userId}/password", new
		{
			Password = password
		}, JsonOptions, cancellationToken);

		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
		}
	}

	public bool IsInternalUser => CurrentUser?.Role is "admin" or "manager" or "staff";

	public bool CanManageOrders => IsInternalUser;

	public bool CanManageProducts => CurrentUser?.Role is "admin" or "manager";

	public bool CanManageUsers => CurrentUser?.Role is "admin";

	// Chuyển dữ liệu sản phẩm API sang model dùng cho UI.
	private static ProductItem MapProduct(ApiProduct product)
	{
		var initials = string.Concat(product.TenSanPham
			.Split(' ', StringSplitOptions.RemoveEmptyEntries)
			.Take(2)
			.Select(x => char.ToUpperInvariant(x[0])));

		if (string.IsNullOrWhiteSpace(initials))
		{
			initials = "SP";
		}

		var isLowStock = product.SoLuongTon > 0 && product.SoLuongTon <= product.MucTonThap;
		var isOutOfStock = product.SoLuongTon <= 0;
		var isActive = product.TrangThai.Contains("Hoat", StringComparison.OrdinalIgnoreCase)
			|| product.TrangThai.Contains("Hoạt", StringComparison.OrdinalIgnoreCase);
		return new ProductItem
		{
			Id = product.MaSanPham,
			Code = $"SP-{product.MaSanPham:0000}",
			Name = product.TenSanPham,
			Description = string.IsNullOrWhiteSpace(product.MoTa) ? "Chưa có mô tả sản phẩm." : product.MoTa,
			Status = product.TrangThai,
			ImportPrice = product.GiaNhap,
			LowStockThreshold = product.MucTonThap,
			UnitPrice = product.GiaBan,
			StockQuantity = product.SoLuongTon,
			ImageUrl = NormalizeImageUrl(product.HinhAnhUrl),
			Price = product.GiaBan.ToString("N0", CultureInfo.InvariantCulture) + " VND",
			Stock = product.SoLuongTon.ToString(CultureInfo.InvariantCulture),
			BadgeText = isOutOfStock ? "Ngừng bán" : isLowStock ? "Sắp hết hàng" : isActive ? "Đang bán" : "Ngừng bán",
			BadgeColor = isOutOfStock ? Colors.Firebrick : isLowStock ? Colors.Orange : isActive ? Colors.SeaGreen : Colors.Firebrick,
			Initials = initials
		};
	}

	// Chuyển dữ liệu đơn hàng API sang model dùng cho UI.
	private static OrderItem MapOrder(ApiOrder order)
	{
		var statusColor = order.TrangThai switch
		{
			"done" => Colors.SeaGreen,
			"processing" => Colors.RoyalBlue,
			"cancel" => Colors.Firebrick,
			_ => Colors.Orange
		};

		return new OrderItem
		{
			Id = order.MaDonHang,
			Code = $"DH-{order.MaDonHang:0000}",
			Customer = $"Người dùng #{order.MaNguoiDung}",
			Email = CurrentUserLabel(order.MaNguoiDung),
			CreatedAt = order.NgayTao.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
			Total = order.TongTien.ToString("N0", CultureInfo.InvariantCulture) + " VND",
			Status = order.TrangThai,
			StatusColor = statusColor,
			Initials = "ND"
		};
	}

	// Chuyển chi tiết đơn hàng API sang dòng hiển thị.
	private static OrderDetailLineItem MapOrderDetail(ApiOrderDetail detail)
	{
		var productName = detail.SanPham?.TenSanPham ?? $"Sản phẩm #{detail.MaSanPham}";
		var lineTotal = detail.DonGia * detail.SoLuong;
		var initials = string.Concat(productName
			.Split(' ', StringSplitOptions.RemoveEmptyEntries)
			.Take(2)
			.Select(x => char.ToUpperInvariant(x[0])));

		if (string.IsNullOrWhiteSpace(initials))
		{
			initials = "SP";
		}

		return new OrderDetailLineItem
		{
			ProductId = detail.MaSanPham,
			Name = productName,
			Variant = $"Mã SP: {detail.MaSanPham}",
			Quantity = detail.SoLuong.ToString(CultureInfo.InvariantCulture),
			UnitPrice = detail.DonGia.ToString("N0", CultureInfo.InvariantCulture) + " VND",
			LineTotal = lineTotal.ToString("N0", CultureInfo.InvariantCulture) + " VND",
			Initials = initials
		};
	}

	// Chuyển giao dịch kho sang dòng lịch sử.
	private static StockHistoryItem MapStockHistory(ApiStockHistory item)
	{
		return new StockHistoryItem
		{
			Time = item.ThoiGian.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
			Type = item.Loại,
			Quantity = item.SoLuong > 0
				? "+" + item.SoLuong.ToString(CultureInfo.InvariantCulture)
				: item.SoLuong.ToString(CultureInfo.InvariantCulture),
			Actor = item.MaNguoiDung.HasValue ? $"User #{item.MaNguoiDung.Value}" : "Hệ thống",
			Note = item.GhiChu
		};
	}

	// Chuyển dữ liệu dashboard API sang snapshot dùng cho màn hình tổng quan.
	// Chuyển giao dịch nhập kho sang model cho trang nhập hàng.
	private static ImportItem MapImportHistory(ApiImportHistory item)
	{
		var amount = item.ThanhTien <= 0 ? item.DonGiaNhap * item.SoLuong : item.ThanhTien;
		return new ImportItem
		{
			Id = item.MaGiaoDich,
			ProductId = item.MaSanPham,
			Code = $"PN-{item.MaGiaoDich:0000}",
			Supplier = item.TenSanPham,
			Date = item.ThoiGian.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
			ProductCount = $"+{item.SoLuong} sản phẩm",
			Amount = amount.ToString("N0", CultureInfo.InvariantCulture) + " VND",
			Status = "Đã nhập kho",
			StatusColor = Colors.SeaGreen,
			Note = string.IsNullOrWhiteSpace(item.GhiChu) ? "Không có ghi chú" : item.GhiChu,
			Actor = item.MaNguoiDung.HasValue ? $"User #{item.MaNguoiDung.Value}" : "Hệ thống"
		};
	}

	// Chuyển log API sang item hiển thị ở trang nhật ký.
	private static LogItem MapActivityLog(ApiActivityLog item)
	{
		var accent = item.Level switch
		{
			"success" => Colors.SeaGreen,
			"warning" => Colors.Orange,
			"danger" => Colors.Firebrick,
			_ => Colors.RoyalBlue
		};

		return new LogItem
		{
			Title = item.Title,
			Description = item.Description,
			Time = item.Time.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
			Tag = item.Tag,
			Icon = item.Icon,
			Accent = accent
		};
	}

	private static DashboardSnapshot MapDashboard(ApiDashboard dashboard)
	{
		var activePercent = dashboard.TotalProducts == 0
			? 0
			: (int)Math.Round(dashboard.ActiveProducts * 100d / dashboard.TotalProducts);

		return new DashboardSnapshot
		{
			Stats =
			[
				new() { Title = "Tổng sản phẩm", Value = dashboard.TotalProducts.ToString(CultureInfo.InvariantCulture), Subtitle = $"{dashboard.ActiveProducts} đang bán", Icon = "SP", Accent = Colors.RoyalBlue },
				new() { Title = "Tổng đơn hang", Value = dashboard.TotalOrders.ToString(CultureInfo.InvariantCulture), Subtitle = $"{dashboard.PendingOrders} cho xu ly", Icon = "DH", Accent = Colors.SeaGreen },
				new() { Title = "Doanh thu", Value = dashboard.TotalRevenue.ToString("N0", CultureInfo.InvariantCulture) + " VND", Subtitle = "Tổng doanh thu", Icon = "$", Accent = Colors.DarkCyan },
				new() { Title = "Hom nay", Value = dashboard.TodayRevenue.ToString("N0", CultureInfo.InvariantCulture) + " VND", Subtitle = "Doanh thu ngay", Icon = "D", Accent = Colors.MediumPurple },
				new() { Title = "Cảnh báo kho", Value = dashboard.LowStockProducts.ToString(CultureInfo.InvariantCulture), Subtitle = $"{dashboard.OutOfStockProducts} ngừng bán", Icon = "!", Accent = Colors.OrangeRed }
			],
			LowStockAlerts = dashboard.LowStock.Select(x => new AlertItem
			{
				Name = x.TenSanPham,
				Code = $"SP-{x.MaSanPham:0000}",
				StockText = $"Còn {x.SoLuongTon} / mức {x.MucTonThap}",
				Accent = x.SoLuongTon <= 0 ? Colors.Firebrick : Colors.Orange
			}).ToList(),
			Activities = dashboard.RecentActivities.Select(x => new ActivityItem
			{
				Title = x.Title,
				Time = x.Time.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
				Icon = x.Type == "stock" ? "K" : "D",
				Accent = x.Type == "stock" ? Colors.RoyalBlue : Colors.SeaGreen
			}).ToList(),
			OrderTrend = dashboard.Trend.Select(x => new OrderTrendItem
			{
				Label = x.Label,
				Orders = x.TotalOrders.ToString(CultureInfo.InvariantCulture),
				Completed = x.CompletedOrders.ToString(CultureInfo.InvariantCulture),
				Processing = x.ProcessingOrders.ToString(CultureInfo.InvariantCulture),
				Revenue = x.Revenue.ToString("N0", CultureInfo.InvariantCulture) + " VND"
			}).ToList(),
			TopProducts = dashboard.TopProducts.Select(x => new TopProductItem
			{
				Code = $"SP-{x.MaSanPham:0000}",
				Name = x.TenSanPham,
				Quantity = x.SoLuongBan.ToString(CultureInfo.InvariantCulture),
				Revenue = x.DoanhThu.ToString("N0", CultureInfo.InvariantCulture) + " VND",
				Accent = Colors.RoyalBlue
			}).ToList(),
			InventoryPercent = activePercent.ToString(CultureInfo.InvariantCulture) + "%",
			InventorySubtitle = $"{dashboard.ActiveProducts}/{dashboard.TotalProducts} sản phẩm đang bán"
		};
	}

	// Chuyển dữ liệu người dùng nội bộ sang model quản trị.
	private static InternalUserItem MapInternalUser(ApiInternalUser user)
	{
		return new InternalUserItem
		{
			Id = user.MaNguoiDung,
			Username = user.TenDangNhap,
			Email = user.Email,
			Role = user.Role,
			IsActive = user.IsActive
		};
	}

	// Chuẩn hóa URL ảnh tương đối thành URL đầy đủ của backend.
	private static string NormalizeImageUrl(string imageUrl)
	{
		if (string.IsNullOrWhiteSpace(imageUrl))
			return string.Empty;

		if (imageUrl.StartsWith("/", StringComparison.Ordinal))
			return new Uri(ApiBaseAddress, imageUrl.TrimStart('/')).ToString();

		return imageUrl;
	}

	// Chuyển URL backend đầy đủ về đường dẫn tương đối trước khi lưu.
	private static string ToStoredImageUrl(string imageUrl)
	{
		if (string.IsNullOrWhiteSpace(imageUrl))
			return string.Empty;

		if (Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri) && uri.Host == ApiBaseAddress.Host && uri.Port == ApiBaseAddress.Port)
			return uri.AbsolutePath;

		return imageUrl;
	}

	// Xác định Content-Type khi upload ảnh.
	private static string GetImageContentType(string filePath)
	{
		return Path.GetExtension(filePath).ToLowerInvariant() switch
		{
			".png" => "image/png",
			".webp" => "image/webp",
			".gif" => "image/gif",
			_ => "image/jpeg"
		};
	}

	// Tạo nhãn email giả cho người dùng trong đơn hàng.
	private static string CurrentUserLabel(int userId)
	{
		return $"user-{userId}@syncchain.local";
	}

	// Đọc nội dung lỗi trả về từ backend.
	private static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
	{
		var message = await response.Content.ReadAsStringAsync(cancellationToken);
		return string.IsNullOrWhiteSpace(message)
			? $"Backend trả về lỗi {(int)response.StatusCode}."
			: message.Trim('"');
	}

	private sealed class LoginResponse
	{
		public string Token { get; set; } = string.Empty;
		public ApiUser User { get; set; } = new();
	}

	private sealed class UploadImageResponse
	{
		public string ImageUrl { get; set; } = string.Empty;
	}

	private sealed class ApiProductDetail
	{
		public ApiProduct Product { get; set; } = new();
		public int SoldCount { get; set; }
		public decimal Revenue { get; set; }
		public List<ApiStockHistory> StockHistory { get; set; } = new();
	}

	private sealed class ApiStockHistory
	{
		public DateTime ThoiGian { get; set; }
		public string Loại { get; set; } = string.Empty;
		public int SoLuong { get; set; }
		public int? MaNguoiDung { get; set; }
		public string GhiChu { get; set; } = string.Empty;
	}

	private sealed class ApiImportHistory
	{
		public int MaGiaoDich { get; set; }
		public int MaSanPham { get; set; }
		public string TenSanPham { get; set; } = string.Empty;
		public int SoLuong { get; set; }
		public DateTime ThoiGian { get; set; }
		public int? MaNguoiDung { get; set; }
		public string GhiChu { get; set; } = string.Empty;
		public decimal DonGiaNhap { get; set; }
		public decimal ThanhTien { get; set; }
	}

	private sealed class ApiActivityLog
	{
		public string Title { get; set; } = string.Empty;
		public string Description { get; set; } = string.Empty;
		public DateTime Time { get; set; }
		public string Tag { get; set; } = string.Empty;
		public string Icon { get; set; } = string.Empty;
		public string Level { get; set; } = string.Empty;
	}

	private sealed class ApiDashboard
	{
		public int TotalProducts { get; set; }
		public int ActiveProducts { get; set; }
		public int LowStockProducts { get; set; }
		public int OutOfStockProducts { get; set; }
		public int TotalOrders { get; set; }
		public int PendingOrders { get; set; }
		public int CompletedOrders { get; set; }
		public int CancelledOrders { get; set; }
		public decimal TotalRevenue { get; set; }
		public decimal TodayRevenue { get; set; }
		public List<ApiOrderTrend> Trend { get; set; } = new();
		public List<ApiTopProduct> TopProducts { get; set; } = new();
		public List<ApiLowStockProduct> LowStock { get; set; } = new();
		public List<ApiRecentActivity> RecentActivities { get; set; } = new();
	}

	private sealed class ApiOrderTrend
	{
		public string Label { get; set; } = string.Empty;
		public int TotalOrders { get; set; }
		public int CompletedOrders { get; set; }
		public int ProcessingOrders { get; set; }
		public decimal Revenue { get; set; }
	}

	private sealed class ApiTopProduct
	{
		public int MaSanPham { get; set; }
		public string TenSanPham { get; set; } = string.Empty;
		public int SoLuongBan { get; set; }
		public decimal DoanhThu { get; set; }
	}

	private sealed class ApiLowStockProduct
	{
		public int MaSanPham { get; set; }
		public string TenSanPham { get; set; } = string.Empty;
		public int SoLuongTon { get; set; }
		public int MucTonThap { get; set; }
	}

	private sealed class ApiRecentActivity
	{
		public string Title { get; set; } = string.Empty;
		public DateTime Time { get; set; }
		public string Type { get; set; } = string.Empty;
	}

	private sealed class ApiInternalUser
	{
		public int MaNguoiDung { get; set; }
		public string TenDangNhap { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
		public bool IsActive { get; set; }
		public string Role { get; set; } = string.Empty;
	}

	public sealed class ApiUser
	{
		public int MaNguoiDung { get; set; }
		public string TenDangNhap { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
		public string Role { get; set; } = string.Empty;
		public string DisplayName => string.IsNullOrWhiteSpace(TenDangNhap) ? Email : TenDangNhap;
		public string Username => string.IsNullOrWhiteSpace(TenDangNhap) ? Email : TenDangNhap;
		public string RoleLabel => Role switch
		{
			"admin" => "Admin",
			"manager" => "Manager",
			"staff" => "Staff",
			"customer" => "Customer",
			_ => Role
		};
		public string Initials
		{
			get
			{
				var initials = string.Concat(DisplayName
					.Split(' ', StringSplitOptions.RemoveEmptyEntries)
					.Take(2)
					.Select(x => char.ToUpperInvariant(x[0])));

				return string.IsNullOrWhiteSpace(initials) ? "ND" : initials;
			}
		}
	}

	public sealed class CreateOrderLineRequest
	{
		public int MaSanPham { get; set; }
		public int SoLuong { get; set; }
	}

	public sealed class CreateOrderResult
	{
		public string Message { get; set; } = string.Empty;
		public int MaDonHang { get; set; }
		public decimal TongTien { get; set; }
	}

	private sealed class ApiProduct
	{
		public int MaSanPham { get; set; }
		public string TenSanPham { get; set; } = string.Empty;
		public decimal GiaBan { get; set; }
		public decimal GiaNhap { get; set; }
		public int SoLuongTon { get; set; }
		public int MucTonThap { get; set; }
		public string TrangThai { get; set; } = string.Empty;
		public string HinhAnhUrl { get; set; } = string.Empty;
		public string MoTa { get; set; } = string.Empty;
	}

	private sealed class ApiOrder
	{
		public int MaDonHang { get; set; }
		public int MaNguoiDung { get; set; }
		public decimal TongTien { get; set; }
		public DateTime NgayTao { get; set; }
		public string TrangThai { get; set; } = string.Empty;
	}

	private sealed class ApiOrderDetail
	{
		public int MaSanPham { get; set; }
		public int SoLuong { get; set; }
		public decimal DonGia { get; set; }
		public ApiProduct? SanPham { get; set; }
	}
}
