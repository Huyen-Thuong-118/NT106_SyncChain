using System.ComponentModel.DataAnnotations;

namespace SyncChain.API.Models;

public class ChatConversation
{
    [Key]
    public int MaCuocTroChuyen { get; set; }

    public int MaNguoiDung1 { get; set; }

    public int MaNguoiDung2 { get; set; }

    public bool LaNhom { get; set; }

    [MaxLength(150)]
    public string TenNhom { get; set; } = string.Empty;

    [MaxLength(500)]
    public string AnhDaiDien { get; set; } = string.Empty;

    public int? MaNguoiTao { get; set; }

    public DateTime NgayTao { get; set; } = DateTime.UtcNow;

    public DateTime CapNhatLuc { get; set; } = DateTime.UtcNow;

    public List<ChatParticipant> ThanhViens { get; set; } = new();

    public List<ChatMessageEntity> TinNhans { get; set; } = new();
}
