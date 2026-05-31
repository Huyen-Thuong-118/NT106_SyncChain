namespace SyncChain.API.DTOs;

// Dữ liệu admin dùng để tạo tài khoản nội bộ.
public class CreateInternalUserDTO
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string Role { get; set; } = string.Empty;
}
