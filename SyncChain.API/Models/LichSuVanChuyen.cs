using System.ComponentModel.DataAnnotations;

namespace SyncChain.API.Models;

public class LichSuVanChuyen
{
    [Key]
    public long MaLichSu { get; set; }

    public int MaVanChuyen { get; set; }

    [MaxLength(30)]
    public string TrangThaiCu { get; set; } = string.Empty;

    [MaxLength(30)]
    public string TrangThaiMoi { get; set; } = string.Empty;

    public DateTime ThoiGian { get; set; } = DateTime.UtcNow;
    public int? MaNguoiDung { get; set; }

    [MaxLength(500)]
    public string GhiChu { get; set; } = string.Empty;

    [MaxLength(100)]
    public string TraceId { get; set; } = string.Empty;

    public VanChuyen VanChuyen { get; set; } = null!;
}
