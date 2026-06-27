namespace SyncChain.API.DTOs.Inventory;

public class CurrentStockDTO
{
    public int MaSanPham { get; set; }
    public string TenSanPham { get; set; } = string.Empty;
    public int TonKhoBanDau { get; set; }
    public int SoLuongTon { get; set; }
    public string TrangThai { get; set; } = string.Empty;
}

public class InventoryChangeResultDTO
{
    public int MaSanPham { get; set; }
    public string TenSanPham { get; set; } = string.Empty;
    public string Loai { get; set; } = string.Empty;
    public int SoLuong { get; set; }
    public int TonTruoc { get; set; }
    public int TonSau { get; set; }
}

public class InventoryTransactionResponseDTO
{
    public int MaGiaoDich { get; set; }
    public int MaSanPham { get; set; }
    public string TenSanPham { get; set; } = string.Empty;
    public string Loai { get; set; } = string.Empty;
    public int SoLuong { get; set; }
    public int TonTruoc { get; set; }
    public int TonSau { get; set; }
    public DateTime ThoiGian { get; set; }
    public int? MaNguoiDung { get; set; }
    public string GhiChu { get; set; } = string.Empty;
    public string NguonNhap { get; set; } = string.Empty;
    public string LyDoXuat { get; set; } = string.Empty;
    public int? MaDonHang { get; set; }
    public int? MaPhieuNhap { get; set; }
    public int? MaPhieuXuat { get; set; }
}

public class InventoryReconcileResultDTO
{
    public int MaSanPham { get; set; }
    public string TenSanPham { get; set; } = string.Empty;
    public int TonHienTai { get; set; }
    public int TonTheoGiaoDich { get; set; }
    public int ChenhLech { get; set; }
    public bool DaDongBo { get; set; }
}
