using Microsoft.Maui.Graphics;

namespace SyncChain.Desktop.Models;

public sealed class StatCard
{
	public string Title { get; init; } = string.Empty;
	public string Value { get; init; } = string.Empty;
	public string Subtitle { get; init; } = string.Empty;
	public string Icon { get; init; } = string.Empty;
	public Color Accent { get; init; } = Colors.Transparent;
}

public sealed class AlertItem
{
	public string Name { get; init; } = string.Empty;
	public string Code { get; init; } = string.Empty;
	public string StockText { get; init; } = string.Empty;
	public Color Accent { get; init; } = Colors.Transparent;
}

public sealed class ActivityItem
{
	public string Title { get; init; } = string.Empty;
	public string Time { get; init; } = string.Empty;
	public string Icon { get; init; } = string.Empty;
	public Color Accent { get; init; } = Colors.Transparent;
}

public sealed class BridgeItem
{
	public string Title { get; init; } = string.Empty;
	public string Description { get; init; } = string.Empty;
	public string Status { get; init; } = string.Empty;
	public Color Accent { get; init; } = Colors.Transparent;
}

public sealed class ProductItem
{
	public int MaSanPham { get; init; }
	public string Code { get; init; } = string.Empty;
	public string Name { get; init; } = string.Empty;
	public string Description { get; init; } = string.Empty;
	public string Price { get; init; } = string.Empty;
	public string Stock { get; init; } = string.Empty;
	public string BadgeText { get; init; } = string.Empty;
	public Color BadgeColor { get; init; } = Colors.Transparent;
	public string Initials { get; init; } = string.Empty;
	public double HealthProgress { get; init; }
}

public sealed class InventoryEvent
{
	public string Date { get; init; } = string.Empty;
	public string Type { get; init; } = string.Empty;
	public string Quantity { get; init; } = string.Empty;
	public string Actor { get; init; } = string.Empty;
	public string Note { get; init; } = string.Empty;
	public Color Accent { get; init; } = Colors.Transparent;
}

public sealed class OrderItem
{
	public int MaDonHang { get; init; }
	public string Code { get; init; } = string.Empty;
	public string Customer { get; init; } = string.Empty;
	public string Email { get; init; } = string.Empty;
	public string CreatedAt { get; init; } = string.Empty;
	public string Total { get; init; } = string.Empty;
	public string Status { get; init; } = string.Empty;
	public Color StatusColor { get; init; } = Colors.Transparent;
	public string Initials { get; init; } = string.Empty;

	public Color StatusBadgeBackground => Status switch
	{
		"Hoàn tất" or "Đã giao" or "Đã nhập" => Color.FromArgb("#d3e5f1"),
		"Đang vận chuyển" or "Đang xử lý" or "Đã xử lý" => Color.FromArgb("#dbeafe"),
		"Chờ duyệt" => Color.FromArgb("#dae2fd"),
		"Hủy" or "Từ chối" => Color.FromArgb("#ffdad6"),
		_ => Color.FromArgb("#eef0f2")
	};
}

public sealed class TimelineItem
{
	public string Title { get; init; } = string.Empty;
	public string Time { get; init; } = string.Empty;
	public string State { get; init; } = string.Empty;
	public Color Accent { get; init; } = Colors.Transparent;
}

public sealed class LineItem
{
	public string Name { get; init; } = string.Empty;
	public string Variant { get; init; } = string.Empty;
	public string Quantity { get; init; } = string.Empty;
	public string Price { get; init; } = string.Empty;
	public string Initials { get; init; } = string.Empty;
}

public sealed class ImportItem
{
	public string Code { get; init; } = string.Empty;
	public string Supplier { get; init; } = string.Empty;
	public string Date { get; init; } = string.Empty;
	public string ProductCount { get; init; } = string.Empty;
	public string Amount { get; init; } = string.Empty;
	public string Status { get; init; } = string.Empty;
	public Color StatusColor { get; init; } = Colors.Transparent;

	public Color StatusBadgeBackground => Status switch
	{
		"Hoàn tất" or "Đã giao" or "Đã nhập" => Color.FromArgb("#d3e5f1"),
		"Đang vận chuyển" or "Đang xử lý" or "Đã xử lý" => Color.FromArgb("#dbeafe"),
		"Chờ duyệt" => Color.FromArgb("#dae2fd"),
		"Hủy" or "Từ chối" => Color.FromArgb("#ffdad6"),
		_ => Color.FromArgb("#eef0f2")
	};
}

public sealed class SupplierItem
{
	public string Name { get; init; } = string.Empty;
	public string Orders { get; init; } = string.Empty;
	public string Amount { get; init; } = string.Empty;
	public string Initial { get; init; } = string.Empty;
	public Color Accent { get; init; } = Colors.Transparent;
}

public sealed class LogItem
{
	public string Title { get; init; } = string.Empty;
	public string Description { get; init; } = string.Empty;
	public string Time { get; init; } = string.Empty;
	public string Tag { get; init; } = string.Empty;
	public string Icon { get; init; } = string.Empty;
	public Color Accent { get; init; } = Colors.Transparent;
}

public sealed class ChatThread
{
	public string Name { get; init; } = string.Empty;
	public string Preview { get; init; } = string.Empty;
	public string Time { get; init; } = string.Empty;
	public string Initials { get; init; } = string.Empty;
	public bool IsActive { get; init; }
}

public sealed class ChatMessage
{
	public string Content { get; init; } = string.Empty;
	public string Time { get; init; } = string.Empty;
	public bool IsOutgoing { get; init; }
	public bool IsDateDivider { get; init; }
}

public sealed class RoleOption
{
	public string Name { get; init; } = string.Empty;
	public string Description { get; init; } = string.Empty;
	public bool IsSelected { get; init; }
}

public sealed class PaymentOption
{
	public string Name { get; init; } = string.Empty;
	public bool IsSelected { get; init; }
}

// ═══════════════════════════════════════════════════════════
//  API RESPONSE MODELS — khớp với backend ASP.NET Core
// ═══════════════════════════════════════════════════════════

/// <summary>
/// Sản phẩm từ API /api/product
/// </summary>
public sealed class SanPhamApi
{
	public int MaSanPham { get; set; }
	public string TenSanPham { get; set; } = string.Empty;
	public decimal GiaBan { get; set; }
	public decimal GiaNhap { get; set; }
	public int SoLuongTon { get; set; }
	public int MucTonThap { get; set; } = 10;
	public string TrangThai { get; set; } = "Hoat dong";
	public string HinhAnhUrl { get; set; } = string.Empty;
	public string MoTa { get; set; } = string.Empty;
}

/// <summary>
/// Đơn hàng từ API /api/order
/// </summary>
public sealed class DonHangApi
{
	public int MaDonHang { get; set; }
	public int MaNguoiDung { get; set; }
	public decimal TongTien { get; set; }
	public string TrangThai { get; set; } = "Draft";
	public DateTime NgayTao { get; set; }
	public string? NguoiNhan { get; set; }
	public string? SoDienThoaiNhan { get; set; }
	public string? DiaChiGiao { get; set; }
}

/// <summary>
/// Chi tiết đơn hàng từ API /api/order/{id}
/// </summary>
public sealed class ChiTietDonHangApi
{
	public int MaSanPham { get; set; }
	public int SoLuong { get; set; }
	public decimal DonGia { get; set; }
	public SanPhamTrongDonApi? SanPham { get; set; }
}

public sealed class SanPhamTrongDonApi
{
	public int MaSanPham { get; set; }
	public string TenSanPham { get; set; } = string.Empty;
	public decimal GiaBan { get; set; }
	public int SoLuongTon { get; set; }
	public int MucTonThap { get; set; }
	public string TrangThai { get; set; } = string.Empty;
}

/// <summary>
/// Chi tiết sản phẩm từ API /api/product/{id}/detail
/// </summary>
public sealed class ProductDetailApi
{
	public SanPhamApi? Product { get; set; }
	public int SoldCount { get; set; }
	public decimal Revenue { get; set; }
	public List<StockHistoryApi> StockHistory { get; set; } = new();
}

public sealed class StockHistoryApi
{
	public DateTime ThoiGian { get; set; }
	public string Loai { get; set; } = string.Empty;
	public int SoLuong { get; set; }
	public int? MaNguoiDung { get; set; }
	public string GhiChu { get; set; } = string.Empty;
}

/// <summary>
/// Dashboard từ API /api/report/dashboard
/// </summary>
public sealed class DashboardApi
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
	public List<TrendApi> Trend { get; set; } = new();
	public List<TopProductApi> TopProducts { get; set; } = new();
	public List<LowStockApi> LowStock { get; set; } = new();
	public List<RecentActivityApi> RecentActivities { get; set; } = new();
}

public sealed class TrendApi
{
	public DateTime Date { get; set; }
	public string Label { get; set; } = string.Empty;
	public int TotalOrders { get; set; }
	public int CompletedOrders { get; set; }
	public int ProcessingOrders { get; set; }
	public decimal Revenue { get; set; }
}

public sealed class TopProductApi
{
	public int MaSanPham { get; set; }
	public string TenSanPham { get; set; } = string.Empty;
	public int SoLuongBan { get; set; }
	public decimal DoanhThu { get; set; }
}

public sealed class LowStockApi
{
	public int MaSanPham { get; set; }
	public string TenSanPham { get; set; } = string.Empty;
	public int SoLuongTon { get; set; }
	public int MucTonThap { get; set; }
	public string TrangThai { get; set; } = string.Empty;
}

public sealed class RecentActivityApi
{
	public string Title { get; set; } = string.Empty;
	public DateTime Time { get; set; }
	public string Type { get; set; } = string.Empty;
}

/// <summary>
/// Giao dịch nhập kho từ API /api/product/imports
/// </summary>
public sealed class ImportApi
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

/// <summary>
/// Log từ API /api/report/logs
/// </summary>
public sealed class LogApi
{
	public string Title { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public DateTime Time { get; set; }
	public string Tag { get; set; } = string.Empty;
	public string Icon { get; set; } = string.Empty;
	public string Level { get; set; } = "info";
}

/// <summary>
/// User từ API /api/admin/users
/// </summary>
public sealed class UserApi
{
	public int MaNguoiDung { get; set; }
	public string TenDangNhap { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public bool IsActive { get; set; }
	public string Role { get; set; } = string.Empty;
}

/// <summary>
/// Profile từ API /api/auth/profile
/// </summary>
public sealed class ProfileApi
{
	public int MaNguoiDung { get; set; }
	public string? Ho { get; set; }
	public string? Ten { get; set; }
	public string TenDangNhap { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public string? SoDienThoai { get; set; }
	public string Role { get; set; } = string.Empty;
	public bool IsActive { get; set; }
	public string HoTen => $"{Ho} {Ten}".Trim();
}

/// <summary>
/// Địa chỉ từ API /api/address
/// </summary>
public sealed class DiaChiApi
{
	public int MaDiaChi { get; set; }
	public string TenNguoiNhan { get; set; } = string.Empty;
	public string SoDienThoai { get; set; } = string.Empty;
	public string TinhThanh { get; set; } = string.Empty;
	public string QuanHuyen { get; set; } = string.Empty;
	public string PhuongXa { get; set; } = string.Empty;
	public string DiaChiChiTiet { get; set; } = string.Empty;
	public bool LaMacDinh { get; set; }
	public string DiaChi { get; set; } = string.Empty;
}

/// <summary>
/// Giỏ hàng từ API /api/cart
/// </summary>
public sealed class CartApi
{
	public int MaGioHang { get; set; }
	public List<CartItemApi> Items { get; set; } = new();
	public decimal TongTien { get; set; }
	public int SoLuong { get; set; }
}

public sealed class CartItemApi
{
	public int MaChiTietGio { get; set; }
	public int MaSanPham { get; set; }
	public string TenSanPham { get; set; } = string.Empty;
	public decimal GiaBan { get; set; }
	public string HinhAnhUrl { get; set; } = string.Empty;
	public int SoLuong { get; set; }
	public decimal ThanhTien { get; set; }
}

public sealed class CreateOrderRequest
{
	public List<OrderItemRequest> Items { get; set; } = new();
	public int? MaDiaChi { get; set; }
}

public sealed class OrderItemRequest
{
	public int MaSanPham { get; set; }
	public int SoLuong { get; set; }
}

public sealed class CreateOrderResponse
{
	public string Message { get; set; } = string.Empty;
	public int MaDonHang { get; set; }
	public decimal TongTien { get; set; }
}

/// <summary>
/// Bước timeline từ API /api/order/{id}/tracking
/// </summary>
public sealed class TrackingTimelineStep
{
	public string Step { get; set; } = string.Empty;
	public string TrangThai { get; set; } = string.Empty; // hoanThanh | hienTai | choDoi | huyBo
	public Color Color => TrangThai switch
	{
		"hoanThanh" => Color.FromArgb("#22c55e"),
		"hienTai"   => Color.FromArgb("#3b82f6"),
		"huyBo"     => Color.FromArgb("#ef4444"),
		_           => Color.FromArgb("#9ca3af")
	};
	public string Label => Step switch
	{
		"Draft"      => "Chờ duyệt",
		"Approved"   => "Đã duyệt",
		"Processing" => "Đang xử lý",
		"Done"       => "Hoàn tất",
		"Cancelled"  => "Đã hủy",
		_            => Step
	};
}

/// <summary>
/// Thông tin thanh toán từ tracking endpoint
/// </summary>
public sealed class ThanhToanInfo
{
	public int MaThanhToan { get; set; }
	public string PhuongThuc { get; set; } = string.Empty;
	public string TrangThaiThanhToan { get; set; } = string.Empty;
	public decimal SoTien { get; set; }
	public DateTime NgayTao { get; set; }
	public DateTime? NgayCapNhat { get; set; }
}

/// <summary>
/// Response từ GET /api/order/{id}/tracking
/// </summary>
public sealed class OrderTrackingResponse
{
	public DonHangTrackingApi Order { get; set; } = new();
	public ThanhToanInfo? Payment { get; set; }
	public List<ChiTietDonHangApi> ChiTiet { get; set; } = new();
	public List<TrackingTimelineStep> Timeline { get; set; } = new();
}

public sealed class DonHangTrackingApi
{
	public int MaDonHang { get; set; }
	public int MaNguoiDung { get; set; }
	public decimal TongTien { get; set; }
	public string TrangThai { get; set; } = "Draft";
	public DateTime NgayTao { get; set; }
	public string? PhuongThucThanhToan { get; set; }
	public string? NguoiNhan { get; set; }
	public string? SoDienThoaiNhan { get; set; }
	public string? DiaChiGiao { get; set; }
}

/// <summary>
/// Response từ POST /api/payment/initiate
/// </summary>
public sealed class PaymentInitResponse
{
	public string? PaymentUrl { get; set; }
	public string PhuongThuc { get; set; } = string.Empty;
	public string? Message { get; set; }
}

/// <summary>
/// Thông báo từ API /api/notification
/// </summary>
public sealed class ThongBaoApi
{
	public int MaThongBao { get; set; }
	public string LoaiThongBao { get; set; } = string.Empty;
	public string TieuDe { get; set; } = string.Empty;
	public string NoiDung { get; set; } = string.Empty;
	public bool DaDoc { get; set; }
	public DateTime NgayTao { get; set; }
	public int? MaDonHang { get; set; }
	public Color BackgroundColor => DaDoc
		? Color.FromArgb("#f9fafb")
		: Color.FromArgb("#eff6ff");
}
