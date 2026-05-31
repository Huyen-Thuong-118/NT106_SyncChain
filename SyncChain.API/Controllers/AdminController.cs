using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SyncChain.API.DTOs.Admin;
using SyncChain.API.Data;
using SyncChain.API.DTOs;
using SyncChain.API.Models;

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

    // Lấy danh sách tài khoản nội bộ để quản trị.
    [HttpGet("users")]
    public IActionResult GetUsers()
    {
        var users = _db.NguoiDung
            .Join(_db.PhanQuyen,
                user => user.MaVaiTro,
                role => role.MaVaiTro,
                (user, role) => new
                {
                    user.MaNguoiDung,
                    user.TenDangNhap,
                    user.Email,
                    user.IsActive,
                    Role = role.TenVaiTro
                })
            .Where(x => x.Role == "manager" || x.Role == "staff")
            .OrderBy(x => x.Role)
            .ThenBy(x => x.Email)
            .ToList();

        return Ok(users);
    }

    // Tạo nhanh tài khoản quản lý.
    [HttpPost("create-manager")]
    public IActionResult CreateManager(RegisterDTO dto)
    {
        return CreateInternalUser(new CreateInternalUserDTO
        {
            Email = dto.Email,
            Password = dto.Password,
            Role = "manager"
        });
    }

    // Tạo nhanh tài khoản nhân viên.
    [HttpPost("create-staff")]
    public IActionResult CreateStaff(RegisterDTO dto)
    {
        return CreateInternalUser(new CreateInternalUserDTO
        {
            Email = dto.Email,
            Password = dto.Password,
            Role = "staff"
        });
    }

    // Tạo tài khoản nội bộ với role được chỉ định.
    [HttpPost("create-user")]
    public IActionResult CreateInternalUser(CreateInternalUserDTO dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var password = dto.Password.Trim();
        var roleName = dto.Role.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return BadRequest("Thieu email hoac mat khau");

        if (password.Length < 6)
            return BadRequest("Mat khau phai >= 6 ky tu");

        if (roleName is not ("manager" or "staff"))
            return BadRequest("Admin chi duoc tao tai khoan manager hoac staff bang endpoint nay");

        if (_db.NguoiDung.Any(x => x.Email == email))
            return BadRequest("Email da ton tai");

        var role = _db.PhanQuyen.FirstOrDefault(x => x.TenVaiTro == roleName);
        if (role == null)
            return BadRequest($"Chua co role {roleName} trong DB");

        var user = new NguoiDung
        {
            Email = email,
            TenDangNhap = string.IsNullOrWhiteSpace(dto.Username) ? email : dto.Username.Trim(),
            MatKhauHash = BCrypt.Net.BCrypt.HashPassword(password),
            MaVaiTro = role.MaVaiTro,
            IsActive = true
        };

        _db.NguoiDung.Add(user);
        _db.SaveChanges();

        return Ok(new
        {
            message = $"Tao {roleName} thanh cong",
            user.MaNguoiDung,
            user.Email,
            role = roleName
        });
    }

    // Cập nhật role và trạng thái hoạt động của tài khoản nội bộ.
    [HttpPut("users/{id}")]
    public IActionResult UpdateInternalUser(int id, UpdateInternalUserDTO dto)
    {
        var user = _db.NguoiDung.Find(id);
        if (user == null)
            return NotFound("Khong tim thay tai khoan");

        var roleName = dto.Role.Trim().ToLowerInvariant();
        if (roleName is not ("manager" or "staff"))
            return BadRequest("Chi duoc gan role manager hoac staff");

        var role = _db.PhanQuyen.FirstOrDefault(x => x.TenVaiTro == roleName);
        if (role == null)
            return BadRequest($"Chua co role {roleName} trong DB");

        user.MaVaiTro = role.MaVaiTro;
        user.IsActive = dto.IsActive;
        _db.SaveChanges();

        return Ok(new
        {
            user.MaNguoiDung,
            user.Email,
            Role = roleName,
            user.IsActive
        });
    }

    // Khóa hoặc mở khóa tài khoản manager/staff.
    [HttpPut("users/{id}/active")]
    public IActionResult SetActive(int id, bool isActive)
    {
        var user = _db.NguoiDung.Find(id);
        if (user == null)
            return NotFound("Khong tim thay tai khoan");

        var role = _db.PhanQuyen.FirstOrDefault(x => x.MaVaiTro == user.MaVaiTro);
        if (role?.TenVaiTro is not ("manager" or "staff"))
            return BadRequest("Chi duoc khoa/mo tai khoan manager hoac staff");

        user.IsActive = isActive;
        _db.SaveChanges();

        return Ok(new
        {
            user.MaNguoiDung,
            user.Email,
            user.IsActive
        });
    }

    // Đặt lại mật khẩu cho tài khoản nội bộ.
    [HttpPut("users/{id}/password")]
    public IActionResult ResetPassword(int id, ResetPasswordDTO dto)
    {
        var password = dto.Password.Trim();
        if (password.Length < 6)
            return BadRequest("Mat khau phai >= 6 ky tu");

        var user = _db.NguoiDung.Find(id);
        if (user == null)
            return NotFound("Khong tim thay tai khoan");

        var role = _db.PhanQuyen.FirstOrDefault(x => x.MaVaiTro == user.MaVaiTro);
        if (role?.TenVaiTro is not ("manager" or "staff"))
            return BadRequest("Chi duoc reset mat khau manager hoac staff");

        user.MatKhauHash = BCrypt.Net.BCrypt.HashPassword(password);
        _db.SaveChanges();

        return Ok("Da reset mat khau");
    }
}
