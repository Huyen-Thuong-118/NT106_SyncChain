using System.ComponentModel.DataAnnotations;

namespace SyncChain.API.DTOs.Shipping;

public class CreateShippingDTO
{
    [Required, MaxLength(100)]
    public string Carrier { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? TrackingNumber { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999")]
    public decimal ShippingFee { get; set; }

    public DateTime? EstimatedDeliveryAt { get; set; }
}

public class UpdateShippingDTO : CreateShippingDTO
{
    [Required, Range(0, int.MaxValue)]
    public int? ConcurrencyVersion { get; set; }
}

public class UpdateShippingStatusDTO
{
    [Required]
    public string Status { get; set; } = string.Empty;

    [Required]
    public string ExpectedStatus { get; set; } = string.Empty;

    [Required, Range(0, int.MaxValue)]
    public int? ConcurrencyVersion { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }
}

public class ShippingResponseDTO
{
    public int ShippingId { get; set; }
    public int OrderId { get; set; }
    public string Carrier { get; set; } = string.Empty;
    public string? TrackingNumber { get; set; }
    public decimal ShippingFee { get; set; }
    public string ShippingStatus { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? EstimatedDeliveryAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public int ConcurrencyVersion { get; set; }
}

public class ShippingStatusResultDTO
{
    public int ShippingId { get; set; }
    public int OrderId { get; set; }
    public string OldStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public int ConcurrencyVersion { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ShippingHistoryResponseDTO
{
    public long HistoryId { get; set; }
    public string OldStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public int? UserId { get; set; }
    public string Note { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
}
