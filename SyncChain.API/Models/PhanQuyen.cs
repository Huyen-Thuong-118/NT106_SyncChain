using System.ComponentModel.DataAnnotations;

namespace SyncChain.API.Models;

// Entity lưu vai trò phân quyền.
public class PhanQuyen
{
    [Key]
    public int MaVaiTro { get; set; }
    public string TenVaiTro { get; set; } = string.Empty;  
}
