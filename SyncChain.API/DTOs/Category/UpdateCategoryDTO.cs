namespace SyncChain.API.DTOs.Category;

public class UpdateCategoryDTO
{
    public string TenDanhMuc { get; set; } = string.Empty;
    public string MoTa { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
