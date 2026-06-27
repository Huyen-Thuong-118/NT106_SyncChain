using System.ComponentModel.DataAnnotations;

namespace SyncChain.API.Models;

public class ChatMessageEntity
{
    [Key]
    public long MaTinNhan { get; set; }

    public int MaCuocTroChuyen { get; set; }

    public int MaNguoiGui { get; set; }

    public int MaNguoiNhan { get; set; }

    [MaxLength(2000)]
    public string NoiDung { get; set; } = string.Empty;

    [MaxLength(30)]
    public string LoaiTinNhan { get; set; } = "text";

    [MaxLength(255)]
    public string TenFile { get; set; } = string.Empty;

    [MaxLength(500)]
    public string DuongDanFile { get; set; } = string.Empty;

    [MaxLength(50)]
    public string TrangThaiCuocGoi { get; set; } = string.Empty;

    public int? ThoiLuongCuocGoiGiay { get; set; }

    public bool DaGhim { get; set; }

    public bool DaThuHoi { get; set; }

    [MaxLength(20)]
    public string CamXuc { get; set; } = string.Empty;

    public DateTime ThoiGianGui { get; set; } = DateTime.UtcNow;

    public DateTime? ThoiGianDoc { get; set; }

    public ChatConversation? CuocTroChuyen { get; set; }
}
