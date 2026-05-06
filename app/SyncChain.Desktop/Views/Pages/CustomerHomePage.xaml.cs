using SyncChain.Desktop.Models;
using SyncChain.Desktop.Services;

namespace SyncChain.Desktop.Views.Pages;

public partial class CustomerHomePage : ContentPage
{
	public CustomerHomePage()
	{
		InitializeComponent();
		BindingContext = this;
	}

	public IReadOnlyList<CustomerMetric> Metrics { get; } =
	[
		new("Đơn đang xử lý", "03", "1 đơn cần theo dõi", "ORD", Color.FromArgb("#2563EB")),
		new("Điểm tích lũy", "1,240", "Hạng bạc", "VIP", Color.FromArgb("#7C3AED")),
		new("Voucher", "05", "2 mã sắp hết hạn", "%", Color.FromArgb("#16A34A")),
		new("Hỗ trợ", "24/7", "Phản hồi nhanh", "CS", Color.FromArgb("#F59E0B"))
	];

	public IReadOnlyList<CustomerOrderCard> RecentOrders { get; } =
	[
		new("#ORD-2023-8942", "Đang chuẩn bị hàng", "14/10/2023", "1,450,000 đ", Color.FromArgb("#2563EB")),
		new("#ORD-2023-8939", "Hoàn tất", "13/10/2023", "5,200,000 đ", Color.FromArgb("#16A34A")),
		new("#ORD-2023-8935", "Hoàn tất", "13/10/2023", "2,780,000 đ", Color.FromArgb("#16A34A"))
	];

	public IReadOnlyList<CustomerAction> QuickActions { get; } =
	[
		new("Theo dõi đơn", "Xem trạng thái xử lý và vận chuyển theo thời gian thực.", "01", Color.FromArgb("#2563EB")),
		new("Mua lại", "Tạo lại đơn từ sản phẩm đã mua gần đây.", "02", Color.FromArgb("#16A34A")),
		new("Hỗ trợ", "Gửi yêu cầu đổi trả, bảo hành hoặc cập nhật giao hàng.", "03", Color.FromArgb("#7C3AED"))
	];

	public IReadOnlyList<CustomerPromotion> Promotions { get; } =
	[
		new("FREESHIP24", "Miễn phí vận chuyển cho đơn từ 500,000 đ", Color.FromArgb("#0F766E")),
		new("TVT10", "Giảm 10% cho phụ kiện trong tuần này", Color.FromArgb("#DC2626"))
	];

	public IReadOnlyList<ProductItem> SuggestedProducts => DemoData.Products.Take(4).ToArray();

	private async void OnTrackOrderClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync(nameof(OrderDetailPage));
	}

	private void OnLogoutClicked(object? sender, EventArgs e)
	{
		App.ShowLogin();
	}
}

public sealed record CustomerMetric(string Title, string Value, string Subtitle, string Icon, Color Accent);

public sealed record CustomerOrderCard(string Code, string Status, string Date, string Total, Color Accent);

public sealed record CustomerAction(string Title, string Description, string Icon, Color Accent);

public sealed record CustomerPromotion(string Code, string Description, Color Accent);
