// Dữ liệu tạo đơn hàng gồm nhiều dòng sản phẩm.
public class CreateOrderDTO
{
    public List<OrderItemDTO> Items { get; set; } = new();
    public int? MaDiaChi { get; set; }
}
