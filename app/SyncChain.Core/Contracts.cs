using System.Text.Json;
using System.Text.Json.Serialization;

namespace SyncChain.Core;

public static class NetworkCommands
{
    public const string Login = "login";
    public const string GetDashboard = "get-dashboard";
    public const string GetProducts = "get-products";
    public const string GetOrders = "get-orders";
    public const string CreateOrder = "create-order";
    public const string UpdateOrderStatus = "update-order-status";
    public const string GetTracking = "get-tracking";
    public const string AddProduct = "add-product";
    public const string DeleteProduct = "delete-product";
}

public static class Roles
{
    public const string Admin = "Admin";
    public const string Distributor = "Distributor";
    public const string Customer = "Customer";
    public const string Factory = "Factory";
}

public static class OrderStatuses
{
    public const string Placed = "Da dat hang";
    public const string Paid = "Da thanh toan";
    public const string Shipping = "Dang van chuyen";
    public const string Delivered = "Da giao hang";
    public const string Completed = "Da hoan tat";
    public const string PendingFactory = "Cho nha may xu ly";
}

public static class PaymentMethods
{
    public const string Online = "Online";
    public const string CashOnDelivery = "COD";
}

public sealed record RequestEnvelope(string Command, JsonElement Payload);

public sealed record ResponseEnvelope(bool Success, string Message, JsonElement? Data = null, string? ErrorCode = null);

public sealed record LoginRequest(string Username, string Password);

public sealed record SessionContext(int UserId, string Username, string Role, string Token);

public sealed record LoginResponse(SessionContext Session, string WelcomeMessage);

public sealed record AuthorizedRequest(string SessionToken);

public sealed record DashboardSnapshot(
    string Username,
    string Role,
    int ProductCount,
    int PendingOrders,
    int ShippingOrders,
    int LowStockCount,
    decimal RevenueToday,
    IReadOnlyList<InventoryAlertDto> Alerts,
    IReadOnlyList<ActivityLogDto> RecentActivities);

public sealed record InventoryAlertDto(int ProductId, string ProductName, int Quantity, int MinimumStock, string Severity);

public sealed record ActivityLogDto(string Username, string Action, string Detail, DateTime CreatedAt);

public sealed record ProductDto(int ProductId, string ProductName, decimal Price, int Quantity, int MinimumStock, string Status);

public sealed record ProductsResponse(IReadOnlyList<ProductDto> Products);

public sealed record AddProductRequest(string SessionToken, string ProductName, decimal Price, int Quantity, int MinimumStock);

public sealed record DeleteProductRequest(string SessionToken, int ProductId);

public sealed record OrderDto(
    long OrderId,
    string CustomerName,
    int ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice,
    string PaymentMethod,
    string OrderStatus,
    string TrackingCode,
    DateTime CreatedAt);

public sealed record OrdersResponse(IReadOnlyList<OrderDto> Orders);

public sealed record CreateOrderRequest(string SessionToken, int ProductId, int Quantity, string PaymentMethod);

public sealed record CreateOrderResponse(long OrderId, string TrackingCode, string OrderStatus, string Message);

public sealed record UpdateOrderStatusRequest(string SessionToken, long OrderId, string OrderStatus, string Note);

public sealed record TrackingEventDto(long OrderId, string OrderStatus, string Note, DateTime CreatedAt, string UpdatedBy);

public sealed record TrackingResponse(IReadOnlyList<TrackingEventDto> Events);

public static class ProtocolSerializer
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string SerializeRequest<T>(string command, T payload)
    {
        var payloadElement = JsonSerializer.SerializeToElement(payload, JsonOptions);
        return JsonSerializer.Serialize(new RequestEnvelope(command, payloadElement), JsonOptions);
    }

    public static string SerializeResponse<T>(bool success, string message, T? data = default, string? errorCode = null)
    {
        JsonElement? payload = data is null ? null : JsonSerializer.SerializeToElement(data, JsonOptions);
        return JsonSerializer.Serialize(new ResponseEnvelope(success, message, payload, errorCode), JsonOptions);
    }

    public static T DeserializePayload<T>(JsonElement element) =>
        JsonSerializer.Deserialize<T>(element.GetRawText(), JsonOptions)
        ?? throw new InvalidOperationException($"Cannot deserialize payload to {typeof(T).Name}.");

    public static T? DeserializeData<T>(ResponseEnvelope response)
    {
        if (response.Data is null)
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(response.Data.Value.GetRawText(), JsonOptions);
    }
}
