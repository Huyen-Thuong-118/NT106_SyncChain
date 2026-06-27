using System.ComponentModel.DataAnnotations;

namespace SyncChain.API.Models;

// Entity luu danh muc san pham.
public class DanhMucSanPham
{
    [Key]
    public int MaDanhMuc { get; set; }

    public string TenDanhMuc { get; set; } = string.Empty;

    public string MoTa { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public List<SanPham> SanPhams { get; set; } = new();
}
