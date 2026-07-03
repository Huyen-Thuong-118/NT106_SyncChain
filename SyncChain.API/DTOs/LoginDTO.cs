namespace SyncChain.API.DTOs;

public class LoginDTO
{
    // Chấp nhận email, tên đăng nhập hoặc số điện thoại
    public string Identifier { get; set; } = "";
    public string Password { get; set; } = "";
}
