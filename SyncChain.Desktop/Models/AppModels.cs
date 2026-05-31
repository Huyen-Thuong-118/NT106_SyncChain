using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls;

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

public sealed class DashboardSnapshot
{
	public IReadOnlyList<StatCard> Stats { get; init; } = Array.Empty<StatCard>();
	public IReadOnlyList<AlertItem> LowStockAlerts { get; init; } = Array.Empty<AlertItem>();
	public IReadOnlyList<ActivityItem> Activities { get; init; } = Array.Empty<ActivityItem>();
	public IReadOnlyList<OrderTrendItem> OrderTrend { get; init; } = Array.Empty<OrderTrendItem>();
	public IReadOnlyList<TopProductItem> TopProducts { get; init; } = Array.Empty<TopProductItem>();
	public string InventoryPercent { get; init; } = "0%";
	public string InventorySubtitle { get; init; } = "Chưa có dữ liệu";
}

public sealed class OrderTrendItem
{
	public string Label { get; init; } = string.Empty;
	public string Orders { get; init; } = string.Empty;
	public string Completed { get; init; } = string.Empty;
	public string Processing { get; init; } = string.Empty;
	public string Revenue { get; init; } = string.Empty;
}

public sealed class TopProductItem
{
	public string Code { get; init; } = string.Empty;
	public string Name { get; init; } = string.Empty;
	public string Quantity { get; init; } = string.Empty;
	public string Revenue { get; init; } = string.Empty;
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
	public int Id { get; init; }
	public string Code { get; init; } = string.Empty;
	public string Name { get; init; } = string.Empty;
	public string Description { get; init; } = string.Empty;
	public string Status { get; init; } = string.Empty;
	public decimal ImportPrice { get; init; }
	public int LowStockThreshold { get; init; }
	public decimal UnitPrice { get; init; }
	public int StockQuantity { get; init; }
	public string ImageUrl { get; init; } = string.Empty;
	// Kiểm tra sản phẩm có ảnh để quyết định hiển thị ảnh hay chữ viết tắt.
	public bool HasImage => DisplayImageSource != null;
	public bool ShowInitials => !HasImage;
	
	private ImageSource? _displayImageSource;
	// Tạo ImageSource một lần từ URL hoặc file ảnh.
    public ImageSource? DisplayImageSource =>
    _displayImageSource ??= CreateImageSource(ImageUrl);
	public string Price { get; init; } = string.Empty;
	public string Stock { get; init; } = string.Empty;
	public string BadgeText { get; init; } = string.Empty;
	public Color BadgeColor { get; init; } = Colors.Transparent;
	public string Initials { get; init; } = string.Empty;

	// Chuyển đường dẫn ảnh thành nguồn ảnh MAUI.
	private static ImageSource? CreateImageSource(string imageUrl)
{
	if (string.IsNullOrWhiteSpace(imageUrl))
		return null;

	try
	{
		// FILE LOCAL
		if (File.Exists(imageUrl))
		{
			return ImageSource.FromStream(() =>
			{
				return File.OpenRead(imageUrl);
			});
		}

		// URL TU BACKEND
		if (imageUrl.StartsWith("/"))
		{
			imageUrl = $"http://localhost:5292{imageUrl}";
		}

		// URL HTTP/HTTPS
		if (Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri))
		{
			// file://
			if (uri.IsFile && File.Exists(uri.LocalPath))
			{
				return ImageSource.FromStream(() =>
				{
					return File.OpenRead(uri.LocalPath);
				});
			}

			// http/https
			if (uri.Scheme is "http" or "https")
			{
				return ImageSource.FromUri(uri);
			}
		}
	}
	catch
	{
		return null;
	}

	return null;
}
}

public sealed class ProductDetailData
{
	public ProductItem Product { get; init; } = new();
	public int SoldCount { get; init; }
	public decimal Revenue { get; init; }
	public IReadOnlyList<StockHistoryItem> StockHistory { get; init; } = Array.Empty<StockHistoryItem>();
}

public sealed class StockHistoryItem
{
	public string Time { get; init; } = string.Empty;
	public string Type { get; init; } = string.Empty;
	public string Quantity { get; init; } = string.Empty;
	public string Actor { get; init; } = string.Empty;
	public string Note { get; init; } = string.Empty;
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
	public int Id { get; init; }
	public string Code { get; init; } = string.Empty;
	public string Customer { get; init; } = string.Empty;
	public string Email { get; init; } = string.Empty;
	public string CreatedAt { get; init; } = string.Empty;
	public string Total { get; init; } = string.Empty;
	public string Status { get; init; } = string.Empty;
	public Color StatusColor { get; init; } = Colors.Transparent;
	public string Initials { get; init; } = string.Empty;
}

public sealed class OrderDetailLineItem
{
	public int ProductId { get; init; }
	public string Name { get; init; } = string.Empty;
	public string Variant { get; init; } = string.Empty;
	public string Quantity { get; init; } = string.Empty;
	public string UnitPrice { get; init; } = string.Empty;
	public string LineTotal { get; init; } = string.Empty;
	public string Initials { get; init; } = string.Empty;
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
	public int Id { get; init; }
	public int ProductId { get; init; }
	public string Code { get; init; } = string.Empty;
	public string Supplier { get; init; } = string.Empty;
	public string Date { get; init; } = string.Empty;
	public string ProductCount { get; init; } = string.Empty;
	public string Amount { get; init; } = string.Empty;
	public string Status { get; init; } = string.Empty;
	public Color StatusColor { get; init; } = Colors.Transparent;
	public string Note { get; init; } = string.Empty;
	public string Actor { get; init; } = string.Empty;
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
}

public sealed class RoleOption
{
	public string Name { get; init; } = string.Empty;
	public string Description { get; init; } = string.Empty;
	public bool IsSelected { get; init; }
}

public sealed class InternalUserItem
{
	public int Id { get; init; }
	public string Username { get; init; } = string.Empty;
	public string Email { get; init; } = string.Empty;
	public string Role { get; init; } = string.Empty;
	public bool IsActive { get; init; }
	public string Code => $"ND-{Id:0000}";
	// Đổi role kỹ thuật thành nhãn hiển thị.
	public string RoleLabel => Role switch
	{
		"manager" => "Manager",
		"staff" => "Staff",
		_ => Role
	};
	// Tạo trạng thái hiển thị của tài khoản nội bộ.
	public string StatusText => IsActive ? "Đang hoạt động" : "Đã khóa";
	public Color StatusColor => IsActive ? Colors.SeaGreen : Colors.Firebrick;
	// Đổi nhãn nút theo trạng thái khóa/mở.
	public string ToggleText => IsActive ? "Khóa" : "Mở khóa";
}

public sealed class PaymentOption
{
	public string Name { get; init; } = string.Empty;
	public bool IsSelected { get; init; }
}
