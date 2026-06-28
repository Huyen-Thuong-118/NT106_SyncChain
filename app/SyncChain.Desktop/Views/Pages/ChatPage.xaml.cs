using System.Collections.ObjectModel;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using SyncChain.Desktop.Models;
using SyncChain.Desktop.Services;

namespace SyncChain.Desktop.Views.Pages;

public partial class ChatPage : ContentPage
{
	private readonly HttpClient _http;
	private HubConnection? _hub;
	private ChatThread? _selectedThread;
	private string? _activeCallId;
	private int? _activeCallUserId;
	private DateTime? _callStartedAt;
	private string _callState = "idle";
	private ChatMessage? _replyingTo;
	private ChatMessage? _pollDetailsMessage;
	private int _scrollRequestVersion;

	public ObservableCollection<ChatThread> Threads { get; } = new();
	public ObservableCollection<ChatUserApi> SearchResults { get; } = new();
	public ObservableCollection<ChatMessage> Messages { get; } = new();
	public ObservableCollection<ChatMessage> MessageSearchResults { get; } = new();
	public ObservableCollection<ChatMessage> PinnedMessages { get; } = new();
	public ObservableCollection<ChatMessage> MediaItems { get; } = new();
	public ObservableCollection<ChatMessage> FileItems { get; } = new();
	public ObservableCollection<ChatPollOptionApi> PollDetailOptions { get; } = new();
	public ObservableCollection<ChatUserApi> GroupCandidates { get; } = new();
	public ObservableCollection<ChatUserApi> SelectedGroupMembers { get; } = new();
	public ObservableCollection<string> EmojiOptions { get; } = new(["😀", "😃", "😄", "😁", "😆", "🥹", "😅", "😂", "🤣", "😊", "😇", "🙂", "🙃", "😉", "😍", "🥰", "😘", "😗", "😚", "😋", "😎", "🤓", "😢", "😭", "😡", "👍", "👏", "🙏", "❤️", "🔥"]);

	public ChatPage() : this(ApiClientProvider.Client) { }

	public ChatPage(HttpClient http)
	{
		InitializeComponent();
		_http = http;
		BindingContext = this;
		Loaded += async (_, _) => await InitializeAsync();
		Unloaded += async (_, _) => await StopHubAsync();
	}

	private async Task InitializeAsync()
	{
		await LoadThreadsAsync();
		await StartHubAsync();
	}

	private async Task LoadThreadsAsync()
	{
		try
		{
			var conversations = await _http.GetFromJsonAsync<List<ChatConversationApi>>("api/chat/conversations") ?? [];
			Threads.Clear();
			foreach (var conversation in conversations)
				Threads.Add(ToThread(conversation));
		}
		catch (Exception ex)
		{
			ConversationSubtitleLabel.Text = $"Không tải được hộp thư: {ex.Message}";
		}
	}

	private async Task SearchUsersAsync()
	{
		var term = AccountSearchEntry.Text?.Trim();
		SearchResults.Clear();
		if (string.IsNullOrWhiteSpace(term))
			return;

		try
		{
			var results = await _http.GetFromJsonAsync<List<ChatUserApi>>(
				$"api/chat/users?search={Uri.EscapeDataString(term)}") ?? [];
			foreach (var user in results)
				SearchResults.Add(user);
		}
		catch (Exception ex)
		{
			ConversationSubtitleLabel.Text = $"Không tìm được tài khoản: {ex.Message}";
		}
	}

	private async Task StartHubAsync()
	{
		if (_hub != null || string.IsNullOrWhiteSpace(ApiClientProvider.Token))
			return;

		_hub = new HubConnectionBuilder()
			.WithUrl(ApiClientProvider.HubUrl, options =>
			{
				options.AccessTokenProvider = () => Task.FromResult<string?>(ApiClientProvider.Token);
			})
			.WithAutomaticReconnect()
			.Build();

		_hub.On<ChatMessageApi>("MessageReceived", message =>
		{
			MainThread.BeginInvokeOnMainThread(async () => await OnRealtimeMessageAsync(message));
		});
		_hub.On<CallEventPayload>("IncomingCall", payload =>
		{
			MainThread.BeginInvokeOnMainThread(() => ShowIncomingCall(payload.CallerId, payload.CallId));
		});
		_hub.On<CallEventPayload>("CallAccepted", payload =>
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				_callStartedAt = DateTime.UtcNow;
				SetCallState("in-call", payload.ReceiverId, "Cuộc gọi đang diễn ra");
			});
		});
		_hub.On<CallEventPayload>("CallRejected", payload =>
		{
			MainThread.BeginInvokeOnMainThread(async () =>
			{
				SetCallState("rejected", payload.ReceiverId, "Cuộc gọi bị từ chối");
				await LogCallAsync(payload.ReceiverId, "rejected", null);
			});
		});
		_hub.On<CallEventPayload>("CallEnded", payload =>
		{
			MainThread.BeginInvokeOnMainThread(async () =>
			{
				var duration = CallDurationSeconds();
				SetCallState("ended", payload.UserId, "Cuộc gọi đã kết thúc");
				await LogCallAsync(payload.UserId, "ended", duration);
			});
		});
		_hub.On<CallEventPayload>("CallBusy", payload =>
		{
			MainThread.BeginInvokeOnMainThread(async () =>
			{
				SetCallState("busy", payload.ReceiverId, "Người nhận đang bận");
				await LogCallAsync(payload.ReceiverId, "busy", null);
			});
		});

		try
		{
			await _hub.StartAsync();
			ConversationSubtitleLabel.Text = "Realtime đã kết nối";
		}
		catch (Exception ex)
		{
			ConversationSubtitleLabel.Text = $"Không kết nối realtime: {ex.Message}";
		}
	}

	private async Task StopHubAsync()
	{
		if (_hub == null)
			return;

		await _hub.DisposeAsync();
		_hub = null;
	}

	private async void OnAccountSearchChanged(object? sender, TextChangedEventArgs e)
	{
		if ((e.NewTextValue ?? string.Empty).Length >= 2)
			await SearchUsersAsync();
		else
			SearchResults.Clear();
	}

	private async void OnAccountSearchCompleted(object? sender, EventArgs e)
	{
		await SearchUsersAsync();
	}

	private void OnSearchFocused(object? sender, FocusEventArgs e)
	{
		InboxPanel.IsVisible = false;
		SearchPanel.IsVisible = true;
		AccountSearchEntry.Focus();
	}

	private void OnBackFromSearchClicked(object? sender, EventArgs e)
	{
		SearchPanel.IsVisible = false;
		InboxPanel.IsVisible = true;
		AccountSearchEntry.Text = string.Empty;
		SearchResults.Clear();
	}

	private async void OnSearchUserTapped(object? sender, TappedEventArgs e)
	{
		if (e.Parameter is not ChatUserApi user)
			return;

		var thread = Threads.FirstOrDefault(x => !x.IsGroup && x.OtherUserId == user.UserId)
			?? new ChatThread
			{
				OtherUserId = user.UserId,
				Name = user.Username,
				Role = user.Role,
				Initials = Initials(user.Username),
				Preview = "Bắt đầu trò chuyện"
			};
		await SelectThreadAsync(thread, createIfNeeded: true);
		SearchPanel.IsVisible = false;
		InboxPanel.IsVisible = true;
		AccountSearchEntry.Text = string.Empty;
		SearchResults.Clear();
	}

	private async void OnThreadTapped(object? sender, TappedEventArgs e)
	{
		if (e.Parameter is ChatThread thread)
			await SelectThreadAsync(thread, createIfNeeded: false);
	}

	private async Task SelectThreadAsync(ChatThread thread, bool createIfNeeded)
	{
		try
		{
			if (thread.ConversationId == 0 && createIfNeeded)
			{
				var response = await _http.PostAsJsonAsync("api/chat/conversations", new { OtherUserId = thread.OtherUserId });
				response.EnsureSuccessStatusCode();
				var dto = await response.Content.ReadFromJsonAsync<ChatConversationApi>();
				if (dto != null)
					thread = ToThread(dto);
			}

			if (thread.ConversationId == 0)
				return;

			_selectedThread = thread;
			foreach (var item in Threads)
				item.IsActive = item.ConversationId == thread.ConversationId;
			if (Threads.All(x => x.ConversationId != thread.ConversationId))
				Threads.Insert(0, thread);

			RefreshThreadsBinding();
			ConversationTitleLabel.Text = thread.Name;
			ConversationSubtitleLabel.Text = thread.IsGroup ? "Nhóm nội bộ" : $"{thread.Role} - đang sẵn sàng";
			InfoTitleLabel.Text = thread.Name;
			InfoAvatarLabel.Text = thread.Initials;
			PollButton.IsVisible = thread.IsGroup;
			RenameGroupInfoButton.IsVisible = thread.IsGroup;
			MessageSearchEntry.Text = string.Empty;
			MessageSearchResults.Clear();
			MessageSearchResultsView.IsVisible = false;
			await LoadMessagesAsync(thread.ConversationId, null);
			await ScrollToLatestMessageAsync();
			await LoadConversationInfoAsync();
			await _http.PostAsync($"api/chat/conversations/{thread.ConversationId}/read", null);
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Không mở được chat", ex.Message, "OK");
		}
	}

	private async Task LoadMessagesAsync(int conversationId, string? search)
	{
		Messages.Clear();
		var path = $"api/chat/conversations/{conversationId}/messages";
		if (!string.IsNullOrWhiteSpace(search))
			path += $"?search={Uri.EscapeDataString(search)}";

		var messages = await _http.GetFromJsonAsync<List<ChatMessageApi>>(path) ?? [];
		foreach (var message in messages)
			Messages.Add(ToMessage(message));
	}

	private async Task ScrollToLatestMessageAsync(bool animate = false)
	{
		var requestVersion = ++_scrollRequestVersion;

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await Task.Yield();

            if (requestVersion != _scrollRequestVersion || Messages.Count == 0)
                return;

            var lastMessage = Messages[^1];

            MessagesCollectionView.ScrollTo(
                lastMessage,
                position: ScrollToPosition.End,
                animate: false);

            await Task.Delay(100);

            if (requestVersion != _scrollRequestVersion || Messages.Count == 0)
                return;

            lastMessage = Messages[^1];

            MessagesCollectionView.ScrollTo(
                lastMessage,
                position: ScrollToPosition.End,
                animate: animate);
        });
	}

	private async Task LoadConversationInfoAsync()
	{
		if (_selectedThread?.ConversationId is not > 0)
			return;

		var info = await _http.GetFromJsonAsync<ChatConversationInfoApi>(
			$"api/chat/conversations/{_selectedThread.ConversationId}/info");
		if (info == null)
			return;

		PinnedMessages.Clear();
		foreach (var message in info.PinnedMessages)
			PinnedMessages.Add(ToMessage(message));

		MediaItems.Clear();
		FileItems.Clear();
		foreach (var message in info.MediaFiles)
		{
			var item = ToMessage(message);
			if (item.MessageType == "file")
				FileItems.Add(item);
			else
				MediaItems.Add(item);
		}
	}

	private async void OnSendClicked(object? sender, EventArgs e)
	{
		await SendCurrentMessageAsync("text", null, null);
	}

	private async void OnMessageCompleted(object? sender, EventArgs e)
	{
		await SendCurrentMessageAsync("text", null, null);
	}

	private async void OnSendAttachmentClicked(object? sender, EventArgs e)
	{
		if (_selectedThread == null)
		{
			await DisplayAlertAsync("Chưa chọn trò chuyện", "Hãy chọn một trò chuyện trước.", "OK");
			return;
		}

		var file = await FilePicker.Default.PickAsync();
		if (file == null)
			return;

		var type = DetectAttachmentType(file.FileName, file.FullPath);
		var upload = await UploadAttachmentAsync(file);
		await SendCurrentMessageAsync(type, upload.FileName, upload.FileUrl);
	}

	private async Task SendCurrentMessageAsync(string type, string? contentOverride, string? fileUrl)
	{
		if (_selectedThread == null)
		{
			await DisplayAlertAsync("Chưa chọn người nhận", "Hãy chọn tài khoản hoặc trò chuyện.", "OK");
			return;
		}

		var content = contentOverride ?? MessageEntry.Text?.Trim();
		if (type == "text" && string.IsNullOrWhiteSpace(content))
			return;
		if (type == "text" && _replyingTo != null)
			content = $"Trả lời {MessageSnippet(_replyingTo)}: {content}";

		try
		{
			MessageEntry.Text = string.Empty;
			ClearReply();
			var response = await _http.PostAsJsonAsync("api/chat/messages", new
			{
				ConversationId = _selectedThread.ConversationId > 0 ? (int?)_selectedThread.ConversationId : null,
				ReceiverId = _selectedThread.IsGroup ? null : (int?)_selectedThread.OtherUserId,
				Content = content ?? string.Empty,
				MessageType = type,
				FileName = type is "file" or "image" or "video" ? content ?? Path.GetFileName(fileUrl ?? string.Empty) : string.Empty,
				FileUrl = fileUrl ?? string.Empty
			});
			response.EnsureSuccessStatusCode();
		}
		catch (Exception ex)
		{
			await DisplayAlertAsync("Không gửi được tin nhắn", ex.Message, "OK");
		}
	}

	private async void OnMessageTapped(object? sender, TappedEventArgs e)
	{
		if (e.Parameter is not ChatMessage message)
			return;

		var baseActions = new List<string> { "Thu hồi", message.IsPinned ? "Bỏ ghim" : "Ghim", "Trả lời tin nhắn này", "Thả cảm xúc" };
		if (message.MessageType is "file" or "image" or "video"
			&& !string.IsNullOrWhiteSpace(message.FileUrl))
			baseActions.Insert(0, "Mở file");
		if (message.IsPoll)
		{
			baseActions.Add("Thêm lựa chọn");
			baseActions.Add("Khóa bình chọn");
		}
		var action = await DisplayActionSheetAsync("Tin nhan", "Hủy", null, baseActions.ToArray());
		if (action == "Mở file")
		{
			if (Uri.TryCreate(message.FileUrl, UriKind.Absolute, out var uri))
				await Launcher.OpenAsync(uri);
			else if (File.Exists(message.FileUrl))
			{
				await Launcher.OpenAsync(new OpenFileRequest(
					string.IsNullOrWhiteSpace(message.FileName) ? Path.GetFileName(message.FileUrl) : message.FileName,
					new ReadOnlyFile(message.FileUrl)));
			}
			return;
		}
		else if (action == "Thu hồi")
		{
			var response = await _http.PostAsync($"api/chat/messages/{message.MessageId}/recall", null);
			response.EnsureSuccessStatusCode();
		}
		else if (action is "Ghim" or "Bỏ ghim")
		{
			var response = await _http.PostAsync($"api/chat/messages/{message.MessageId}/pin", null);
			response.EnsureSuccessStatusCode();
		}
		else if (action == "Trả lời tin nhắn này")
		{
			_replyingTo = message;
			ReplyPreviewLabel.Text = MessageSnippet(message);
			ReplyPanel.IsVisible = true;
			MessageEntry.Focus();
			return;
		}
		else if (action == "Thả cảm xúc")
		{
			var reaction = await DisplayActionSheetAsync("Cảm xúc", "Hủy", null, "♥", "😆", "😮", "😢", "😡", "👍");
			if (!string.IsNullOrWhiteSpace(reaction) && reaction != "Hủy")
			{
				var response = await _http.PostAsJsonAsync($"api/chat/messages/{message.MessageId}/reaction", new { Reaction = reaction });
				response.EnsureSuccessStatusCode();
			}
		}
		else if (action == "Thêm lựa chọn")
		{
			var text = await DisplayPromptAsync("Thêm lựa chọn", "Nhập lựa chọn mới");
			if (!string.IsNullOrWhiteSpace(text))
			{
				var response = await _http.PostAsJsonAsync($"api/chat/messages/{message.MessageId}/poll/options", new { Text = text });
				response.EnsureSuccessStatusCode();
			}
		}
		else if (action == "Khóa bình chọn")
		{
			var response = await _http.PostAsync($"api/chat/messages/{message.MessageId}/poll/lock", null);
			response.EnsureSuccessStatusCode();
		}
		else
		{
			return;
		}

		await LoadConversationInfoAsync();
	}

	private void OnToggleEmojiClicked(object? sender, EventArgs e)
	{
		EmojiPanel.IsVisible = !EmojiPanel.IsVisible;
	}

	private async void OnEmojiPicked(object? sender, EventArgs e)
	{
		if (sender is Button button && !string.IsNullOrWhiteSpace(button.Text))
		{
			MessageEntry.Text += button.Text;
			MessageEntry.Focus();
		}
	}

	private async void OnPollOptionClicked(object? sender, EventArgs e)
	{
		if (sender is not Button button || button.CommandParameter is not ChatPollOptionApi option)
			return;

		var pollMessage = Messages.FirstOrDefault(x => x.Poll?.Options.Any(o => o.OptionId == option.OptionId) == true);
		if (pollMessage?.Poll == null)
			return;

		var optionIds = pollMessage.Poll.AllowMultipleChoices
			? pollMessage.Poll.Options
				.Where(x => x.VotedByMe && x.OptionId != option.OptionId)
				.Select(x => x.OptionId)
				.ToList()
			: [];
		if (!option.VotedByMe || !pollMessage.Poll.AllowMultipleChoices)
			optionIds.Add(option.OptionId);
		if (optionIds.Count == 0)
			optionIds.Add(option.OptionId);

		var response = await _http.PostAsJsonAsync($"api/chat/messages/{pollMessage.MessageId}/poll/vote", new
		{
			OptionIds = optionIds
		});
		response.EnsureSuccessStatusCode();
	}

	private void OnPollDetailsTapped(object? sender, TappedEventArgs e)
	{
		if (e.Parameter is not ChatMessage message || message.Poll == null)
			return;

		ShowPollDetails(message);
	}

	private void ShowPollDetails(ChatMessage message)
	{
		if (message.Poll == null)
			return;

		_pollDetailsMessage = message;
		PollDetailsQuestionLabel.Text = message.Poll.Question;
		PollDetailsMetaLabel.Text = $"Tao luc {message.Time}";
		PollDetailsModeLabel.Text = message.Poll.AllowMultipleChoices
			? "☷ Chon nhieu phuong an"
			: "☷ Chi chon mot phuong an";

		var voterCount = message.Poll.Options
			.SelectMany(x => x.Voters)
			.Select(x => x.UserId)
			.Distinct()
			.Count();
		var voteCount = message.Poll.Options.Sum(x => Math.Max(0, x.VoteCount));
		PollDetailsSummaryLabel.Text = message.Poll.ResultsHidden
			? "Ket qua dang an cho den khi ban binh chon"
			: message.Poll.HideVoters
				? $"{voteCount} luot binh chon"
				: $"{voterCount} nguoi binh chon, {voteCount} luot binh chon";

		PollDetailsAddOptionButton.IsVisible = message.Poll.AllowAddOptions && !message.Poll.IsClosed;
		PollDetailOptions.Clear();
		foreach (var option in message.Poll.Options)
			PollDetailOptions.Add(option);

		PollDetailsOverlay.IsVisible = true;
	}

	private void OnClosePollDetailsClicked(object? sender, EventArgs e)
	{
		PollDetailsOverlay.IsVisible = false;
		_pollDetailsMessage = null;
	}

	private async void OnPollDetailsAddOptionClicked(object? sender, EventArgs e)
	{
		if (_pollDetailsMessage?.Poll == null)
			return;

		var text = await DisplayPromptAsync("Them lua chon", "Nhap lua chon moi");
		if (string.IsNullOrWhiteSpace(text))
			return;

		var response = await _http.PostAsJsonAsync(
			$"api/chat/messages/{_pollDetailsMessage.MessageId}/poll/options",
			new { Text = text.Trim() });
		response.EnsureSuccessStatusCode();
		PollDetailsOverlay.IsVisible = false;
	}

	private async void OnCreatePollClicked(object? sender, EventArgs e)
	{
		if (_selectedThread == null || !_selectedThread.IsGroup)
		{
			await DisplayAlertAsync("Chi dung cho nhom", "Binh chon chi tao trong tin nhan nhom.", "OK");
			return;
		}

		ResetPollForm();
		PollCreateOverlay.IsVisible = true;
		PollQuestionEditor.Focus();
	}

	private void ResetPollForm()
	{
		PollQuestionEditor.Text = string.Empty;
		PollQuestionCountLabel.Text = "0/200";
		PollOption1Entry.Text = string.Empty;
		PollOption2Entry.Text = string.Empty;
		PollOption3Entry.Text = string.Empty;
		PollOption4Entry.Text = string.Empty;
		PollOption3Entry.IsVisible = false;
		PollOption4Entry.IsVisible = false;
		PollEndDateSwitch.IsToggled = false;
		PollEndDatePicker.Date = DateTime.Today;
		PollPinSwitch.IsToggled = false;
		PollAllowMultipleSwitch.IsToggled = true;
		PollAllowAddOptionsSwitch.IsToggled = true;
		PollHideResultsSwitch.IsToggled = false;
		PollHideVotersSwitch.IsToggled = false;
	}

	private void OnPollQuestionChanged(object? sender, TextChangedEventArgs e)
	{
		PollQuestionCountLabel.Text = $"{PollQuestionEditor.Text?.Length ?? 0}/200";
	}

	private void OnAddPollOptionClicked(object? sender, EventArgs e)
	{
		if (!PollOption3Entry.IsVisible)
		{
			PollOption3Entry.IsVisible = true;
			PollOption3Entry.Focus();
			return;
		}

		if (!PollOption4Entry.IsVisible)
		{
			PollOption4Entry.IsVisible = true;
			PollOption4Entry.Focus();
		}
	}

	private void OnCancelPollClicked(object? sender, EventArgs e)
	{
		PollCreateOverlay.IsVisible = false;
	}

	private async void OnConfirmPollClicked(object? sender, EventArgs e)
	{
		if (_selectedThread == null)
			return;

		var question = PollQuestionEditor.Text?.Trim();
		if (string.IsNullOrWhiteSpace(question))
		{
			await DisplayAlertAsync("Thieu chu de", "Vui long nhap cau hoi binh chon.", "OK");
			return;
		}

		var options = new[]
			{
				PollOption1Entry.Text,
				PollOption2Entry.Text,
				PollOption3Entry.IsVisible ? PollOption3Entry.Text : null,
				PollOption4Entry.IsVisible ? PollOption4Entry.Text : null
			}
			.Where(x => !string.IsNullOrWhiteSpace(x))
			.Select(x => x!.Trim())
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();
		if (options.Count < 2)
		{
			await DisplayAlertAsync("Thieu lua chon", "Can it nhat 2 lua chon.", "OK");
			return;
		}

		var multi = PollAllowMultipleSwitch.IsToggled;
		var addOptions = PollAllowAddOptionsSwitch.IsToggled;
		var pinToTop = PollPinSwitch.IsToggled;
		var hideResults = PollHideResultsSwitch.IsToggled;
		var hideVoters = PollHideVotersSwitch.IsToggled;
		DateTime? endsAt = PollEndDateSwitch.IsToggled
			? (PollEndDatePicker.Date ?? DateTime.Today).Date.AddDays(1).AddTicks(-1).ToUniversalTime()
			: null;

		var response = await _http.PostAsJsonAsync("api/chat/polls", new
		{
			ConversationId = _selectedThread.ConversationId,
			Question = question,
			Options = options,
			AllowMultipleChoices = multi,
			AllowAddOptions = addOptions,
			PinToTop = pinToTop,
			HideResultsUntilVoted = hideResults,
			HideVoters = hideVoters,
			EndsAt = endsAt
		});
		response.EnsureSuccessStatusCode();
		PollCreateOverlay.IsVisible = false;
	}
	private async Task OnRealtimeMessageAsync(ChatMessageApi message)
	{
		await LoadThreadsAsync();
		if (_selectedThread?.ConversationId == message.ConversationId)
		{
			if (Messages.All(x => x.MessageId != message.MessageId))
				Messages.Add(ToMessage(message));
			else
				await LoadMessagesAsync(message.ConversationId, null);

			await _http.PostAsync($"api/chat/conversations/{message.ConversationId}/read", null);
			await LoadConversationInfoAsync();
			if (PollDetailsOverlay.IsVisible && _pollDetailsMessage != null)
			{
				var updatedPollMessage = Messages.FirstOrDefault(x => x.MessageId == _pollDetailsMessage.MessageId);
				if (updatedPollMessage?.Poll != null)
					ShowPollDetails(updatedPollMessage);
			}
			await ScrollToLatestMessageAsync();
		}
	}

	private async void OnCreateGroupClicked(object? sender, EventArgs e)
	{
		GroupCreateOverlay.IsVisible = true;
		GroupNameEntry.Text = string.Empty;
		GroupSearchEntry.Text = string.Empty;
		SelectedGroupMembers.Clear();
		await LoadGroupCandidatesAsync(null);
	}

	private async void OnRenameGroupClicked(object? sender, EventArgs e)
	{
		if (_selectedThread == null || !_selectedThread.IsGroup)
			return;

		var name = await DisplayPromptAsync("Đổi tên nhóm", "Nhập tên mới", initialValue: _selectedThread.Name);
		if (string.IsNullOrWhiteSpace(name))
			return;

		var response = await _http.PutAsJsonAsync($"api/chat/conversations/{_selectedThread.ConversationId}/name", new { Name = name });
		response.EnsureSuccessStatusCode();
		await LoadThreadsAsync();
		ConversationTitleLabel.Text = name;
		InfoTitleLabel.Text = name;
	}

	private void OnToggleInfoClicked(object? sender, EventArgs e)
	{
		InfoPanel.IsVisible = !InfoPanel.IsVisible;
		InfoColumn.Width = InfoPanel.IsVisible ? new GridLength(300) : new GridLength(0);
	}

	private async void OnPinnedMessageTapped(object? sender, TappedEventArgs e)
	{
		if (e.Parameter is not ChatMessage pinned || _selectedThread == null)
			return;

		if (Messages.All(x => x.MessageId != pinned.MessageId))
			await LoadMessagesAsync(_selectedThread.ConversationId, null);

		var index = Messages.ToList().FindIndex(x => x.MessageId == pinned.MessageId);
		if (index >= 0)
			MessagesCollectionView.ScrollTo(Messages[index], position: ScrollToPosition.Center, animate: true);
	}

	private void OnCancelReplyClicked(object? sender, EventArgs e)
	{
		ClearReply();
	}

	private async void OnGroupSearchChanged(object? sender, TextChangedEventArgs e)
	{
		await LoadGroupCandidatesAsync(e.NewTextValue);
	}

	private void OnGroupCandidateTapped(object? sender, TappedEventArgs e)
	{
		if (e.Parameter is not ChatUserApi user)
			return;

		if (SelectedGroupMembers.Any(x => x.UserId == user.UserId))
			return;

		SelectedGroupMembers.Add(user);
	}

	private void OnSelectedGroupMemberTapped(object? sender, TappedEventArgs e)
	{
		if (e.Parameter is ChatUserApi user)
			SelectedGroupMembers.Remove(user);
	}

	private void OnCancelGroupClicked(object? sender, EventArgs e)
	{
		GroupCreateOverlay.IsVisible = false;
		GroupCandidates.Clear();
		SelectedGroupMembers.Clear();
	}

	private async void OnConfirmGroupClicked(object? sender, EventArgs e)
	{
		var name = GroupNameEntry.Text?.Trim();
		if (string.IsNullOrWhiteSpace(name))
		{
			await DisplayAlertAsync("Thiếu tên nhóm", "Hãy nhập tên nhóm.", "OK");
			return;
		}

		var memberIds = SelectedGroupMembers.Select(x => x.UserId).Distinct().ToList();
		if (memberIds.Count == 0)
		{
			await DisplayAlertAsync("Thiếu thành viên", "Hãy chọn ít nhất một nhân viên.", "OK");
			return;
		}

		var response = await _http.PostAsJsonAsync("api/chat/groups", new { Name = name, MemberIds = memberIds });
		response.EnsureSuccessStatusCode();
		var group = await response.Content.ReadFromJsonAsync<ChatConversationApi>();
		GroupCreateOverlay.IsVisible = false;
		await LoadThreadsAsync();
		if (group != null)
			await SelectThreadAsync(ToThread(group), createIfNeeded: false);
	}

	private async Task LoadGroupCandidatesAsync(string? search)
	{
		try
		{
			var path = "api/chat/users";
			if (!string.IsNullOrWhiteSpace(search))
				path += $"?search={Uri.EscapeDataString(search.Trim())}";
			var users = await _http.GetFromJsonAsync<List<ChatUserApi>>(path) ?? [];
			GroupCandidates.Clear();
			foreach (var user in users)
				GroupCandidates.Add(user);
		}
		catch (Exception ex)
		{
			ConversationSubtitleLabel.Text = $"Không tải được danh sách nhân viên: {ex.Message}";
		}
	}

	private void ClearReply()
	{
		_replyingTo = null;
		ReplyPreviewLabel.Text = string.Empty;
		ReplyPanel.IsVisible = false;
	}

	private static string MessageSnippet(ChatMessage message)
	{
		var text = message.DisplayText;
		return string.IsNullOrWhiteSpace(text)
			? "tin nhan"
			: text.Length > 60 ? $"{text[..60]}..." : text;
	}

	private async void OnMessageSearchCompleted(object? sender, EventArgs e)
	{
		await SearchMessagesAsync();
	}

	private async void OnMessageSearchChanged(object? sender, TextChangedEventArgs e)
	{
		if (string.IsNullOrWhiteSpace(e.NewTextValue))
		{
			MessageSearchResults.Clear();
			MessageSearchResultsView.IsVisible = false;
			return;
		}

		if (e.NewTextValue.Trim().Length >= 2)
			await SearchMessagesAsync();
	}

	private async Task SearchMessagesAsync()
	{
		if (_selectedThread?.ConversationId is not > 0)
			return;

		var term = MessageSearchEntry.Text?.Trim();
		MessageSearchResults.Clear();
		MessageSearchResultsView.IsVisible = false;
		if (string.IsNullOrWhiteSpace(term))
			return;

		var results = await _http.GetFromJsonAsync<List<ChatMessageApi>>(
			$"api/chat/conversations/{_selectedThread.ConversationId}/messages?search={Uri.EscapeDataString(term)}") ?? [];
		foreach (var message in results)
			MessageSearchResults.Add(ToMessage(message));

		MessageSearchResultsView.IsVisible = MessageSearchResults.Count > 0;
	}

	private async void OnSearchResultTapped(object? sender, TappedEventArgs e)
	{
		if (e.Parameter is not ChatMessage result || _selectedThread == null)
			return;

		if (Messages.Count == 0 || Messages.Any(x => x.MessageId == result.MessageId) == false)
			await LoadMessagesAsync(_selectedThread.ConversationId, null);

		var index = Messages.ToList().FindIndex(x => x.MessageId == result.MessageId);
		if (index >= 0)
			MessagesCollectionView.ScrollTo(Messages[index], position: ScrollToPosition.Center, animate: true);
	}

	private async void OnStartCallTapped(object? sender, EventArgs e)
	{
		if (_selectedThread == null || _selectedThread.IsGroup)
		{
			await DisplayAlertAsync("Không thể gọi", "Hãy chọn một tài khoản 1-1 để gọi.", "OK");
			return;
		}

		if (_hub?.State != HubConnectionState.Connected)
		{
			await DisplayAlertAsync("Chưa có realtime", "Kết nối realtime chưa sẵn sàng.", "OK");
			return;
		}

		_activeCallId = Guid.NewGuid().ToString("N");
		_activeCallUserId = _selectedThread.OtherUserId;
		_callStartedAt = null;
		SetCallState("outgoing", _selectedThread.OtherUserId, $"Đang gọi {_selectedThread.Name}");
		await _hub.InvokeAsync("StartCall", _selectedThread.OtherUserId, _activeCallId);
		await LogCallAsync(_selectedThread.OtherUserId, "started", null);
	}

	private async void OnAcceptCallClicked(object? sender, EventArgs e)
	{
		if (_hub == null || !_activeCallUserId.HasValue || string.IsNullOrWhiteSpace(_activeCallId))
			return;

		_callStartedAt = DateTime.UtcNow;
		SetCallState("in-call", _activeCallUserId.Value, "Cuộc gọi đang diễn ra");
		await _hub.InvokeAsync("AcceptCall", _activeCallUserId.Value, _activeCallId);
	}

	private async void OnRejectCallClicked(object? sender, EventArgs e)
	{
		if (_hub == null || !_activeCallUserId.HasValue || string.IsNullOrWhiteSpace(_activeCallId))
			return;

		var callerId = _activeCallUserId.Value;
		var callId = _activeCallId;
		SetCallState("rejected", callerId, "Đã từ chối cuộc gọi");
		await _hub.InvokeAsync("RejectCall", callerId, callId);
		await LogCallAsync(callerId, "missed", null);
	}

	private async void OnEndCallClicked(object? sender, EventArgs e)
	{
		if (_hub == null || !_activeCallUserId.HasValue || string.IsNullOrWhiteSpace(_activeCallId))
			return;

		var otherUserId = _activeCallUserId.Value;
		var callId = _activeCallId;
		var duration = CallDurationSeconds();
		SetCallState("ended", otherUserId, "Cuộc gọi đã kết thúc");
		await _hub.InvokeAsync("EndCall", otherUserId, callId);
		await LogCallAsync(otherUserId, "ended", duration);
	}

	private void ShowIncomingCall(int callerId, string callId)
	{
		if (_callState is "outgoing" or "in-call" or "incoming")
		{
			_ = _hub?.InvokeAsync("Busy", callerId, callId);
			return;
		}

		_activeCallId = callId;
		_activeCallUserId = callerId;
		SetCallState("incoming", callerId, $"Cuộc gọi đến từ nhân viên #{callerId}");
	}

	private void SetCallState(string state, int otherUserId, string status)
	{
		_callState = state;
		_activeCallUserId = otherUserId;
		CallPanel.IsVisible = state != "idle";
		CallStatusLabel.Text = status;
		CallTimeLabel.Text = state == "in-call" ? "Đang nối máy" : DateTime.Now.ToString("HH:mm");
		AcceptCallButton.IsVisible = state == "incoming";
		RejectCallButton.IsVisible = state == "incoming";
		EndCallButton.IsVisible = state is "outgoing" or "in-call";

		if (state is "ended" or "rejected" or "busy")
		{
			EndCallButton.IsVisible = false;
			_ = Task.Delay(2500).ContinueWith(_ =>
				MainThread.BeginInvokeOnMainThread(() =>
				{
					CallPanel.IsVisible = false;
					_callState = "idle";
					_activeCallId = null;
					_activeCallUserId = null;
					_callStartedAt = null;
				}));
		}
	}

	private async Task LogCallAsync(int otherUserId, string status, int? duration)
	{
		try
		{
			var response = await _http.PostAsJsonAsync("api/chat/calls/log", new
			{
				OtherUserId = otherUserId,
				Status = status,
				DurationSeconds = duration
			});
			response.EnsureSuccessStatusCode();
		}
		catch
		{
			// Call signaling should not be interrupted if call history logging fails.
		}
	}

	private int? CallDurationSeconds()
	{
		return _callStartedAt.HasValue
			? Math.Max(1, (int)(DateTime.UtcNow - _callStartedAt.Value).TotalSeconds)
			: null;
	}

	private void RefreshThreadsBinding()
	{
		var copy = Threads.ToList();
		Threads.Clear();
		foreach (var item in copy)
			Threads.Add(item);
	}

	private static ChatThread ToThread(ChatConversationApi conversation)
	{
		var title = string.IsNullOrWhiteSpace(conversation.Title)
			? conversation.OtherUser.Username
			: conversation.Title;

		return new ChatThread
		{
			ConversationId = conversation.ConversationId,
			OtherUserId = conversation.OtherUser.UserId,
			IsGroup = conversation.IsGroup,
			Name = title,
			Preview = PreviewText(conversation.LastMessageType, conversation.LastMessage),
			Time = conversation.LastMessageAt?.ToLocalTime().ToString("HH:mm") ?? string.Empty,
			Initials = Initials(title),
			Role = conversation.IsGroup ? $"{conversation.Participants.Count} thanh vien" : conversation.OtherUser.Role,
			AvatarUrl = conversation.AvatarUrl,
			UnreadCount = conversation.UnreadCount
		};
	}

	private static ChatMessage ToMessage(ChatMessageApi message)
	{
		return new ChatMessage
		{
			MessageId = message.MessageId,
			ConversationId = message.ConversationId,
			SenderId = message.SenderId,
			ReceiverId = message.ReceiverId,
			Content = message.Content,
			MessageType = message.MessageType,
			FileName = message.FileName,
			FileUrl = ResolveFileUrl(message.FileUrl),
			CallStatus = message.CallStatus,
			CallDurationSeconds = message.CallDurationSeconds,
			IsPinned = message.IsPinned,
			IsRecalled = message.IsRecalled,
			Reaction = message.Reaction,
			Poll = message.Poll,
			IsRead = message.ReadAt.HasValue,
			Time = message.SentAt.ToLocalTime().ToString("HH:mm"),
			IsOutgoing = ApiClientProvider.UserId.HasValue && message.SenderId == ApiClientProvider.UserId.Value
		};
	}

	private async Task<AttachmentUploadResponse> UploadAttachmentAsync(FileResult file)
	{
		await using var stream = await file.OpenReadAsync();
		using var content = new MultipartFormDataContent();
		using var fileContent = new StreamContent(stream);
		if (!string.IsNullOrWhiteSpace(file.ContentType))
			fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
		content.Add(fileContent, "file", file.FileName);

		var response = await _http.PostAsync("api/chat/attachments", content);
		response.EnsureSuccessStatusCode();

		return await response.Content.ReadFromJsonAsync<AttachmentUploadResponse>()
			?? new AttachmentUploadResponse { FileName = file.FileName };
	}

	private static string ResolveFileUrl(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return string.Empty;
		return Uri.TryCreate(value, UriKind.Absolute, out _)
			? value
			: new Uri(ApiClientProvider.Client.BaseAddress!, value.TrimStart('/')).ToString();
	}

	private static string PreviewText(string type, string content) => type switch
	{
		"file" => "[File]",
		"image" => "[Anh]",
		"video" => "[Video]",
		"icon" => "[Icon]",
		"poll" => "[Tham do]",
		"call" => "[Cuoc goi]",
		_ => string.IsNullOrWhiteSpace(content) ? "Bắt đầu trò chuyện" : content
	};

	private static string DetectAttachmentType(string? fileName, string? path)
	{
		var extension = Path.GetExtension(fileName);
		if (string.IsNullOrWhiteSpace(extension))
			extension = Path.GetExtension(path);
		if (string.IsNullOrWhiteSpace(extension))
			return "file";
		extension = extension.TrimStart('.').ToLowerInvariant();

		if (new[] { "jpg", "jpeg", "png", "gif", "bmp", "webp" }.Contains(extension))
			return "image";
		if (new[] { "mp4", "mov", "avi", "mkv", "webm", "wmv", "m4v" }.Contains(extension))
			return "video";

		return "file";
	}

	private static string Initials(string value)
	{
		var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		if (parts.Length == 0)
			return "?";
		if (parts.Length == 1)
			return parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant();
		return string.Concat(parts.Take(2).Select(x => char.ToUpperInvariant(x[0])));
	}

	private sealed class CallEventPayload
	{
		public string CallId { get; set; } = string.Empty;
		public int CallerId { get; set; }
		public int ReceiverId { get; set; }
		public int UserId { get; set; }
	}

	private sealed class AttachmentUploadResponse
	{
		public string FileName { get; set; } = string.Empty;
		public string FileUrl { get; set; } = string.Empty;
	}
}
