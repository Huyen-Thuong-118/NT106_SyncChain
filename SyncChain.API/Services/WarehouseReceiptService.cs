using Microsoft.EntityFrameworkCore;
using Npgsql;
using SyncChain.API.Data;
using SyncChain.API.DTOs.WarehouseReceipt;
using SyncChain.API.Models;

namespace SyncChain.API.Services;

public class WarehouseReceiptService
{
    private readonly AppDbContext _db;
    private readonly InventoryService _inventory;
    private readonly IAuditService _audit;

    public WarehouseReceiptService(AppDbContext db, InventoryService inventory, IAuditService audit)
    {
        _db = db;
        _inventory = inventory;
        _audit = audit;
    }

    public async Task<List<WarehouseReceiptResponseDTO>> GetAllAsync(
        string? status,
        string? source,
        DateTime? fromDate,
        DateTime? toDate)
    {
        if (!string.IsNullOrWhiteSpace(status) &&
            !WarehouseReceiptStatuses.All.Contains(status))
        {
            throw new InvalidOperationException("Trang thai phieu nhap khong hop le");
        }

        if (fromDate.HasValue && toDate.HasValue && fromDate > toDate)
            throw new InvalidOperationException("Khoang thoi gian khong hop le");

        var query = ReceiptQuery();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(x => x.TrangThai == status);

        if (!string.IsNullOrWhiteSpace(source))
        {
            var normalizedSource = source.Trim().ToLower();
            query = query.Where(x => x.TenNguonNhap.ToLower().Contains(normalizedSource));
        }

        if (fromDate.HasValue)
            query = query.Where(x => x.NgayTao >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(x => x.NgayTao <= toDate.Value);

        var receipts = await query
            .OrderByDescending(x => x.NgayTao)
            .ToListAsync();

        return receipts.Select(ToResponse).ToList();
    }

    public async Task<WarehouseReceiptResponseDTO> GetByIdAsync(int id)
    {
        var receipt = await ReceiptQuery()
            .FirstOrDefaultAsync(x => x.MaPhieuNhap == id);

        if (receipt == null)
            throw new KeyNotFoundException("Phieu nhap kho khong ton tai");

        return ToResponse(receipt);
    }

    public async Task<WarehouseReceiptResponseDTO> CreateAsync(
        CreateWarehouseReceiptDTO dto,
        int userId)
    {
        await ValidateAsync(dto.TenNguonNhap, dto.ChiTiet);

        var receipt = new PhieuNhapKho
        {
            SoPhieu = GenerateReceiptNumber(),
            TenNguonNhap = dto.TenNguonNhap.Trim(),
            DiaChiNguonNhap = dto.DiaChiNguonNhap?.Trim() ?? string.Empty,
            NguoiLienHe = dto.NguoiLienHe?.Trim() ?? string.Empty,
            GhiChu = dto.GhiChu?.Trim() ?? string.Empty,
            TrangThai = WarehouseReceiptStatuses.Draft,
            NgayTao = DateTime.UtcNow,
            MaNguoiTao = userId,
            ChiTietPhieuNhap = BuildItems(dto.ChiTiet)
        };

        await using var transaction = await _db.Database.BeginTransactionAsync();
        _db.PhieuNhapKho.Add(receipt);
        await _db.SaveChangesAsync();
        _audit.AddSuccess(AuditActions.Create, "PhieuNhapKho", receipt.MaPhieuNhap.ToString(),
            after: new { receipt.SoPhieu, receipt.TrangThai, receipt.TenNguonNhap });
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return await GetByIdAsync(receipt.MaPhieuNhap);
    }

    public async Task<WarehouseReceiptResponseDTO> UpdateAsync(
        int id,
        UpdateWarehouseReceiptDTO dto)
    {
        var receipt = await _db.PhieuNhapKho
            .Include(x => x.ChiTietPhieuNhap)
            .FirstOrDefaultAsync(x => x.MaPhieuNhap == id);

        if (receipt == null)
            throw new KeyNotFoundException("Phieu nhap kho khong ton tai");

        if (receipt.TrangThai != WarehouseReceiptStatuses.Draft &&
            receipt.TrangThai != WarehouseReceiptStatuses.Pending)
        {
            throw new InvalidOperationException("Khong the sua phieu nhap o trang thai hien tai");
        }

        await ValidateAsync(dto.TenNguonNhap, dto.ChiTiet);

        var before = new { receipt.TenNguonNhap, receipt.GhiChu, receipt.TrangThai };
        receipt.TenNguonNhap = dto.TenNguonNhap.Trim();
        receipt.DiaChiNguonNhap = dto.DiaChiNguonNhap?.Trim() ?? string.Empty;
        receipt.NguoiLienHe = dto.NguoiLienHe?.Trim() ?? string.Empty;
        receipt.GhiChu = dto.GhiChu?.Trim() ?? string.Empty;

        _db.ChiTietPhieuNhap.RemoveRange(receipt.ChiTietPhieuNhap);
        receipt.ChiTietPhieuNhap = BuildItems(dto.ChiTiet);
        _audit.AddSuccess(AuditActions.Update, "PhieuNhapKho", id.ToString(), before,
            new { receipt.TenNguonNhap, receipt.GhiChu, receipt.TrangThai });
        await _db.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public Task<WarehouseReceiptResponseDTO> SubmitAsync(int id)
    {
        return ChangeStatusAsync(
            id,
            WarehouseReceiptStatuses.Draft,
            WarehouseReceiptStatuses.Pending);
    }

    public Task<WarehouseReceiptResponseDTO> ApproveAsync(int id, int approverId)
    {
        return ChangeStatusAsync(
            id,
            WarehouseReceiptStatuses.Pending,
            WarehouseReceiptStatuses.Approved,
            approverId);
    }

    public async Task<WarehouseReceiptResponseDTO> CompleteAsync(int id, int userId)
    {
        try
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            var receipt = await _db.PhieuNhapKho
                .AsNoTracking()
                .Include(x => x.ChiTietPhieuNhap)
                .FirstOrDefaultAsync(x => x.MaPhieuNhap == id);

            if (receipt == null)
                throw new KeyNotFoundException("Phieu nhap kho khong ton tai");

            if (receipt.TrangThai != WarehouseReceiptStatuses.Approved)
                throw new InvalidOperationException("Chi phieu da duyet moi duoc hoan tat");

            var completedAt = DateTime.UtcNow;
            var claimedRows = await _db.PhieuNhapKho
                .Where(x => x.MaPhieuNhap == id &&
                            x.TrangThai == WarehouseReceiptStatuses.Approved)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.TrangThai, WarehouseReceiptStatuses.Completed)
                    .SetProperty(x => x.NgayHoanTat, completedAt));

            if (claimedRows != 1)
                throw new InvalidOperationException("Phieu nhap da duoc xu ly boi yeu cau khac");

            foreach (var item in receipt.ChiTietPhieuNhap.OrderBy(x => x.MaSanPham))
            {
                await _inventory.IncreaseStockAsync(
                    item.MaSanPham,
                    item.SoLuong,
                    InventoryTransactionTypes.Receipt,
                    userId,
                    $"Phieu {receipt.SoPhieu}: {receipt.GhiChu}".Trim(),
                    receiptId: receipt.MaPhieuNhap,
                    source: receipt.TenNguonNhap);

                await _db.SanPham
                    .Where(x => x.MaSanPham == item.MaSanPham)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => x.GiaNhap, item.DonGiaNhap));
            }

            _audit.AddSuccess(AuditActions.StatusChange, "PhieuNhapKho", id.ToString(),
                before: new { status = WarehouseReceiptStatuses.Approved },
                after: new { status = WarehouseReceiptStatuses.Completed },
                metadata: new { stockReceived = true });
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            _db.ChangeTracker.Clear();
            return await GetByIdAsync(id);
        }
        catch (PostgresException ex) when (
            ex.SqlState == PostgresErrorCodes.SerializationFailure ||
            ex.SqlState == PostgresErrorCodes.DeadlockDetected)
        {
            throw new InvalidOperationException("Phieu nhap dang duoc xu ly, vui long thu lai");
        }
    }

    public async Task<WarehouseReceiptResponseDTO> CancelAsync(int id)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();
        var oldStatus = await _db.PhieuNhapKho.AsNoTracking()
            .Where(x => x.MaPhieuNhap == id).Select(x => x.TrangThai).FirstOrDefaultAsync();
        var changedRows = await _db.PhieuNhapKho
            .Where(x => x.MaPhieuNhap == id &&
                        (x.TrangThai == WarehouseReceiptStatuses.Pending ||
                         x.TrangThai == WarehouseReceiptStatuses.Approved))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.TrangThai, WarehouseReceiptStatuses.Cancelled));

        if (changedRows != 1)
            await ThrowStatusErrorAsync(id, "Khong the huy phieu nhap o trang thai hien tai");

        _audit.AddSuccess(AuditActions.StatusChange, "PhieuNhapKho", id.ToString(),
            before: new { status = oldStatus },
            after: new { status = WarehouseReceiptStatuses.Cancelled });
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return await GetByIdAsync(id);
    }

    public async Task DeleteAsync(int id)
    {
        var receipt = await _db.PhieuNhapKho.FindAsync(id);
        if (receipt == null)
            throw new KeyNotFoundException("Phieu nhap kho khong ton tai");

        if (receipt.TrangThai != WarehouseReceiptStatuses.Draft)
            throw new InvalidOperationException("Chi duoc xoa phieu nhap dang nhap");

        _db.PhieuNhapKho.Remove(receipt);
        _audit.AddSuccess(AuditActions.Delete, "PhieuNhapKho", id.ToString(),
            before: new { receipt.SoPhieu, receipt.TrangThai });
        await _db.SaveChangesAsync();
    }

    private async Task<WarehouseReceiptResponseDTO> ChangeStatusAsync(
        int id,
        string expectedStatus,
        string nextStatus,
        int? approverId = null)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();
        int changedRows;
        if (nextStatus == WarehouseReceiptStatuses.Approved)
        {
            var approvedAt = DateTime.UtcNow;
            changedRows = await _db.PhieuNhapKho
                .Where(x => x.MaPhieuNhap == id && x.TrangThai == expectedStatus)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.TrangThai, nextStatus)
                    .SetProperty(x => x.MaNguoiDuyet, approverId)
                    .SetProperty(x => x.NgayDuyet, approvedAt));
        }
        else
        {
            changedRows = await _db.PhieuNhapKho
                .Where(x => x.MaPhieuNhap == id && x.TrangThai == expectedStatus)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.TrangThai, nextStatus));
        }

        if (changedRows != 1)
            await ThrowStatusErrorAsync(id, "Buoc chuyen trang thai phieu nhap khong hop le");

        _audit.AddSuccess(
            nextStatus == WarehouseReceiptStatuses.Approved ? AuditActions.Approve : AuditActions.StatusChange,
            "PhieuNhapKho",
            id.ToString(),
            before: new { status = expectedStatus },
            after: new { status = nextStatus });
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return await GetByIdAsync(id);
    }

    private async Task ThrowStatusErrorAsync(int id, string message)
    {
        if (!await _db.PhieuNhapKho.AnyAsync(x => x.MaPhieuNhap == id))
            throw new KeyNotFoundException("Phieu nhap kho khong ton tai");

        throw new InvalidOperationException(message);
    }

    private async Task ValidateAsync(
        string? sourceName,
        IReadOnlyCollection<WarehouseReceiptItemDTO>? items)
    {
        if (string.IsNullOrWhiteSpace(sourceName))
            throw new InvalidOperationException("Nguon nhap khong duoc de trong");

        if (items == null || items.Count == 0)
            throw new InvalidOperationException("Phieu nhap phai co it nhat mot san pham");

        if (items.Any(x => x.SoLuong <= 0))
            throw new InvalidOperationException("So luong nhap phai lon hon 0");

        if (items.Any(x => x.DonGiaNhap < 0))
            throw new InvalidOperationException("Don gia nhap khong duoc am");

        var productIds = items.Select(x => x.MaSanPham).ToList();
        if (productIds.Distinct().Count() != productIds.Count)
            throw new InvalidOperationException("San pham khong duoc lap trong cung phieu nhap");

        var existingCount = await _db.SanPham.CountAsync(x => productIds.Contains(x.MaSanPham));
        if (existingCount != productIds.Count)
            throw new InvalidOperationException("Co san pham khong ton tai");
    }

    private static List<ChiTietPhieuNhap> BuildItems(
        IEnumerable<WarehouseReceiptItemDTO> items)
    {
        return items.Select(x => new ChiTietPhieuNhap
        {
            MaSanPham = x.MaSanPham,
            SoLuong = x.SoLuong,
            DonGiaNhap = x.DonGiaNhap
        }).ToList();
    }

    private IQueryable<PhieuNhapKho> ReceiptQuery()
    {
        return _db.PhieuNhapKho
            .AsNoTracking()
            .Include(x => x.ChiTietPhieuNhap)
            .ThenInclude(x => x.SanPham);
    }

    private static WarehouseReceiptResponseDTO ToResponse(PhieuNhapKho receipt)
    {
        var items = receipt.ChiTietPhieuNhap
            .OrderBy(x => x.MaChiTiet)
            .Select(x => new WarehouseReceiptItemResponseDTO
            {
                MaChiTiet = x.MaChiTiet,
                MaSanPham = x.MaSanPham,
                TenSanPham = x.SanPham.TenSanPham,
                SoLuong = x.SoLuong,
                DonGiaNhap = x.DonGiaNhap,
                ThanhTien = x.ThanhTien
            })
            .ToList();

        return new WarehouseReceiptResponseDTO
        {
            MaPhieuNhap = receipt.MaPhieuNhap,
            SoPhieu = receipt.SoPhieu,
            TenNguonNhap = receipt.TenNguonNhap,
            DiaChiNguonNhap = receipt.DiaChiNguonNhap,
            NguoiLienHe = receipt.NguoiLienHe,
            GhiChu = receipt.GhiChu,
            TrangThai = receipt.TrangThai,
            NgayTao = receipt.NgayTao,
            NgayDuyet = receipt.NgayDuyet,
            NgayHoanTat = receipt.NgayHoanTat,
            MaNguoiTao = receipt.MaNguoiTao,
            MaNguoiDuyet = receipt.MaNguoiDuyet,
            TongTien = items.Sum(x => x.ThanhTien),
            ChiTiet = items
        };
    }

    private static string GenerateReceiptNumber()
    {
        return $"PN-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..29].ToUpperInvariant();
    }
}
