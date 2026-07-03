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
    private readonly IAuditService _audit;

    public AuthService(AppDbContext db, IConfiguration config, IAuditService audit)
    {
        _db = db;
        _config = config;
        _audit = audit;
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

        // Ưu tiên tên đăng nhập client gửi; nếu trống thì lấy phần trước @ của email.
        var tenDangNhap = string.IsNullOrWhiteSpace(dto.TenDangNhap)
            ? email.Split('@')[0]
            : dto.TenDangNhap.Trim();

        var user = new NguoiDung
        {
            Email = email,
            TenDangNhap = tenDangNhap,
            MatKhauHash = BCrypt.Net.BCrypt.HashPassword(password),
            MaVaiTro = role.MaVaiTro,
            IsActive = true,
            Ho = dto.Ho?.Trim() ?? "",
            Ten = dto.Ten?.Trim() ?? "",
            SoDienThoai = dto.SoDienThoai?.Trim() ?? ""
        };

        using var transaction = _db.Database.BeginTransaction();
        _db.NguoiDung.Add(user);
        _db.SaveChanges();
        _audit.AddSuccess(
            AuditActions.Create,
            "NguoiDung",
            user.MaNguoiDung.ToString(),
            after: new { user.TenDangNhap, user.Email, role = "customer", user.IsActive },
            actor: new AuditActor(user.MaNguoiDung, user.TenDangNhap, "customer"));
        _db.SaveChanges();
        transaction.Commit();

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
            _audit.AddFailureAsync(
                    AuditActions.LoginFailed,
                    "Authentication",
                    email,
                    metadata: new { email, reason = "invalid_credentials" },
                    actor: new AuditActor(null, email, string.Empty))
                .GetAwaiter().GetResult();
            throw new Exception("Sai thong tin dang nhap");
        }

        if (!user.IsActive)
        {
            _audit.AddFailureAsync(
                    AuditActions.LoginFailed,
                    "Authentication",
                    user.MaNguoiDung.ToString(),
                    metadata: new { email, reason = "account_locked" },
                    actor: new AuditActor(user.MaNguoiDung, user.TenDangNhap, GetRoleName(user.MaVaiTro)))
                .GetAwaiter().GetResult();
            throw new Exception("Tai khoan bi khoa");
        }

        var roleName = ResolveRoleName(user.MaVaiTro);

        var jwtSettings = _config.GetSection("Jwt");

        // Gắn user id và role vào token để các API khác phân quyền.
        var claims = new[]
        {
            new Claim("user_id", user.MaNguoiDung.ToString()),
            new Claim(ClaimTypes.Name, user.TenDangNhap),
            new Claim(ClaimTypes.Email, user.Email),
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

        _audit.AddSuccess(
            AuditActions.Login,
            "Authentication",
            user.MaNguoiDung.ToString(),
            after: new { authenticated = true },
            metadata: new
            {
                device = dto.Device?.Trim() ?? string.Empty,
                location = dto.Location?.Trim() ?? string.Empty
            },
            actor: new AuditActor(user.MaNguoiDung, user.TenDangNhap, roleName));
        _db.SaveChanges();

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

        var roleName = ResolveRoleName(user.MaVaiTro);

        return new
        {
            user.MaNguoiDung,
            user.TenDangNhap,
            user.Email,
            user.Ho,
            user.Ten,
            user.SoDienThoai,
            role = roleName,
            user.IsActive
        };
    }

    // Cập nhật tên hiển thị rồi trả lại hồ sơ mới.
    public object UpdateProfile(int userId, UpdateProfileDTO dto)
    {
        var user = _db.NguoiDung.FirstOrDefault(x => x.MaNguoiDung == userId)
            ?? throw new Exception("Khong tim thay tai khoan");

        var username = dto.Username?.Trim() ?? "";
        var oldUsername = user.TenDangNhap;
        if (!string.IsNullOrWhiteSpace(username))
            user.TenDangNhap = username;

        // Cập nhật thông tin cá nhân khách hàng nếu có gửi lên.
        if (!string.IsNullOrWhiteSpace(dto.Ho)) user.Ho = dto.Ho.Trim();
        if (!string.IsNullOrWhiteSpace(dto.Ten)) user.Ten = dto.Ten.Trim();
        if (!string.IsNullOrWhiteSpace(dto.SoDienThoai)) user.SoDienThoai = dto.SoDienThoai.Trim();

        _audit.AddSuccess(
            AuditActions.Update,
            "NguoiDung",
            userId.ToString(),
            before: new { username = oldUsername },
            after: new { username = user.TenDangNhap, user.Ho, user.Ten, user.SoDienThoai });
        _db.SaveChanges();

        return GetProfile(userId);
    }

    // Lấy tên role từ PhanQuyen theo MaVaiTro — tránh hardcode số nguyên.
    private string ResolveRoleName(int maVaiTro)
    {
        return _db.PhanQuyen.FirstOrDefault(x => x.MaVaiTro == maVaiTro)?.TenVaiTro ?? "unknown";
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
        _audit.AddSuccess(
            AuditActions.PasswordChange,
            "NguoiDung",
            userId.ToString(),
            after: new { passwordChanged = true });
        _db.SaveChanges();
    }

    private static string GetRoleName(int roleId) => roleId switch
    {
        1 => "customer",
        2 => "staff",
        3 => "manager",
        4 => "admin",
        _ => "unknown"
    };
}
