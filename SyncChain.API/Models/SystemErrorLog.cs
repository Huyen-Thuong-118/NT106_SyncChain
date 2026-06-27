using System.ComponentModel.DataAnnotations;

namespace SyncChain.API.Models;

public class SystemErrorLog
{
    [Key]
    public long Id { get; set; }

    [MaxLength(100)]
    public string TraceId { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? RequestPath { get; set; }

    [MaxLength(20)]
    public string? HttpMethod { get; set; }

    public int? StatusCode { get; set; }

    [MaxLength(100)]
    public string ErrorCode { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Message { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ExceptionType { get; set; }

    public string? StackTrace { get; set; }

    public string? DetailsJson { get; set; } = "{}";

    public int? UserId { get; set; }

    [MaxLength(150)]
    public string? Username { get; set; }

    [MaxLength(50)]
    public string? Role { get; set; }

    [MaxLength(64)]
    public string? IpAddress { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
