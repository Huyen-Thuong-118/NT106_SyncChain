using System.ComponentModel.DataAnnotations;

namespace SyncChain.API.Models;

public class ChiTietPhieuXuat
{
    [Key]
    public int MaChiTiet { get; set; }

    public int MaPhieuXuat { get; set; }
    public int MaSanPham { get; set; }
    public int SoLuong { get; set; }

    public PhieuXuatKho PhieuXuatKho { get; set; } = null!;
    public SanPham SanPham { get; set; } = null!;
}
