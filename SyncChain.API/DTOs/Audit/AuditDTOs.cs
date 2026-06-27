using System.ComponentModel.DataAnnotations;

namespace SyncChain.API.DTOs.Audit;

public class AuditLogQueryDTO
{
    public int? UserId { get; set; }
    public string? Username { get; set; }
    public string? Role { get; set; }
    public string? Action { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? Result { get; set; }
    public string? TraceId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }

    [Range(1, 1000000)]
    public int Page { get; set; } = 1;

    [Range(1, 200)]
    public int PageSize { get; set; } = 50;
}

public class AuditLogResponseDTO
{
    public long Id { get; set; }
    public int? UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string Result { get; set; } = string.Empty;
    public string Before { get; set; } = "{}";
    public string After { get; set; } = "{}";
    public string Metadata { get; set; } = "{}";
    public DateTime Timestamp { get; set; }
    public string TraceId { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
}

public class AuditLogPageDTO
{
    public List<AuditLogResponseDTO> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
}
