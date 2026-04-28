using SyncChain.API.Data;
using SyncChain.API.Models;
using SyncChain.API.DTOs;
using BCrypt.Net;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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

    // 🔐 REGISTER
    public string Register(RegisterDTO dto)
    {
        if (_db.NguoiDung.Any(x => x.Email == dto.Email))
            throw new Exception("Email đã tồn tại");

        if (dto.Password.Length < 6)
            throw new Exception("Mật khẩu phải >= 6 ký tự");

        string hash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        var role = _db.PhanQuyen.FirstOrDefault(x => x.TenVaiTro == "customer");
        if (role == null)
            throw new Exception("Chưa có role customer trong DB");

        var user = new NguoiDung
        {
            Email = dto.Email,
            TenDangNhap = dto.Email,
            MatKhauHash = hash,
            MaVaiTro = role.MaVaiTro
        };

        _db.NguoiDung.Add(user);
        _db.SaveChanges();

        return "Đăng ký thành công";
    }

    // 🔐 LOGIN
   public object Login(LoginDTO dto)
{
    if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
        throw new Exception("Thiếu email hoặc mật khẩu");

    var email = dto.Email.Trim().ToLower();
    var password = dto.Password.Trim();

    var user = _db.NguoiDung.FirstOrDefault(x => x.Email == email);

    if (user == null)
    {
        Console.WriteLine($"LOGIN FAIL: {dto.Email}");
        throw new Exception("Sai thông tin đăng nhập");
    }

    bool isValid = BCrypt.Net.BCrypt.Verify(password, user.MatKhauHash);

    if (!isValid)
    {
        Console.WriteLine($"LOGIN FAIL: {dto.Email}");
        throw new Exception("Sai thông tin đăng nhập");
    }

    if (!user.IsActive)
    {
        Console.WriteLine($"LOGIN BLOCKED: {dto.Email}");
        throw new Exception("Tài khoản bị khóa");
    }

    // 🔥 map role
    string roleName = user.MaVaiTro switch
    {
        1 => "customer",
        2 => "staff",
        3 => "manager",
        4 => "admin",
        _ => "unknown"
    };

    var jwtSettings = _config.GetSection("Jwt");

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
            user.Email,
            role = roleName
        }
    };
}
}