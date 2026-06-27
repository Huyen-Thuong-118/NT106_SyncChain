using System.ComponentModel.DataAnnotations;

namespace SyncChain.API.Models;

public class AuditLog
{
    [Key]
    public long MaAudit { get; set; }
    public int? MaNguoiDung { get; set; }

    [MaxLength(150)]
    public string TenDangNhap { get; set; } = string.Empty;

    [MaxLength(50)]
    public string VaiTro { get; set; } = string.Empty;

    [MaxLength(100)]
    public string HanhDong { get; set; } = string.Empty;

    [MaxLength(100)]
    public string LoaiDoiTuong { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? MaDoiTuong { get; set; }

    [MaxLength(20)]
    public string TrangThaiKetQua { get; set; } = AuditResultStatuses.Success;

    public string DuLieuTruoc { get; set; } = "{}";
    public string DuLieuSau { get; set; } = "{}";
    public string Metadata { get; set; } = "{}";
    public DateTime ThoiGian { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string TraceId { get; set; } = string.Empty;

    [MaxLength(64)]
    public string IpAddress { get; set; } = string.Empty;

    [MaxLength(500)]
    public string UserAgent { get; set; } = string.Empty;
}
