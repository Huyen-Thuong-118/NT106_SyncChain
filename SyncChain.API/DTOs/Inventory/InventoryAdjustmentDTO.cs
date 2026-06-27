namespace SyncChain.API.DTOs.Inventory;

public class InventoryAdjustmentDTO
{
    public int MaSanPham { get; set; }
    public int SoLuongThayDoi { get; set; }
    public string LyDo { get; set; } = string.Empty;
    public string GhiChu { get; set; } = string.Empty;
}
