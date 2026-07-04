using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Graphics;

namespace SyncChain.Desktop.Models;

public sealed class StatCard
{
	public string Title { get; init; } = string.Empty;
	public string Value { get; init; } = string.Empty;
	public string Subtitle { get; init; } = string.Empty;
	public string Icon { get; init; } = string.Empty;
	public Color Accent { get; init; } = Colors.Transparent;
	public Color Background { get; init; } = Colors.White;
	public string IconGlyph { get; init; } = string.Empty;
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

public sealed class InventoryDistributionItem
{
	public string CategoryName { get; init; } = string.Empty;
	public int StockQuantity { get; init; }
	public string QuantityText => $"{StockQuantity:N0} SP";
}

public sealed class OrderTrendPoint
{
	public string Label { get; init; } = string.Empty;
	public int Completed { get; init; }
	public int Processing { get; init; }
}

public sealed class InventorySlice
{
	public string Label { get; init; } = string.Empty;
	public int Quantity { get; init; }
	public double Percent { get; init; }
	public Color Color { get; init; } = Colors.Transparent;
	public string PercentText => $"{Percent:0.#}%";
	public string QuantityText => $"{Quantity:N0} SP";
}

public sealed class QuickActionItem
{
	public string Title { get; init; } = string.Empty;
	public string Icon { get; init; } = string.Empty;
	public string Route { get; init; } = string.Empty;
}

public sealed class ProductItem
{
	public int Id { get; init; }
	public string Code { get; init; } = string.Empty;
	public string Name { get; init; } = string.Empty;
	public string Description { get; init; } = string.Empty;
	public string ImageUrl { get; init; } = string.Empty;
	public string Category { get; init; } = string.Empty;
	public string Price { get; init; } = string.Empty;
	public string Stock { get; init; } = string.Empty;
	public string BadgeText { get; init; } = string.Empty;
	public Color BadgeColor { get; init; } = Colors.Transparent;
	public string Initials { get; init; } = string.Empty;
	public string PerformanceText { get; init; } = string.Empty;
	public string PerformanceIcon { get; init; } = string.Empty;
	public Color PerformanceColor { get; init; } = Colors.Transparent;
	public double HealthProgress { get; init; }
	public string ActionText { get; init; } = string.Empty;
}

public sealed class ProductCategoryFilterItem
{
	public int? CategoryId { get; init; }
	public bool IsAll { get; init; }
	public string Name { get; init; } = string.Empty;
	public string DisplayText { get; init; } = string.Empty;

	public override string ToString() => DisplayText;
}

public sealed class PageButtonItem
{
	public int PageNumber { get; init; }
	public string Text { get; init; } = string.Empty;
	public bool IsCurrent { get; init; }
	public Color BackgroundColor => IsCurrent ? Color.FromArgb("#213145") : Colors.Transparent;
	public Color TextColor => IsCurrent ? Colors.White : Color.FromArgb("#213145");
}

public sealed class ProductImageItem
{
	public string Url { get; init; } = string.Empty;
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
	public string ProductSummary { get; init; } = string.Empty;
	public int ConcurrencyVersion { get; init; }

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
	public bool ShowConnector { get; set; } = true;
}

public sealed class LineItem
{
	public string Name { get; init; } = string.Empty;
	public string Variant { get; init; } = string.Empty;
	public string Quantity { get; init; } = string.Empty;
	public string Price { get; init; } = string.Empty;
	public string Initials { get; init; } = string.Empty;
}

public sealed class CreateOrderLine : INotifyPropertyChanged
{
	public int ProductId { get; init; }
	public string Name { get; init; } = string.Empty;
	public int Stock { get; init; }
	private int _quantity = 1;
	public int Quantity
	{
		get => _quantity;
		set
		{
			if (_quantity == value) return;
			_quantity = value;
			OnPropertyChanged();
			OnPropertyChanged(nameof(QuantityText));
			OnPropertyChanged(nameof(LineTotalText));
		}
	}
	public decimal UnitPrice { get; init; }
	public string Initials { get; init; } = string.Empty;
	public string StockText => $"Tồn kho: {Stock}";
	public string QuantityText => Quantity.ToString("N0");
	public string LineTotalText => $"{UnitPrice * Quantity:N0} đ";
	public event PropertyChangedEventHandler? PropertyChanged;

	private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class ProductSelectionItem : INotifyPropertyChanged
{
	public int ProductId { get; init; }
	public string Name { get; init; } = string.Empty;
	public string ImageUrl { get; init; } = string.Empty;
	public string Initials { get; init; } = string.Empty;
	public decimal UnitPrice { get; init; }
	public int Stock { get; init; }
	private int _selectedQuantity;
	public int SelectedQuantity
	{
		get => _selectedQuantity;
		set
		{
			if (_selectedQuantity == value) return;
			_selectedQuantity = value;
			OnPropertyChanged();
			OnPropertyChanged(nameof(IsSelected));
			OnPropertyChanged(nameof(IsNotSelected));
			OnPropertyChanged(nameof(SelectedQuantityText));
		}
	}
	public bool IsSelected => SelectedQuantity > 0;
	public bool IsNotSelected => !IsSelected;
	public string PriceText => $"{UnitPrice:N0} đ";
	public string StockText => $"Còn lại: {Stock:N0}";
	public string SelectedQuantityText => SelectedQuantity.ToString("N0");
	public event PropertyChangedEventHandler? PropertyChanged;

	private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class ImportItem
{
	public int Id { get; init; }
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

public sealed class WarehouseReceiptApi
{
	public int MaPhieuNhap { get; set; }
	public string SoPhieu { get; set; } = string.Empty;
	public string TenNguonNhap { get; set; } = string.Empty;
	public string DiaChiNguonNhap { get; set; } = string.Empty;
	public string NguoiLienHe { get; set; } = string.Empty;
	public string GhiChu { get; set; } = string.Empty;
	public string TrangThai { get; set; } = string.Empty;
	public DateTime NgayTao { get; set; }
	public DateTime? NgayDuyet { get; set; }
	public DateTime? NgayHoanTat { get; set; }
	public int MaNguoiTao { get; set; }
	public int? MaNguoiDuyet { get; set; }
	public decimal TongTien { get; set; }
	public List<WarehouseReceiptItemApi> ChiTiet { get; set; } = new();
}

public sealed class WarehouseReceiptItemApi
{
	public int MaChiTiet { get; set; }
	public int MaSanPham { get; set; }
	public string TenSanPham { get; set; } = string.Empty;
	public int SoLuong { get; set; }
	public decimal DonGiaNhap { get; set; }
	public decimal ThanhTien { get; set; }
}

public sealed class ReceiptProductOption
{
	public int ProductId { get; init; }
	public string Name { get; init; } = string.Empty;
	public decimal CurrentCost { get; init; }
	public string DisplayText => $"{Name} · giá hiện tại {CurrentCost:N0} đ";
}

public sealed class ReceiptDraftLine
{
	public int ProductId { get; init; }
	public string ProductName { get; init; } = string.Empty;
	public int Quantity { get; set; } = 1;
	public decimal UnitCost { get; set; }
	public decimal LineTotal => Quantity * UnitCost;
	public string LineTotalText => $"{LineTotal:N0} đ";
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
	public long Id { get; init; }
	public string Title { get; init; } = string.Empty;
	public string Description { get; init; } = string.Empty;
	public string Time { get; init; } = string.Empty;
	public string Tag { get; init; } = string.Empty;
	public string Icon { get; init; } = string.Empty;
	public Color Accent { get; init; } = Colors.Transparent;
	public string ResultText { get; init; } = string.Empty;
}

public sealed class AuditLogPageApi
{
	public List<AuditLogApi> Items { get; set; } = new();
	public int Page { get; set; }
	public int PageSize { get; set; }
	public int TotalItems { get; set; }
	public int TotalPages { get; set; }
}

public sealed class AuditLogApi
{
	public long Id { get; set; }
	public int? UserId { get; set; }
	public string Username { get; set; } = string.Empty;
	public string Role { get; set; } = string.Empty;
	public string Action { get; set; } = string.Empty;
	public string EntityType { get; set; } = string.Empty;
	public string? EntityId { get; set; }
	public string Result { get; set; } = string.Empty;
	public string Before { get; set; } = "{}";
	public string After { get; set; } = "{}";
	public string Metadata { get; set; } = "{}";
	public DateTime Timestamp { get; set; }
	public string TraceId { get; set; } = string.Empty;
	public string IpAddress { get; set; } = string.Empty;
	public string UserAgent { get; set; } = string.Empty;
}

public sealed class ChatThread
{
	public int ConversationId { get; set; }
	public int OtherUserId { get; set; }
	public bool IsGroup { get; set; }
	public string Name { get; set; } = string.Empty;
	public string Preview { get; set; } = string.Empty;
	public string Time { get; set; } = string.Empty;
	public string Initials { get; set; } = string.Empty;
	public bool IsActive { get; set; }
	public string Role { get; set; } = string.Empty;
	public string AvatarUrl { get; set; } = string.Empty;
	public int UnreadCount { get; set; }
}

public sealed class ChatMessage
{
	public long MessageId { get; set; }
	public int ConversationId { get; set; }
	public int SenderId { get; set; }
	public int ReceiverId { get; set; }
	public string Content { get; set; } = string.Empty;
	public string Time { get; set; } = string.Empty;
	public string MessageType { get; set; } = "text";
	public string FileName { get; set; } = string.Empty;
	public string FileUrl { get; set; } = string.Empty;
	public string CallStatus { get; set; } = string.Empty;
	public int? CallDurationSeconds { get; set; }
	public bool IsPinned { get; set; }
	public bool IsRecalled { get; set; }
	public string Reaction { get; set; } = string.Empty;
	public bool IsRead { get; set; }
	public ChatPollApi? Poll { get; set; }
	public bool IsOutgoing { get; set; }
	public bool IsDateDivider { get; set; }
	public string StatusText => IsRead && IsOutgoing ? "Da doc" : string.Empty;
	public string PinText => IsPinned ? "PIN" : string.Empty;
	public bool IsImage => MessageType == "image" && !string.IsNullOrWhiteSpace(FileUrl);
	public bool IsVideo => MessageType == "video" && !string.IsNullOrWhiteSpace(FileUrl);
	public bool IsFileCard => MessageType == "file";
	public bool IsMedia => IsImage || IsVideo;
	public bool IsPoll => MessageType == "poll" && Poll != null;
	public bool IsTextVisible => IsRecalled || MessageType is not ("image" or "video" or "file");
	public string ReactionText => string.IsNullOrWhiteSpace(Reaction) ? string.Empty : Reaction;
	public string FileSizeText
	{
		get
		{
			try
			{
				if (string.IsNullOrWhiteSpace(FileUrl) || !File.Exists(FileUrl))
					return string.Empty;
				var bytes = new FileInfo(FileUrl).Length;
				return bytes < 1024 * 1024
					? $"{bytes / 1024d:0.##} KB"
					: $"{bytes / (1024d * 1024d):0.##} MB";
			}
			catch
			{
				return string.Empty;
			}
		}
	}
	public string DisplayText => MessageType switch
	{
		_ when IsRecalled => "Tin nhan da duoc thu hoi",
		"file" => string.IsNullOrWhiteSpace(FileName) ? Content : FileName,
		"image" => string.IsNullOrWhiteSpace(FileName) ? Content : FileName,
		"video" => string.IsNullOrWhiteSpace(FileName) ? Content : FileName,
		"poll" => Poll?.Question ?? Content,
		"call" => CallStatus == "missed" ? "Da nho cuoc goi" : "Cuoc goi",
		_ => Content
	};
	public string DetailText => MessageType switch
	{
		"file" => "Tep dinh kem",
		"image" => FileSizeText,
		"video" => FileSizeText,
		"call" => CallDurationSeconds.HasValue ? $"{CallDurationSeconds.Value} giay" : Time,
		_ => Time
	};
	public bool IsCall => MessageType == "call";
}

public sealed class ChatUserApi
{
	public int UserId { get; set; }
	public string Username { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public string Role { get; set; } = string.Empty;
}

public sealed class ChatConversationApi
{
	public int ConversationId { get; set; }
	public bool IsGroup { get; set; }
	public string Title { get; set; } = string.Empty;
	public string AvatarUrl { get; set; } = string.Empty;
	public ChatUserApi OtherUser { get; set; } = new();
	public List<ChatUserApi> Participants { get; set; } = new();
	public string LastMessage { get; set; } = string.Empty;
	public string LastMessageType { get; set; } = "text";
	public DateTime? LastMessageAt { get; set; }
	public int UnreadCount { get; set; }
}

public sealed class ChatMessageApi
{
	public long MessageId { get; set; }
	public int ConversationId { get; set; }
	public int SenderId { get; set; }
	public int ReceiverId { get; set; }
	public string Content { get; set; } = string.Empty;
	public string MessageType { get; set; } = "text";
	public string FileName { get; set; } = string.Empty;
	public string FileUrl { get; set; } = string.Empty;
	public string CallStatus { get; set; } = string.Empty;
	public int? CallDurationSeconds { get; set; }
	public bool IsPinned { get; set; }
	public bool IsRecalled { get; set; }
	public string Reaction { get; set; } = string.Empty;
	public ChatPollApi? Poll { get; set; }
	public DateTime SentAt { get; set; }
	public DateTime? ReadAt { get; set; }
}

public sealed class ChatPollApi
{
	public int PollId { get; set; }
	public string Question { get; set; } = string.Empty;
	public bool AllowMultipleChoices { get; set; }
	public bool AllowAddOptions { get; set; }
	public bool HideResultsUntilVoted { get; set; }
	public bool HideVoters { get; set; }
	public bool ResultsHidden { get; set; }
	public bool IsClosed { get; set; }
	public DateTime? EndsAt { get; set; }
	public List<ChatPollOptionApi> Options { get; set; } = new();
	public string ModeText => AllowMultipleChoices ? "Nhieu lua chon" : "Moi nguoi 1 lua chon";
	public string StateText => IsClosed ? "Da khoa" : EndsAt.HasValue ? $"Ket thuc {EndsAt.Value.ToLocalTime():dd/MM HH:mm}" : "Dang mo";
}

public sealed class ChatPollOptionApi
{
	public int OptionId { get; set; }
	public string Text { get; set; } = string.Empty;
	public int VoteCount { get; set; }
	public bool VotedByMe { get; set; }
	public List<ChatPollVoterApi> Voters { get; set; } = new();
	public string VoteMarker => VotedByMe ? "●" : "○";
	public string VoteText => VoteCount < 0 ? "An ket qua" : $"{VoteCount} phieu";
	public string DisplayText => VotedByMe ? $"{Text} - {VoteText} - da chon" : $"{Text} - {VoteText}";
	public string VoterSummary => Voters.Count == 0 ? string.Empty : string.Join(", ", Voters.Select(x => x.Username));
}

public sealed class ChatPollVoterApi
{
	public int UserId { get; set; }
	public string Username { get; set; } = string.Empty;
	public string Initials { get; set; } = string.Empty;
}

public sealed class ChatConversationInfoApi
{
	public ChatConversationApi Conversation { get; set; } = new();
	public List<ChatMessageApi> PinnedMessages { get; set; } = new();
	public List<ChatMessageApi> MediaFiles { get; set; } = new();
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

public sealed class LoginResponseApi
{
	public string Token { get; set; } = string.Empty;
	public LoginUserApi? User { get; set; }
}

public sealed class LoginUserApi
{
	public int MaNguoiDung { get; set; }
	public string TenDangNhap { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
	public string Role { get; set; } = string.Empty;
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
	public int? MaDanhMuc { get; set; }
	public ProductCategoryApi? DanhMuc { get; set; }
	public int DaBanThangNay { get; set; }
	public int DaBanThangTruoc { get; set; }
	public decimal HieuSuatPhanTram { get; set; }
}

public sealed class ProductCategoryApi
{
	public int MaDanhMuc { get; set; }
	public string TenDanhMuc { get; set; } = string.Empty;
	public string MoTa { get; set; } = string.Empty;
	public bool IsActive { get; set; }
}

public sealed class CategoryApi
{
	public int MaDanhMuc { get; set; }
	public string TenDanhMuc { get; set; } = string.Empty;
	public string MoTa { get; set; } = string.Empty;
	public bool IsActive { get; set; }
	public int ProductCount { get; set; }

	public override string ToString() => TenDanhMuc;
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
	public int ConcurrencyVersion { get; set; }
	public string CustomerName { get; set; } = string.Empty;
	public string CustomerEmail { get; set; } = string.Empty;
	public string SoDienThoai { get; set; } = string.Empty;
	public string DiaChiGiaoHang { get; set; } = string.Empty;
	public string TinhThanh { get; set; } = string.Empty;
	public string PhuongXa { get; set; } = string.Empty;
	public string LoaiDichVu { get; set; } = string.Empty;
	public decimal TrongLuongKg { get; set; }
	public List<string> ProductNames { get; set; } = new();
	public int ProductCount { get; set; }
	public ShippingApi? Shipping { get; set; }
	public decimal ProductTotal { get; set; }
}

public sealed class OrderDetailApi
{
	public int MaDonHang { get; set; }
	public int MaNguoiDung { get; set; }
	public decimal TongTien { get; set; }
	public DateTime NgayTao { get; set; }
	public string TrangThai { get; set; } = string.Empty;
	public int ConcurrencyVersion { get; set; }
	public string CustomerName { get; set; } = string.Empty;
	public string CustomerEmail { get; set; } = string.Empty;
	public string SoDienThoai { get; set; } = string.Empty;
	public string DiaChiGiaoHang { get; set; } = string.Empty;
	public string TinhThanh { get; set; } = string.Empty;
	public string PhuongXa { get; set; } = string.Empty;
	public string LoaiDichVu { get; set; } = string.Empty;
	public decimal TrongLuongKg { get; set; }
	public string GhiChu { get; set; } = string.Empty;
	public List<ChiTietDonHangApi> Details { get; set; } = new();
	public ShippingApi? Shipping { get; set; }
}

public sealed class ShippingApi
{
	public int ShippingId { get; set; }
	public string Carrier { get; set; } = string.Empty;
	public string? TrackingNumber { get; set; }
	public decimal ShippingFee { get; set; }
	public string ShippingStatus { get; set; } = string.Empty;
	public DateTime? EstimatedDeliveryAt { get; set; }
	public DateTime? DeliveredAt { get; set; }
	public int ConcurrencyVersion { get; set; }
}

public sealed class DeliveryEstimateApi
{
	public string Address { get; set; } = string.Empty;
	public string Warehouse { get; set; } = string.Empty;
	public DateTime EarliestDelivery { get; set; }
	public DateTime LatestDelivery { get; set; }
	public int DeliveryDays { get; set; }
	public decimal ShippingFee { get; set; }
	public int ConfidencePercent { get; set; }
	public int EstimatedDistanceKm { get; set; }
	public string AreaType { get; set; } = string.Empty;
	public string WarehouseProcessing { get; set; } = string.Empty;
	public string TransitTime { get; set; } = string.Empty;
	public string Factors { get; set; } = string.Empty;
	public string Assumption { get; set; } = string.Empty;
}

public sealed class ProvinceApi
{
	public int Code { get; set; }
	public string Name { get; set; } = string.Empty;
	public List<WardApi> Wards { get; set; } = new();

	public override string ToString() => Name;
}

public sealed class WardApi
{
	public int Code { get; set; }
	public string Name { get; set; } = string.Empty;

	public override string ToString() => Name;
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
	public int CurrentMonthSold { get; set; }
	public int PreviousMonthSold { get; set; }
	public decimal PerformancePercent { get; set; }
	public int ReviewCount { get; set; }
	public decimal AverageRating { get; set; }
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
	public DashboardOrdersApi? Orders { get; set; }
	public DashboardRevenueApi? Revenue { get; set; }
	public DashboardInventoryApi? Inventory { get; set; }
	public DashboardShippingApi? Shipping { get; set; }

	public int TotalProducts => Inventory?.TotalProducts ?? 0;
	public int ActiveProducts => Inventory?.ActiveProducts ?? 0;
	public int LowStockProducts => Inventory?.LowStockProducts ?? 0;
	public int OutOfStockProducts => Inventory?.OutOfStockProducts ?? 0;
	public int TotalOrders => Orders?.Total ?? 0;
	public int PendingOrders => (Orders?.Pending ?? 0) + (Orders?.Processing ?? 0);
	public int CompletedOrders => Orders?.Done ?? 0;
	public int CancelledOrders => Orders?.Cancel ?? 0;
	public decimal TotalRevenue => Revenue?.Net ?? 0;
	public decimal TodayRevenue { get; set; }
	public List<TrendApi> Trend { get; set; } = new();
	public List<TopProductApi> TopProducts { get; set; } = new();
	public List<LowStockApi> LowStock { get; set; } = new();
	public List<RecentActivityApi> RecentActivities { get; set; } = new();
}

public sealed class DashboardOrdersApi
{
	public int Total { get; set; }
	public int Pending { get; set; }
	public int Processing { get; set; }
	public int Done { get; set; }
	public int Cancel { get; set; }
}

public sealed class DashboardRevenueApi
{
	public decimal Gross { get; set; }
	public decimal Net { get; set; }
	public decimal ShippingFee { get; set; }
	public decimal CancelledValue { get; set; }
}

public sealed class DashboardInventoryApi
{
	public int TotalProducts { get; set; }
	public int ActiveProducts { get; set; }
	public int LowStockProducts { get; set; }
	public int OutOfStockProducts { get; set; }
	public decimal TotalInventoryValue { get; set; }
}

public sealed class DashboardShippingApi
{
	public int Total { get; set; }
	public int Pending { get; set; }
	public int Ready { get; set; }
	public int PickedUp { get; set; }
	public int InTransit { get; set; }
	public int Delivered { get; set; }
	public int Failed { get; set; }
	public int Returned { get; set; }
	public int Cancelled { get; set; }
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

public sealed class InventoryReportApi
{
	public int TotalProducts { get; set; }
	public int TotalQuantity { get; set; }
	public decimal TotalInventoryValue { get; set; }
	public int LowStockThreshold { get; set; }
	public List<InventoryReportProductApi> LowStockProducts { get; set; } = new();
	public List<InventoryReportProductApi> OutOfStockProducts { get; set; } = new();
}

public sealed class InventoryReportProductApi
{
	public int ProductId { get; set; }
	public string ProductName { get; set; } = string.Empty;
	public int Quantity { get; set; }
	public string CategoryName { get; set; } = string.Empty;
	public decimal UnitCost { get; set; }
	public decimal InventoryValue { get; set; }
}

public sealed class CategoryReportPageApi
{
	public List<CategoryReportItemApi> Items { get; set; } = new();
}

public sealed class CategoryReportItemApi
{
	public int? CategoryId { get; set; }
	public string CategoryName { get; set; } = string.Empty;
	public int ProductCount { get; set; }
	public int ActiveProductCount { get; set; }
	public int StockQuantity { get; set; }
	public int SoldQuantity { get; set; }
	public decimal Revenue { get; set; }
}

public sealed class OrderReportApi
{
	public int Total { get; set; }
	public List<OrderDayReportItemApi> ByDay { get; set; } = new();
}

public sealed class OrderDayReportItemApi
{
	public DateTime Date { get; set; }
	public int Total { get; set; }
	public int Pending { get; set; }
	public int Processing { get; set; }
	public int Done { get; set; }
	public int Cancel { get; set; }
}

public sealed class TopProductReportApi
{
	public List<TopProductReportItemApi> Items { get; set; } = new();
}

public sealed class TopProductReportItemApi
{
	public int ProductId { get; set; }
	public string ProductName { get; set; } = string.Empty;
	public string CategoryName { get; set; } = string.Empty;
	public int SoldQuantity { get; set; }
	public decimal Revenue { get; set; }
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
	public string StatusText => IsActive ? "Đang hoạt động" : "Đã khóa";
	public Color StatusColor => IsActive ? Colors.Green : Colors.Red;
	public string ActiveActionText => IsActive ? "KHÓA" : "MỞ KHÓA";
}

public sealed class LoginHistoryApi
{
	public DateTime Timestamp { get; set; }
	public string Device { get; set; } = string.Empty;
	public string Location { get; set; } = string.Empty;
	public string IpAddress { get; set; } = string.Empty;
	public string TimeText => Timestamp.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");
	public string LocationText => string.IsNullOrWhiteSpace(IpAddress)
		? Location
		: $"{Location} · {IpAddress}";
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

	// Thông tin cá nhân khách hàng (module khách hàng).
	public string? Ho { get; set; }
	public string? Ten { get; set; }
	public string? SoDienThoai { get; set; }
	public string HoTen => $"{Ho} {Ten}".Trim();
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
