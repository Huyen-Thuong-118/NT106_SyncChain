namespace SyncChain.API.Models;

public static class AuditActions
{
    public const string Create = "CREATE";
    public const string Update = "UPDATE";
    public const string Delete = "DELETE";
    public const string StatusChange = "STATUS_CHANGE";
    public const string Approve = "APPROVE";
    public const string Reject = "REJECT";
    public const string Login = "LOGIN";
    public const string LoginFailed = "LOGIN_FAILED";
    public const string Logout = "LOGOUT";
    public const string PasswordChange = "PASSWORD_CHANGE";
    public const string RoleChange = "ROLE_CHANGE";
    public const string InventoryAdjustment = "INVENTORY_ADJUSTMENT";
    public const string OrderStatusChange = "ORDER_STATUS_CHANGE";
    public const string ShippingStatusChange = "SHIPPING_STATUS_CHANGE";
}
