namespace SyncChain.API.Models;
using System.ComponentModel.DataAnnotations;
public class NguoiDung
{
    [Key]
    public int  MaNguoiDung  { get; set; }
    public string TenDangNhap { get; set; } = string.Empty;
    public string MatKhauHash { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int MaVaiTro { get; set; }
    public bool IsActive { get; set; } = true;
}