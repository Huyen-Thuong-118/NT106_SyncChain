using Microsoft.EntityFrameworkCore;
using SyncChain.API.Data;
using SyncChain.API.DTOs.Chat;
using SyncChain.API.Exceptions;
using SyncChain.API.Models;

namespace SyncChain.API.Services;

public class ChatService
{
    private static readonly int[] InternalRoleIds = [2, 3, 4];
    private static readonly string[] AllowedMessageTypes = ["text", "icon", "file", "image", "video", "call", "poll"];
    private readonly AppDbContext _db;

    public ChatService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<ChatUserDTO>> GetInternalUsersAsync(int currentUserId, string? search = null)
    {
        var query = _db.NguoiDung
            .Where(x => x.IsActive && x.MaNguoiDung != currentUserId && InternalRoleIds.Contains(x.MaVaiTro));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x => x.TenDangNhap.ToLower().Contains(term) || x.Email.ToLower().Contains(term));
        }

        return await query
            .OrderBy(x => x.TenDangNhap)
            .Select(x => new ChatUserDTO
            {
                UserId = x.MaNguoiDung,
                Username = x.TenDangNhap,
                Email = x.Email,
                Role = x.MaVaiTro == 4 ? "admin" : x.MaVaiTro == 3 ? "manager" : "staff"
            })
            .ToListAsync();
    }

    public async Task<List<ChatConversationDTO>> GetConversationsAsync(int currentUserId)
    {
        var conversations = await _db.ChatConversations
            .Include(x => x.ThanhViens)
            .Where(x => x.ThanhViens.Any(p => p.MaNguoiDung == currentUserId)
                || x.MaNguoiDung1 == currentUserId
                || x.MaNguoiDung2 == currentUserId)
            .OrderByDescending(x => x.CapNhatLuc)
            .ToListAsync();

        var result = new List<ChatConversationDTO>();
        foreach (var conversation in conversations)
            result.Add(await ToConversationDTOAsync(conversation, currentUserId));

        return result;
    }

    public async Task<ChatConversationDTO> GetOrCreateConversationAsync(int currentUserId, int otherUserId)
    {
        if (currentUserId == otherUserId)
            throw new ValidationApiException("Khong the tao cuoc tro chuyen voi chinh minh.");

        await EnsureInternalUserAsync(currentUserId);
        await EnsureInternalUserAsync(otherUserId);

        var (firstUserId, secondUserId) = OrderedPair(currentUserId, otherUserId);
        var conversation = await _db.ChatConversations
            .Include(x => x.ThanhViens)
            .FirstOrDefaultAsync(x => !x.LaNhom && x.MaNguoiDung1 == firstUserId && x.MaNguoiDung2 == secondUserId);

        if (conversation == null)
        {
            conversation = new ChatConversation
            {
                MaNguoiDung1 = firstUserId,
                MaNguoiDung2 = secondUserId,
                MaNguoiTao = currentUserId,
                NgayTao = DateTime.UtcNow,
                CapNhatLuc = DateTime.UtcNow,
                ThanhViens =
                [
                    new() { MaNguoiDung = firstUserId },
                    new() { MaNguoiDung = secondUserId }
                ]
            };
            _db.ChatConversations.Add(conversation);
            await _db.SaveChangesAsync();
        }

        return await ToConversationDTOAsync(conversation, currentUserId);
    }

    public async Task<ChatConversationDTO> CreateGroupAsync(int currentUserId, CreateGroupConversationRequest request)
    {
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new ValidationApiException("Ten nhom khong duoc de trong.");

        var memberIds = request.MemberIds
            .Where(x => x != currentUserId)
            .Distinct()
            .ToList();
        if (memberIds.Count == 0)
            throw new ValidationApiException("Nhom can it nhat mot thanh vien khac.");

        await EnsureInternalUserAsync(currentUserId);
        foreach (var memberId in memberIds)
            await EnsureInternalUserAsync(memberId);

        var now = DateTime.UtcNow;
        var conversation = new ChatConversation
        {
            LaNhom = true,
            TenNhom = name,
            MaNguoiTao = currentUserId,
            NgayTao = now,
            CapNhatLuc = now,
            ThanhViens = memberIds
                .Append(currentUserId)
                .Distinct()
                .Select(id => new ChatParticipant { MaNguoiDung = id, ThamGiaLuc = now })
                .ToList()
        };

        _db.ChatConversations.Add(conversation);
        await _db.SaveChangesAsync();
        return await ToConversationDTOAsync(conversation, currentUserId);
    }

    public async Task<ChatConversationDTO> RenameConversationAsync(int currentUserId, int conversationId, string name)
    {
        var conversation = await EnsureConversationParticipantAsync(currentUserId, conversationId);
        if (!conversation.LaNhom)
            throw new ValidationApiException("Chi co the doi ten nhom.");

        var trimmed = name.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            throw new ValidationApiException("Ten nhom khong duoc de trong.");

        conversation.TenNhom = trimmed;
        conversation.CapNhatLuc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return await ToConversationDTOAsync(conversation, currentUserId);
    }

    public async Task<ChatConversationInfoDTO> GetConversationInfoAsync(int currentUserId, int conversationId)
    {
        var conversation = await EnsureConversationParticipantAsync(currentUserId, conversationId);
        return new ChatConversationInfoDTO
        {
            Conversation = await ToConversationDTOAsync(conversation, currentUserId),
            PinnedMessages = await _db.ChatMessages
                .Where(x => x.MaCuocTroChuyen == conversationId && x.DaGhim)
                .OrderByDescending(x => x.ThoiGianGui)
                .Select(x => ToMessageDTO(x))
                .ToListAsync(),
            MediaFiles = await _db.ChatMessages
                .Where(x => x.MaCuocTroChuyen == conversationId && (x.LoaiTinNhan == "file" || x.LoaiTinNhan == "image" || x.LoaiTinNhan == "video"))
                .OrderByDescending(x => x.ThoiGianGui)
                .Select(x => ToMessageDTO(x))
                .ToListAsync()
        };
    }

    public async Task<List<ChatMessageDTO>> GetMessagesAsync(int currentUserId, int conversationId, string? search = null)
    {
        await EnsureConversationParticipantAsync(currentUserId, conversationId);

        var query = _db.ChatMessages.Where(x => x.MaCuocTroChuyen == conversationId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x => x.NoiDung.ToLower().Contains(term) || x.TenFile.ToLower().Contains(term));
        }

        var messages = await query
            .OrderBy(x => x.ThoiGianGui)
            .ToListAsync();
        return await ToMessageDTOsAsync(messages, currentUserId);
    }

    public async Task<ChatMessageDTO> SendMessageAsync(int currentUserId, SendChatMessageRequest request)
    {
        var type = NormalizeMessageType(request.MessageType);
        var content = request.Content.Trim();
        var fileName = request.FileName.Trim();
        if (string.IsNullOrWhiteSpace(fileName) && !string.IsNullOrWhiteSpace(request.FileUrl))
            fileName = Path.GetFileName(request.FileUrl.Trim());
        if (type is "text" or "icon" && string.IsNullOrWhiteSpace(content))
            throw new ValidationApiException("Noi dung tin nhan khong duoc de trong.");
        if (type is "file" or "image" or "video" && string.IsNullOrWhiteSpace(fileName))
            throw new ValidationApiException("Ten file khong duoc de trong.");

        await EnsureInternalUserAsync(currentUserId);

        ChatConversationDTO conversationDto;
        if (request.ConversationId.HasValue)
        {
            var conversation = await EnsureConversationParticipantAsync(currentUserId, request.ConversationId.Value);
            conversationDto = await ToConversationDTOAsync(conversation, currentUserId);
        }
        else if (request.ReceiverId.HasValue)
        {
            if (currentUserId == request.ReceiverId.Value)
                throw new ValidationApiException("Khong the gui tin nhan cho chinh minh.");

            conversationDto = await GetOrCreateConversationAsync(currentUserId, request.ReceiverId.Value);
        }
        else
        {
            throw new ValidationApiException("Can conversationId hoac receiverId.");
        }

        var receiverId = request.ReceiverId
            ?? conversationDto.Participants.FirstOrDefault(x => x.UserId != currentUserId)?.UserId
            ?? 0;

        var now = DateTime.UtcNow;
        var message = new ChatMessageEntity
        {
            MaCuocTroChuyen = conversationDto.ConversationId,
            MaNguoiGui = currentUserId,
            MaNguoiNhan = receiverId,
            NoiDung = type is "file" or "image" or "video" && string.IsNullOrWhiteSpace(content) ? fileName : content,
            LoaiTinNhan = type,
            TenFile = fileName,
            DuongDanFile = request.FileUrl.Trim(),
            ThoiGianGui = now
        };

        var entity = await _db.ChatConversations.FindAsync(conversationDto.ConversationId)
            ?? throw new ValidationApiException("Cuoc tro chuyen khong ton tai.");
        entity.CapNhatLuc = now;
        _db.ChatMessages.Add(message);
        await _db.SaveChangesAsync();

        return await ToMessageDTOAsync(message, currentUserId);
    }

    public async Task<ChatMessageDTO> CreatePollAsync(int currentUserId, CreatePollRequest request)
    {
        var conversation = await EnsureConversationParticipantAsync(currentUserId, request.ConversationId);
        if (!conversation.LaNhom)
            throw new ValidationApiException("Chi co the tao tham do trong nhom.");

        var question = request.Question.Trim();
        var options = request.Options
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();
        if (string.IsNullOrWhiteSpace(question))
            throw new ValidationApiException("Cau hoi khong duoc de trong.");
        if (options.Count < 2)
            throw new ValidationApiException("Tham do can it nhat 2 lua chon.");

        var now = DateTime.UtcNow;
        var message = new ChatMessageEntity
        {
            MaCuocTroChuyen = conversation.MaCuocTroChuyen,
            MaNguoiGui = currentUserId,
            MaNguoiNhan = 0,
            NoiDung = question,
            LoaiTinNhan = "poll",
            DaGhim = request.PinToTop,
            ThoiGianGui = now
        };

        conversation.CapNhatLuc = now;
        _db.ChatMessages.Add(message);
        await _db.SaveChangesAsync();

        _db.ChatPolls.Add(new ChatPoll
        {
            MaTinNhan = message.MaTinNhan,
            CauHoi = question,
            ChoPhepNhieuLuaChon = request.AllowMultipleChoices,
            ChoPhepThemLuaChon = request.AllowAddOptions,
            AnKetQuaKhiChuaBinhChon = request.HideResultsUntilVoted,
            AnNguoiBinhChon = request.HideVoters,
            KetThucLuc = request.EndsAt,
            LuaChons = options.Select(x => new ChatPollOption { NoiDung = x }).ToList()
        });
        await _db.SaveChangesAsync();

        return await ToMessageDTOAsync(message, currentUserId);
    }

    public async Task<ChatMessageDTO> VotePollAsync(int currentUserId, long messageId, PollVoteRequest request)
    {
        var message = await _db.ChatMessages.FindAsync(messageId)
            ?? throw new ValidationApiException("Tin nhan khong ton tai.");
        await EnsureConversationParticipantAsync(currentUserId, message.MaCuocTroChuyen);

        var poll = await _db.ChatPolls
            .Include(x => x.LuaChons)
            .ThenInclude(x => x.BinhChons)
            .FirstOrDefaultAsync(x => x.MaTinNhan == messageId)
            ?? throw new ValidationApiException("Tham do khong ton tai.");

        if (poll.DaKhoa || (poll.KetThucLuc.HasValue && poll.KetThucLuc.Value <= DateTime.UtcNow))
            throw new ValidationApiException("Tham do da khoa hoac da het han.");

        var selected = request.OptionIds.Distinct().ToList();
        if (selected.Count == 0)
            throw new ValidationApiException("Can chon it nhat mot lua chon.");
        if (!poll.ChoPhepNhieuLuaChon && selected.Count > 1)
            throw new ValidationApiException("Tham do nay chi cho phep mot lua chon.");

        var validOptionIds = poll.LuaChons.Select(x => x.MaLuaChon).ToHashSet();
        if (selected.Any(x => !validOptionIds.Contains(x)))
            throw new ValidationApiException("Lua chon khong hop le.");

        var oldVotes = poll.LuaChons.SelectMany(x => x.BinhChons).Where(x => x.MaNguoiDung == currentUserId);
        _db.ChatPollVotes.RemoveRange(oldVotes);
        foreach (var optionId in selected)
            _db.ChatPollVotes.Add(new ChatPollVote { MaLuaChon = optionId, MaNguoiDung = currentUserId });

        await _db.SaveChangesAsync();
        return await ToMessageDTOAsync(message, currentUserId);
    }

    public async Task<ChatMessageDTO> AddPollOptionAsync(int currentUserId, long messageId, AddPollOptionRequest request)
    {
        var message = await _db.ChatMessages.FindAsync(messageId)
            ?? throw new ValidationApiException("Tin nhan khong ton tai.");
        await EnsureConversationParticipantAsync(currentUserId, message.MaCuocTroChuyen);

        var poll = await _db.ChatPolls.FirstOrDefaultAsync(x => x.MaTinNhan == messageId)
            ?? throw new ValidationApiException("Tham do khong ton tai.");
        if (!poll.ChoPhepThemLuaChon || poll.DaKhoa)
            throw new ValidationApiException("Khong the them lua chon vao tham do nay.");

        var text = request.Text.Trim();
        if (string.IsNullOrWhiteSpace(text))
            throw new ValidationApiException("Lua chon khong duoc de trong.");

        _db.ChatPollOptions.Add(new ChatPollOption { MaThamDo = poll.MaThamDo, NoiDung = text });
        await _db.SaveChangesAsync();
        return await ToMessageDTOAsync(message, currentUserId);
    }

    public async Task<ChatMessageDTO> LockPollAsync(int currentUserId, long messageId)
    {
        var message = await _db.ChatMessages.FindAsync(messageId)
            ?? throw new ValidationApiException("Tin nhan khong ton tai.");
        await EnsureConversationParticipantAsync(currentUserId, message.MaCuocTroChuyen);

        var poll = await _db.ChatPolls.FirstOrDefaultAsync(x => x.MaTinNhan == messageId)
            ?? throw new ValidationApiException("Tham do khong ton tai.");
        poll.DaKhoa = true;
        await _db.SaveChangesAsync();
        return await ToMessageDTOAsync(message, currentUserId);
    }

    public async Task<ChatMessageDTO> AddCallMessageAsync(int currentUserId, ChatCallLogRequest request)
    {
        if (currentUserId == request.OtherUserId)
            throw new ValidationApiException("Khong the goi chinh minh.");

        var conversation = await GetOrCreateConversationAsync(currentUserId, request.OtherUserId);
        var now = DateTime.UtcNow;
        var status = string.IsNullOrWhiteSpace(request.Status) ? "missed" : request.Status.Trim();
        var message = new ChatMessageEntity
        {
            MaCuocTroChuyen = conversation.ConversationId,
            MaNguoiGui = currentUserId,
            MaNguoiNhan = request.OtherUserId,
            NoiDung = status == "missed" ? "Da nho cuoc goi" : "Cuoc goi",
            LoaiTinNhan = "call",
            TrangThaiCuocGoi = status,
            ThoiLuongCuocGoiGiay = request.DurationSeconds,
            ThoiGianGui = now
        };

        var entity = await _db.ChatConversations.FindAsync(conversation.ConversationId)
            ?? throw new ValidationApiException("Cuoc tro chuyen khong ton tai.");
        entity.CapNhatLuc = now;
        _db.ChatMessages.Add(message);
        await _db.SaveChangesAsync();
        return await ToMessageDTOAsync(message, currentUserId);
    }

    public async Task<ChatMessageDTO> TogglePinAsync(int currentUserId, long messageId)
    {
        var message = await _db.ChatMessages.FindAsync(messageId)
            ?? throw new ValidationApiException("Tin nhan khong ton tai.");
        await EnsureConversationParticipantAsync(currentUserId, message.MaCuocTroChuyen);

        message.DaGhim = !message.DaGhim;
        await _db.SaveChangesAsync();
        return await ToMessageDTOAsync(message, currentUserId);
    }

    public async Task<ChatMessageDTO> RecallMessageAsync(int currentUserId, long messageId)
    {
        var message = await _db.ChatMessages.FindAsync(messageId)
            ?? throw new ValidationApiException("Tin nhan khong ton tai.");
        await EnsureConversationParticipantAsync(currentUserId, message.MaCuocTroChuyen);
        if (message.MaNguoiGui != currentUserId)
            throw new ValidationApiException("Chi nguoi gui moi duoc thu hoi tin nhan.");

        message.DaThuHoi = true;
        message.NoiDung = "Tin nhan da duoc thu hoi";
        message.TenFile = string.Empty;
        message.DuongDanFile = string.Empty;
        message.CamXuc = string.Empty;
        await _db.SaveChangesAsync();
        return await ToMessageDTOAsync(message, currentUserId);
    }

    public async Task<ChatMessageDTO> ReactMessageAsync(int currentUserId, long messageId, ReactMessageRequest request)
    {
        var message = await _db.ChatMessages.FindAsync(messageId)
            ?? throw new ValidationApiException("Tin nhan khong ton tai.");
        await EnsureConversationParticipantAsync(currentUserId, message.MaCuocTroChuyen);

        var reaction = request.Reaction.Trim();
        if (string.IsNullOrWhiteSpace(reaction))
            throw new ValidationApiException("Cam xuc khong duoc de trong.");

        message.CamXuc = reaction;
        await _db.SaveChangesAsync();
        return await ToMessageDTOAsync(message, currentUserId);
    }

    public async Task MarkReadAsync(int currentUserId, int conversationId)
    {
        await EnsureConversationParticipantAsync(currentUserId, conversationId);

        var now = DateTime.UtcNow;
        var unread = await _db.ChatMessages
            .Where(x => x.MaCuocTroChuyen == conversationId
                && x.MaNguoiGui != currentUserId
                && x.ThoiGianDoc == null)
            .ToListAsync();

        foreach (var message in unread)
            message.ThoiGianDoc = now;

        if (unread.Count > 0)
            await _db.SaveChangesAsync();
    }

    public async Task<List<int>> GetParticipantIdsAsync(int conversationId)
    {
        var participantIds = await _db.ChatParticipants
            .Where(x => x.MaCuocTroChuyen == conversationId)
            .Select(x => x.MaNguoiDung)
            .ToListAsync();

        if (participantIds.Count > 0)
            return participantIds;

        var conversation = await _db.ChatConversations.FindAsync(conversationId);
        return conversation == null
            ? []
            : [conversation.MaNguoiDung1, conversation.MaNguoiDung2];
    }

    public async Task<ChatConversation?> FindConversationAsync(int conversationId)
    {
        return await _db.ChatConversations.FindAsync(conversationId);
    }

    private async Task<ChatConversation> EnsureConversationParticipantAsync(int currentUserId, int conversationId)
    {
        var conversation = await _db.ChatConversations
            .Include(x => x.ThanhViens)
            .FirstOrDefaultAsync(x => x.MaCuocTroChuyen == conversationId)
            ?? throw new ValidationApiException("Cuoc tro chuyen khong ton tai.");

        var isParticipant = conversation.ThanhViens.Any(x => x.MaNguoiDung == currentUserId)
            || (!conversation.LaNhom && (conversation.MaNguoiDung1 == currentUserId || conversation.MaNguoiDung2 == currentUserId));
        if (!isParticipant)
            throw new ValidationApiException("Ban khong thuoc cuoc tro chuyen nay.");

        return conversation;
    }

    private async Task EnsureInternalUserAsync(int userId)
    {
        if (await GetInternalUserAsync(userId) == null)
            throw new ValidationApiException("Chi co the chat voi tai khoan noi bo dang hoat dong.");
    }

    private async Task<ChatUserDTO?> GetInternalUserAsync(int userId)
    {
        return await _db.NguoiDung
            .Where(x => x.MaNguoiDung == userId && x.IsActive && InternalRoleIds.Contains(x.MaVaiTro))
            .Select(x => new ChatUserDTO
            {
                UserId = x.MaNguoiDung,
                Username = x.TenDangNhap,
                Email = x.Email,
                Role = x.MaVaiTro == 4 ? "admin" : x.MaVaiTro == 3 ? "manager" : "staff"
            })
            .FirstOrDefaultAsync();
    }

    private async Task<ChatConversationDTO> ToConversationDTOAsync(ChatConversation conversation, int currentUserId)
    {
        var participantIds = conversation.ThanhViens.Select(x => x.MaNguoiDung).ToList();
        if (participantIds.Count == 0 && !conversation.LaNhom)
            participantIds = [conversation.MaNguoiDung1, conversation.MaNguoiDung2];

        var participants = await _db.NguoiDung
            .Where(x => participantIds.Contains(x.MaNguoiDung))
            .Select(x => new ChatUserDTO
            {
                UserId = x.MaNguoiDung,
                Username = x.TenDangNhap,
                Email = x.Email,
                Role = x.MaVaiTro == 4 ? "admin" : x.MaVaiTro == 3 ? "manager" : "staff"
            })
            .ToListAsync();

        var otherUser = participants.FirstOrDefault(x => x.UserId != currentUserId) ?? new ChatUserDTO();
        var lastMessage = await _db.ChatMessages
            .Where(x => x.MaCuocTroChuyen == conversation.MaCuocTroChuyen)
            .OrderByDescending(x => x.ThoiGianGui)
            .FirstOrDefaultAsync();

        var unreadCount = await _db.ChatMessages
            .CountAsync(x => x.MaCuocTroChuyen == conversation.MaCuocTroChuyen
                && x.MaNguoiGui != currentUserId
                && x.ThoiGianDoc == null);

        return new ChatConversationDTO
        {
            ConversationId = conversation.MaCuocTroChuyen,
            IsGroup = conversation.LaNhom,
            Title = conversation.LaNhom ? conversation.TenNhom : otherUser.Username,
            AvatarUrl = conversation.AnhDaiDien,
            OtherUser = otherUser,
            Participants = participants,
            LastMessage = lastMessage?.NoiDung ?? string.Empty,
            LastMessageType = lastMessage?.LoaiTinNhan ?? "text",
            LastMessageAt = lastMessage?.ThoiGianGui,
            UnreadCount = unreadCount
        };
    }

    private static ChatMessageDTO ToMessageDTO(ChatMessageEntity message) => new()
    {
        MessageId = message.MaTinNhan,
        ConversationId = message.MaCuocTroChuyen,
        SenderId = message.MaNguoiGui,
        ReceiverId = message.MaNguoiNhan,
        Content = message.NoiDung,
        MessageType = message.LoaiTinNhan,
        FileName = message.TenFile,
        FileUrl = message.DuongDanFile,
        CallStatus = message.TrangThaiCuocGoi,
        CallDurationSeconds = message.ThoiLuongCuocGoiGiay,
        IsPinned = message.DaGhim,
        IsRecalled = message.DaThuHoi,
        Reaction = message.CamXuc,
        SentAt = message.ThoiGianGui,
        ReadAt = message.ThoiGianDoc
    };

    private async Task<List<ChatMessageDTO>> ToMessageDTOsAsync(List<ChatMessageEntity> messages, int currentUserId)
    {
        var result = new List<ChatMessageDTO>();
        foreach (var message in messages)
            result.Add(await ToMessageDTOAsync(message, currentUserId));

        return result;
    }

    private async Task<ChatMessageDTO> ToMessageDTOAsync(ChatMessageEntity message, int currentUserId)
    {
        var dto = ToMessageDTO(message);
        if (message.LoaiTinNhan != "poll")
            return dto;

        var poll = await _db.ChatPolls
            .Include(x => x.LuaChons)
            .ThenInclude(x => x.BinhChons)
            .FirstOrDefaultAsync(x => x.MaTinNhan == message.MaTinNhan);
        if (poll == null)
            return dto;

        var votedByCurrentUser = poll.LuaChons.Any(x => x.BinhChons.Any(v => v.MaNguoiDung == currentUserId));
        var resultsHidden = poll.AnKetQuaKhiChuaBinhChon && !votedByCurrentUser;
        var voterIds = poll.LuaChons
            .SelectMany(x => x.BinhChons)
            .Select(x => x.MaNguoiDung)
            .Distinct()
            .ToList();
        var voters = voterIds.Count == 0 || poll.AnNguoiBinhChon
            ? new Dictionary<int, ChatPollVoterDTO>()
            : await _db.NguoiDung
                .Where(x => voterIds.Contains(x.MaNguoiDung))
                .Select(x => new ChatPollVoterDTO
                {
                    UserId = x.MaNguoiDung,
                    Username = x.TenDangNhap,
                    Initials = x.TenDangNhap.Length == 0 ? "?" : x.TenDangNhap.Substring(0, 1).ToUpper()
                })
                .ToDictionaryAsync(x => x.UserId);

        dto.Poll = new ChatPollDTO
        {
            PollId = poll.MaThamDo,
            Question = poll.CauHoi,
            AllowMultipleChoices = poll.ChoPhepNhieuLuaChon,
            AllowAddOptions = poll.ChoPhepThemLuaChon,
            HideResultsUntilVoted = poll.AnKetQuaKhiChuaBinhChon,
            HideVoters = poll.AnNguoiBinhChon,
            ResultsHidden = resultsHidden,
            IsClosed = poll.DaKhoa || (poll.KetThucLuc.HasValue && poll.KetThucLuc.Value <= DateTime.UtcNow),
            EndsAt = poll.KetThucLuc,
            Options = poll.LuaChons
                .OrderBy(x => x.MaLuaChon)
                .Select(x => new ChatPollOptionDTO
                {
                    OptionId = x.MaLuaChon,
                    Text = x.NoiDung,
                    VoteCount = resultsHidden ? -1 : x.BinhChons.Count,
                    VotedByMe = x.BinhChons.Any(v => v.MaNguoiDung == currentUserId),
                    Voters = resultsHidden || poll.AnNguoiBinhChon
                        ? new List<ChatPollVoterDTO>()
                        : x.BinhChons
                            .Select(v => voters.TryGetValue(v.MaNguoiDung, out var voter) ? voter : null)
                            .Where(v => v != null)
                            .Select(v => v!)
                            .ToList()
                })
                .ToList()
        };

        return dto;
    }

    private static (int FirstUserId, int SecondUserId) OrderedPair(int firstUserId, int secondUserId)
    {
        return firstUserId < secondUserId
            ? (firstUserId, secondUserId)
            : (secondUserId, firstUserId);
    }

    private static string NormalizeMessageType(string value)
    {
        var type = string.IsNullOrWhiteSpace(value) ? "text" : value.Trim().ToLowerInvariant();
        return AllowedMessageTypes.Contains(type) ? type : "text";
    }
}
