using System.ComponentModel.DataAnnotations;

namespace SyncChain.API.Models;

public class OtpReset
{
    [Key]
    public int MaOtp { get; set; }
    public int MaNguoiDung { get; set; }
    public string MaOtpHash { get; set; } = string.Empty;
    public DateTime HetHan { get; set; }
    public bool DaDung { get; set; } = false;
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;

    public NguoiDung NguoiDung { get; set; } = null!;
}
