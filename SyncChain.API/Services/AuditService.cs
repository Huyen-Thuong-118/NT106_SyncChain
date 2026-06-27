using System.Text.Json;
using System.Text.Json.Nodes;
using SyncChain.API.Data;
using SyncChain.API.Models;

namespace SyncChain.API.Services;

public interface IAuditService
{
    void AddSuccess(
        string action,
        string entityType,
        string? entityId,
        object? before = null,
        object? after = null,
        object? metadata = null,
        AuditActor? actor = null,
        AuditRequestContext? requestContext = null);

    Task AddFailureAsync(
        string action,
        string entityType,
        string? entityId,
        object? metadata = null,
        AuditActor? actor = null,
        AuditRequestContext? requestContext = null,
        CancellationToken cancellationToken = default);
}

public class AuditService : IAuditService
{
    private const int MaxJsonLength = 32768;
    private static readonly string[] SensitiveNames =
    {
        "password", "matkhau", "token", "jwt", "secret", "apikey",
        "api_key", "connectionstring", "refresh"
    };

    private readonly AppDbContext _db;
    private readonly IAuditContextAccessor _contextAccessor;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuditService> _logger;

    public AuditService(
        AppDbContext db,
        IAuditContextAccessor contextAccessor,
        IServiceScopeFactory scopeFactory,
        ILogger<AuditService> logger)
    {
        _db = db;
        _contextAccessor = contextAccessor;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public void AddSuccess(
        string action,
        string entityType,
        string? entityId,
        object? before = null,
        object? after = null,
        object? metadata = null,
        AuditActor? actor = null,
        AuditRequestContext? requestContext = null)
    {
        _db.AuditLog.Add(Build(
            action,
            entityType,
            entityId,
            AuditResultStatuses.Success,
            before,
            after,
            metadata,
            actor ?? _contextAccessor.Actor,
            requestContext ?? _contextAccessor.RequestContext));
    }

    public async Task AddFailureAsync(
        string action,
        string entityType,
        string? entityId,
        object? metadata = null,
        AuditActor? actor = null,
        AuditRequestContext? requestContext = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var resolvedActor = actor ?? _contextAccessor.Actor;
            var resolvedRequest = requestContext ?? _contextAccessor.RequestContext;
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.AuditLog.Add(Build(
                action,
                entityType,
                entityId,
                AuditResultStatuses.Failed,
                null,
                null,
                metadata,
                resolvedActor,
                resolvedRequest));
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist FAILED audit log for {Action} {EntityType}.", action, entityType);
        }
    }

    private static AuditLog Build(
        string action,
        string entityType,
        string? entityId,
        string result,
        object? before,
        object? after,
        object? metadata,
        AuditActor actor,
        AuditRequestContext request) => new()
    {
        MaNguoiDung = actor.UserId,
        TenDangNhap = Limit(actor.Username, 150),
        VaiTro = Limit(actor.Role, 50),
        HanhDong = Limit(action.Trim(), 100),
        LoaiDoiTuong = Limit(entityType.Trim(), 100),
        MaDoiTuong = entityId == null ? null : Limit(entityId.Trim(), 100),
        TrangThaiKetQua = result,
        DuLieuTruoc = SafeJson(before),
        DuLieuSau = SafeJson(after),
        Metadata = SafeJson(metadata),
        ThoiGian = DateTime.UtcNow,
        TraceId = Limit(request.TraceId, 100),
        IpAddress = Limit(request.IpAddress, 64),
        UserAgent = Limit(request.UserAgent, 500)
    };

    private static string SafeJson(object? value)
    {
        if (value == null)
            return "{}";

        try
        {
            var node = JsonSerializer.SerializeToNode(value);
            Sanitize(node);
            var json = node?.ToJsonString() ?? "{}";
            return json.Length <= MaxJsonLength
                ? json
                : JsonSerializer.Serialize(new { truncated = true, originalLength = json.Length });
        }
        catch
        {
            return JsonSerializer.Serialize(new { serializationFailed = true });
        }
    }

    private static void Sanitize(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            foreach (var property in obj.ToList())
            {
                var normalized = property.Key.Replace("_", string.Empty).ToLowerInvariant();
                if (normalized != "passwordchanged" &&
                    SensitiveNames.Any(x => normalized.Contains(x.Replace("_", string.Empty))))
                {
                    obj.Remove(property.Key);
                    continue;
                }
                Sanitize(property.Value);
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var item in array)
                Sanitize(item);
        }
    }

    private static string Limit(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
