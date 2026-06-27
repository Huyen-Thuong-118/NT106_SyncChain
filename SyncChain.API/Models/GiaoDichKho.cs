using System.ComponentModel.DataAnnotations;

namespace SyncChain.API.Models;

// Entity lưu lịch sử nhập/xuất kho của sản phẩm.
public class GiaoDichKho
{
    [Key]
    public int MaGiaoDich { get; set; }

    public int MaSanPham { get; set; }

    public string Loai { get; set; } = string.Empty;

    public int SoLuong { get; set; }

    public int TonTruoc { get; set; }

    public int TonSau { get; set; }

    public DateTime ThoiGian { get; set; } = DateTime.Now;

    public int? MaNguoiDung { get; set; }

    public string GhiChu { get; set; } = string.Empty;

    public int? MaPhieuNhap { get; set; }

    public string NguonNhap { get; set; } = string.Empty;

    public int? MaPhieuXuat { get; set; }

    public string LyDoXuat { get; set; } = string.Empty;

    public int? MaDonHang { get; set; }

    public SanPham SanPham { get; set; } = null!;

    public PhieuNhapKho? PhieuNhapKho { get; set; }

    public PhieuXuatKho? PhieuXuatKho { get; set; }

    public DonHang? DonHang { get; set; }
}
