using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
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

    // 🔐 REGISTER (chỉ customer)
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

    // 🔐 LOGIN (trả JWT)
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

    // 🔐 PROFILE (cần token)
    [Authorize]
    [HttpGet("profile")]
    public IActionResult Profile()
    {
        var userId = User.FindFirst("user_id")?.Value;

        // nếu bạn đã chuyển sang ClaimTypes.Role
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        return Ok(new
        {
            message = "Đã đăng nhập",
            userId,
            role
        });
    }
}