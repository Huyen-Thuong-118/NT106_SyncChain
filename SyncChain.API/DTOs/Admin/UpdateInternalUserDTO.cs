namespace SyncChain.API.DTOs.Admin;

// Dữ liệu cập nhật role và trạng thái tài khoản nội bộ.
public class UpdateInternalUserDTO
{
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
