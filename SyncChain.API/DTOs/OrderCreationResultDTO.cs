namespace SyncChain.API.DTOs;

public class OrderCreationResultDTO
{
    public string Message { get; set; } = string.Empty;
    public int MaDonHang { get; set; }

    // Tach ro de client hien thi minh bach: Subtotal + ShippingFee = TongTien.
    public decimal Subtotal { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal TongTien { get; set; }

    public bool IsReplay { get; set; }
}
