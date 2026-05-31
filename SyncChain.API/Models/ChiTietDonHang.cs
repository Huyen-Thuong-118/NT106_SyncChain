using System.ComponentModel.DataAnnotations;
using SyncChain.API.Models;

// Entity lưu từng dòng sản phẩm trong đơn hàng.
public class ChiTietDonHang
{
    [Key]
    public int MaChiTiet { get; set; }

    public int MaDonHang { get; set; }

    public int MaSanPham { get; set; }

    public int SoLuong { get; set; }

    public decimal DonGia { get; set; }

    public DonHang? DonHang { get; set; }
    public SanPham SanPham { get; set; } = null!;
}
