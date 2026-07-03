namespace SyncChain.API.DTOs;

// Dữ liệu client gửi khi đăng ký.
public class RegisterDTO
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";

    // Thông tin cá nhân (tùy chọn) khi khách hàng tự đăng ký.
    public string Ho { get; set; } = "";
    public string Ten { get; set; } = "";
    public string TenDangNhap { get; set; } = "";
    public string SoDienThoai { get; set; } = "";
}
