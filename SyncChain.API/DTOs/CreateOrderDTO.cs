// Dữ liệu tạo đơn hàng gồm nhiều dòng sản phẩm.
public class CreateOrderDTO
{
    public List<OrderItemDTO> Items { get; set; } = new();
    public string? IdempotencyKey { get; set; }
    public string SalesChannel { get; set; } = "Online";

    // Khách tự đặt: chỉ gửi mã địa chỉ trong sổ; server nạp thông tin người nhận
    // + địa chỉ giao hàng từ bản ghi DiaChi (không tin dữ liệu địa chỉ từ client).
    public int? MaDiaChi { get; set; }

    public string TenNguoiNhan { get; set; } = string.Empty;
    public string SoDienThoai { get; set; } = string.Empty;
    public string EmailNguoiNhan { get; set; } = string.Empty;
    public string DiaChiGiaoHang { get; set; } = string.Empty;
    public string TinhThanh { get; set; } = string.Empty;
    public string PhuongXa { get; set; } = string.Empty;
    public string LoaiDichVu { get; set; } = "Tieu chuan";
    public decimal TrongLuongKg { get; set; } = 1;
    public decimal ShippingFee { get; set; }
    public string GhiChu { get; set; } = string.Empty;
}
