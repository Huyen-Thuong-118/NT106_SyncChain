using System.ComponentModel.DataAnnotations;

namespace SyncChain.API.Models;

public class GioHang
{
    [Key]
    public int MaGioHang { get; set; }
    public int MaNguoiDung { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
    public DateTime NgayCapNhat { get; set; } = DateTime.UtcNow;

    public List<ChiTietGioHang> ChiTietGioHang { get; set; } = new();
}
