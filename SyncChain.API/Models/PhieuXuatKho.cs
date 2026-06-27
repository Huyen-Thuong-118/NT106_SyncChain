using System.ComponentModel.DataAnnotations;

namespace SyncChain.API.Models;

public class PhieuXuatKho
{
    [Key]
    public int MaPhieuXuat { get; set; }

    public string SoPhieu { get; set; } = string.Empty;
    public string LyDoXuat { get; set; } = string.Empty;
    public string GhiChu { get; set; } = string.Empty;
    public string TrangThai { get; set; } = WarehouseIssueStatuses.Draft;
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
    public DateTime? NgayHoanTat { get; set; }
    public int MaNguoiTao { get; set; }
    public int? MaNguoiHoanTat { get; set; }

    public List<ChiTietPhieuXuat> ChiTietPhieuXuat { get; set; } = new();
}

public static class WarehouseIssueStatuses
{
    public const string Draft = "draft";
    public const string Pending = "pending";
    public const string Completed = "completed";
    public const string Cancelled = "cancelled";

    public static readonly string[] All =
    {
        Draft,
        Pending,
        Completed,
        Cancelled
    };
}
