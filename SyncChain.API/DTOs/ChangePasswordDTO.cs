namespace SyncChain.API.DTOs;

// Dữ liệu đổi mật khẩu.
public class ChangePasswordDTO
{
    public string CurrentPassword { get; set; } = "";
    public string NewPassword { get; set; } = "";
}
