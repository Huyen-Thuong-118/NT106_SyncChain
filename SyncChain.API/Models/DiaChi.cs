using System.ComponentModel.DataAnnotations;

namespace SyncChain.API.Models;

public class DiaChi
{
    [Key]
    public int MaDiaChi { get; set; }
    public int MaNguoiDung { get; set; }
    public string TenNguoiNhan { get; set; } = string.Empty;
    public string SoDienThoai { get; set; } = string.Empty;
    public string TinhThanh { get; set; } = string.Empty;
    public string QuanHuyen { get; set; } = string.Empty;
    public string PhuongXa { get; set; } = string.Empty;
    public string DiaChiChiTiet { get; set; } = string.Empty;
    public bool LaMacDinh { get; set; } = false;
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;

    public NguoiDung NguoiDung { get; set; } = null!;
}
