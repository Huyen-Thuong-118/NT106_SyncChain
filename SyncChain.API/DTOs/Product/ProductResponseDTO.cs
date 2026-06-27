namespace SyncChain.API.DTOs.Product;

public class ProductResponseDTO
{
    public int MaSanPham { get; set; }
    public string TenSanPham { get; set; } = string.Empty;
    public decimal GiaBan { get; set; }
    public decimal GiaNhap { get; set; }
    public int SoLuongTon { get; set; }
    public int TonKhoBanDau { get; set; }
    public int MucTonThap { get; set; }
    public string TrangThai { get; set; } = string.Empty;
    public string HinhAnhUrl { get; set; } = string.Empty;
    public string MoTa { get; set; } = string.Empty;
    public int? MaDanhMuc { get; set; }
    public ProductCategoryResponseDTO? DanhMuc { get; set; }
    public int DaBanThangNay { get; set; }
    public int DaBanThangTruoc { get; set; }
    public decimal HieuSuatPhanTram { get; set; }
}

public class ProductCategoryResponseDTO
{
    public int MaDanhMuc { get; set; }
    public string TenDanhMuc { get; set; } = string.Empty;
    public string MoTa { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
