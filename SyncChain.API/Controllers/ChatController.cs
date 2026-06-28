using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SyncChain.API.DTOs.Chat;
using SyncChain.API.Exceptions;
using SyncChain.API.Hubs;
using SyncChain.API.Services;

namespace SyncChain.API.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize(Policy = "InternalOnly")]
public class ChatController : ControllerBase
{
    private readonly ChatService _chat;
    private readonly IHubContext<ChatHub> _hub;
    private readonly IWebHostEnvironment _environment;

    public ChatController(ChatService chat, IHubContext<ChatHub> hub, IWebHostEnvironment environment)
    {
        _chat = chat;
        _hub = hub;
        _environment = environment;
    }

    [HttpGet("users")]
    public async Task<ActionResult<List<ChatUserDTO>>> GetUsers([FromQuery] string? search)
    {
        return Ok(await _chat.GetInternalUsersAsync(GetRequiredUserId(), search));
    }

    [HttpGet("conversations")]
    public async Task<ActionResult<List<ChatConversationDTO>>> GetConversations()
    {
        return Ok(await _chat.GetConversationsAsync(GetRequiredUserId()));
    }

    [HttpPost("conversations")]
    public async Task<ActionResult<ChatConversationDTO>> GetOrCreateConversation(CreateConversationRequest request)
    {
        return Ok(await _chat.GetOrCreateConversationAsync(GetRequiredUserId(), request.OtherUserId));
    }

    [HttpPost("groups")]
    public async Task<ActionResult<ChatConversationDTO>> CreateGroup(CreateGroupConversationRequest request)
    {
        return Ok(await _chat.CreateGroupAsync(GetRequiredUserId(), request));
    }

    [HttpPut("conversations/{conversationId:int}/name")]
    public async Task<ActionResult<ChatConversationDTO>> RenameConversation(int conversationId, RenameConversationRequest request)
    {
        return Ok(await _chat.RenameConversationAsync(GetRequiredUserId(), conversationId, request.Name));
    }

    [HttpGet("conversations/{conversationId:int}/info")]
    public async Task<ActionResult<ChatConversationInfoDTO>> GetConversationInfo(int conversationId)
    {
        return Ok(await _chat.GetConversationInfoAsync(GetRequiredUserId(), conversationId));
    }

    [HttpGet("conversations/{conversationId:int}/messages")]
    public async Task<ActionResult<List<ChatMessageDTO>>> GetMessages(int conversationId, [FromQuery] string? search)
    {
        return Ok(await _chat.GetMessagesAsync(GetRequiredUserId(), conversationId, search));
    }

    [HttpPost("messages")]
    public async Task<ActionResult<ChatMessageDTO>> SendMessage(SendChatMessageRequest request)
    {
        var message = await _chat.SendMessageAsync(GetRequiredUserId(), request);
        await PushMessageAsync(message);
        return Ok(message);
    }

    [HttpPost("attachments")]
    public async Task<IActionResult> UploadAttachment(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "File khong hop le." });

        var webRoot = _environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
            webRoot = Path.Combine(_environment.ContentRootPath, "wwwroot");

        var uploadRoot = Path.Combine(webRoot, "uploads", "chat");
        Directory.CreateDirectory(uploadRoot);

        var extension = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(uploadRoot, fileName);

        await using (var stream = System.IO.File.Create(filePath))
            await file.CopyToAsync(stream);

        return Ok(new
        {
            fileName = file.FileName,
            fileUrl = $"/uploads/chat/{fileName}"
        });
    }

    [HttpPost("messages/{messageId:long}/pin")]
    public async Task<ActionResult<ChatMessageDTO>> TogglePin(long messageId)
    {
        var message = await _chat.TogglePinAsync(GetRequiredUserId(), messageId);
        await PushMessageAsync(message);
        return Ok(message);
    }

    [HttpPost("messages/{messageId:long}/recall")]
    public async Task<ActionResult<ChatMessageDTO>> Recall(long messageId)
    {
        var message = await _chat.RecallMessageAsync(GetRequiredUserId(), messageId);
        await PushMessageAsync(message);
        return Ok(message);
    }

    [HttpPost("messages/{messageId:long}/reaction")]
    public async Task<ActionResult<ChatMessageDTO>> React(long messageId, ReactMessageRequest request)
    {
        var message = await _chat.ReactMessageAsync(GetRequiredUserId(), messageId, request);
        await PushMessageAsync(message);
        return Ok(message);
    }

    [HttpPost("calls/log")]
    public async Task<ActionResult<ChatMessageDTO>> LogCall(ChatCallLogRequest request)
    {
        var message = await _chat.AddCallMessageAsync(GetRequiredUserId(), request);
        await PushMessageAsync(message);
        return Ok(message);
    }

    [HttpPost("polls")]
    public async Task<ActionResult<ChatMessageDTO>> CreatePoll(CreatePollRequest request)
    {
        var message = await _chat.CreatePollAsync(GetRequiredUserId(), request);
        await PushMessageAsync(message);
        return Ok(message);
    }

    [HttpPost("messages/{messageId:long}/poll/vote")]
    public async Task<ActionResult<ChatMessageDTO>> VotePoll(long messageId, PollVoteRequest request)
    {
        var message = await _chat.VotePollAsync(GetRequiredUserId(), messageId, request);
        await PushMessageAsync(message);
        return Ok(message);
    }

    [HttpPost("messages/{messageId:long}/poll/options")]
    public async Task<ActionResult<ChatMessageDTO>> AddPollOption(long messageId, AddPollOptionRequest request)
    {
        var message = await _chat.AddPollOptionAsync(GetRequiredUserId(), messageId, request);
        await PushMessageAsync(message);
        return Ok(message);
    }

    [HttpPost("messages/{messageId:long}/poll/lock")]
    public async Task<ActionResult<ChatMessageDTO>> LockPoll(long messageId)
    {
        var message = await _chat.LockPollAsync(GetRequiredUserId(), messageId);
        await PushMessageAsync(message);
        return Ok(message);
    }

    [HttpPost("conversations/{conversationId:int}/read")]
    public async Task<IActionResult> MarkRead(int conversationId)
    {
        await _chat.MarkReadAsync(GetRequiredUserId(), conversationId);
        return NoContent();
    }

    private async Task PushMessageAsync(ChatMessageDTO message)
    {
        var participantIds = await _chat.GetParticipantIdsAsync(message.ConversationId);
        foreach (var participantId in participantIds.Distinct())
            await _hub.Clients.Group(ChatHub.UserGroup(participantId)).SendAsync("MessageReceived", message);
    }

    private int GetRequiredUserId()
    {
        var value = User.FindFirst("user_id")?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(value, out var userId))
            throw new AuthenticationApiException("Yeu cau chua duoc xac thuc.");

        return userId;
    }
}
