using System.ComponentModel.DataAnnotations;

namespace SyncChain.API.Models;

// Entity lưu thông tin sản phẩm và tồn kho.
public class SanPham
{
    [Key]
    public int MaSanPham { get; set; }

    public string TenSanPham { get; set; } = string.Empty;

    public decimal GiaBan { get; set; }

    public decimal GiaNhap { get; set; }

    public int SoLuongTon { get; set; }

    public int TonKhoBanDau { get; set; }

    public int MucTonThap { get; set; } = 10;

    public string TrangThai { get; set; } = "Hoat dong";

    public string HinhAnhUrl { get; set; } = string.Empty;

    public string MoTa { get; set; } = string.Empty;

    public int? MaDanhMuc { get; set; }

    public DanhMucSanPham? DanhMuc { get; set; }
}
