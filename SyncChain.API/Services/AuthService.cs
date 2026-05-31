using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SyncChain.API.Data;
using SyncChain.API.DTOs;
using SyncChain.API.Models;

namespace SyncChain.API.Services;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    // Tạo tài khoản customer sau khi kiểm tra email và mật khẩu.
    public string Register(RegisterDTO dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var password = dto.Password.Trim();

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            throw new Exception("Thieu email hoac mat khau");

        if (_db.NguoiDung.Any(x => x.Email == email))
            throw new Exception("Email da ton tai");

        if (password.Length < 6)
            throw new Exception("Mat khau phai >= 6 ky tu");

        var role = _db.PhanQuyen.FirstOrDefault(x => x.TenVaiTro == "customer");
        if (role == null)
            throw new Exception("Chua co role customer trong DB");

        var user = new NguoiDung
        {
            Email = email,
            TenDangNhap = email,
            MatKhauHash = BCrypt.Net.BCrypt.HashPassword(password),
            MaVaiTro = role.MaVaiTro,
            IsActive = true
        };

        _db.NguoiDung.Add(user);
        _db.SaveChanges();

        return "Dang ky thanh cong";
    }

    // Xác thực đăng nhập, kiểm tra trạng thái và sinh JWT.
    public object Login(LoginDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            throw new Exception("Thieu email hoac mat khau");

        var email = dto.Email.Trim().ToLowerInvariant();
        var password = dto.Password.Trim();

        var user = _db.NguoiDung.FirstOrDefault(x => x.Email == email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.MatKhauHash))
        {
            Console.WriteLine($"LOGIN FAIL: {dto.Email}");
            throw new Exception("Sai thong tin dang nhap");
        }

        if (!user.IsActive)
        {
            Console.WriteLine($"LOGIN BLOCKED: {dto.Email}");
            throw new Exception("Tai khoan bi khoa");
        }

        // Chuyển mã vai trò trong DB sang tên role dùng trong policy.
        var roleName = user.MaVaiTro switch
        {
            1 => "customer",
            2 => "staff",
            3 => "manager",
            4 => "admin",
            _ => "unknown"
        };

        var jwtSettings = _config.GetSection("Jwt");

        // Gắn user id và role vào token để các API khác phân quyền.
        var claims = new[]
        {
            new Claim("user_id", user.MaNguoiDung.ToString()),
            new Claim(ClaimTypes.Role, roleName)
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings["Key"]!)
        );

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.Now.AddHours(2),
            signingCredentials: creds
        );

        Console.WriteLine($"LOGIN SUCCESS: {user.Email}");

        return new
        {
            token = new JwtSecurityTokenHandler().WriteToken(token),
            user = new
            {
                user.MaNguoiDung,
                user.TenDangNhap,
                user.Email,
                role = roleName
            }
        };
    }

    // Trả thông tin hồ sơ theo user id trong token.
    public object GetProfile(int userId)
    {
        var user = _db.NguoiDung.FirstOrDefault(x => x.MaNguoiDung == userId)
            ?? throw new Exception("Khong tim thay tai khoan");

        var roleName = user.MaVaiTro switch
        {
            1 => "customer",
            2 => "staff",
            3 => "manager",
            4 => "admin",
            _ => "unknown"
        };

        return new
        {
            user.MaNguoiDung,
            user.TenDangNhap,
            user.Email,
            role = roleName,
            user.IsActive
        };
    }

    // Cập nhật tên hiển thị rồi trả lại hồ sơ mới.
    public object UpdateProfile(int userId, UpdateProfileDTO dto)
    {
        var username = dto.Username.Trim();
        if (string.IsNullOrWhiteSpace(username))
            throw new Exception("Ten hien thi khong duoc de trong");

        var user = _db.NguoiDung.FirstOrDefault(x => x.MaNguoiDung == userId)
            ?? throw new Exception("Khong tim thay tai khoan");

        user.TenDangNhap = username;
        _db.SaveChanges();

        return GetProfile(userId);
    }

    // Đổi mật khẩu sau khi kiểm tra mật khẩu hiện tại.
    public void ChangePassword(int userId, ChangePasswordDTO dto)
    {
        var currentPassword = dto.CurrentPassword.Trim();
        var newPassword = dto.NewPassword.Trim();

        if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
            throw new Exception("Vui long nhap du mat khau hien tai va mat khau moi");

        if (newPassword.Length < 6)
            throw new Exception("Mat khau moi phai >= 6 ky tu");

        var user = _db.NguoiDung.FirstOrDefault(x => x.MaNguoiDung == userId)
            ?? throw new Exception("Khong tim thay tai khoan");

        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.MatKhauHash))
            throw new Exception("Mat khau hien tai khong dung");

        user.MatKhauHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        _db.SaveChanges();
    }
}
