namespace SyncChain.API.DTOs.Shipping;

public class EstimateDeliveryDTO
{
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public string Province { get; set; } = string.Empty;
    public string Ward { get; set; } = string.Empty;
    public string ServiceType { get; set; } = "Tieu chuan";
    public decimal WeightKg { get; set; } = 1;
    public decimal OrderTotal { get; set; }
}

public class DeliveryEstimateResponseDTO
{
    public string Address { get; set; } = string.Empty;
    public string Warehouse { get; set; } = "TP.HCM";
    public DateTime EarliestDelivery { get; set; }
    public DateTime LatestDelivery { get; set; }
    public int DeliveryDays { get; set; }
    public decimal ShippingFee { get; set; }
    public int ConfidencePercent { get; set; }
    public int EstimatedDistanceKm { get; set; }
    public string AreaType { get; set; } = string.Empty;
    public string WarehouseProcessing { get; set; } = string.Empty;
    public string TransitTime { get; set; } = string.Empty;
    public string Factors { get; set; } = string.Empty;
    public string Assumption { get; set; } = string.Empty;
}
