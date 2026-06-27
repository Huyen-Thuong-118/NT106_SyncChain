namespace SyncChain.API.DTOs.WarehouseIssue;

public class UpdateWarehouseIssueDTO
{
    public string LyDoXuat { get; set; } = string.Empty;
    public string GhiChu { get; set; } = string.Empty;
    public List<WarehouseIssueItemDTO> ChiTiet { get; set; } = new();
}
