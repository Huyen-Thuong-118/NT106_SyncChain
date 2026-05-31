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
	public string TrangThai { get; set; } = "pending";
	public DateTime NgayTao { get; set; }
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
	public string TenDangNhap { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public string Role { get; set; } = string.Empty;
	public bool IsActive { get; set; }
}

public sealed class CreateOrderRequest
{
	public List<OrderItemRequest> Items { get; set; } = new();
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
