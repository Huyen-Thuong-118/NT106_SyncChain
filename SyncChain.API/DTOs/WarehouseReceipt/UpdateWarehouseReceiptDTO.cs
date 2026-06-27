namespace SyncChain.API.DTOs.WarehouseReceipt;

public class UpdateWarehouseReceiptDTO
{
    public string TenNguonNhap { get; set; } = string.Empty;
    public string DiaChiNguonNhap { get; set; } = string.Empty;
    public string NguoiLienHe { get; set; } = string.Empty;
    public string GhiChu { get; set; } = string.Empty;
    public List<WarehouseReceiptItemDTO> ChiTiet { get; set; } = new();
}
