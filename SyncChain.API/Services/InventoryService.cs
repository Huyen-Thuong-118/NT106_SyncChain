using Microsoft.EntityFrameworkCore;
using SyncChain.API.Data;
using SyncChain.API.DTOs.Inventory;
using SyncChain.API.Exceptions;
using SyncChain.API.Models;

namespace SyncChain.API.Services;

public class InventoryService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;

    public InventoryService(AppDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public Task<InventoryChangeResultDTO> IncreaseStockAsync(
        int productId,
        int quantity,
        string transactionType,
        int? userId,
        string note,
        int? orderId = null,
        int? receiptId = null,
        int? issueId = null,
        string source = "",
        string reason = "")
    {
        if (quantity <= 0)
            throw new ValidationApiException("So luong tang phai lon hon 0");

        if (transactionType != InventoryTransactionTypes.Receipt &&
            transactionType != InventoryTransactionTypes.CancelledOrderReturn &&
            transactionType != InventoryTransactionTypes.AdjustmentIncrease)
        {
            throw new ValidationApiException("Loai giao dich tang kho khong hop le");
        }

        return ExecuteInTransactionAsync(() => ChangeStockCoreAsync(
            productId,
            quantity,
            transactionType,
            userId,
            note,
            orderId,
            receiptId,
            issueId,
            source,
            reason));
    }

    public Task<InventoryChangeResultDTO> DecreaseStockAsync(
        int productId,
        int quantity,
        string transactionType,
        int? userId,
        string note,
        int? orderId = null,
        int? receiptId = null,
        int? issueId = null,
        string reason = "",
        bool requireActiveProduct = false)
    {
        if (quantity <= 0)
            throw new ValidationApiException("So luong giam phai lon hon 0");

        if (transactionType != InventoryTransactionTypes.OrderIssue &&
            transactionType != InventoryTransactionTypes.ManualIssue &&
            transactionType != InventoryTransactionTypes.AdjustmentDecrease)
        {
            throw new ValidationApiException("Loai giao dich giam kho khong hop le");
        }

        return ExecuteInTransactionAsync(() => ChangeStockCoreAsync(
            productId,
            -quantity,
            transactionType,
            userId,
            note,
            orderId,
            receiptId,
            issueId,
            string.Empty,
            reason,
            requireActiveProduct));
    }

    public Task<InventoryChangeResultDTO> AdjustStockAsync(
        InventoryAdjustmentDTO dto,
        int userId)
    {
        if (dto.SoLuongThayDoi == 0)
            throw new ValidationApiException("So luong dieu chinh phai khac 0");

        if (dto.SoLuongThayDoi == int.MinValue)
            throw new ValidationApiException("So luong dieu chinh vuot qua gioi han cho phep");

        if (string.IsNullOrWhiteSpace(dto.LyDo))
            throw new ValidationApiException("Ly do dieu chinh khong duoc de trong");

        var note = string.IsNullOrWhiteSpace(dto.GhiChu)
            ? dto.LyDo.Trim()
            : $"{dto.LyDo.Trim()}: {dto.GhiChu.Trim()}";

        return ExecuteInTransactionAsync(async () =>
        {
            var transactionType = dto.SoLuongThayDoi > 0
                ? InventoryTransactionTypes.AdjustmentIncrease
                : InventoryTransactionTypes.AdjustmentDecrease;
            var result = await ChangeStockCoreAsync(
                dto.MaSanPham,
                dto.SoLuongThayDoi,
                transactionType,
                userId,
                note,
                null,
                null,
                null,
                string.Empty,
                dto.LyDo.Trim());
            _audit.AddSuccess(
                AuditActions.InventoryAdjustment,
                "SanPham",
                dto.MaSanPham.ToString(),
                before: new { stock = result.TonTruoc },
                after: new { stock = result.TonSau },
                metadata: new { quantity = dto.SoLuongThayDoi, reason = dto.LyDo.Trim() });
            await _db.SaveChangesAsync();
            return result;
        });
    }

    public async Task<CurrentStockDTO> GetCurrentStockAsync(int productId)
    {
        var product = await _db.SanPham
            .AsNoTracking()
            .Where(x => x.MaSanPham == productId)
            .Select(x => new CurrentStockDTO
            {
                MaSanPham = x.MaSanPham,
                TenSanPham = x.TenSanPham,
                TonKhoBanDau = x.TonKhoBanDau,
                SoLuongTon = x.SoLuongTon,
                TrangThai = x.TrangThai
            })
            .FirstOrDefaultAsync();

        return product ?? throw new ProductNotFoundException(productId);
    }

    public async Task<List<InventoryTransactionResponseDTO>> GetTransactionHistoryAsync(
        int? productId,
        string? transactionType,
        DateTime? fromDate,
        DateTime? toDate)
    {
        if (!string.IsNullOrWhiteSpace(transactionType) &&
            !InventoryTransactionTypes.All.Contains(transactionType))
        {
            throw new ValidationApiException("Loai giao dich kho khong hop le");
        }

        if (fromDate.HasValue && toDate.HasValue && fromDate > toDate)
            throw new ValidationApiException("Khoang thoi gian khong hop le");

        var query = _db.GiaoDichKho.AsNoTracking().AsQueryable();

        if (productId.HasValue)
            query = query.Where(x => x.MaSanPham == productId.Value);

        if (!string.IsNullOrWhiteSpace(transactionType))
            query = query.Where(x => x.Loai == transactionType);

        if (fromDate.HasValue)
            query = query.Where(x => x.ThoiGian >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(x => x.ThoiGian <= toDate.Value);

        return await query
            .OrderByDescending(x => x.ThoiGian)
            .ThenByDescending(x => x.MaGiaoDich)
            .Select(x => new InventoryTransactionResponseDTO
            {
                MaGiaoDich = x.MaGiaoDich,
                MaSanPham = x.MaSanPham,
                TenSanPham = x.SanPham.TenSanPham,
                Loai = x.Loai,
                SoLuong = x.SoLuong,
                TonTruoc = x.TonTruoc,
                TonSau = x.TonSau,
                ThoiGian = x.ThoiGian,
                MaNguoiDung = x.MaNguoiDung,
                GhiChu = x.GhiChu,
                NguonNhap = x.NguonNhap,
                LyDoXuat = x.LyDoXuat,
                MaDonHang = x.MaDonHang,
                MaPhieuNhap = x.MaPhieuNhap,
                MaPhieuXuat = x.MaPhieuXuat
            })
            .ToListAsync();
    }

    public async Task<List<InventoryReconcileResultDTO>> ReconcileStockAsync(
        bool applyFix,
        int? userId)
    {
        var products = await _db.SanPham
            .AsNoTracking()
            .OrderBy(x => x.MaSanPham)
            .Select(x => new
            {
                x.MaSanPham,
                x.TenSanPham,
                x.TonKhoBanDau,
                x.SoLuongTon
            })
            .ToListAsync();

        var transactionTotals = await _db.GiaoDichKho
            .AsNoTracking()
            .GroupBy(x => x.MaSanPham)
            .Select(group => new
            {
                MaSanPham = group.Key,
                Total = group.Sum(x => x.SoLuong)
            })
            .ToDictionaryAsync(x => x.MaSanPham, x => x.Total);

        var results = products
            .Select(product =>
            {
                var expected = product.TonKhoBanDau +
                    transactionTotals.GetValueOrDefault(product.MaSanPham);
                return new InventoryReconcileResultDTO
                {
                    MaSanPham = product.MaSanPham,
                    TenSanPham = product.TenSanPham,
                    TonHienTai = product.SoLuongTon,
                    TonTheoGiaoDich = expected,
                    ChenhLech = product.SoLuongTon - expected
                };
            })
            .Where(x => x.ChenhLech != 0)
            .ToList();

        if (!applyFix || results.Count == 0)
            return results;

        if (!userId.HasValue)
            throw new ValidationApiException("Khong xac dinh duoc nguoi thuc hien doi soat");

        await using var transaction = await _db.Database.BeginTransactionAsync();

        foreach (var result in results.OrderBy(x => x.MaSanPham))
        {
            await _db.SanPham
                .Where(x => x.MaSanPham == result.MaSanPham)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.SoLuongTon, x => x.SoLuongTon));

            var product = await _db.SanPham
                .AsNoTracking()
                .FirstAsync(x => x.MaSanPham == result.MaSanPham);
            var movementTotal = await _db.GiaoDichKho
                .Where(x => x.MaSanPham == result.MaSanPham)
                .SumAsync(x => (int?)x.SoLuong) ?? 0;
            var ledgerStock = product.TonKhoBanDau + movementTotal;
            var difference = product.SoLuongTon - ledgerStock;

            if (difference == 0)
                continue;

            _db.GiaoDichKho.Add(new GiaoDichKho
            {
                MaSanPham = product.MaSanPham,
                Loai = difference > 0
                    ? InventoryTransactionTypes.AdjustmentIncrease
                    : InventoryTransactionTypes.AdjustmentDecrease,
                SoLuong = difference,
                TonTruoc = ledgerStock,
                TonSau = product.SoLuongTon,
                ThoiGian = DateTime.UtcNow,
                MaNguoiDung = userId,
                GhiChu = "Dong bo so cai giao dich voi ton kho hien tai"
            });

            result.TonHienTai = product.SoLuongTon;
            result.TonTheoGiaoDich = product.SoLuongTon;
            result.ChenhLech = 0;
            result.DaDongBo = true;
        }

        _audit.AddSuccess(
            AuditActions.InventoryAdjustment,
            "TonKho",
            null,
            metadata: new
            {
                operation = "RECONCILE",
                affectedProducts = results.Where(x => x.DaDongBo).Select(x => x.MaSanPham).ToArray()
            });
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return results;
    }

    private async Task<InventoryChangeResultDTO> ChangeStockCoreAsync(
        int productId,
        int signedQuantity,
        string transactionType,
        int? userId,
        string note,
        int? orderId,
        int? receiptId,
        int? issueId,
        string source,
        string reason,
        bool requireActiveProduct = false)
    {
        int changedRows;
        if (signedQuantity > 0)
        {
            changedRows = await _db.SanPham
                .Where(x => x.MaSanPham == productId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.SoLuongTon, x => x.SoLuongTon + signedQuantity)
                    .SetProperty(
                        x => x.TrangThai,
                        x => x.TrangThai == "Ngung ban" ? "Hoat dong" : x.TrangThai));
        }
        else
        {
            var quantity = Math.Abs(signedQuantity);
            changedRows = await _db.SanPham
                .Where(x => x.MaSanPham == productId &&
                            x.SoLuongTon >= quantity &&
                            (!requireActiveProduct || x.TrangThai != "Ngung ban"))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.SoLuongTon, x => x.SoLuongTon - quantity)
                    .SetProperty(
                        x => x.TrangThai,
                        x => x.SoLuongTon - quantity <= 0 ? "Ngung ban" : x.TrangThai));
        }

        var product = await _db.SanPham
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.MaSanPham == productId);

        if (product == null)
            throw new ProductNotFoundException(productId);

        if (changedRows != 1)
        {
            var requestedQuantity = Math.Abs(signedQuantity);
            if (product.SoLuongTon <= 0)
            {
                throw new OutOfStockException(
                    product.MaSanPham,
                    product.TenSanPham,
                    requestedQuantity);
            }

            if (requireActiveProduct && product.TrangThai == "Ngung ban")
                throw new ProductUnavailableException(product.MaSanPham, product.TenSanPham);

            if (product.SoLuongTon < requestedQuantity)
            {
                throw new InsufficientStockException(
                    product.MaSanPham,
                    product.TenSanPham,
                    requestedQuantity,
                    product.SoLuongTon);
            }

            throw new ConcurrencyConflictException(
                "Ton kho da thay doi, vui long thu lai.");
        }

        var stockAfter = product.SoLuongTon;
        var stockBefore = stockAfter - signedQuantity;

        _db.GiaoDichKho.Add(new GiaoDichKho
        {
            MaSanPham = productId,
            Loai = transactionType,
            SoLuong = signedQuantity,
            TonTruoc = stockBefore,
            TonSau = stockAfter,
            ThoiGian = DateTime.UtcNow,
            MaNguoiDung = userId,
            GhiChu = note?.Trim() ?? string.Empty,
            MaDonHang = orderId,
            MaPhieuNhap = receiptId,
            MaPhieuXuat = issueId,
            NguonNhap = source?.Trim() ?? string.Empty,
            LyDoXuat = reason?.Trim() ?? string.Empty
        });

        await _db.SaveChangesAsync();

        return new InventoryChangeResultDTO
        {
            MaSanPham = product.MaSanPham,
            TenSanPham = product.TenSanPham,
            Loai = transactionType,
            SoLuong = signedQuantity,
            TonTruoc = stockBefore,
            TonSau = stockAfter
        };
    }

    private async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action)
    {
        if (_db.Database.CurrentTransaction != null)
            return await action();

        await using var transaction = await _db.Database.BeginTransactionAsync();
        var result = await action();
        await transaction.CommitAsync();
        return result;
    }
}
