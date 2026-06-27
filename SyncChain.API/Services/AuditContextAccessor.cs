using System.Security.Claims;

namespace SyncChain.API.Services;

public sealed record AuditActor(int? UserId, string Username, string Role);
public sealed record AuditRequestContext(string TraceId, string IpAddress, string UserAgent);

public interface IAuditContextAccessor
{
    AuditActor Actor { get; }
    AuditRequestContext RequestContext { get; }
}

public class AuditContextAccessor : IAuditContextAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public AuditActor Actor
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            var userIdValue = context?.User.FindFirst("user_id")?.Value;
            int? userId = int.TryParse(userIdValue, out var parsed) ? parsed : null;
            var username = context?.User.FindFirst(ClaimTypes.Name)?.Value
                ?? context?.User.FindFirst(ClaimTypes.Email)?.Value
                ?? string.Empty;
            var role = context?.User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
            return new AuditActor(userId, Limit(username, 150), Limit(role, 50));
        }
    }

    public AuditRequestContext RequestContext
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            return new AuditRequestContext(
                Limit(context?.TraceIdentifier ?? string.Empty, 100),
                Limit(context?.Connection.RemoteIpAddress?.ToString() ?? string.Empty, 64),
                Limit(context?.Request.Headers.UserAgent.ToString() ?? string.Empty, 500));
        }
    }

    private static string Limit(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
