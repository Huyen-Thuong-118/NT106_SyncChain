using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using SyncChain.API.Data;
using SyncChain.API.DTOs;
using SyncChain.API.DTOs.Admin;
using SyncChain.API.Models;
using SyncChain.API.Services;

namespace SyncChain.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = "AdminOnly")]
public class AdminController : ControllerBase
{
    private static readonly string[] InternalRoles = { "admin", "manager", "staff" };
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;

    public AdminController(AppDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    // Lay danh sach tai khoan noi bo de quan tri.
    [HttpGet("users")]
    public IActionResult GetUsers()
    {
        var users = GetInternalUserQuery()
            .OrderBy(x => x.Role)
            .ThenBy(x => x.Email)
            .ToList();

        return Ok(users);
    }

    // Lay chi tiet mot tai khoan noi bo.
    [HttpGet("users/{id}")]
    public IActionResult GetUserById(int id)
    {
        var user = GetInternalUserQuery()
            .FirstOrDefault(x => x.MaNguoiDung == id);

        if (user == null)
            return NotFound("Khong tim thay tai khoan noi bo");

        return Ok(user);
    }

    // Lịch sử đăng nhập thành công của một tài khoản nội bộ.
    [HttpGet("users/{id}/login-history")]
    public async Task<IActionResult> GetLoginHistory(int id)
    {
        var user = GetInternalUserQuery().FirstOrDefault(x => x.MaNguoiDung == id);
        if (user == null)
            return NotFound("Khong tim thay tai khoan noi bo");

        var logs = await _db.AuditLog.AsNoTracking()
            .Where(x => x.MaNguoiDung == id &&
                        x.HanhDong == AuditActions.Login &&
                        x.TrangThaiKetQua == AuditResultStatuses.Success)
            .OrderByDescending(x => x.ThoiGian)
            .Take(100)
            .Select(x => new
            {
                x.ThoiGian,
                x.IpAddress,
                x.UserAgent,
                x.Metadata
            })
            .ToListAsync();

        return Ok(logs.Select(x =>
        {
            var metadata = ReadLoginMetadata(x.Metadata);
            return new
            {
                Timestamp = x.ThoiGian,
                Device = string.IsNullOrWhiteSpace(metadata.Device)
                    ? DescribeDevice(x.UserAgent)
                    : metadata.Device,
                Location = string.IsNullOrWhiteSpace(metadata.Location)
                    ? DescribeLocation(x.IpAddress)
                    : metadata.Location,
                x.IpAddress
            };
        }));
    }

    // Tao nhanh tai khoan quan ly, giu tuong thich voi client cu.
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

    // Tao nhanh tai khoan nhan vien, giu tuong thich voi client cu.
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

    // Tao tai khoan noi bo voi role admin, manager hoac staff.
    [HttpPost("users")]
    [HttpPost("create-user")]
    public IActionResult CreateInternalUser(CreateInternalUserDTO dto)
    {
        var email = Normalize(dto.Email);
        var password = dto.Password?.Trim() ?? string.Empty;
        var roleName = Normalize(dto.Role);

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return BadRequest("Thieu email hoac mat khau");

        if (password.Length < 6)
            return BadRequest("Mat khau phai >= 6 ky tu");

        if (!IsInternalRole(roleName))
            return BadRequest("Role khong hop le. Chi cho phep admin, manager hoac staff");

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

        using var transaction = _db.Database.BeginTransaction();
        _db.NguoiDung.Add(user);
        _db.SaveChanges();
        _audit.AddSuccess(
            AuditActions.Create,
            "NguoiDung",
            user.MaNguoiDung.ToString(),
            after: new
            {
                user.TenDangNhap, user.Email, Role = roleName, user.IsActive
            });
        _db.SaveChanges();
        transaction.Commit();

        return Ok(new
        {
            message = $"Tao {roleName} thanh cong",
            user.MaNguoiDung,
            user.TenDangNhap,
            user.Email,
            Role = roleName,
            user.IsActive
        });
    }

    // Cap nhat thong tin, role va trang thai tai khoan noi bo.
    [HttpPut("users/{id}")]
    public IActionResult UpdateInternalUser(int id, UpdateInternalUserDTO dto)
    {
        var user = _db.NguoiDung.Find(id);
        if (user == null)
            return NotFound("Khong tim thay tai khoan noi bo");

        var currentRole = GetRoleName(user.MaVaiTro);
        if (!IsInternalRole(currentRole))
            return BadRequest("Chi duoc quan ly tai khoan noi bo admin, manager hoac staff");

        var email = Normalize(dto.Email);
        var roleName = Normalize(dto.Role);

        if (string.IsNullOrWhiteSpace(email))
            return BadRequest("Thieu email");

        if (!IsInternalRole(roleName))
            return BadRequest("Role khong hop le. Chi cho phep admin, manager hoac staff");

        if (_db.NguoiDung.Any(x => x.Email == email && x.MaNguoiDung != id))
            return BadRequest("Email da ton tai");

        if (id == GetCurrentUserId() && user.IsActive && !dto.IsActive)
            return BadRequest("Khong duoc khoa chinh tai khoan admin dang dang nhap");

        var role = _db.PhanQuyen.FirstOrDefault(x => x.TenVaiTro == roleName);
        if (role == null)
            return BadRequest($"Chua co role {roleName} trong DB");

        var before = new
        {
            user.TenDangNhap, user.Email, Role = currentRole, user.IsActive
        };
        user.Email = email;
        user.TenDangNhap = string.IsNullOrWhiteSpace(dto.Username) ? email : dto.Username.Trim();
        user.MaVaiTro = role.MaVaiTro;
        user.IsActive = dto.IsActive;
        _audit.AddSuccess(
            currentRole == roleName ? AuditActions.Update : AuditActions.RoleChange,
            "NguoiDung",
            id.ToString(),
            before,
            new { user.TenDangNhap, user.Email, Role = roleName, user.IsActive });
        _db.SaveChanges();

        return Ok(new
        {
            user.MaNguoiDung,
            user.TenDangNhap,
            user.Email,
            Role = roleName,
            user.IsActive
        });
    }

    // Khoa hoac mo khoa tai khoan noi bo.
    [HttpPut("users/{id}/active")]
    public IActionResult SetActive(int id, SetActiveDTO dto)
    {
        var user = _db.NguoiDung.Find(id);
        if (user == null)
            return NotFound("Khong tim thay tai khoan noi bo");

        var roleName = GetRoleName(user.MaVaiTro);
        if (!IsInternalRole(roleName))
            return BadRequest("Chi duoc khoa/mo tai khoan noi bo admin, manager hoac staff");

        if (id == GetCurrentUserId() && !dto.IsActive)
            return BadRequest("Khong duoc khoa chinh tai khoan admin dang dang nhap");

        var wasActive = user.IsActive;
        user.IsActive = dto.IsActive;
        _audit.AddSuccess(
            AuditActions.StatusChange,
            "NguoiDung",
            id.ToString(),
            before: new { isActive = wasActive },
            after: new { user.IsActive });
        _db.SaveChanges();

        return Ok(new
        {
            user.MaNguoiDung,
            user.Email,
            Role = roleName,
            user.IsActive
        });
    }

    // Dat lai mat khau cho tai khoan noi bo.
    [HttpPut("users/{id}/password")]
    public IActionResult ResetPassword(int id, ResetPasswordDTO dto)
    {
        var password = dto.Password?.Trim() ?? string.Empty;
        if (password.Length < 6)
            return BadRequest("Mat khau phai >= 6 ky tu");

        var user = _db.NguoiDung.Find(id);
        if (user == null)
            return NotFound("Khong tim thay tai khoan noi bo");

        var roleName = GetRoleName(user.MaVaiTro);
        if (!IsInternalRole(roleName))
            return BadRequest("Chi duoc reset mat khau tai khoan noi bo admin, manager hoac staff");

        user.MatKhauHash = BCrypt.Net.BCrypt.HashPassword(password);
        _audit.AddSuccess(
            AuditActions.PasswordChange,
            "NguoiDung",
            id.ToString(),
            after: new { passwordChanged = true });
        _db.SaveChanges();

        return Ok("Da reset mat khau");
    }

    // Vo hieu hoa tai khoan noi bo de giu lai du lieu lien quan.
    [HttpDelete("users/{id}")]
    public IActionResult DeleteInternalUser(int id)
    {
        var user = _db.NguoiDung.Find(id);
        if (user == null)
            return NotFound("Khong tim thay tai khoan noi bo");

        var roleName = GetRoleName(user.MaVaiTro);
        if (!IsInternalRole(roleName))
            return BadRequest("Chi duoc vo hieu hoa tai khoan noi bo admin, manager hoac staff");

        if (id == GetCurrentUserId())
            return BadRequest("Khong duoc xoa hoac vo hieu hoa chinh tai khoan admin dang dang nhap");

        var wasActive = user.IsActive;
        user.IsActive = false;
        _audit.AddSuccess(
            AuditActions.Delete,
            "NguoiDung",
            id.ToString(),
            before: new { isActive = wasActive },
            after: new { isActive = false },
            metadata: new { softDelete = true });
        _db.SaveChanges();

        return Ok(new
        {
            message = "Da vo hieu hoa tai khoan",
            user.MaNguoiDung,
            user.Email,
            Role = roleName,
            user.IsActive
        });
    }

    private IQueryable<InternalUserResponse> GetInternalUserQuery()
    {
        return _db.NguoiDung
            .Join(_db.PhanQuyen,
                user => user.MaVaiTro,
                role => role.MaVaiTro,
                (user, role) => new InternalUserResponse
                {
                    MaNguoiDung = user.MaNguoiDung,
                    TenDangNhap = user.TenDangNhap,
                    Email = user.Email,
                    IsActive = user.IsActive,
                    Role = role.TenVaiTro
                })
            .Where(x => InternalRoles.Contains(x.Role));
    }

    private string GetRoleName(int roleId)
    {
        return _db.PhanQuyen
            .Where(x => x.MaVaiTro == roleId)
            .Select(x => x.TenVaiTro)
            .FirstOrDefault() ?? string.Empty;
    }

    private int GetCurrentUserId()
    {
        var value = User.FindFirst("user_id")?.Value;
        return int.TryParse(value, out var userId) ? userId : 0;
    }

    private static bool IsInternalRole(string roleName)
    {
        return InternalRoles.Contains(roleName);
    }

    private static string Normalize(string? value)
    {
        return value?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    private static (string Device, string Location) ReadLoginMetadata(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            var device = root.TryGetProperty("device", out var deviceValue)
                ? deviceValue.GetString() ?? string.Empty
                : string.Empty;
            var location = root.TryGetProperty("location", out var locationValue)
                ? locationValue.GetString() ?? string.Empty
                : string.Empty;
            return (device, location);
        }
        catch
        {
            return (string.Empty, string.Empty);
        }
    }

    private static string DescribeDevice(string userAgent) =>
        string.IsNullOrWhiteSpace(userAgent) ? "Không xác định" : userAgent;

    private static string DescribeLocation(string ipAddress) =>
        ipAddress is "::1" or "127.0.0.1"
            ? "Máy cục bộ"
            : string.IsNullOrWhiteSpace(ipAddress)
                ? "Không xác định"
                : $"IP {ipAddress}";

    private sealed class InternalUserResponse
    {
        public int MaNguoiDung { get; set; }
        public string TenDangNhap { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string Role { get; set; } = string.Empty;
    }
}
