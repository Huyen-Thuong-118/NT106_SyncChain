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

    // Thông tin giao hàng — lưu tại thời điểm đặt, không bị ảnh hưởng nếu khách sau đó đổi địa chỉ.
    public string? NguoiNhan { get; set; }
    public string? SoDienThoaiNhan { get; set; }
    public string? DiaChiGiao { get; set; }

    public string? PhuongThucThanhToan { get; set; }

    public List<ChiTietDonHang> ChiTietDonHang { get; set; } = new();
}
