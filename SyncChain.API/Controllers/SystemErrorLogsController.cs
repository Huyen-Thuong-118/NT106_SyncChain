using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SyncChain.API.Data;
using SyncChain.API.DTOs.SystemErrorLog;
using SyncChain.API.Exceptions;

namespace SyncChain.API.Controllers;

[ApiController]
[Route("api/system-error-logs")]
[Authorize(Policy = "SystemErrorLogRead")]
public class SystemErrorLogsController : ControllerBase
{
    private readonly AppDbContext _db;

    public SystemErrorLogsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] SystemErrorLogQueryDTO filter)
    {
        var from = NormalizeUtc(filter.From);
        var to = NormalizeUtc(filter.To);
        if (from.HasValue && to.HasValue && from > to)
            throw new ValidationApiException("Khoang thoi gian log loi khong hop le.");

        var query = _db.SystemErrorLog.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(filter.TraceId))
            query = query.Where(x => x.TraceId == filter.TraceId.Trim());
        if (!string.IsNullOrWhiteSpace(filter.ErrorCode))
            query = query.Where(x => x.ErrorCode == filter.ErrorCode.Trim().ToUpperInvariant());
        if (filter.StatusCode.HasValue)
            query = query.Where(x => x.StatusCode == filter.StatusCode);
        if (filter.UserId.HasValue)
            query = query.Where(x => x.UserId == filter.UserId);
        if (!string.IsNullOrWhiteSpace(filter.Path))
            query = query.Where(x => x.RequestPath != null && x.RequestPath.Contains(filter.Path.Trim()));
        if (from.HasValue)
            query = query.Where(x => x.CreatedAt >= from.Value);
        if (to.HasValue)
            query = query.Where(x => x.CreatedAt <= to.Value);

        var totalItems = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(x => new SystemErrorLogSummaryDTO
            {
                Id = x.Id,
                TraceId = x.TraceId,
                RequestPath = x.RequestPath,
                HttpMethod = x.HttpMethod,
                StatusCode = x.StatusCode,
                ErrorCode = x.ErrorCode,
                Message = x.Message,
                UserId = x.UserId,
                Username = x.Username,
                Role = x.Role,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        return Ok(new SystemErrorLogPageDTO
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
        var item = await _db.SystemErrorLog.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new SystemErrorLogDetailDTO
            {
                Id = x.Id,
                TraceId = x.TraceId,
                RequestPath = x.RequestPath,
                HttpMethod = x.HttpMethod,
                StatusCode = x.StatusCode,
                ErrorCode = x.ErrorCode,
                Message = x.Message,
                ExceptionType = x.ExceptionType,
                StackTrace = x.StackTrace,
                DetailsJson = x.DetailsJson,
                UserId = x.UserId,
                Username = x.Username,
                Role = x.Role,
                IpAddress = x.IpAddress,
                UserAgent = x.UserAgent,
                CreatedAt = x.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (item == null)
            throw new SystemErrorLogNotFoundException(id);

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
