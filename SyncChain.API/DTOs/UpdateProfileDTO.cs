namespace SyncChain.API.DTOs;

// Dữ liệu cập nhật hồ sơ cá nhân.
public class UpdateProfileDTO
{
    public string Username { get; set; } = "";

    // Thông tin cá nhân khách hàng (tùy chọn).
    public string Ho { get; set; } = "";
    public string Ten { get; set; } = "";
    public string SoDienThoai { get; set; } = "";
}
