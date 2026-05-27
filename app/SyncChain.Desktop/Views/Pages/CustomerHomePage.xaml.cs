using SyncChain.Desktop.Models;
using SyncChain.Desktop.Services;

namespace SyncChain.Desktop.Views.Pages;

public partial class CustomerHomePage : ContentPage
{
	private static readonly Color Sapphire = Color.FromArgb("#213145");
	private static readonly Color Blue = Color.FromArgb("#50616B");
	private static readonly Color Mist = Color.FromArgb("#5C647A");
	private static readonly Color Ice = Color.FromArgb("#B7C9D5");
	private static readonly Color Critical = Color.FromArgb("#BA1A1A");

	public CustomerHomePage()
	{
		InitializeComponent();
		BindingContext = this;
	}

	public IReadOnlyList<CustomerMetric> Metrics { get; } =
	[
		new("Đơn đang xử lý", "03", "1 đơn cần theo dõi", "ORD", Blue),
		new("Điểm tích lũy", "1,240", "Hạng bạc", "VIP", Mist),
		new("Voucher", "05", "2 mã sắp hết hạn", "VC", Ice),
		new("Hỗ trợ", "24/7", "Phản hồi nhanh", "CS", Sapphire)
	];

	public IReadOnlyList<CustomerOrderCard> RecentOrders { get; } =
	[
		new("#ORD-2023-8942", "Đang chuẩn bị hàng", "14/10/2023", "1,450,000 đ", Blue),
		new("#ORD-2023-8939", "Hoàn tất", "13/10/2023", "5,200,000 đ", Sapphire),
		new("#ORD-2023-8935", "Hoàn tất", "13/10/2023", "2,780,000 đ", Sapphire)
	];

	public IReadOnlyList<CustomerAction> QuickActions { get; } =
	[
		new("Theo dõi đơn", "Xem trạng thái xử lý và vận chuyển theo thời gian thực.", "01", Blue),
		new("Mua lại", "Tạo lại đơn từ sản phẩm đã mua gần đây.", "02", Sapphire),
		new("Hỗ trợ", "Gửi yêu cầu đổi trả, bảo hành hoặc cập nhật giao hàng.", "03", Mist)
	];

	public IReadOnlyList<CustomerPromotion> Promotions { get; } =
	[
		new("FREESHIP24", "Miễn phí vận chuyển cho đơn từ 500,000 đ", Blue),
		new("TVT10", "Giảm 10% cho phụ kiện trong tuần này", Critical)
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
