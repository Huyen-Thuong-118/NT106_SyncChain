namespace SyncChain.API.DTOs.WarehouseReceipt;

public class WarehouseReceiptResponseDTO
{
    public int MaPhieuNhap { get; set; }
    public string SoPhieu { get; set; } = string.Empty;
    public string TenNguonNhap { get; set; } = string.Empty;
    public string DiaChiNguonNhap { get; set; } = string.Empty;
    public string NguoiLienHe { get; set; } = string.Empty;
    public string GhiChu { get; set; } = string.Empty;
    public string TrangThai { get; set; } = string.Empty;
    public DateTime NgayTao { get; set; }
    public DateTime? NgayDuyet { get; set; }
    public DateTime? NgayHoanTat { get; set; }
    public int MaNguoiTao { get; set; }
    public int? MaNguoiDuyet { get; set; }
    public decimal TongTien { get; set; }
    public List<WarehouseReceiptItemResponseDTO> ChiTiet { get; set; } = new();
}

public class WarehouseReceiptItemResponseDTO
{
    public int MaChiTiet { get; set; }
    public int MaSanPham { get; set; }
    public string TenSanPham { get; set; } = string.Empty;
    public int SoLuong { get; set; }
    public decimal DonGiaNhap { get; set; }
    public decimal ThanhTien { get; set; }
}
