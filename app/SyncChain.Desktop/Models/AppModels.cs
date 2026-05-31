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
//  API RESPONSE MODELS — khớp với backend Node.js
// ═══════════════════════════════════════════════════════════

public sealed class ApiResponse<T>
{
	public bool success { get; set; }
	public T? data { get; set; }
	public string? message { get; set; }
}

public sealed class SanPhamApi
{
	public int MaSanPham { get; set; }
	public string TenSanPham { get; set; } = string.Empty;
	public decimal GiaBan { get; set; }
	public int SoLuongTon { get; set; }
	public int MucTonThap { get; set; } = 10;
	public string TrangThai { get; set; } = "Hoat dong";
}

public sealed class DonHangApi
{
	public int MaDonHang { get; set; }
	public int MaKhachHang { get; set; }
	public decimal TongTien { get; set; }
	public string TrangThaiDon { get; set; } = "Da dat hang";
	public DateTime NgayTao { get; set; }
}

public sealed class ChiTietDonHangApi
{
	public int MaSanPham { get; set; }
	public string TenSanPham { get; set; } = string.Empty;
	public int SoLuong { get; set; }
	public decimal DonGia { get; set; }
}

public sealed class DonHangDetailApi
{
	public int MaDonHang { get; set; }
	public int MaKhachHang { get; set; }
	public decimal TongTien { get; set; }
	public string TrangThaiDon { get; set; } = string.Empty;
	public DateTime NgayTao { get; set; }
	public List<ChiTietDonHangApi> items { get; set; } = new();
}

public sealed class CreateOrderRequest
{
	public int MaKhachHang { get; set; }
	public List<OrderItemRequest> items { get; set; } = new();
}

public sealed class OrderItemRequest
{
	public int MaSanPham { get; set; }
	public int SoLuong { get; set; }
}

public sealed class CreateOrderResponse
{
	public int MaDonHang { get; set; }
	public decimal TongTien { get; set; }
}
