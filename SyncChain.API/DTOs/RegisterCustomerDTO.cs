namespace SyncChain.API.DTOs;

public class RegisterCustomerDTO
{
    public string Ho { get; set; } = "";
    public string Ten { get; set; } = "";
    public string TenDangNhap { get; set; } = "";
    public string SoDienThoai { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string ConfirmPassword { get; set; } = "";
}
