namespace SyncChain.API.DTOs.Admin;

// Du lieu cap nhat tai khoan noi bo.
public class UpdateInternalUserDTO
{
    public string Email { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
