using System.ComponentModel.DataAnnotations;

namespace SyncChain.API.Models;

public class ThongBao
{
    [Key]
    public int MaThongBao { get; set; }
    public int MaNguoiDung { get; set; }
    public string LoaiThongBao { get; set; } = string.Empty;
    public string TieuDe { get; set; } = string.Empty;
    public string NoiDung { get; set; } = string.Empty;
    public bool DaDoc { get; set; } = false;
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
    public int? MaDonHang { get; set; }
}
