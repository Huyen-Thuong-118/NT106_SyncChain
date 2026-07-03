namespace SyncChain.API.DTOs.Payment;

public class InitiatePaymentDTO
{
    public int MaDonHang { get; set; }
    public string PhuongThuc { get; set; } = string.Empty; // vnpay | momo | cod
}
