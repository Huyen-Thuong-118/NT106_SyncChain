using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncChain.API.DTOs;
using SyncChain.API.Services;

namespace SyncChain.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;

    public AuthController(AuthService auth)
    {
        _auth = auth;
    }

    // Đăng ký tài khoản khách hàng mới.
    [HttpPost("register")]
    public IActionResult Register([FromBody] RegisterDTO dto)
    {
        try
        {
            var result = _auth.Register(dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // Đăng nhập và trả JWT cho ứng dụng client.
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginDTO dto)
    {
        try
        {
            var result = _auth.Login(dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    // Lấy thông tin hồ sơ từ user id trong token.
    [Authorize]
    [HttpGet("profile")]
    public IActionResult Profile()
    {
        try
        {
            return Ok(_auth.GetProfile(GetCurrentUserId()));
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // Cập nhật tên hiển thị của tài khoản hiện tại.
    [Authorize]
    [HttpPut("profile")]
    public IActionResult UpdateProfile([FromBody] UpdateProfileDTO dto)
    {
        try
        {
            return Ok(_auth.UpdateProfile(GetCurrentUserId(), dto));
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // Đổi mật khẩu sau khi xác thực mật khẩu cũ.
    [Authorize]
    [HttpPut("change-password")]
    public IActionResult ChangePassword([FromBody] ChangePasswordDTO dto)
    {
        try
        {
            _auth.ChangePassword(GetCurrentUserId(), dto);
            return Ok("Da doi mat khau");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // Đọc mã người dùng từ claim trong JWT.
    private int GetCurrentUserId()
    {
        var value = User.FindFirst("user_id")?.Value;
        if (!int.TryParse(value, out var userId))
            throw new Exception("Token khong hop le");

        return userId;
    }
}
