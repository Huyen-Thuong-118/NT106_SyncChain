namespace SyncChain.API.DTOs.WarehouseIssue;

public class WarehouseIssueResponseDTO
{
    public int MaPhieuXuat { get; set; }
    public string SoPhieu { get; set; } = string.Empty;
    public string LyDoXuat { get; set; } = string.Empty;
    public string GhiChu { get; set; } = string.Empty;
    public string TrangThai { get; set; } = string.Empty;
    public DateTime NgayTao { get; set; }
    public DateTime? NgayHoanTat { get; set; }
    public int MaNguoiTao { get; set; }
    public int? MaNguoiHoanTat { get; set; }
    public int TongSoLuong { get; set; }
    public List<WarehouseIssueItemResponseDTO> ChiTiet { get; set; } = new();
}

public class WarehouseIssueItemResponseDTO
{
    public int MaChiTiet { get; set; }
    public int MaSanPham { get; set; }
    public string TenSanPham { get; set; } = string.Empty;
    public int SoLuong { get; set; }
}

public class WarehouseIssueHistoryDTO
{
    public int MaGiaoDich { get; set; }
    public int MaPhieuXuat { get; set; }
    public string SoPhieu { get; set; } = string.Empty;
    public int MaSanPham { get; set; }
    public string TenSanPham { get; set; } = string.Empty;
    public int SoLuong { get; set; }
    public string LyDoXuat { get; set; } = string.Empty;
    public int? MaNguoiDung { get; set; }
    public DateTime ThoiGian { get; set; }
    public string GhiChu { get; set; } = string.Empty;
}
