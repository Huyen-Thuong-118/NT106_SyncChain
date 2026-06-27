namespace SyncChain.API.Models;

public static class InventoryTransactionTypes
{
    public const string Receipt = "Nhap kho";
    public const string OrderIssue = "Xuat kho don hang";
    public const string CancelledOrderReturn = "Hoan kho don huy";
    public const string ManualIssue = "Xuat kho thu cong";
    public const string AdjustmentIncrease = "Dieu chinh tang";
    public const string AdjustmentDecrease = "Dieu chinh giam";

    public static readonly string[] All =
    {
        Receipt,
        OrderIssue,
        CancelledOrderReturn,
        ManualIssue,
        AdjustmentIncrease,
        AdjustmentDecrease
    };
}
