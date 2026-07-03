using System.ComponentModel.DataAnnotations;

namespace SyncChain.API.Models;

public class ThanhToan
{
    [Key]
    public int MaThanhToan { get; set; }
    public int MaDonHang { get; set; }
    public string PhuongThuc { get; set; } = string.Empty;
    public string TrangThaiThanhToan { get; set; } = "Pending";
    public decimal SoTien { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
    public DateTime? NgayCapNhat { get; set; }
    public string? MaGiaoDich { get; set; }
    public string? DuLieuCallback { get; set; }

    public DonHang DonHang { get; set; } = null!;
}
