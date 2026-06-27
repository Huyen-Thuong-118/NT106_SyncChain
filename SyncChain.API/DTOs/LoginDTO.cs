namespace SyncChain.API.DTOs;

// Dữ liệu client gửi khi đăng nhập.
public class LoginDTO
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string Device { get; set; } = "";
    public string Location { get; set; } = "";
}
