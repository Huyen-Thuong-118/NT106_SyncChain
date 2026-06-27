using Microsoft.EntityFrameworkCore;
using Npgsql;
using SyncChain.API.Data;
using SyncChain.API.DTOs.WarehouseIssue;
using SyncChain.API.Models;

namespace SyncChain.API.Services;

public class WarehouseIssueService
{
    private readonly AppDbContext _db;
    private readonly InventoryService _inventory;
    private readonly IAuditService _audit;

    public WarehouseIssueService(AppDbContext db, InventoryService inventory, IAuditService audit)
    {
        _db = db;
        _inventory = inventory;
        _audit = audit;
    }

    public async Task<List<WarehouseIssueResponseDTO>> GetAllAsync(
        string? status,
        string? reason,
        DateTime? fromDate,
        DateTime? toDate)
    {
        if (!string.IsNullOrWhiteSpace(status) &&
            !WarehouseIssueStatuses.All.Contains(status))
        {
            throw new InvalidOperationException("Trang thai phieu xuat khong hop le");
        }

        if (fromDate.HasValue && toDate.HasValue && fromDate > toDate)
            throw new InvalidOperationException("Khoang thoi gian khong hop le");

        var query = IssueQuery();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(x => x.TrangThai == status);

        if (!string.IsNullOrWhiteSpace(reason))
        {
            var normalizedReason = reason.Trim().ToLower();
            query = query.Where(x => x.LyDoXuat.ToLower().Contains(normalizedReason));
        }

        if (fromDate.HasValue)
            query = query.Where(x => x.NgayTao >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(x => x.NgayTao <= toDate.Value);

        var issues = await query.OrderByDescending(x => x.NgayTao).ToListAsync();
        return issues.Select(ToResponse).ToList();
    }

    public async Task<WarehouseIssueResponseDTO> GetByIdAsync(int id)
    {
        var issue = await IssueQuery().FirstOrDefaultAsync(x => x.MaPhieuXuat == id);
        if (issue == null)
            throw new KeyNotFoundException("Phieu xuat kho khong ton tai");

        return ToResponse(issue);
    }

    public async Task<List<WarehouseIssueHistoryDTO>> GetHistoryAsync()
    {
        return await _db.GiaoDichKho
            .AsNoTracking()
            .Include(x => x.SanPham)
            .Include(x => x.PhieuXuatKho)
            .Where(x => x.Loai == InventoryTransactionTypes.ManualIssue && x.MaPhieuXuat.HasValue)
            .OrderByDescending(x => x.ThoiGian)
            .Select(x => new WarehouseIssueHistoryDTO
            {
                MaGiaoDich = x.MaGiaoDich,
                MaPhieuXuat = x.MaPhieuXuat!.Value,
                SoPhieu = x.PhieuXuatKho != null ? x.PhieuXuatKho.SoPhieu : string.Empty,
                MaSanPham = x.MaSanPham,
                TenSanPham = x.SanPham.TenSanPham,
                SoLuong = x.SoLuong,
                LyDoXuat = x.LyDoXuat,
                MaNguoiDung = x.MaNguoiDung,
                ThoiGian = x.ThoiGian,
                GhiChu = x.GhiChu
            })
            .ToListAsync();
    }

    public async Task<WarehouseIssueResponseDTO> CreateAsync(
        CreateWarehouseIssueDTO dto,
        int userId)
    {
        await ValidateAsync(dto.LyDoXuat, dto.ChiTiet);

        var issue = new PhieuXuatKho
        {
            SoPhieu = GenerateIssueNumber(),
            LyDoXuat = dto.LyDoXuat.Trim(),
            GhiChu = dto.GhiChu?.Trim() ?? string.Empty,
            TrangThai = WarehouseIssueStatuses.Draft,
            NgayTao = DateTime.UtcNow,
            MaNguoiTao = userId,
            ChiTietPhieuXuat = BuildItems(dto.ChiTiet)
        };

        await using var transaction = await _db.Database.BeginTransactionAsync();
        _db.PhieuXuatKho.Add(issue);
        await _db.SaveChangesAsync();
        _audit.AddSuccess(AuditActions.Create, "PhieuXuatKho", issue.MaPhieuXuat.ToString(),
            after: new { issue.SoPhieu, issue.TrangThai, issue.LyDoXuat });
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return await GetByIdAsync(issue.MaPhieuXuat);
    }

    public async Task<WarehouseIssueResponseDTO> UpdateAsync(
        int id,
        UpdateWarehouseIssueDTO dto)
    {
        await ValidateAsync(dto.LyDoXuat, dto.ChiTiet);
        await using var transaction = await _db.Database.BeginTransactionAsync();

        var changedRows = await _db.PhieuXuatKho
            .Where(x => x.MaPhieuXuat == id && x.TrangThai == WarehouseIssueStatuses.Draft)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.LyDoXuat, dto.LyDoXuat.Trim())
                .SetProperty(x => x.GhiChu, dto.GhiChu == null ? string.Empty : dto.GhiChu.Trim()));

        if (changedRows != 1)
            await ThrowStatusErrorAsync(id, "Chi duoc sua phieu xuat dang nhap");

        await _db.ChiTietPhieuXuat
            .Where(x => x.MaPhieuXuat == id)
            .ExecuteDeleteAsync();

        var newItems = BuildItems(dto.ChiTiet);
        foreach (var item in newItems)
            item.MaPhieuXuat = id;

        _db.ChiTietPhieuXuat.AddRange(newItems);
        _audit.AddSuccess(AuditActions.Update, "PhieuXuatKho", id.ToString(),
            metadata: new { dto.LyDoXuat, itemCount = newItems.Count });
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        _db.ChangeTracker.Clear();
        return await GetByIdAsync(id);
    }

    public async Task<WarehouseIssueResponseDTO> SubmitAsync(int id)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();
        var changedRows = await _db.PhieuXuatKho
            .Where(x => x.MaPhieuXuat == id && x.TrangThai == WarehouseIssueStatuses.Draft)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.TrangThai, WarehouseIssueStatuses.Pending));

        if (changedRows != 1)
            await ThrowStatusErrorAsync(id, "Chi phieu dang nhap moi duoc gui xu ly");

        _audit.AddSuccess(AuditActions.StatusChange, "PhieuXuatKho", id.ToString(),
            before: new { status = WarehouseIssueStatuses.Draft },
            after: new { status = WarehouseIssueStatuses.Pending });
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return await GetByIdAsync(id);
    }

    public async Task<WarehouseIssueResponseDTO> CompleteAsync(int id, int userId)
    {
        try
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            var issue = await _db.PhieuXuatKho
                .AsNoTracking()
                .Include(x => x.ChiTietPhieuXuat)
                .FirstOrDefaultAsync(x => x.MaPhieuXuat == id);

            if (issue == null)
                throw new KeyNotFoundException("Phieu xuat kho khong ton tai");

            if (issue.TrangThai != WarehouseIssueStatuses.Pending)
                throw new InvalidOperationException("Chi phieu cho xu ly moi duoc hoan tat");

            var completedAt = DateTime.UtcNow;
            var claimedRows = await _db.PhieuXuatKho
                .Where(x => x.MaPhieuXuat == id && x.TrangThai == WarehouseIssueStatuses.Pending)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.TrangThai, WarehouseIssueStatuses.Completed)
                    .SetProperty(x => x.NgayHoanTat, completedAt)
                    .SetProperty(x => x.MaNguoiHoanTat, userId));

            if (claimedRows != 1)
                throw new InvalidOperationException("Phieu xuat da duoc xu ly boi yeu cau khac");

            foreach (var item in issue.ChiTietPhieuXuat.OrderBy(x => x.MaSanPham))
            {
                await _inventory.DecreaseStockAsync(
                    item.MaSanPham,
                    item.SoLuong,
                    InventoryTransactionTypes.ManualIssue,
                    userId,
                    $"Phieu {issue.SoPhieu}: {issue.GhiChu}".Trim(),
                    issueId: issue.MaPhieuXuat,
                    reason: issue.LyDoXuat);
            }

            _audit.AddSuccess(AuditActions.StatusChange, "PhieuXuatKho", id.ToString(),
                before: new { status = WarehouseIssueStatuses.Pending },
                after: new { status = WarehouseIssueStatuses.Completed },
                metadata: new { stockIssued = true });
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            _db.ChangeTracker.Clear();
            return await GetByIdAsync(id);
        }
        catch (PostgresException ex) when (
            ex.SqlState == PostgresErrorCodes.SerializationFailure ||
            ex.SqlState == PostgresErrorCodes.DeadlockDetected)
        {
            throw new InvalidOperationException("Phieu xuat dang duoc xu ly, vui long thu lai");
        }
    }

    public async Task<WarehouseIssueResponseDTO> CancelAsync(int id)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();
        var oldStatus = await _db.PhieuXuatKho.AsNoTracking()
            .Where(x => x.MaPhieuXuat == id).Select(x => x.TrangThai).FirstOrDefaultAsync();
        var changedRows = await _db.PhieuXuatKho
            .Where(x => x.MaPhieuXuat == id &&
                        (x.TrangThai == WarehouseIssueStatuses.Draft ||
                         x.TrangThai == WarehouseIssueStatuses.Pending))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.TrangThai, WarehouseIssueStatuses.Cancelled));

        if (changedRows != 1)
            await ThrowStatusErrorAsync(id, "Khong the huy phieu xuat o trang thai hien tai");

        _audit.AddSuccess(AuditActions.StatusChange, "PhieuXuatKho", id.ToString(),
            before: new { status = oldStatus },
            after: new { status = WarehouseIssueStatuses.Cancelled });
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return await GetByIdAsync(id);
    }

    public async Task DeleteAsync(int id)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();
        var snapshot = await _db.PhieuXuatKho.AsNoTracking()
            .Where(x => x.MaPhieuXuat == id)
            .Select(x => new { x.SoPhieu, x.TrangThai })
            .FirstOrDefaultAsync();
        var deletedRows = await _db.PhieuXuatKho
            .Where(x => x.MaPhieuXuat == id && x.TrangThai == WarehouseIssueStatuses.Draft)
            .ExecuteDeleteAsync();

        if (deletedRows != 1)
            await ThrowStatusErrorAsync(id, "Chi duoc xoa phieu xuat dang nhap");

        _audit.AddSuccess(AuditActions.Delete, "PhieuXuatKho", id.ToString(), before: snapshot);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private async Task ValidateAsync(
        string? reason,
        IReadOnlyCollection<WarehouseIssueItemDTO>? items)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Ly do xuat khong duoc de trong");

        if (items == null || items.Count == 0)
            throw new InvalidOperationException("Phieu xuat phai co it nhat mot san pham");

        if (items.Any(x => x.SoLuong <= 0))
            throw new InvalidOperationException("So luong xuat phai lon hon 0");

        var productIds = items.Select(x => x.MaSanPham).ToList();
        if (productIds.Distinct().Count() != productIds.Count)
            throw new InvalidOperationException("San pham khong duoc lap trong cung phieu xuat");

        var existingCount = await _db.SanPham.CountAsync(x => productIds.Contains(x.MaSanPham));
        if (existingCount != productIds.Count)
            throw new InvalidOperationException("Co san pham khong ton tai");
    }

    private async Task ThrowStatusErrorAsync(int id, string message)
    {
        if (!await _db.PhieuXuatKho.AnyAsync(x => x.MaPhieuXuat == id))
            throw new KeyNotFoundException("Phieu xuat kho khong ton tai");

        throw new InvalidOperationException(message);
    }

    private IQueryable<PhieuXuatKho> IssueQuery()
    {
        return _db.PhieuXuatKho
            .AsNoTracking()
            .Include(x => x.ChiTietPhieuXuat)
            .ThenInclude(x => x.SanPham);
    }

    private static List<ChiTietPhieuXuat> BuildItems(IEnumerable<WarehouseIssueItemDTO> items)
    {
        return items.Select(x => new ChiTietPhieuXuat
        {
            MaSanPham = x.MaSanPham,
            SoLuong = x.SoLuong
        }).ToList();
    }

    private static WarehouseIssueResponseDTO ToResponse(PhieuXuatKho issue)
    {
        var items = issue.ChiTietPhieuXuat
            .OrderBy(x => x.MaChiTiet)
            .Select(x => new WarehouseIssueItemResponseDTO
            {
                MaChiTiet = x.MaChiTiet,
                MaSanPham = x.MaSanPham,
                TenSanPham = x.SanPham.TenSanPham,
                SoLuong = x.SoLuong
            })
            .ToList();

        return new WarehouseIssueResponseDTO
        {
            MaPhieuXuat = issue.MaPhieuXuat,
            SoPhieu = issue.SoPhieu,
            LyDoXuat = issue.LyDoXuat,
            GhiChu = issue.GhiChu,
            TrangThai = issue.TrangThai,
            NgayTao = issue.NgayTao,
            NgayHoanTat = issue.NgayHoanTat,
            MaNguoiTao = issue.MaNguoiTao,
            MaNguoiHoanTat = issue.MaNguoiHoanTat,
            TongSoLuong = items.Sum(x => x.SoLuong),
            ChiTiet = items
        };
    }

    private static string GenerateIssueNumber()
    {
        return $"PX-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..29].ToUpperInvariant();
    }
}
