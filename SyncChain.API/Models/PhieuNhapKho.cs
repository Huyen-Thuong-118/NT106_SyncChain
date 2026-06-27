using System.ComponentModel.DataAnnotations;

namespace SyncChain.API.Models;

public class PhieuNhapKho
{
    [Key]
    public int MaPhieuNhap { get; set; }

    public string SoPhieu { get; set; } = string.Empty;
    public string TenNguonNhap { get; set; } = string.Empty;
    public string DiaChiNguonNhap { get; set; } = string.Empty;
    public string NguoiLienHe { get; set; } = string.Empty;
    public string GhiChu { get; set; } = string.Empty;
    public string TrangThai { get; set; } = WarehouseReceiptStatuses.Draft;
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
    public DateTime? NgayDuyet { get; set; }
    public DateTime? NgayHoanTat { get; set; }
    public int MaNguoiTao { get; set; }
    public int? MaNguoiDuyet { get; set; }

    public List<ChiTietPhieuNhap> ChiTietPhieuNhap { get; set; } = new();
}

public static class WarehouseReceiptStatuses
{
    public const string Draft = "draft";
    public const string Pending = "pending";
    public const string Approved = "approved";
    public const string Completed = "completed";
    public const string Cancelled = "cancelled";

    public static readonly string[] All =
    {
        Draft,
        Pending,
        Approved,
        Completed,
        Cancelled
    };
}
