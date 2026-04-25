using Microsoft.Maui.Graphics;
using SyncChain.Desktop.Models;

namespace SyncChain.Desktop.Services;

public static class DemoData
{
	public static IReadOnlyList<StatCard> DashboardStats { get; } =
	[
		new() { Title = "Tổng sản phẩm", Value = "1,284", Subtitle = "+12% trong 7 ngày", Icon = "□", Accent = Color.FromArgb("#2563EB") },
		new() { Title = "Tổng đơn hàng", Value = "452", Subtitle = "+5.4% theo ngày", Icon = "▣", Accent = Color.FromArgb("#0F766E") },
		new() { Title = "Đơn chờ xử lý", Value = "28", Subtitle = "Cần khóa tồn trước", Icon = "!", Accent = Color.FromArgb("#F59E0B") },
		new() { Title = "Sắp hết hàng", Value = "14", Subtitle = "Cảnh báo realtime", Icon = "▲", Accent = Color.FromArgb("#DC2626") },
		new() { Title = "Nhập chờ duyệt", Value = "08", Subtitle = "Từ nhà máy", Icon = "↗", Accent = Color.FromArgb("#7C3AED") }
	];

	public static IReadOnlyList<AlertItem> LowStockAlerts { get; } =
	[
		new() { Name = "Đồng hồ thông minh S3", Code = "WTC-0012", StockText = "Còn 02 / hạn mức 10", Accent = Color.FromArgb("#DC2626") },
		new() { Name = "Tai nghe Studio Pro", Code = "AUD-0982", StockText = "Còn 05 / hạn mức 20", Accent = Color.FromArgb("#F97316") },
		new() { Name = "Classic Gen 2", Code = "WTC-0045", StockText = "Còn 08 / hạn mức 15", Accent = Color.FromArgb("#F59E0B") }
	];

	public static IReadOnlyList<ActivityItem> Activities { get; } =
	[
		new() { Title = "Đơn #ORD-5621 đã hoàn tất và gửi email xác nhận", Time = "5 phút trước", Icon = "✓", Accent = Color.FromArgb("#16A34A") },
		new() { Title = "Phiếu nhập #IMP-0023 được tạo từ nhà máy", Time = "28 phút trước", Icon = "↗", Accent = Color.FromArgb("#2563EB") },
		new() { Title = "Tài khoản Trần Thị B được cấp quyền Manager", Time = "2 giờ trước", Icon = "+", Accent = Color.FromArgb("#F59E0B") },
		new() { Title = "Đơn #ORD-5602 bị hủy do khách đổi phương thức thanh toán", Time = "4 giờ trước", Icon = "×", Accent = Color.FromArgb("#DC2626") }
	];

	public static IReadOnlyList<BridgeItem> Bridges { get; } =
	[
		new() { Title = "Realtime order tracking bridge", Description = "Sẵn điểm nối WebSocket/SignalR để đẩy trạng thái đóng gói, vận chuyển và xác nhận giao hàng.", Status = "Frontend ready", Accent = Color.FromArgb("#2563EB") },
		new() { Title = "Online payment confirmation bridge", Description = "Khung UI cho COD, chuyển khoản, ví điện tử và vùng callback xác nhận giao dịch realtime.", Status = "Mock provider", Accent = Color.FromArgb("#16A34A") },
		new() { Title = "Registration and notification bridge", Description = "Form đăng ký, email xác thực, cảnh báo lỗi đơn và thông báo tức thời cho khách hàng.", Status = "Awaiting API", Accent = Color.FromArgb("#7C3AED") }
	];

	public static IReadOnlyList<ProductItem> Products { get; } =
	[
		new() { Code = "WTC-0012", Name = "Đồng hồ thông minh S3", Description = "Thiết bị theo dõi sức khỏe và thông báo thời gian thực.", Price = "890,000 đ", Stock = "02", BadgeText = "Đang bán", BadgeColor = Color.FromArgb("#16A34A"), Initials = "S3" },
		new() { Code = "AUD-0982", Name = "Tai nghe Studio Pro", Description = "Tai nghe chống ồn chủ động dành cho khách lẻ cao cấp.", Price = "450,000 đ", Stock = "05", BadgeText = "Đang bán", BadgeColor = Color.FromArgb("#16A34A"), Initials = "AU" },
		new() { Code = "WTC-0045", Name = "Classic Gen 2", Description = "Đồng hồ cổ điển, tồn ổn định cho kênh đại lý.", Price = "1,250,000 đ", Stock = "58", BadgeText = "Mới", BadgeColor = Color.FromArgb("#2563EB"), Initials = "CG" },
		new() { Code = "PER-1108", Name = "Mini Rose", Description = "Nước hoa mini cho combo bán lẻ.", Price = "320,000 đ", Stock = "08", BadgeText = "Sắp hết", BadgeColor = Color.FromArgb("#F59E0B"), Initials = "MR" },
		new() { Code = "SHO-2031", Name = "Runner", Description = "Giày thể thao bán tốt ở kênh online.", Price = "690,000 đ", Stock = "120", BadgeText = "Đang bán", BadgeColor = Color.FromArgb("#16A34A"), Initials = "RN" },
		new() { Code = "GLA-7712", Name = "Urban Shade", Description = "Kính mát thời trang đã tạm ngưng bán.", Price = "250,000 đ", Stock = "00", BadgeText = "Ngừng bán", BadgeColor = Color.FromArgb("#DC2626"), Initials = "US" }
	];

	public static IReadOnlyList<InventoryEvent> InventoryEvents { get; } =
	[
		new() { Date = "14/10/2023", Type = "Xuất kho", Quantity = "-1", Actor = "Nguyễn Văn A", Note = "Đơn #ORD-8942", Accent = Color.FromArgb("#DC2626") },
		new() { Date = "10/10/2023", Type = "Nhập kho", Quantity = "+20", Actor = "Lê Minh Tuấn", Note = "Phiếu nhập #IMP-0021", Accent = Color.FromArgb("#16A34A") },
		new() { Date = "02/10/2023", Type = "Xuất kho", Quantity = "-3", Actor = "Hệ thống", Note = "Bán hàng online", Accent = Color.FromArgb("#DC2626") }
	];

	public static IReadOnlyList<OrderItem> Orders { get; } =
	[
		new() { Code = "#ORD-2023-8942", Customer = "Nguyễn Văn Lâm", Email = "lam.nv@gmail.com", CreatedAt = "14/10/2023 10:24", Total = "1,450,000 đ", Status = "Đã xử lý", StatusColor = Color.FromArgb("#2563EB"), Initials = "NL" },
		new() { Code = "#ORD-2023-8941", Customer = "Trần Thị Hoa", Email = "hoa.tran@gmail.com", CreatedAt = "14/10/2023 09:15", Total = "890,000 đ", Status = "Đang xử lý", StatusColor = Color.FromArgb("#F59E0B"), Initials = "TH" },
		new() { Code = "#ORD-2023-8939", Customer = "Phạm Hoàng Nam", Email = "nam.pham@gmail.com", CreatedAt = "13/10/2023 16:45", Total = "5,200,000 đ", Status = "Hoàn tất", StatusColor = Color.FromArgb("#16A34A"), Initials = "PN" },
		new() { Code = "#ORD-2023-8938", Customer = "Lê Minh", Email = "leminh.cm@gmail.com", CreatedAt = "13/10/2023 14:30", Total = "120,000 đ", Status = "Hủy", StatusColor = Color.FromArgb("#DC2626"), Initials = "LM" },
		new() { Code = "#ORD-2023-8935", Customer = "Quách Tĩnh", Email = "tinh.quach@gmail.com", CreatedAt = "13/10/2023 11:10", Total = "2,780,000 đ", Status = "Hoàn tất", StatusColor = Color.FromArgb("#16A34A"), Initials = "QT" }
	];

	public static IReadOnlyList<LineItem> OrderLines { get; } =
	[
		new() { Name = "Đồng hồ thông minh S3", Variant = "Màu đen / Size M", Quantity = "x1", Price = "890,000 đ", Initials = "S3" },
		new() { Name = "Tai nghe Studio Pro", Variant = "Màu trắng", Quantity = "x1", Price = "450,000 đ", Initials = "AU" },
		new() { Name = "Dây sạc USB-C", Variant = "Dài 1 mét", Quantity = "x2", Price = "110,000 đ", Initials = "UC" }
	];

	public static IReadOnlyList<TimelineItem> Timeline { get; } =
	[
		new() { Title = "Đơn hàng đã được tạo và khóa tồn kho", Time = "14/10/2023 - 10:24", State = "done", Accent = Color.FromArgb("#16A34A") },
		new() { Title = "Thanh toán online thành công", Time = "14/10/2023 - 10:25", State = "done", Accent = Color.FromArgb("#16A34A") },
		new() { Title = "Chuẩn bị hàng và gán vận đơn", Time = "14/10/2023 - 10:40", State = "active", Accent = Color.FromArgb("#2563EB") },
		new() { Title = "Đang vận chuyển và đẩy tracking realtime", Time = "Đang chờ", State = "waiting", Accent = Color.FromArgb("#CBD5E1") },
		new() { Title = "Khách/đơn vị vận chuyển xác nhận đã nhận", Time = "Đang chờ", State = "waiting", Accent = Color.FromArgb("#CBD5E1") }
	];

	public static IReadOnlyList<ImportItem> Imports { get; } =
	[
		new() { Code = "#IMP-2023-0023", Supplier = "Công ty TNHH TechWorld", Date = "15/10/2023", ProductCount = "12 sản phẩm", Amount = "24,500,000 đ", Status = "Chờ duyệt", StatusColor = Color.FromArgb("#F59E0B") },
		new() { Code = "#IMP-2023-0022", Supplier = "Kho phụ kiện Sài Gòn", Date = "14/10/2023", ProductCount = "08 sản phẩm", Amount = "8,900,000 đ", Status = "Đã nhập", StatusColor = Color.FromArgb("#16A34A") },
		new() { Code = "#IMP-2023-0021", Supplier = "Global Mobile Parts", Date = "13/10/2023", ProductCount = "20 sản phẩm", Amount = "42,700,000 đ", Status = "Đang xử lý", StatusColor = Color.FromArgb("#2563EB") },
		new() { Code = "#IMP-2023-0020", Supplier = "Smart Home Việt Nam", Date = "12/10/2023", ProductCount = "05 sản phẩm", Amount = "5,200,000 đ", Status = "Từ chối", StatusColor = Color.FromArgb("#DC2626") }
	];

	public static IReadOnlyList<SupplierItem> Suppliers { get; } =
	[
		new() { Name = "TechWorld", Orders = "48 phiếu nhập", Amount = "124,5tr", Initial = "T", Accent = Color.FromArgb("#2563EB") },
		new() { Name = "Saigon Accessories", Orders = "36 phiếu nhập", Amount = "89,2tr", Initial = "S", Accent = Color.FromArgb("#16A34A") },
		new() { Name = "Global Parts", Orders = "22 phiếu nhập", Amount = "76,8tr", Initial = "G", Accent = Color.FromArgb("#7C3AED") }
	];

	public static IReadOnlyList<LogItem> Logs { get; } =
	[
		new() { Title = "Hoàn tất đơn hàng #ORD-2023-8942", Description = "Nguyễn Văn A đã chuyển trạng thái đơn sang Đã xử lý.", Time = "5 phút trước", Tag = "Đơn hàng", Icon = "✓", Accent = Color.FromArgb("#16A34A") },
		new() { Title = "Thêm sản phẩm mới", Description = "Lê Minh Tuấn thêm sản phẩm Classic Gen 2.", Time = "28 phút trước", Tag = "Sản phẩm", Icon = "+", Accent = Color.FromArgb("#2563EB") },
		new() { Title = "Cảnh báo tồn kho thấp", Description = "Tai nghe Studio Pro chỉ còn 05 sản phẩm trong kho.", Time = "1 giờ trước", Tag = "Kho hàng", Icon = "!", Accent = Color.FromArgb("#F59E0B") },
		new() { Title = "Tạo tài khoản nhân viên", Description = "Admin đã tạo tài khoản mới cho Trần Thị B.", Time = "2 giờ trước", Tag = "Người dùng", Icon = "●", Accent = Color.FromArgb("#7C3AED") },
		new() { Title = "Hủy đơn hàng #ORD-2023-8938", Description = "Đơn hàng bị hủy theo yêu cầu của khách hàng.", Time = "4 giờ trước", Tag = "Hủy đơn", Icon = "×", Accent = Color.FromArgb("#DC2626") }
	];

	public static IReadOnlyList<ChatThread> Threads { get; } =
	[
		new() { Name = "Lê Minh Tuấn", Preview = "Đã xử lý đơn #8942", Time = "2m", Initials = "LM", IsActive = true },
		new() { Name = "Nguyễn Thảo", Preview = "Đã nhập hàng mới", Time = "15m", Initials = "NT", IsActive = false },
		new() { Name = "Quang Anh", Preview = "Check tồn kho giúp mình", Time = "1h", Initials = "QA", IsActive = false }
	];

	public static IReadOnlyList<ChatMessage> Messages { get; } =
	[
		new() { Content = "Đơn hàng #8942 đã xử lý xong nhé.", Time = "10:24", IsOutgoing = false },
		new() { Content = "Ok, mình sẽ giao cho bên vận chuyển.", Time = "10:25", IsOutgoing = true },
		new() { Content = "Nhớ kiểm tra lại sản phẩm trước khi gửi.", Time = "10:26", IsOutgoing = false }
	];

	public static IReadOnlyList<RoleOption> Roles { get; } =
	[
		new() { Name = "Admin", Description = "Toàn quyền hệ thống, passkey bắt buộc.", IsSelected = true },
		new() { Name = "Manager", Description = "Quản lý tồn kho, đơn hàng và nhập hàng.", IsSelected = false },
		new() { Name = "Staff", Description = "Xử lý đơn, chat nội bộ, theo dõi vận chuyển.", IsSelected = false }
	];

	public static IReadOnlyList<PaymentOption> Payments { get; } =
	[
		new() { Name = "Tiền mặt / COD", IsSelected = true },
		new() { Name = "Chuyển khoản", IsSelected = false },
		new() { Name = "Ví điện tử", IsSelected = false }
	];
}
