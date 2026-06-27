using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncChain.API.Models;

public class ChiTietPhieuNhap
{
    [Key]
    public int MaChiTiet { get; set; }

    public int MaPhieuNhap { get; set; }
    public int MaSanPham { get; set; }
    public int SoLuong { get; set; }
    public decimal DonGiaNhap { get; set; }

    [NotMapped]
    public decimal ThanhTien => SoLuong * DonGiaNhap;

    public PhieuNhapKho PhieuNhapKho { get; set; } = null!;
    public SanPham SanPham { get; set; } = null!;
}
