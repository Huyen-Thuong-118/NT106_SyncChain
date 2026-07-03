using System.ComponentModel.DataAnnotations;

namespace SyncChain.API.Models;

public class ChiTietGioHang
{
    [Key]
    public int MaChiTietGio { get; set; }
    public int MaGioHang { get; set; }
    public int MaSanPham { get; set; }
    public int SoLuong { get; set; }
    public DateTime NgayThem { get; set; } = DateTime.UtcNow;

    public GioHang GioHang { get; set; } = null!;
    public SanPham SanPham { get; set; } = null!;
}
