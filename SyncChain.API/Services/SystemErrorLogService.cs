using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SyncChain.API.Data;
using SyncChain.API.Exceptions;
using SyncChain.API.Models;

namespace SyncChain.API.Services;

public sealed record SystemErrorLogEntry(
    string ErrorCode,
    string Message,
    int? StatusCode,
    object? Details = null,
    Exception? Exception = null);

public interface ISystemErrorLogService
{
    Task LogAsync(SystemErrorLogEntry entry, CancellationToken cancellationToken = default);
}

public class SystemErrorLogService : ISystemErrorLogService
{
    private const int MaxJsonLength = 32768;
    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "matkhau", "matkhauhash", "confirmPassword", "token", "refreshToken",
        "authorization", "cookie", "connectionString", "database_url", "secret",
        "apiKey", "api_key", "clientSecret"
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<SystemErrorLogService> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public SystemErrorLogService(
        IServiceScopeFactory scopeFactory,
        IHttpContextAccessor httpContextAccessor,
        ILogger<SystemErrorLogService> logger,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _environment = environment;
        _configuration = configuration;
    }

    public async Task LogAsync(SystemErrorLogEntry entry, CancellationToken cancellationToken = default)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var user = httpContext?.User;
            var userIdValue = user?.FindFirst("user_id")?.Value;
            int? userId = int.TryParse(userIdValue, out var parsedUserId) ? parsedUserId : null;

            var log = new SystemErrorLog
            {
                TraceId = LimitRequired(httpContext?.TraceIdentifier, 100),
                RequestPath = Limit(httpContext?.Request.Path.Value ?? string.Empty, 500),
                HttpMethod = Limit(httpContext?.Request.Method ?? string.Empty, 20),
                StatusCode = entry.StatusCode,
                ErrorCode = LimitRequired(entry.ErrorCode, 100),
                Message = LimitRequired(entry.Message, 1000),
                ExceptionType = Limit(entry.Exception?.GetType().FullName, 500),
                StackTrace = ShouldIncludeStackTrace() ? entry.Exception?.ToString() : null,
                DetailsJson = SerializeSanitized(entry.Details),
                UserId = userId,
                Username = Limit(
                    user?.FindFirst(ClaimTypes.Name)?.Value ??
                    user?.FindFirst(ClaimTypes.Email)?.Value,
                    150),
                Role = Limit(user?.FindFirst(ClaimTypes.Role)?.Value, 50),
                IpAddress = Limit(httpContext?.Connection.RemoteIpAddress?.ToString(), 64),
                UserAgent = Limit(httpContext?.Request.Headers.UserAgent.ToString(), 500),
                CreatedAt = DateTime.UtcNow
            };

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.SystemErrorLog.Add(log);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception logException)
        {
            _logger.LogError(
                logException,
                "Failed to write system error log for {ErrorCode}. Original error: {Message}",
                entry.ErrorCode,
                entry.Message);
        }
    }

    private bool ShouldIncludeStackTrace()
    {
        var configured = _configuration.GetValue<bool?>("SystemErrorLog:IncludeStackTrace");
        if (configured.HasValue)
            return configured.Value;
        return _environment.IsDevelopment() || _environment.IsStaging();
    }

    private static string SerializeSanitized(object? value)
    {
        if (value == null)
            return "{}";

        try
        {
            var json = JsonSerializer.Serialize(Sanitize(value), new JsonSerializerOptions
            {
                WriteIndented = false
            });
            return Limit(json, MaxJsonLength) ?? "{}";
        }
        catch
        {
            return "{\"serialization\":\"failed\"}";
        }
    }

    private static object? Sanitize(object? value)
    {
        if (value == null)
            return null;

        var element = JsonSerializer.SerializeToElement(value);
        return SanitizeElement(element);
    }

    private static object? SanitizeElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(
                property => property.Name,
                property => IsSensitiveKey(property.Name)
                    ? "[REDACTED]"
                    : SanitizeElement(property.Value)),
            JsonValueKind.Array => element.EnumerateArray().Select(SanitizeElement).ToList(),
            JsonValueKind.String => SanitizeString(element.GetString()),
            JsonValueKind.Number => element.TryGetInt64(out var longValue)
                ? longValue
                : element.TryGetDecimal(out var decimalValue) ? decimalValue : element.GetRawText(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null
        };
    }

    private static object? SanitizeString(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value;
        return LooksSensitive(value) ? "[REDACTED]" : Limit(value, 2000);
    }

    private static bool IsSensitiveKey(string key) =>
        SensitiveKeys.Contains(key) ||
        SensitiveKeys.Any(sensitive => key.Contains(sensitive, StringComparison.OrdinalIgnoreCase));

    private static bool LooksSensitive(string value) =>
        Regex.IsMatch(value, @"Bearer\s+[A-Za-z0-9._~+/=-]+", RegexOptions.IgnoreCase) ||
        Regex.IsMatch(value, @"(Host=|Username=|Password=|Database=|DATABASE_URL=)", RegexOptions.IgnoreCase);

    private static string? Limit(string? value, int maxLength) =>
        value == null || value.Length <= maxLength ? value : value[..maxLength];

    private static string LimitRequired(string? value, int maxLength) =>
        Limit(value ?? string.Empty, maxLength) ?? string.Empty;
}
