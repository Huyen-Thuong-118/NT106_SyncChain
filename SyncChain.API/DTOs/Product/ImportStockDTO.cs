namespace SyncChain.API.DTOs.Product;

// Dữ liệu nhập thêm hàng vào kho.
public class ImportStockDTO
{
    public int SoLuong { get; set; }
    public string GhiChu { get; set; } = string.Empty;
}
