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

    public List<ChiTietDonHang> ChiTietDonHang { get; set; } = new();
}
