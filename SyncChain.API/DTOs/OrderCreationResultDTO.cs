namespace SyncChain.API.DTOs;

public class OrderCreationResultDTO
{
    public string Message { get; set; } = string.Empty;
    public int MaDonHang { get; set; }
    public decimal TongTien { get; set; }
    public bool IsReplay { get; set; }
}
