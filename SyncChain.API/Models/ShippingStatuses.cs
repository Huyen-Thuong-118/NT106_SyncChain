namespace SyncChain.API.Models;

public static class ShippingStatuses
{
    public const string Pending = "pending";
    public const string Ready = "ready";
    public const string PickedUp = "picked_up";
    public const string InTransit = "in_transit";
    public const string Delivered = "delivered";
    public const string Failed = "failed";
    public const string Returned = "returned";
    public const string Cancelled = "cancelled";

    public static readonly string[] All =
    {
        Pending, Ready, PickedUp, InTransit,
        Delivered, Failed, Returned, Cancelled
    };

    public static bool IsTerminal(string status) =>
        status is Delivered or Returned or Cancelled;

    public static bool CanTransition(string current, string requested) =>
        (current, requested) switch
        {
            (Pending, Ready) => true,
            (Pending, Cancelled) => true,
            (Ready, PickedUp) => true,
            (Ready, Cancelled) => true,
            (PickedUp, InTransit) => true,
            (PickedUp, Failed) => true,
            (InTransit, Delivered) => true,
            (InTransit, Failed) => true,
            (Failed, InTransit) => true,
            (Failed, Returned) => true,
            _ => false
        };
}
