using System.ComponentModel.DataAnnotations;

namespace SyncChain.API.Models;

public class ChatPollOption
{
    [Key]
    public int MaLuaChon { get; set; }

    public int MaThamDo { get; set; }

    [MaxLength(200)]
    public string NoiDung { get; set; } = string.Empty;

    public ChatPoll? ThamDo { get; set; }

    public List<ChatPollVote> BinhChons { get; set; } = new();
}
