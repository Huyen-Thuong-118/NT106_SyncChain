using System.ComponentModel.DataAnnotations;

namespace SyncChain.API.Models;

public class SanPham
{
    [Key]
    public int MaSanPham { get; set; }

    public string TenSanPham { get; set; } = string.Empty;

    public decimal GiaBan { get; set; }

    public int SoLuongTon { get; set; }

    public int MucTonThap { get; set; } = 10;

    public string TrangThai { get; set; } = "Hoat dong";
}