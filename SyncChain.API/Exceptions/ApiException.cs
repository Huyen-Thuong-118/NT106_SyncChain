namespace SyncChain.API.Exceptions;

public abstract class ApiException : Exception
{
    protected ApiException(
        int statusCode,
        string code,
        string message,
        IReadOnlyDictionary<string, object?>? details = null) : base(message)
    {
        StatusCode = statusCode;
        Code = code;
        Details = details ?? new Dictionary<string, object?>();
    }

    public int StatusCode { get; }
    public string Code { get; }
    public IReadOnlyDictionary<string, object?> Details { get; }
}

public class ValidationApiException : ApiException
{
    public ValidationApiException(string message, object? details = null)
        : base(
            StatusCodes.Status400BadRequest,
            "VALIDATION_ERROR",
            message,
            ToDetails(details))
    {
    }

    private static IReadOnlyDictionary<string, object?>? ToDetails(object? details)
    {
        return details == null
            ? null
            : new Dictionary<string, object?> { ["validation"] = details };
    }
}

public class ProductNotFoundException : ApiException
{
    public ProductNotFoundException(int productId)
        : base(
            StatusCodes.Status404NotFound,
            "PRODUCT_NOT_FOUND",
            $"San pham #{productId} khong ton tai.",
            new Dictionary<string, object?> { ["productId"] = productId })
    {
    }
}

public class ProductUnavailableException : ApiException
{
    public ProductUnavailableException(int productId, string productName)
        : base(
            StatusCodes.Status409Conflict,
            "PRODUCT_UNAVAILABLE",
            $"San pham {productName} hien dang ngung ban.",
            new Dictionary<string, object?>
            {
                ["productId"] = productId,
                ["productName"] = productName
            })
    {
    }
}

public class OutOfStockException : ApiException
{
    public OutOfStockException(int productId, string productName, int requestedQuantity)
        : base(
            StatusCodes.Status409Conflict,
            "OUT_OF_STOCK",
            $"San pham {productName} da het hang.",
            new Dictionary<string, object?>
            {
                ["productId"] = productId,
                ["productName"] = productName,
                ["requestedQuantity"] = requestedQuantity,
                ["availableQuantity"] = 0
            })
    {
    }
}

public class InsufficientStockException : ApiException
{
    public InsufficientStockException(
        int productId,
        string productName,
        int requestedQuantity,
        int availableQuantity)
        : base(
            StatusCodes.Status409Conflict,
            "INSUFFICIENT_STOCK",
            $"San pham {productName} chi con {availableQuantity} trong kho.",
            new Dictionary<string, object?>
            {
                ["productId"] = productId,
                ["productName"] = productName,
                ["requestedQuantity"] = requestedQuantity,
                ["availableQuantity"] = availableQuantity
            })
    {
    }
}

public class OrderNotFoundException : ApiException
{
    public OrderNotFoundException(int orderId)
        : base(
            StatusCodes.Status404NotFound,
            "ORDER_NOT_FOUND",
            $"Don hang #{orderId} khong ton tai.",
            new Dictionary<string, object?> { ["orderId"] = orderId })
    {
    }
}

public class OrderAlreadyProcessedException : ApiException
{
    public OrderAlreadyProcessedException(
        int orderId,
        string currentStatus,
        string requestedStatus,
        string expectedStatus,
        int currentVersion)
        : base(
            StatusCodes.Status409Conflict,
            "ORDER_ALREADY_PROCESSED",
            $"Don hang #{orderId} da o trang thai {currentStatus}.",
            new Dictionary<string, object?>
            {
                ["orderId"] = orderId,
                ["currentStatus"] = currentStatus,
                ["requestedStatus"] = requestedStatus,
                ["expectedStatus"] = expectedStatus,
                ["currentVersion"] = currentVersion
            })
    {
    }
}

public class InvalidOrderStateException : ApiException
{
    public InvalidOrderStateException(
        int orderId,
        string currentStatus,
        string requestedStatus,
        string expectedStatus,
        int currentVersion)
        : base(
            StatusCodes.Status409Conflict,
            "INVALID_ORDER_STATE",
            $"Khong the chuyen don hang tu {currentStatus} sang {requestedStatus}.",
            new Dictionary<string, object?>
            {
                ["orderId"] = orderId,
                ["currentStatus"] = currentStatus,
                ["requestedStatus"] = requestedStatus,
                ["expectedStatus"] = expectedStatus,
                ["currentVersion"] = currentVersion
            })
    {
    }
}

public class IdempotencyConflictException : ApiException
{
    public IdempotencyConflictException(string message)
        : base(StatusCodes.Status409Conflict, "IDEMPOTENCY_KEY_CONFLICT", message)
    {
    }
}

public class ConcurrencyConflictException : ApiException
{
    public ConcurrencyConflictException(string message)
        : base(StatusCodes.Status409Conflict, "CONCURRENCY_CONFLICT", message)
    {
    }

    public ConcurrencyConflictException(
        string message,
        IReadOnlyDictionary<string, object?> details)
        : base(StatusCodes.Status409Conflict, "CONCURRENCY_CONFLICT", message, details)
    {
    }

    public ConcurrencyConflictException(
        string message,
        int orderId,
        string currentStatus,
        string requestedStatus,
        string expectedStatus,
        int currentVersion)
        : base(
            StatusCodes.Status409Conflict,
            "CONCURRENCY_CONFLICT",
            message,
            new Dictionary<string, object?>
            {
                ["orderId"] = orderId,
                ["currentStatus"] = currentStatus,
                ["requestedStatus"] = requestedStatus,
                ["expectedStatus"] = expectedStatus,
                ["currentVersion"] = currentVersion
            })
    {
    }
}

public class AuthenticationApiException : ApiException
{
    public AuthenticationApiException(string message)
        : base(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", message)
    {
    }
}

public class ShippingNotFoundException : ApiException
{
    public ShippingNotFoundException(int orderId)
        : base(
            StatusCodes.Status404NotFound,
            "SHIPPING_NOT_FOUND",
            $"Don hang #{orderId} chua co thong tin van chuyen.",
            new Dictionary<string, object?> { ["orderId"] = orderId })
    {
    }

    public ShippingNotFoundException(string trackingNumber)
        : base(
            StatusCodes.Status404NotFound,
            "SHIPPING_NOT_FOUND",
            "Khong tim thay thong tin van chuyen.",
            new Dictionary<string, object?> { ["trackingNumber"] = trackingNumber })
    {
    }
}

public class ShippingConflictException : ApiException
{
    public ShippingConflictException(
        string code,
        string message,
        IReadOnlyDictionary<string, object?> details)
        : base(StatusCodes.Status409Conflict, code, message, details)
    {
    }
}

public class AuditLogNotFoundException : ApiException
{
    public AuditLogNotFoundException(long auditId)
        : base(
            StatusCodes.Status404NotFound,
            "AUDIT_LOG_NOT_FOUND",
            $"Audit log #{auditId} khong ton tai.",
            new Dictionary<string, object?> { ["auditId"] = auditId })
    {
    }
}

public class SystemErrorLogNotFoundException : ApiException
{
    public SystemErrorLogNotFoundException(long logId)
        : base(
            StatusCodes.Status404NotFound,
            "SYSTEM_ERROR_LOG_NOT_FOUND",
            $"System error log #{logId} khong ton tai.",
            new Dictionary<string, object?> { ["logId"] = logId })
    {
    }
}
