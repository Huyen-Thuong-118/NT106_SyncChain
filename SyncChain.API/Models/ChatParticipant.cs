using System.ComponentModel.DataAnnotations;

namespace SyncChain.API.Models;

public class ChatParticipant
{
    [Key]
    public int MaThanhVien { get; set; }

    public int MaCuocTroChuyen { get; set; }

    public int MaNguoiDung { get; set; }

    public DateTime ThamGiaLuc { get; set; } = DateTime.UtcNow;

    public ChatConversation? CuocTroChuyen { get; set; }
}
