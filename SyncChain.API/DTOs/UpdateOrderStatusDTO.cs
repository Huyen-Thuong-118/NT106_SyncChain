using System.ComponentModel.DataAnnotations;

namespace SyncChain.API.DTOs;

public class UpdateOrderStatusDTO
{
    [Required]
    public string Status { get; set; } = string.Empty;

    [Required]
    public string ExpectedStatus { get; set; } = string.Empty;

    [Required]
    [Range(0, int.MaxValue)]
    public int? ConcurrencyVersion { get; set; }
}
