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

    public AuthController(AuthService auth) => _auth = auth;

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterCustomerDTO dto)
    {
        try { return Ok(await _auth.RegisterCustomerAsync(dto)); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginDTO dto)
    {
        try { return Ok(_auth.Login(dto)); }
        catch (Exception ex) { return Unauthorized(new { message = ex.Message }); }
    }

    [Authorize]
    [HttpGet("profile")]
    public IActionResult Profile()
    {
        try { return Ok(_auth.GetProfile(GetUserId())); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [Authorize]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateCustomerProfileDTO dto)
    {
        try { return Ok(await _auth.UpdateCustomerProfileAsync(GetUserId(), dto)); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [Authorize]
    [HttpPut("change-password")]
    public IActionResult ChangePassword([FromBody] ChangePasswordDTO dto)
    {
        try
        {
            _auth.ChangePassword(GetUserId(), dto);
            return Ok(new { message = "Đổi mật khẩu thành công" });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("forgot-password")]
    public IActionResult ForgotPassword([FromBody] ForgotPasswordDTO dto)
    {
        try
        {
            _auth.ForgotPassword(dto);
            return Ok(new { message = "OTP đã được gửi đến email của bạn" });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("reset-password")]
    public IActionResult ResetPassword([FromBody] ResetPasswordOtpDTO dto)
    {
        try
        {
            _auth.ResetPasswordWithOtp(dto);
            return Ok(new { message = "Đặt lại mật khẩu thành công" });
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    private int GetUserId()
    {
        var value = User.FindFirst("user_id")?.Value;
        if (!int.TryParse(value, out var id)) throw new Exception("Token không hợp lệ");
        return id;
    }
}
