using Microsoft.EntityFrameworkCore;
using SyncChain.API.Data;
using SyncChain.API.Models;

namespace SyncChain.API.Services;

public class ShippingAutoCompletionService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ShippingAutoCompletionService> _logger;

    public ShippingAutoCompletionService(
        IServiceScopeFactory scopeFactory,
        ILogger<ShippingAutoCompletionService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var threshold = DateTime.UtcNow.AddDays(-7);
                var shipments = await db.VanChuyen
                    .Include(x => x.DonHang)
                    .Where(x => x.DonHang.TrangThai == OrderStatuses.Shipping &&
                                x.NgayGiaoDuKien != null &&
                                x.NgayGiaoDuKien <= threshold &&
                                x.TrangThaiGiaoHang != ShippingStatuses.Delivered)
                    .ToListAsync(stoppingToken);
                foreach (var shipping in shipments)
                {
                    shipping.TrangThaiGiaoHang = ShippingStatuses.Delivered;
                    shipping.NgayGiaoThucTe = DateTime.UtcNow;
                    shipping.NgayCapNhat = DateTime.UtcNow;
                    shipping.ConcurrencyVersion++;
                    shipping.DonHang.TrangThai = OrderStatuses.Done;
                    shipping.DonHang.ConcurrencyVersion++;
                }
                if (shipments.Count > 0)
                    await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Khong the tu dong hoan thanh don giao qua han.");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
