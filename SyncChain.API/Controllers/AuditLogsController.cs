using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SyncChain.API.Data;
using SyncChain.API.DTOs.Audit;
using SyncChain.API.Exceptions;
using SyncChain.API.Models;

namespace SyncChain.API.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize(Policy = "AuditRead")]
public class AuditLogsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AuditLogsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] AuditLogQueryDTO filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Result) &&
            !AuditResultStatuses.All.Contains(filter.Result.Trim().ToUpperInvariant()))
            throw new ValidationApiException("Trang thai ket qua audit khong hop le.");

        var from = NormalizeUtc(filter.From);
        var to = NormalizeUtc(filter.To);
        if (from.HasValue && to.HasValue && from > to)
            throw new ValidationApiException("Khoang thoi gian audit khong hop le.");

        var query = _db.AuditLog.AsNoTracking().AsQueryable();
        if (filter.UserId.HasValue)
            query = query.Where(x => x.MaNguoiDung == filter.UserId);
        if (!string.IsNullOrWhiteSpace(filter.Username))
            query = query.Where(x => x.TenDangNhap == filter.Username.Trim());
        if (!string.IsNullOrWhiteSpace(filter.Role))
            query = query.Where(x => x.VaiTro == filter.Role.Trim());
        if (!string.IsNullOrWhiteSpace(filter.Action))
            query = query.Where(x => x.HanhDong == filter.Action.Trim().ToUpperInvariant());
        if (!string.IsNullOrWhiteSpace(filter.EntityType))
            query = query.Where(x => x.LoaiDoiTuong == filter.EntityType.Trim());
        if (!string.IsNullOrWhiteSpace(filter.EntityId))
            query = query.Where(x => x.MaDoiTuong == filter.EntityId.Trim());
        if (!string.IsNullOrWhiteSpace(filter.Result))
            query = query.Where(x => x.TrangThaiKetQua == filter.Result.Trim().ToUpperInvariant());
        if (!string.IsNullOrWhiteSpace(filter.TraceId))
            query = query.Where(x => x.TraceId == filter.TraceId.Trim());
        if (from.HasValue)
            query = query.Where(x => x.ThoiGian >= from.Value);
        if (to.HasValue)
            query = query.Where(x => x.ThoiGian <= to.Value);

        var totalItems = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => x.ThoiGian)
            .ThenByDescending(x => x.MaAudit)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(x => new AuditLogResponseDTO
            {
                Id = x.MaAudit, UserId = x.MaNguoiDung, Username = x.TenDangNhap,
                Role = x.VaiTro, Action = x.HanhDong, EntityType = x.LoaiDoiTuong,
                EntityId = x.MaDoiTuong, Result = x.TrangThaiKetQua,
                Before = x.DuLieuTruoc, After = x.DuLieuSau, Metadata = x.Metadata,
                Timestamp = x.ThoiGian, TraceId = x.TraceId,
                IpAddress = x.IpAddress, UserAgent = x.UserAgent
            })
            .ToListAsync();

        return Ok(new AuditLogPageDTO
        {
            Items = items,
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalItems = totalItems,
            TotalPages = (int)Math.Ceiling(totalItems / (double)filter.PageSize)
        });
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var item = await _db.AuditLog.AsNoTracking()
            .Where(x => x.MaAudit == id)
            .Select(x => new AuditLogResponseDTO
            {
                Id = x.MaAudit, UserId = x.MaNguoiDung, Username = x.TenDangNhap,
                Role = x.VaiTro, Action = x.HanhDong, EntityType = x.LoaiDoiTuong,
                EntityId = x.MaDoiTuong, Result = x.TrangThaiKetQua,
                Before = x.DuLieuTruoc, After = x.DuLieuSau, Metadata = x.Metadata,
                Timestamp = x.ThoiGian, TraceId = x.TraceId,
                IpAddress = x.IpAddress, UserAgent = x.UserAgent
            })
            .FirstOrDefaultAsync();
        if (item == null)
            throw new AuditLogNotFoundException(id);
        return Ok(item);
    }

    private static DateTime? NormalizeUtc(DateTime? value)
    {
        if (!value.HasValue)
            return null;
        return value.Value.Kind switch
        {
            DateTimeKind.Utc => value.Value,
            DateTimeKind.Local => value.Value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
        };
    }
}
