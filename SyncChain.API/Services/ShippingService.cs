using Microsoft.EntityFrameworkCore;
using Npgsql;
using SyncChain.API.Data;
using SyncChain.API.DTOs.Shipping;
using SyncChain.API.Exceptions;
using SyncChain.API.Models;

namespace SyncChain.API.Services;

public class ShippingService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;
    private readonly IAuditContextAccessor _auditContext;

    public ShippingService(
        AppDbContext db,
        IAuditService audit,
        IAuditContextAccessor auditContext)
    {
        _db = db;
        _audit = audit;
        _auditContext = auditContext;
    }

    public async Task<ShippingResponseDTO> CreateAsync(
        int orderId,
        CreateShippingDTO dto,
        int userId)
    {
        var carrier = NormalizeRequired(dto.Carrier, "Don vi van chuyen");
        var trackingNumber = NormalizeOptional(dto.TrackingNumber)
            ?? await GenerateUniqueTrackingNumberAsync();
        var estimatedAt = NormalizeUtc(dto.EstimatedDeliveryAt);
        ValidateFee(dto.ShippingFee);

        try
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            var order = await GetOrderSnapshotAsync(orderId);
            if (order == null)
                throw new OrderNotFoundException(orderId);
            EnsureOrderCanBeShipped(orderId, order.Status);

            if (await _db.VanChuyen.AsNoTracking().AnyAsync(x => x.MaDonHang == orderId))
                throw ShippingAlreadyExists(orderId);

            var now = DateTime.UtcNow;
            var shipping = new VanChuyen
            {
                MaDonHang = orderId,
                DonViVanChuyen = carrier,
                MaVanDon = trackingNumber,
                PhiVanChuyen = dto.ShippingFee,
                TrangThaiGiaoHang = string.IsNullOrWhiteSpace(trackingNumber)
                    ? ShippingStatuses.Pending
                    : ShippingStatuses.InTransit,
                NgayTao = now,
                NgayCapNhat = now,
                NgayGiaoDuKien = estimatedAt
            };

            _db.VanChuyen.Add(shipping);
            if (!string.IsNullOrWhiteSpace(trackingNumber) &&
                order.Status == OrderStatuses.Processing)
            {
                await UpdateOrderStateAsync(orderId, order, OrderStatuses.Shipping);
            }
            await _db.SaveChangesAsync();

            _audit.AddSuccess(
                AuditActions.Create,
                "VanChuyen",
                shipping.MaVanChuyen.ToString(),
                after: ToAuditValue(shipping),
                metadata: new { orderId });
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return ToResponse(shipping);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex, out var constraint))
        {
            _db.ChangeTracker.Clear();
            throw MapUniqueConflict(orderId, trackingNumber, constraint);
        }
        catch (PostgresException ex) when (IsUniqueViolation(ex, out var constraint))
        {
            _db.ChangeTracker.Clear();
            throw MapUniqueConflict(orderId, trackingNumber, constraint);
        }
    }

    public async Task<ShippingResponseDTO> UpdateAsync(
        int orderId,
        UpdateShippingDTO dto,
        int userId)
    {
        var carrier = NormalizeRequired(dto.Carrier, "Don vi van chuyen");
        var trackingNumber = NormalizeOptional(dto.TrackingNumber);
        var estimatedAt = NormalizeUtc(dto.EstimatedDeliveryAt);
        ValidateFee(dto.ShippingFee);
        var expectedVersion = dto.ConcurrencyVersion
            ?? throw new ValidationApiException("concurrencyVersion la bat buoc.");

        try
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();
            var current = await GetShippingSnapshotByOrderAsync(orderId);
            if (current == null)
            {
                if (!await _db.DonHang.AsNoTracking().AnyAsync(x => x.MaDonHang == orderId))
                    throw new OrderNotFoundException(orderId);
                throw new ShippingNotFoundException(orderId);
            }

            if (current.Version != expectedVersion)
                throw ConcurrencyConflict(current, current.Status, current.Status);

            if (current.Status is not ShippingStatuses.Pending and not ShippingStatuses.Ready)
            {
                throw ShippingStateConflict(
                    "INVALID_SHIPPING_STATE",
                    "Khong the sua thong tin van chuyen sau khi da lay hang.",
                    current,
                    current.Status,
                    current.Status);
            }

            var now = DateTime.UtcNow;
            var changedRows = await _db.VanChuyen
                .Where(x => x.MaVanChuyen == current.Id &&
                            x.TrangThaiGiaoHang == current.Status &&
                            x.ConcurrencyVersion == expectedVersion)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.DonViVanChuyen, carrier)
                    .SetProperty(x => x.MaVanDon, trackingNumber)
                    .SetProperty(x => x.PhiVanChuyen, dto.ShippingFee)
                    .SetProperty(x => x.NgayGiaoDuKien, estimatedAt)
                    .SetProperty(x => x.NgayCapNhat, now)
                    .SetProperty(x => x.ConcurrencyVersion, x => x.ConcurrencyVersion + 1));

            if (changedRows != 1)
                throw await BuildCurrentConcurrencyConflictAsync(orderId, current.Status, current.Status);

            var updated = current with
            {
                Carrier = carrier,
                TrackingNumber = trackingNumber,
                Fee = dto.ShippingFee,
                EstimatedAt = estimatedAt,
                UpdatedAt = now,
                Version = expectedVersion + 1
            };

            _audit.AddSuccess(
                AuditActions.Update,
                "VanChuyen",
                updated.Id.ToString(),
                before: ToAuditValue(current),
                after: ToAuditValue(updated),
                metadata: new { orderId });
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            return ToResponse(updated);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex, out var constraint))
        {
            _db.ChangeTracker.Clear();
            throw MapUniqueConflict(orderId, trackingNumber, constraint);
        }
        catch (PostgresException ex) when (IsUniqueViolation(ex, out var constraint))
        {
            _db.ChangeTracker.Clear();
            throw MapUniqueConflict(orderId, trackingNumber, constraint);
        }
    }

    public async Task<ShippingStatusResultDTO> UpdateStatusAsync(
        int orderId,
        UpdateShippingStatusDTO dto,
        int userId)
    {
        var requested = NormalizeStatus(dto.Status);
        var expected = NormalizeStatus(dto.ExpectedStatus);
        if (!ShippingStatuses.All.Contains(requested) || !ShippingStatuses.All.Contains(expected))
            throw new ValidationApiException("Trang thai van chuyen khong hop le.");
        var expectedVersion = dto.ConcurrencyVersion
            ?? throw new ValidationApiException("concurrencyVersion la bat buoc.");

        await using var transaction = await _db.Database.BeginTransactionAsync();
        var current = await GetShippingSnapshotByOrderAsync(orderId);
        if (current == null)
        {
            if (!await _db.DonHang.AsNoTracking().AnyAsync(x => x.MaDonHang == orderId))
                throw new OrderNotFoundException(orderId);
            throw new ShippingNotFoundException(orderId);
        }

        if (current.Status != expected || current.Version != expectedVersion)
            throw ConcurrencyConflict(current, requested, expected);
        if (ShippingStatuses.IsTerminal(current.Status))
        {
            throw ShippingStateConflict(
                "SHIPPING_ALREADY_COMPLETED",
                "Van chuyen da o trang thai ket thuc.",
                current,
                requested,
                expected);
        }
        if (current.Status == requested || !ShippingStatuses.CanTransition(current.Status, requested))
        {
            throw ShippingStateConflict(
                "INVALID_SHIPPING_STATE",
                $"Khong the chuyen van chuyen tu {current.Status} sang {requested}.",
                current,
                requested,
                expected);
        }

        var order = await GetOrderSnapshotAsync(orderId)
            ?? throw new OrderNotFoundException(orderId);
        await SynchronizeOrderAsync(orderId, order, requested);

        var now = DateTime.UtcNow;
        var changedRows = await _db.VanChuyen
            .Where(x => x.MaVanChuyen == current.Id &&
                        x.TrangThaiGiaoHang == expected &&
                        x.ConcurrencyVersion == expectedVersion)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.TrangThaiGiaoHang, requested)
                .SetProperty(x => x.NgayCapNhat, now)
                .SetProperty(
                    x => x.NgayGiaoThucTe,
                    x => requested == ShippingStatuses.Delivered ? now : x.NgayGiaoThucTe)
                .SetProperty(x => x.ConcurrencyVersion, x => x.ConcurrencyVersion + 1));

        if (changedRows != 1)
            throw await BuildCurrentConcurrencyConflictAsync(orderId, requested, expected);

        var note = NormalizeNote(dto.Note);
        _db.LichSuVanChuyen.Add(new LichSuVanChuyen
        {
            MaVanChuyen = current.Id,
            TrangThaiCu = current.Status,
            TrangThaiMoi = requested,
            ThoiGian = now,
            MaNguoiDung = userId,
            GhiChu = note,
            TraceId = _auditContext.RequestContext.TraceId
        });

        var updated = current with
        {
            Status = requested,
            UpdatedAt = now,
            DeliveredAt = requested == ShippingStatuses.Delivered ? now : current.DeliveredAt,
            Version = expectedVersion + 1
        };
        _audit.AddSuccess(
            AuditActions.ShippingStatusChange,
            "VanChuyen",
            updated.Id.ToString(),
            before: new { status = current.Status, version = current.Version },
            after: new { status = requested, version = updated.Version },
            metadata: new { orderId, note });

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return new ShippingStatusResultDTO
        {
            ShippingId = current.Id,
            OrderId = orderId,
            OldStatus = current.Status,
            NewStatus = requested,
            ConcurrencyVersion = expectedVersion + 1,
            UpdatedAt = now
        };
    }

    public async Task<ShippingResponseDTO> GetByOrderAsync(int orderId)
    {
        var shipping = await _db.VanChuyen.AsNoTracking()
            .FirstOrDefaultAsync(x => x.MaDonHang == orderId);
        if (shipping == null)
        {
            if (!await _db.DonHang.AsNoTracking().AnyAsync(x => x.MaDonHang == orderId))
                throw new OrderNotFoundException(orderId);
            throw new ShippingNotFoundException(orderId);
        }
        return ToResponse(shipping);
    }

    public async Task<ShippingResponseDTO> GetByTrackingAsync(string trackingNumber)
    {
        var normalized = NormalizeRequired(trackingNumber, "Ma van don");
        var shipping = await _db.VanChuyen.AsNoTracking()
            .FirstOrDefaultAsync(x => x.MaVanDon == normalized);
        if (shipping == null)
            throw new ShippingNotFoundException(normalized);
        return ToResponse(shipping);
    }

    public async Task<List<ShippingHistoryResponseDTO>> GetHistoryAsync(int orderId)
    {
        var shippingId = await _db.VanChuyen.AsNoTracking()
            .Where(x => x.MaDonHang == orderId)
            .Select(x => (int?)x.MaVanChuyen)
            .FirstOrDefaultAsync();
        if (shippingId == null)
            throw new ShippingNotFoundException(orderId);

        return await _db.LichSuVanChuyen.AsNoTracking()
            .Where(x => x.MaVanChuyen == shippingId)
            .OrderBy(x => x.MaLichSu)
            .Select(x => new ShippingHistoryResponseDTO
            {
                HistoryId = x.MaLichSu,
                OldStatus = x.TrangThaiCu,
                NewStatus = x.TrangThaiMoi,
                ChangedAt = x.ThoiGian,
                UserId = x.MaNguoiDung,
                Note = x.GhiChu,
                TraceId = x.TraceId
            })
            .ToListAsync();
    }

    private async Task SynchronizeOrderAsync(
        int orderId,
        OrderSnapshot order,
        string requestedShippingStatus)
    {
        if (order.Status == OrderStatuses.Cancel)
        {
            if (requestedShippingStatus == ShippingStatuses.Cancelled)
                return;
            throw InvalidOrderForShipping(orderId, order.Status, requestedShippingStatus);
        }
        if (order.Status == OrderStatuses.Done)
            throw InvalidOrderForShipping(orderId, order.Status, requestedShippingStatus);

        if (requestedShippingStatus is ShippingStatuses.PickedUp or ShippingStatuses.InTransit)
        {
            if (order.Status == OrderStatuses.Shipping)
                return;
            if (order.Status != OrderStatuses.Processing ||
                await UpdateOrderStateAsync(orderId, order, OrderStatuses.Shipping) != 1)
            {
                throw InvalidOrderForShipping(orderId, order.Status, requestedShippingStatus);
            }
        }

        if (requestedShippingStatus == ShippingStatuses.Delivered &&
            (order.Status != OrderStatuses.Shipping ||
             await UpdateOrderStateAsync(orderId, order, OrderStatuses.Done) != 1))
        {
            throw InvalidOrderForShipping(orderId, order.Status, requestedShippingStatus);
        }
    }

    private Task<int> UpdateOrderStateAsync(int orderId, OrderSnapshot order, string newStatus) =>
        _db.DonHang
            .Where(x => x.MaDonHang == orderId &&
                        x.TrangThai == order.Status &&
                        x.ConcurrencyVersion == order.Version)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.TrangThai, newStatus)
                .SetProperty(x => x.ConcurrencyVersion, x => x.ConcurrencyVersion + 1));

    private async Task<OrderSnapshot?> GetOrderSnapshotAsync(int orderId) =>
        await _db.DonHang.AsNoTracking()
            .Where(x => x.MaDonHang == orderId)
            .Select(x => new OrderSnapshot(x.TrangThai, x.ConcurrencyVersion))
            .FirstOrDefaultAsync();

    private async Task<ShippingSnapshot?> GetShippingSnapshotByOrderAsync(int orderId) =>
        await _db.VanChuyen.AsNoTracking()
            .Where(x => x.MaDonHang == orderId)
            .Select(x => new ShippingSnapshot(
                x.MaVanChuyen, x.MaDonHang, x.DonViVanChuyen, x.MaVanDon,
                x.PhiVanChuyen, x.TrangThaiGiaoHang, x.NgayTao, x.NgayCapNhat,
                x.NgayGiaoDuKien, x.NgayGiaoThucTe, x.ConcurrencyVersion))
            .FirstOrDefaultAsync();

    private async Task<ConcurrencyConflictException> BuildCurrentConcurrencyConflictAsync(
        int orderId,
        string requested,
        string expected)
    {
        var current = await GetShippingSnapshotByOrderAsync(orderId);
        if (current == null)
            throw new ShippingNotFoundException(orderId);
        return ConcurrencyConflict(current, requested, expected);
    }

    private static ShippingConflictException ShippingAlreadyExists(int orderId) =>
        new(
            "SHIPPING_ALREADY_EXISTS",
            $"Don hang #{orderId} da co thong tin van chuyen.",
            new Dictionary<string, object?> { ["orderId"] = orderId });

    private static ShippingConflictException TrackingConflict(int orderId, string? trackingNumber) =>
        new(
            "TRACKING_NUMBER_CONFLICT",
            "Ma van don da duoc su dung.",
            new Dictionary<string, object?>
            {
                ["orderId"] = orderId,
                ["trackingNumber"] = trackingNumber
            });

    private static ShippingConflictException ShippingStateConflict(
        string code,
        string message,
        ShippingSnapshot current,
        string requested,
        string expected) =>
        new(code, message, ShippingDetails(current, requested, expected));

    private static ConcurrencyConflictException ConcurrencyConflict(
        ShippingSnapshot current,
        string requested,
        string expected) =>
        new(
            "Trang thai hoac phien ban van chuyen da thay doi.",
            ShippingDetails(current, requested, expected));

    private static IReadOnlyDictionary<string, object?> ShippingDetails(
        ShippingSnapshot current,
        string requested,
        string expected) =>
        new Dictionary<string, object?>
        {
            ["orderId"] = current.OrderId,
            ["shippingId"] = current.Id,
            ["currentStatus"] = current.Status,
            ["requestedStatus"] = requested,
            ["expectedStatus"] = expected,
            ["currentVersion"] = current.Version,
            ["trackingNumber"] = current.TrackingNumber
        };

    private static ShippingConflictException InvalidOrderForShipping(
        int orderId,
        string orderStatus,
        string requestedShippingStatus) =>
        new(
            "INVALID_ORDER_STATE",
            $"Don hang o trang thai {orderStatus} khong the chuyen van chuyen sang {requestedShippingStatus}.",
            new Dictionary<string, object?>
            {
                ["orderId"] = orderId,
                ["currentStatus"] = orderStatus,
                ["requestedStatus"] = requestedShippingStatus
            });

    private static void EnsureOrderCanBeShipped(int orderId, string orderStatus)
    {
        if (orderStatus is OrderStatuses.Done or OrderStatuses.Cancel)
            throw InvalidOrderForShipping(orderId, orderStatus, ShippingStatuses.Pending);
    }

    private static ApiException MapUniqueConflict(
        int orderId,
        string? trackingNumber,
        string? constraint) =>
        constraint?.Contains("MaDonHang", StringComparison.OrdinalIgnoreCase) == true
            ? ShippingAlreadyExists(orderId)
            : TrackingConflict(orderId, trackingNumber);

    private static bool IsUniqueViolation(DbUpdateException exception, out string? constraint)
    {
        if (exception.InnerException is PostgresException postgres &&
            postgres.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            constraint = postgres.ConstraintName;
            return true;
        }
        constraint = null;
        return false;
    }

    private static bool IsUniqueViolation(PostgresException exception, out string? constraint)
    {
        constraint = exception.ConstraintName;
        return exception.SqlState == PostgresErrorCodes.UniqueViolation;
    }

    private static void ValidateFee(decimal fee)
    {
        if (fee < 0)
            throw new ValidationApiException("Phi van chuyen khong duoc am.");
    }

    private static string NormalizeRequired(string? value, string field)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
            throw new ValidationApiException($"{field} khong duoc de trong.");
        return normalized;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task<string> GenerateUniqueTrackingNumberAsync()
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var randomPart = Convert.ToHexString(
                System.Security.Cryptography.RandomNumberGenerator.GetBytes(5));
            var trackingNumber = $"SC{DateTime.UtcNow:yyMMdd}{randomPart}";
            if (!await _db.VanChuyen.AsNoTracking()
                    .AnyAsync(x => x.MaVanDon == trackingNumber))
                return trackingNumber;
        }

        throw new InvalidOperationException("Khong tao duoc ma van don duy nhat.");
    }

    private static string NormalizeStatus(string? value) =>
        value?.Trim().ToLowerInvariant() ?? string.Empty;

    private static string NormalizeNote(string? value) =>
        value?.Trim() ?? string.Empty;

    private static DateTime? NormalizeUtc(DateTime? value)
    {
        if (value == null)
            return null;
        return value.Value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.Value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
        };
    }

    private static ShippingResponseDTO ToResponse(VanChuyen value) => ToResponse(ToSnapshot(value));

    private static ShippingResponseDTO ToResponse(ShippingSnapshot value) => new()
    {
        ShippingId = value.Id,
        OrderId = value.OrderId,
        Carrier = value.Carrier,
        TrackingNumber = value.TrackingNumber,
        ShippingFee = value.Fee,
        ShippingStatus = value.Status,
        CreatedAt = value.CreatedAt,
        UpdatedAt = value.UpdatedAt,
        EstimatedDeliveryAt = value.EstimatedAt,
        DeliveredAt = value.DeliveredAt,
        ConcurrencyVersion = value.Version
    };

    private static ShippingSnapshot ToSnapshot(VanChuyen value) => new(
        value.MaVanChuyen, value.MaDonHang, value.DonViVanChuyen, value.MaVanDon,
        value.PhiVanChuyen, value.TrangThaiGiaoHang, value.NgayTao, value.NgayCapNhat,
        value.NgayGiaoDuKien, value.NgayGiaoThucTe, value.ConcurrencyVersion);

    private static object ToAuditValue(VanChuyen value) => ToAuditValue(ToSnapshot(value));
    private static object ToAuditValue(ShippingSnapshot value) => new
    {
        carrier = value.Carrier,
        trackingNumber = value.TrackingNumber,
        shippingFee = value.Fee,
        shippingStatus = value.Status,
        estimatedDeliveryAt = value.EstimatedAt,
        concurrencyVersion = value.Version
    };

    private sealed record OrderSnapshot(string Status, int Version);
    private sealed record ShippingSnapshot(
        int Id,
        int OrderId,
        string Carrier,
        string? TrackingNumber,
        decimal Fee,
        string Status,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        DateTime? EstimatedAt,
        DateTime? DeliveredAt,
        int Version);
}
