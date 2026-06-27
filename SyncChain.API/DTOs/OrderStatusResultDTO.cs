namespace SyncChain.API.DTOs;

public class OrderStatusResultDTO
{
    public int MaDonHang { get; set; }
    public string TrangThaiCu { get; set; } = string.Empty;
    public string TrangThaiMoi { get; set; } = string.Empty;
    public int ConcurrencyVersion { get; set; }
}
