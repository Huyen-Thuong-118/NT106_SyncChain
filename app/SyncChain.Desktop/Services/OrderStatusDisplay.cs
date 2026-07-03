namespace SyncChain.Desktop.Services;

// Nguồn hiển thị trạng thái đơn DUY NHẤT cho toàn client, khớp đúng bộ trạng thái
// lowercase của backend (OrderStatuses.cs: pending/processing/shipping/done/cancel).
// Trước đây mỗi trang tự map (có nơi dùng PascalCase Draft/Approved... không khớp
// backend) khiến nhãn/màu/logic sai — gom về một chỗ để không lệch nữa.
public static class OrderStatusDisplay
{
	public static string Label(string? status) => (status ?? string.Empty) switch
	{
		"pending" => "Chờ duyệt",
		"processing" => "Đang xử lý",
		"shipping" => "Đang giao",
		"done" => "Hoàn thành",
		"cancel" => "Đã hủy",
		_ => string.IsNullOrWhiteSpace(status) ? "Không rõ" : status
	};

	public static Color Color(string? status) => (status ?? string.Empty) switch
	{
		"pending" => Microsoft.Maui.Graphics.Color.FromArgb("#dae2fd"),
		"processing" => Microsoft.Maui.Graphics.Color.FromArgb("#dbeafe"),
		"shipping" => Microsoft.Maui.Graphics.Color.FromArgb("#fff3cd"),
		"done" => Microsoft.Maui.Graphics.Color.FromArgb("#d3e5f1"),
		"cancel" => Microsoft.Maui.Graphics.Color.FromArgb("#ffdad6"),
		_ => Colors.LightGray
	};

	public static (string Text, Color Color) Badge(string? status) => (Label(status), Color(status));

	// Đơn đang "trong tiến trình" (khách cần theo dõi): chưa hoàn thành/hủy.
	public static bool IsActive(string? status) =>
		status is "pending" or "processing" or "shipping";
}
