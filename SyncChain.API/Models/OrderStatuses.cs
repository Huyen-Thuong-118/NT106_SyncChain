namespace SyncChain.API.Models;

public static class OrderStatuses
{
    public const string Pending = "pending";
    public const string Processing = "processing";
    public const string Shipping = "shipping";
    public const string Done = "done";
    public const string Cancel = "cancel";

    public static readonly string[] All =
    {
        Pending,
        Processing,
        Shipping,
        Done,
        Cancel
    };

    public static bool IsTerminal(string status)
    {
        return status is Done or Cancel;
    }

    public static bool CanTransition(string currentStatus, string requestedStatus)
    {
        return (currentStatus, requestedStatus) switch
        {
            (Pending, Processing) => true,
            (Pending, Cancel) => true,
            (Processing, Shipping) => true,
            (Processing, Cancel) => true,
            (Shipping, Done) => true,
            (Shipping, Cancel) => true,
            _ => false
        };
    }
}
