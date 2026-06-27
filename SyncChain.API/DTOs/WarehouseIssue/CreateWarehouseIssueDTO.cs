namespace SyncChain.API.DTOs.WarehouseIssue;

public class CreateWarehouseIssueDTO
{
    public string LyDoXuat { get; set; } = string.Empty;
    public string GhiChu { get; set; } = string.Empty;
    public List<WarehouseIssueItemDTO> ChiTiet { get; set; } = new();
}
