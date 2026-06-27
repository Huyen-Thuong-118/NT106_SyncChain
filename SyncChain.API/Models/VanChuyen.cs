using System.ComponentModel.DataAnnotations;

namespace SyncChain.API.Models;

public class VanChuyen
{
    [Key]
    public int MaVanChuyen { get; set; }

    public int MaDonHang { get; set; }

    [MaxLength(100)]
    public string DonViVanChuyen { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? MaVanDon { get; set; }

    public decimal PhiVanChuyen { get; set; }

    [MaxLength(30)]
    public string TrangThaiGiaoHang { get; set; } = ShippingStatuses.Pending;

    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
    public DateTime NgayCapNhat { get; set; } = DateTime.UtcNow;
    public DateTime? NgayGiaoDuKien { get; set; }
    public DateTime? NgayGiaoThucTe { get; set; }
    public int ConcurrencyVersion { get; set; }

    public DonHang DonHang { get; set; } = null!;
    public List<LichSuVanChuyen> LichSu { get; set; } = new();
}
