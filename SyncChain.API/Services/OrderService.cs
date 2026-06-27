using Microsoft.EntityFrameworkCore;
using Npgsql;
using SyncChain.API.Data;
using SyncChain.API.DTOs;
using SyncChain.API.Exceptions;
using SyncChain.API.Models;

namespace SyncChain.API.Services;

public class OrderService
{
    private readonly AppDbContext _db;
    private readonly InventoryService _inventory;
    private readonly IAuditService _audit;
    private readonly DeliveryEstimateService _deliveryEstimate;

    public OrderService(
        AppDbContext db,
        InventoryService inventory,
        IAuditService audit,
        DeliveryEstimateService deliveryEstimate)
    {
        _db = db;
        _inventory = inventory;
        _audit = audit;
        _deliveryEstimate = deliveryEstimate;
    }

    public async Task<OrderCreationResultDTO> CreateOrderAsync(
        int userId,
        CreateOrderDTO dto)
    {
        var idempotencyKey = NormalizeIdempotencyKey(dto.IdempotencyKey);
        if (idempotencyKey != null)
        {
            var existing = await FindByIdempotencyKeyAsync(idempotencyKey);
            if (existing != null)
                return BuildReplayResult(existing, userId);
        }

        var requestedItems = ValidateAndMergeItems(dto);
        var shippingService = NormalizeShippingService(dto.LoaiDichVu);
        if (shippingService == "Hoa toc" &&
            !_deliveryEstimate.SupportsExpress(dto.TinhThanh, dto.PhuongXa))
        {
            throw new ValidationApiException(
                "Hoa toc chi ap dung cho khu vuc noi thanh TP. Ho Chi Minh.");
        }

        try
        {
            return await CreateOrderCoreAsync(
                userId,
                idempotencyKey,
                requestedItems,
                dto);
        }
        catch (DbUpdateException ex) when (IsIdempotencyViolation(ex) && idempotencyKey != null)
        {
            _db.ChangeTracker.Clear();
            var existing = await FindByIdempotencyKeyAsync(idempotencyKey);
            if (existing != null)
                return BuildReplayResult(existing, userId);

            throw new IdempotencyConflictException(
                "Idempotency key dang duoc xu ly, vui long thu lai.");
        }
        catch (DbUpdateException ex) when (IsConcurrencyFailure(ex))
        {
            throw new ConcurrencyConflictException("Xung dot ton kho, vui long thu lai.");
        }
        catch (PostgresException ex) when (IsConcurrencyFailure(ex))
        {
            throw new ConcurrencyConflictException("Xung dot ton kho, vui long thu lai.");
        }
    }

    public async Task<OrderStatusResultDTO> UpdateStatusAsync(
        int orderId,
        UpdateOrderStatusDTO? request,
        string? legacyStatus,
        int? userId)
    {
        var requestedStatus = NormalizeStatus(request?.Status ?? legacyStatus);
        if (!OrderStatuses.All.Contains(requestedStatus))
        {
            throw new ValidationApiException(
                "Trang thai don hang khong hop le.",
                new { orderId, requestedStatus });
        }

        var usesDto = request != null;
        if (usesDto && (string.IsNullOrWhiteSpace(request!.ExpectedStatus) ||
                        request.ConcurrencyVersion == null))
        {
            throw new ValidationApiException(
                "expectedStatus va concurrencyVersion la bat buoc.",
                new { orderId, requestedStatus });
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();

        var snapshot = await GetOrderSnapshotAsync(orderId);
        if (snapshot == null)
            throw new OrderNotFoundException(orderId);

        var expectedStatus = usesDto
            ? NormalizeStatus(request!.ExpectedStatus)
            : snapshot.Status;
        var expectedVersion = usesDto
            ? request!.ConcurrencyVersion!.Value
            : snapshot.Version;

        if (!OrderStatuses.All.Contains(expectedStatus))
        {
            throw new ValidationApiException(
                "Trang thai du kien khong hop le.",
                new { orderId, requestedStatus, expectedStatus, currentVersion = snapshot.Version });
        }

        if (OrderStatuses.IsTerminal(snapshot.Status))
        {
            throw new OrderAlreadyProcessedException(
                orderId,
                snapshot.Status,
                requestedStatus,
                expectedStatus,
                snapshot.Version);
        }

        if (snapshot.Status != expectedStatus || snapshot.Version != expectedVersion)
        {
            throw BuildConcurrencyConflict(
                orderId,
                snapshot,
                requestedStatus,
                expectedStatus);
        }

        if (snapshot.Status == requestedStatus)
        {
            throw new InvalidOrderStateException(
                orderId,
                snapshot.Status,
                requestedStatus,
                expectedStatus,
                snapshot.Version);
        }

        if (!OrderStatuses.CanTransition(snapshot.Status, requestedStatus))
        {
            throw new InvalidOrderStateException(
                orderId,
                snapshot.Status,
                requestedStatus,
                expectedStatus,
                snapshot.Version);
        }

        var changedRows = await _db.DonHang
            .Where(x => x.MaDonHang == orderId &&
                        x.TrangThai == expectedStatus &&
                        x.ConcurrencyVersion == expectedVersion)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.TrangThai, requestedStatus)
                .SetProperty(x => x.ConcurrencyVersion, x => x.ConcurrencyVersion + 1));

        if (changedRows != 1)
        {
            var current = await GetOrderSnapshotAsync(orderId);
            if (current == null)
                throw new OrderNotFoundException(orderId);

            throw BuildConcurrencyConflict(
                orderId,
                current,
                requestedStatus,
                expectedStatus);
        }

        if (requestedStatus == OrderStatuses.Cancel)
            await RestoreCancelledOrderStockAsync(orderId, userId);

        _audit.AddSuccess(
            AuditActions.OrderStatusChange,
            "DonHang",
            orderId.ToString(),
            before: new { status = expectedStatus, version = expectedVersion },
            after: new { status = requestedStatus, version = expectedVersion + 1 },
            metadata: new { stockRestored = requestedStatus == OrderStatuses.Cancel });
        await _db.SaveChangesAsync();

        await transaction.CommitAsync();

        return new OrderStatusResultDTO
        {
            MaDonHang = orderId,
            TrangThaiCu = expectedStatus,
            TrangThaiMoi = requestedStatus,
            ConcurrencyVersion = expectedVersion + 1
        };
    }

    private async Task RestoreCancelledOrderStockAsync(int orderId, int? userId)
    {
        var items = await _db.ChiTietDonHang
            .AsNoTracking()
            .Where(x => x.MaDonHang == orderId)
            .GroupBy(x => x.MaSanPham)
            .Select(group => new
            {
                ProductId = group.Key,
                Quantity = group.Sum(x => x.SoLuong)
            })
            .ToListAsync();

        foreach (var item in items)
        {
            await _inventory.IncreaseStockAsync(
                item.ProductId,
                item.Quantity,
                InventoryTransactionTypes.CancelledOrderReturn,
                userId,
                $"Hoan kho don huy #{orderId}",
                orderId: orderId);
        }
    }

    private async Task<OrderStatusSnapshot?> GetOrderSnapshotAsync(int orderId)
    {
        return await _db.DonHang
            .AsNoTracking()
            .Where(x => x.MaDonHang == orderId)
            .Select(x => new OrderStatusSnapshot(x.TrangThai, x.ConcurrencyVersion))
            .FirstOrDefaultAsync();
    }

    private static ConcurrencyConflictException BuildConcurrencyConflict(
        int orderId,
        OrderStatusSnapshot current,
        string requestedStatus,
        string expectedStatus)
    {
        return new ConcurrencyConflictException(
            "Trang thai hoac phien ban don hang da thay doi.",
            orderId,
            current.Status,
            requestedStatus,
            expectedStatus,
            current.Version);
    }

    private static string NormalizeStatus(string? status)
    {
        return status?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    private async Task<OrderCreationResultDTO> CreateOrderCoreAsync(
        int userId,
        string? idempotencyKey,
        List<RequestedOrderItem> requestedItems,
        CreateOrderDTO dto)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();

        var productIds = requestedItems.Select(x => x.MaSanPham).ToList();
        var products = await _db.SanPham
            .AsNoTracking()
            .Where(x => productIds.Contains(x.MaSanPham))
            .ToDictionaryAsync(x => x.MaSanPham);

        foreach (var item in requestedItems)
        {
            if (!products.TryGetValue(item.MaSanPham, out var product))
                throw new ProductNotFoundException(item.MaSanPham);

            if (product.SoLuongTon <= 0)
            {
                throw new OutOfStockException(
                    product.MaSanPham,
                    product.TenSanPham,
                    item.SoLuong);
            }

            if (product.TrangThai == "Ngung ban")
                throw new ProductUnavailableException(product.MaSanPham, product.TenSanPham);
        }

        var order = new DonHang
        {
            MaNguoiDung = userId,
            NgayTao = DateTime.UtcNow,
            TrangThai = OrderStatuses.Pending,
            IdempotencyKey = idempotencyKey,
            TenNguoiNhan = dto.TenNguoiNhan?.Trim() ?? string.Empty,
            SoDienThoai = dto.SoDienThoai?.Trim() ?? string.Empty,
            EmailNguoiNhan = dto.EmailNguoiNhan?.Trim() ?? string.Empty,
            DiaChiGiaoHang = dto.DiaChiGiaoHang?.Trim() ?? string.Empty,
            TinhThanh = dto.TinhThanh?.Trim() ?? string.Empty,
            PhuongXa = dto.PhuongXa?.Trim() ?? string.Empty,
            LoaiDichVu = NormalizeShippingService(dto.LoaiDichVu),
            TrongLuongKg = dto.TrongLuongKg <= 0 ? 1 : dto.TrongLuongKg,
            GhiChu = dto.GhiChu?.Trim() ?? string.Empty
        };

        _db.DonHang.Add(order);
        await _db.SaveChangesAsync();

        decimal total = 0;
        var details = new List<ChiTietDonHang>();

        foreach (var item in requestedItems)
        {
            var product = products[item.MaSanPham];
            await _inventory.DecreaseStockAsync(
                product.MaSanPham,
                item.SoLuong,
                InventoryTransactionTypes.OrderIssue,
                userId,
                $"Tao don hang #{order.MaDonHang}",
                orderId: order.MaDonHang,
                requireActiveProduct: true);

            details.Add(new ChiTietDonHang
            {
                MaDonHang = order.MaDonHang,
                MaSanPham = product.MaSanPham,
                SoLuong = item.SoLuong,
                DonGia = product.GiaBan
            });

            total += product.GiaBan * item.SoLuong;
        }

        order.TongTien = total;
        _db.ChiTietDonHang.AddRange(details);
        _audit.AddSuccess(
            AuditActions.Create,
            "DonHang",
            order.MaDonHang.ToString(),
            after: new
            {
                status = order.TrangThai,
                total,
                items = requestedItems.Select(x => new
                {
                    productId = x.MaSanPham,
                    quantity = x.SoLuong
                })
            },
            metadata: new { idempotencyKeyUsed = idempotencyKey != null });
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return new OrderCreationResultDTO
        {
            Message = "Tao don thanh cong",
            MaDonHang = order.MaDonHang,
            TongTien = total
        };
    }

    private static List<RequestedOrderItem> ValidateAndMergeItems(CreateOrderDTO dto)
    {
        if (dto.Items == null || dto.Items.Count == 0)
            throw new ValidationApiException("Don hang phai co it nhat mot san pham");

        var groupedItems = dto.Items
            .GroupBy(x => x.MaSanPham)
            .Select(group => new
            {
                MaSanPham = group.Key,
                SoLuong = group.Sum(x => (long)x.SoLuong)
            })
            .OrderBy(x => x.MaSanPham)
            .ToList();

        if (groupedItems.Any(x => x.SoLuong <= 0 || x.SoLuong > int.MaxValue))
            throw new ValidationApiException("So luong san pham khong hop le");

        return groupedItems
            .Select(x => new RequestedOrderItem(x.MaSanPham, (int)x.SoLuong))
            .ToList();
    }

    private async Task<DonHang?> FindByIdempotencyKeyAsync(string idempotencyKey)
    {
        return await _db.DonHang
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey);
    }

    private static OrderCreationResultDTO BuildReplayResult(DonHang order, int userId)
    {
        if (order.MaNguoiDung != userId)
        {
            throw new IdempotencyConflictException(
                "Idempotency key da duoc su dung boi tai khoan khac.");
        }

        return new OrderCreationResultDTO
        {
            Message = "Don hang da duoc tao truoc do",
            MaDonHang = order.MaDonHang,
            TongTien = order.TongTien,
            IsReplay = true
        };
    }

    private static string? NormalizeIdempotencyKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        var normalized = key.Trim();
        if (normalized.Length > 100)
            throw new ValidationApiException("Idempotency key khong duoc vuot qua 100 ky tu");

        return normalized;
    }

    private static string NormalizeShippingService(string? service)
    {
        var value = service?.Trim().ToLowerInvariant() ?? string.Empty;
        return value switch
        {
            "hoa toc" or "hỏa tốc" => "Hoa toc",
            "nhanh" => "Nhanh",
            _ => "Tieu chuan"
        };
    }

    private static bool IsIdempotencyViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException postgres &&
               postgres.SqlState == PostgresErrorCodes.UniqueViolation &&
               postgres.ConstraintName?.Contains("IdempotencyKey", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static bool IsConcurrencyFailure(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException postgres &&
               IsConcurrencyFailure(postgres);
    }

    private static bool IsConcurrencyFailure(PostgresException exception)
    {
        return exception.SqlState == PostgresErrorCodes.SerializationFailure ||
               exception.SqlState == PostgresErrorCodes.DeadlockDetected;
    }

    private sealed record RequestedOrderItem(int MaSanPham, int SoLuong);
    private sealed record OrderStatusSnapshot(string Status, int Version);
}
