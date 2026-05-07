namespace SyncChain.API.DTOs;

// Dữ liệu client gửi khi đăng ký.
public class RegisterDTO
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}
