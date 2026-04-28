using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SyncChain.API.Data;
using SyncChain.API.Models;
using SyncChain.API.DTOs;

namespace SyncChain.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost("create-staff")]
    public IActionResult CreateStaff(RegisterDTO dto)
    {
        if (_db.NguoiDung.Any(x => x.Email == dto.Email))
            return BadRequest("Email đã tồn tại");

        var user = new NguoiDung
        {
            Email = dto.Email.Trim().ToLower(),
            TenDangNhap = dto.Email,
            MatKhauHash = BCrypt.Net.BCrypt.HashPassword(dto.Password.Trim()),
            MaVaiTro = 2
        };

        _db.NguoiDung.Add(user);
        _db.SaveChanges();

        return Ok("Tạo staff thành công");
    }
}