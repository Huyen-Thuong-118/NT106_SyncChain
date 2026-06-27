namespace SyncChain.API.DTOs.Product;

// Dữ liệu tạo sản phẩm mới.
public class CreateProductDTO
{
    public string TenSanPham { get; set; } = string.Empty;
    public decimal GiaBan { get; set; }
    public decimal GiaNhap { get; set; }
    public int SoLuongTon { get; set; }
    public string HinhAnhUrl { get; set; } = string.Empty;
    public string MoTa { get; set; } = string.Empty;
    public int? MaDanhMuc { get; set; }
}
