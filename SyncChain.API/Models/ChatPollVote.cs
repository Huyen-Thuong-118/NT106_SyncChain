using System.ComponentModel.DataAnnotations;

namespace SyncChain.API.Models;

public class ChatPollVote
{
    [Key]
    public int MaBinhChon { get; set; }

    public int MaLuaChon { get; set; }

    public int MaNguoiDung { get; set; }

    public DateTime BinhChonLuc { get; set; } = DateTime.UtcNow;

    public ChatPollOption? LuaChon { get; set; }
}
