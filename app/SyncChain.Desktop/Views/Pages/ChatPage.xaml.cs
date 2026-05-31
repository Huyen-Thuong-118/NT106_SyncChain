using SyncChain.Desktop.Models;

namespace SyncChain.Desktop.Views.Pages;

public partial class ChatPage : ContentPage
{
	public IReadOnlyList<ChatThread> Threads { get; } =
	[
		new() { Name = "Hỗ trợ kỹ thuật", Preview = "Hệ thống đang hoạt động bình thường.", Time = "10:30", Initials = "HT", IsActive = true },
		new() { Name = "Kho hàng", Preview = "Đã xác nhận nhập 50 đơn vị.", Time = "09:15", Initials = "KH", IsActive = false },
		new() { Name = "Bán hàng", Preview = "Khách yêu cầu đổi trả.", Time = "Hôm qua", Initials = "BH", IsActive = false }
	];

	public IReadOnlyList<ChatMessage> Messages { get; } =
	[
		new() { Content = "Chào bạn, hệ thống SyncChain đang hoạt động ổn định.", Time = "10:30", IsOutgoing = false, IsDateDivider = false },
		new() { Content = "Cảm ơn, tôi muốn kiểm tra tồn kho.", Time = "10:31", IsOutgoing = true, IsDateDivider = false },
		new() { Content = "Tồn kho hiện tại: 1,847 sản phẩm. 342 đang trên biển.", Time = "10:32", IsOutgoing = false, IsDateDivider = false }
	];

	public ChatPage()
	{
		InitializeComponent();
		BindingContext = this;
	}
}
