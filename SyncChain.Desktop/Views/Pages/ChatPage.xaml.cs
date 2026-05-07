using SyncChain.Desktop.Models;
using SyncChain.Desktop.Services;

namespace SyncChain.Desktop.Views.Pages;

public partial class ChatPage : ContentPage
{
	// Cung cấp dữ liệu mẫu cho danh sách cuộc trò chuyện.
	public IReadOnlyList<ChatThread> Threads => DemoData.Threads;
	// Cung cấp dữ liệu mẫu cho nội dung tin nhắn.
	public IReadOnlyList<ChatMessage> Messages => DemoData.Messages;

	public ChatPage()
	{
		InitializeComponent();
		BindingContext = this;
	}
}
