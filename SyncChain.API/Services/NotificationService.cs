using Microsoft.AspNetCore.SignalR;
using SyncChain.API.Data;
using SyncChain.API.Hubs;
using SyncChain.API.Models;

namespace SyncChain.API.Services;

public interface INotificationService
{
    Task SaveNotificationAsync(int userId, string type, string title, string content, int? orderId = null);
    Task PushOrderStatusAsync(int userId, int orderId, string status);
    Task PushPaymentResultAsync(int userId, int orderId, bool success, string method);
}

public class NotificationService : INotificationService
{
    private readonly IHubContext<OrderHub> _hub;
    private readonly AppDbContext _db;

    public NotificationService(IHubContext<OrderHub> hub, AppDbContext db)
    {
        _hub = hub;
        _db  = db;
    }

    public async Task SaveNotificationAsync(int userId, string type, string title, string content, int? orderId = null)
    {
        _db.ThongBao.Add(new ThongBao
        {
            MaNguoiDung  = userId,
            LoaiThongBao = type,
            TieuDe       = title,
            NoiDung      = content,
            MaDonHang    = orderId,
            NgayTao      = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    public async Task PushOrderStatusAsync(int userId, int orderId, string status)
    {
        var statusVi = status switch
        {
            OrderStatuses.Pending    => "Chờ xử lý",
            OrderStatuses.Processing => "Đang xử lý",
            OrderStatuses.Shipping   => "Đang giao",
            OrderStatuses.Done       => "Hoàn tất",
            OrderStatuses.Cancel     => "Đã hủy",
            _                        => status
        };
        await SaveNotificationAsync(userId, "order_status",
            $"Đơn hàng #{orderId} cập nhật",
            $"Trạng thái đơn hàng của bạn: {statusVi}",
            orderId);

        var ts = DateTime.UtcNow.ToString("o");
        await _hub.Clients.Group($"user_{userId}")
                  .SendAsync("OrderStatusUpdated", orderId, status, ts);
        await _hub.Clients.Group("staff")
                  .SendAsync("OrderStatusUpdated", orderId, status, ts);
    }

    public async Task PushPaymentResultAsync(int userId, int orderId, bool success, string method)
    {
        var title   = success ? "Thanh toán thành công" : "Thanh toán thất bại";
        var content = success
            ? $"Đơn hàng #{orderId} đã thanh toán qua {method.ToUpperInvariant()}"
            : $"Thanh toán đơn hàng #{orderId} thất bại";

        await SaveNotificationAsync(userId, "payment_result", title, content, orderId);

        await _hub.Clients.Group($"user_{userId}")
                  .SendAsync("PaymentResult", orderId, success, method, DateTime.UtcNow.ToString("o"));
    }
}
