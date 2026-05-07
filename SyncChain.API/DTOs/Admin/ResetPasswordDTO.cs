namespace SyncChain.API.DTOs.Admin;

// Dữ liệu đặt lại mật khẩu tài khoản nội bộ.
public class ResetPasswordDTO
{
    public string Password { get; set; } = string.Empty;
}
