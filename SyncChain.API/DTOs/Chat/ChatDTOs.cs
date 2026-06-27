using System.ComponentModel.DataAnnotations;

namespace SyncChain.API.DTOs.Chat;

public sealed class ChatUserDTO
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public sealed class ChatConversationDTO
{
    public int ConversationId { get; set; }
    public bool IsGroup { get; set; }
    public string Title { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public ChatUserDTO OtherUser { get; set; } = new();
    public List<ChatUserDTO> Participants { get; set; } = new();
    public string LastMessage { get; set; } = string.Empty;
    public string LastMessageType { get; set; } = "text";
    public DateTime? LastMessageAt { get; set; }
    public int UnreadCount { get; set; }
}

public sealed class ChatMessageDTO
{
    public long MessageId { get; set; }
    public int ConversationId { get; set; }
    public int SenderId { get; set; }
    public int ReceiverId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string MessageType { get; set; } = "text";
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string CallStatus { get; set; } = string.Empty;
    public int? CallDurationSeconds { get; set; }
    public bool IsPinned { get; set; }
    public bool IsRecalled { get; set; }
    public string Reaction { get; set; } = string.Empty;
    public ChatPollDTO? Poll { get; set; }
    public DateTime SentAt { get; set; }
    public DateTime? ReadAt { get; set; }
}

public sealed class ReactMessageRequest
{
    [Required]
    [MaxLength(20)]
    public string Reaction { get; set; } = string.Empty;
}

public sealed class CreateConversationRequest
{
    [Required]
    public int OtherUserId { get; set; }
}

public sealed class SendChatMessageRequest
{
    public int? ReceiverId { get; set; }

    public int? ConversationId { get; set; }

    [Required]
    [MaxLength(2000)]
    public string Content { get; set; } = string.Empty;

    [MaxLength(30)]
    public string MessageType { get; set; } = "text";

    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string FileUrl { get; set; } = string.Empty;
}

public sealed class CreateGroupConversationRequest
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    public List<int> MemberIds { get; set; } = new();
}

public sealed class RenameConversationRequest
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;
}

public sealed class ChatConversationInfoDTO
{
    public ChatConversationDTO Conversation { get; set; } = new();
    public List<ChatMessageDTO> PinnedMessages { get; set; } = new();
    public List<ChatMessageDTO> MediaFiles { get; set; } = new();
}

public sealed class ChatCallLogRequest
{
    [Required]
    public int OtherUserId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = string.Empty;

    public int? DurationSeconds { get; set; }
}

public sealed class CreatePollRequest
{
    [Required]
    public int ConversationId { get; set; }

    [Required]
    [MaxLength(300)]
    public string Question { get; set; } = string.Empty;

    public List<string> Options { get; set; } = new();

    public bool AllowMultipleChoices { get; set; }

    public bool AllowAddOptions { get; set; }

    public bool PinToTop { get; set; }

    public bool HideResultsUntilVoted { get; set; }

    public bool HideVoters { get; set; }

    public DateTime? EndsAt { get; set; }
}

public sealed class PollVoteRequest
{
    public List<int> OptionIds { get; set; } = new();
}

public sealed class AddPollOptionRequest
{
    [Required]
    [MaxLength(200)]
    public string Text { get; set; } = string.Empty;
}

public sealed class ChatPollDTO
{
    public int PollId { get; set; }
    public string Question { get; set; } = string.Empty;
    public bool AllowMultipleChoices { get; set; }
    public bool AllowAddOptions { get; set; }
    public bool HideResultsUntilVoted { get; set; }
    public bool HideVoters { get; set; }
    public bool ResultsHidden { get; set; }
    public bool IsClosed { get; set; }
    public DateTime? EndsAt { get; set; }
    public List<ChatPollOptionDTO> Options { get; set; } = new();
}

public sealed class ChatPollOptionDTO
{
    public int OptionId { get; set; }
    public string Text { get; set; } = string.Empty;
    public int VoteCount { get; set; }
    public bool VotedByMe { get; set; }
    public List<ChatPollVoterDTO> Voters { get; set; } = new();
}

public sealed class ChatPollVoterDTO
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
}
