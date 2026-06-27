using System.ComponentModel.DataAnnotations;

namespace SyncChain.API.DTOs.Report;

public class ReportDateRangeQueryDTO
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

public class InventoryReportQueryDTO
{
    [Range(0, int.MaxValue)]
    public int LowStockThreshold { get; set; } = 10;
}

public class RevenueReportQueryDTO : ReportDateRangeQueryDTO
{
    public string GroupBy { get; set; } = "day";
}

public class TopProductReportQueryDTO : ReportDateRangeQueryDTO
{
    [Range(1, 100)]
    public int Take { get; set; } = 10;

    public string SortBy { get; set; } = "revenue";
}

public class DashboardSummaryDTO
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public OrderSummaryDTO Orders { get; set; } = new();
    public RevenueSummaryDTO Revenue { get; set; } = new();
    public InventorySummaryDTO Inventory { get; set; } = new();
    public ShippingSummaryDTO Shipping { get; set; } = new();
}

public class OrderSummaryDTO
{
    public int Total { get; set; }
    public int Pending { get; set; }
    public int Processing { get; set; }
    public int Done { get; set; }
    public int Cancel { get; set; }
}

public class RevenueSummaryDTO
{
    public decimal Gross { get; set; }
    public decimal Net { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal CancelledValue { get; set; }
}

public class InventorySummaryDTO
{
    public int TotalProducts { get; set; }
    public int ActiveProducts { get; set; }
    public int LowStockProducts { get; set; }
    public int OutOfStockProducts { get; set; }
    public decimal TotalInventoryValue { get; set; }
}

public class ShippingSummaryDTO
{
    public int Total { get; set; }
    public int Pending { get; set; }
    public int Ready { get; set; }
    public int PickedUp { get; set; }
    public int InTransit { get; set; }
    public int Delivered { get; set; }
    public int Failed { get; set; }
    public int Returned { get; set; }
    public int Cancelled { get; set; }
}

public class CategoryReportPageDTO
{
    public List<CategoryReportItemDTO> Items { get; set; } = new();
}

public class CategoryReportItemDTO
{
    public int? CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int ProductCount { get; set; }
    public int ActiveProductCount { get; set; }
    public int StockQuantity { get; set; }
    public int SoldQuantity { get; set; }
    public decimal Revenue { get; set; }
}

public class InventoryReportDTO
{
    public int TotalProducts { get; set; }
    public int TotalQuantity { get; set; }
    public decimal TotalInventoryValue { get; set; }
    public int LowStockThreshold { get; set; }
    public List<InventoryReportProductDTO> LowStockProducts { get; set; } = new();
    public List<InventoryReportProductDTO> OutOfStockProducts { get; set; } = new();
}

public class InventoryReportProductDTO
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal UnitCost { get; set; }
    public decimal InventoryValue { get; set; }
}

public class ShippingReportDTO
{
    public int TotalShipments { get; set; }
    public decimal TotalShippingFee { get; set; }
    public List<ShippingStatusReportItemDTO> ByStatus { get; set; } = new();
    public List<ShippingCarrierReportItemDTO> ByCarrier { get; set; } = new();
}

public class ShippingStatusReportItemDTO
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class ShippingCarrierReportItemDTO
{
    public string Carrier { get; set; } = string.Empty;
    public int Count { get; set; }
    public int Delivered { get; set; }
    public int Failed { get; set; }
    public int Returned { get; set; }
    public decimal ShippingFee { get; set; }
}

public class RevenueReportDTO
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public string GroupBy { get; set; } = string.Empty;
    public decimal GrossRevenue { get; set; }
    public decimal NetRevenue { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal CancelledValue { get; set; }
    public List<RevenueReportItemDTO> Items { get; set; } = new();
}

public class RevenueReportItemDTO
{
    public string Period { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal GrossRevenue { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal NetRevenue { get; set; }
}

public class OrderReportDTO
{
    public int Total { get; set; }
    public List<OrderStatusReportItemDTO> ByStatus { get; set; } = new();
    public List<OrderDayReportItemDTO> ByDay { get; set; } = new();
}

public class OrderStatusReportItemDTO
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class OrderDayReportItemDTO
{
    public DateTime Date { get; set; }
    public int Total { get; set; }
    public int Pending { get; set; }
    public int Processing { get; set; }
    public int Done { get; set; }
    public int Cancel { get; set; }
}

public class TopProductReportDTO
{
    public List<TopProductReportItemDTO> Items { get; set; } = new();
}

public class TopProductReportItemDTO
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int SoldQuantity { get; set; }
    public decimal Revenue { get; set; }
}
