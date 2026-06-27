using System.ComponentModel.DataAnnotations;

namespace SyncChain.API.DTOs.SystemErrorLog;

public class SystemErrorLogQueryDTO
{
    public string? TraceId { get; set; }
    public string? ErrorCode { get; set; }
    public int? StatusCode { get; set; }
    public int? UserId { get; set; }
    public string? Path { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }

    [Range(1, 1000000)]
    public int Page { get; set; } = 1;

    [Range(1, 200)]
    public int PageSize { get; set; } = 50;
}

public class SystemErrorLogSummaryDTO
{
    public long Id { get; set; }
    public string TraceId { get; set; } = string.Empty;
    public string? RequestPath { get; set; }
    public string? HttpMethod { get; set; }
    public int? StatusCode { get; set; }
    public string ErrorCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int? UserId { get; set; }
    public string? Username { get; set; }
    public string? Role { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SystemErrorLogDetailDTO : SystemErrorLogSummaryDTO
{
    public string? ExceptionType { get; set; }
    public string? StackTrace { get; set; }
    public string? DetailsJson { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

public class SystemErrorLogPageDTO
{
    public List<SystemErrorLogSummaryDTO> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
}
