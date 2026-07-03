using System.Net;

namespace SyncChain.Desktop.Services;

// Xử lý tập trung tình huống token thiếu/hết hạn (HTTP 401) cho các trang cần
// xác thực. Thay vì để HttpRequestException 401 ném ra từ async void OnAppearing
// và làm sập toàn bộ ứng dụng, các trang gọi các helper này để báo cho người dùng
// và quay lại màn hình đăng nhập một cách an toàn.
public static class SessionGuard
{
	// True nếu lỗi HTTP là do chưa xác thực (token thiếu, sai hoặc đã hết hạn).
	public static bool IsUnauthorized(this HttpRequestException ex)
		=> ex.StatusCode == HttpStatusCode.Unauthorized;

	// Thông báo phiên hết hạn rồi xoá session và đưa người dùng về trang đăng nhập.
	public static async Task HandleUnauthorizedAsync(this Page page)
	{
		await page.DisplayAlertAsync(
			"Phiên đăng nhập đã hết hạn",
			"Bạn chưa đăng nhập hoặc phiên đã hết hạn. Vui lòng đăng nhập lại để tiếp tục.",
			"Đăng nhập");

		// ShowLogin() đã tự ClearSession() và dừng SignalR nên trạng thái xác thực
		// được dọn sạch trước khi hiển thị lại LoginPage.
		App.ShowLogin();
	}
}
