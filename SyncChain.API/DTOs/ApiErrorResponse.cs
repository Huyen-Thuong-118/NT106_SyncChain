namespace SyncChain.API.DTOs;

public class ApiErrorResponse
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public object Details { get; set; } = new { };
    public string TraceId { get; set; } = string.Empty;
}
