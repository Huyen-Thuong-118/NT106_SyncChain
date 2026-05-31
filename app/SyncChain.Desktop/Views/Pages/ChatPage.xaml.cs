using SyncChain.Desktop.Models;
using SyncChain.Desktop.Services;

namespace SyncChain.Desktop.Views.Pages;

public partial class ChatPage : ContentPage
{
	public IReadOnlyList<ChatThread> Threads => DemoData.Threads;
	public IReadOnlyList<ChatMessage> Messages => DemoData.Messages;

	public ChatPage()
	{
		InitializeComponent();
		BindingContext = this;
	}
}
