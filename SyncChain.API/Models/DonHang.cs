using System.ComponentModel.DataAnnotations;

// Entity lưu đơn hàng của người dùng.
public class DonHang
{
    [Key]
    public int MaDonHang { get; set; }

    public int MaNguoiDung { get; set; }

    public decimal TongTien { get; set; }

    public DateTime NgayTao { get; set; } = DateTime.Now;

    public string TrangThai { get; set; } = "Draft";

    [MaxLength(100)]
    public string? IdempotencyKey { get; set; }

    public int ConcurrencyVersion { get; set; }

    [MaxLength(150)]
    public string TenNguoiNhan { get; set; } = string.Empty;

    [MaxLength(30)]
    public string SoDienThoai { get; set; } = string.Empty;

    [MaxLength(150)]
    public string EmailNguoiNhan { get; set; } = string.Empty;

    [MaxLength(500)]
    public string DiaChiGiaoHang { get; set; } = string.Empty;

    [MaxLength(100)]
    public string TinhThanh { get; set; } = string.Empty;

    [MaxLength(100)]
    public string PhuongXa { get; set; } = string.Empty;

    [MaxLength(30)]
    public string LoaiDichVu { get; set; } = "Tieu chuan";

    public decimal TrongLuongKg { get; set; } = 1;

    [MaxLength(500)]
    public string GhiChu { get; set; } = string.Empty;

    // Phương thức thanh toán (module khách hàng): cod | vnpay | momo.
    [MaxLength(30)]
    public string PhuongThucThanhToan { get; set; } = "cod";

    public List<ChiTietDonHang> ChiTietDonHang { get; set; } = new();
    public SyncChain.API.Models.VanChuyen? VanChuyen { get; set; }
}
