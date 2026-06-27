using System.ComponentModel.DataAnnotations;

namespace SyncChain.API.Models;

public class ChatPoll
{
    [Key]
    public int MaThamDo { get; set; }

    public long MaTinNhan { get; set; }

    [MaxLength(300)]
    public string CauHoi { get; set; } = string.Empty;

    public bool ChoPhepNhieuLuaChon { get; set; }

    public bool ChoPhepThemLuaChon { get; set; }

    public bool AnKetQuaKhiChuaBinhChon { get; set; }

    public bool AnNguoiBinhChon { get; set; }

    public bool DaKhoa { get; set; }

    public DateTime? KetThucLuc { get; set; }

    public List<ChatPollOption> LuaChons { get; set; } = new();

    public ChatMessageEntity? TinNhan { get; set; }
}
