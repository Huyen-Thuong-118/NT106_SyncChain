using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.IdentityModel.Tokens;
using SyncChain.API.Data;
using SyncChain.API.DTOs;
using SyncChain.API.Models;

namespace SyncChain.API.Services;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly EmailService _email;

    public AuthService(AppDbContext db, IConfiguration config, EmailService email)
    {
        _db = db;
        _config = config;
        _email = email;
    }

    // ─── Đăng ký khách hàng ──────────────────────────────────────
    public async Task<object> RegisterCustomerAsync(RegisterCustomerDTO dto)
    {
        var ho = dto.Ho.Trim();
        var ten = dto.Ten.Trim();
        var username = dto.TenDangNhap.Trim().ToLowerInvariant();
        var phone = dto.SoDienThoai.Trim();
        var email = dto.Email.Trim().ToLowerInvariant();
        var password = dto.Password;
        var confirm = dto.ConfirmPassword;

        if (string.IsNullOrWhiteSpace(ho))   throw new Exception("Vui lòng nhập họ");
        if (string.IsNullOrWhiteSpace(ten))  throw new Exception("Vui lòng nhập tên");
        if (string.IsNullOrWhiteSpace(username)) throw new Exception("Vui lòng nhập tên đăng nhập");
        if (string.IsNullOrWhiteSpace(phone)) throw new Exception("Vui lòng nhập số điện thoại");
        if (string.IsNullOrWhiteSpace(email)) throw new Exception("Vui lòng nhập email");
        if (string.IsNullOrWhiteSpace(password)) throw new Exception("Vui lòng nhập mật khẩu");

        if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            throw new Exception("Email không đúng định dạng");

        if (password.Length < 8)
            throw new Exception("Mật khẩu phải ít nhất 8 ký tự");

        if (password != confirm)
            throw new Exception("Mật khẩu xác nhận không khớp");

        if (!Regex.IsMatch(phone, @"^\d{9,11}$"))
            throw new Exception("Số điện thoại không hợp lệ (9-11 chữ số)");

        if (_db.NguoiDung.Any(x => x.Email == email))
            throw new Exception("Email đã được sử dụng");

        if (_db.NguoiDung.Any(x => x.TenDangNhap == username))
            throw new Exception("Tên đăng nhập đã tồn tại");

        if (_db.NguoiDung.Any(x => x.SoDienThoai == phone))
            throw new Exception("Số điện thoại đã được đăng ký");

        var role = _db.PhanQuyen.FirstOrDefault(x => x.TenVaiTro == "customer")
            ?? throw new Exception("Hệ thống chưa cấu hình role customer");

        var user = new NguoiDung
        {
            Ho = ho,
            Ten = ten,
            TenDangNhap = username,
            SoDienThoai = phone,
            Email = email,
            MatKhauHash = BCrypt.Net.BCrypt.HashPassword(password),
            MaVaiTro = role.MaVaiTro,
            IsActive = true,
            NgayTao = DateTime.UtcNow,
            NgayCapNhat = DateTime.UtcNow
        };

        _db.NguoiDung.Add(user);
        await _db.SaveChangesAsync();

        return new { message = "Đăng ký thành công", user.MaNguoiDung, user.Email };
    }

    // ─── Đăng ký nội bộ (dùng bởi AdminController) ───────────────
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

        var role = _db.PhanQuyen.FirstOrDefault(x => x.TenVaiTro == "customer")
            ?? throw new Exception("Chua co role customer trong DB");

        _db.NguoiDung.Add(new NguoiDung
        {
            Email = email,
            TenDangNhap = email,
            MatKhauHash = BCrypt.Net.BCrypt.HashPassword(password),
            MaVaiTro = role.MaVaiTro,
            IsActive = true
        });
        _db.SaveChanges();
        return "Dang ky thanh cong";
    }

    // ─── Đăng nhập (email / username / phone) ─────────────────────
    public object Login(LoginDTO dto)
    {
        var identifier = dto.Identifier.Trim();
        var password = dto.Password;

        if (string.IsNullOrWhiteSpace(identifier))
            throw new Exception("Vui lòng nhập tên đăng nhập, email hoặc số điện thoại");

        if (string.IsNullOrWhiteSpace(password))
            throw new Exception("Vui lòng nhập mật khẩu");

        var lower = identifier.ToLowerInvariant();
        var user = _db.NguoiDung.FirstOrDefault(x =>
            x.Email == lower ||
            x.TenDangNhap.ToLower() == lower ||   // case-insensitive username
            x.SoDienThoai == identifier);

        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.MatKhauHash))
            throw new Exception("Thông tin đăng nhập không đúng");

        if (!user.IsActive)
            throw new Exception("Tài khoản đã bị khóa");

        var roleName = ResolveRoleName(user.MaVaiTro);
        var token = BuildJwt(user.MaNguoiDung, roleName);

        return new
        {
            token,
            user = new
            {
                user.MaNguoiDung,
                user.TenDangNhap,
                user.Email,
                HoTen = BuildHoTen(user.Ho, user.Ten, user.TenDangNhap),
                role = roleName
            }
        };
    }

    // ─── Hồ sơ ────────────────────────────────────────────────────
    public object GetProfile(int userId)
    {
        var user = _db.NguoiDung.FirstOrDefault(x => x.MaNguoiDung == userId)
            ?? throw new Exception("Không tìm thấy tài khoản");

        return new
        {
            user.MaNguoiDung,
            user.Ho,
            user.Ten,
            HoTen = BuildHoTen(user.Ho, user.Ten, user.TenDangNhap),
            user.TenDangNhap,
            user.Email,
            user.SoDienThoai,
            role = ResolveRoleName(user.MaVaiTro),
            user.IsActive,
            user.NgayTao
        };
    }

    public async Task<object> UpdateCustomerProfileAsync(int userId, UpdateCustomerProfileDTO dto)
    {
        var user = _db.NguoiDung.FirstOrDefault(x => x.MaNguoiDung == userId)
            ?? throw new Exception("Không tìm thấy tài khoản");

        var ho       = dto.Ho.Trim();
        var ten      = dto.Ten.Trim();
        var username = dto.TenDangNhap.Trim().ToLowerInvariant();
        var phone    = dto.SoDienThoai.Trim();
        var email    = dto.Email.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(ho))       throw new Exception("Vui lòng nhập họ");
        if (string.IsNullOrWhiteSpace(ten))      throw new Exception("Vui lòng nhập tên");
        if (string.IsNullOrWhiteSpace(username)) throw new Exception("Vui lòng nhập tên đăng nhập");

        if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            throw new Exception("Email không đúng định dạng");

        if (!string.IsNullOrWhiteSpace(phone) && !Regex.IsMatch(phone, @"^\d{9,11}$"))
            throw new Exception("Số điện thoại không hợp lệ");

        if (_db.NguoiDung.Any(x => x.Email == email && x.MaNguoiDung != userId))
            throw new Exception("Email đã được sử dụng");

        if (_db.NguoiDung.Any(x => x.TenDangNhap == username && x.MaNguoiDung != userId))
            throw new Exception("Tên đăng nhập đã tồn tại");

        if (!string.IsNullOrWhiteSpace(phone) &&
            _db.NguoiDung.Any(x => x.SoDienThoai == phone && x.MaNguoiDung != userId))
            throw new Exception("Số điện thoại đã được đăng ký");

        user.Ho          = ho;
        user.Ten         = ten;
        user.TenDangNhap = username;
        user.Email       = email;
        user.SoDienThoai = string.IsNullOrWhiteSpace(phone) ? user.SoDienThoai : phone;
        user.NgayCapNhat = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return GetProfile(userId);
    }

    // ─── Đổi mật khẩu ────────────────────────────────────────────
    public void ChangePassword(int userId, ChangePasswordDTO dto)
    {
        var current = dto.CurrentPassword.Trim();
        var next = dto.NewPassword.Trim();

        if (string.IsNullOrWhiteSpace(current) || string.IsNullOrWhiteSpace(next))
            throw new Exception("Vui lòng nhập đủ mật khẩu hiện tại và mới");

        if (next.Length < 8)
            throw new Exception("Mật khẩu mới phải ít nhất 8 ký tự");

        var user = _db.NguoiDung.FirstOrDefault(x => x.MaNguoiDung == userId)
            ?? throw new Exception("Không tìm thấy tài khoản");

        if (!BCrypt.Net.BCrypt.Verify(current, user.MatKhauHash))
            throw new Exception("Mật khẩu hiện tại không đúng");

        user.MatKhauHash = BCrypt.Net.BCrypt.HashPassword(next);
        user.NgayCapNhat = DateTime.UtcNow;
        _db.SaveChanges();
    }

    // ─── Quên mật khẩu — yêu cầu OTP ────────────────────────────
    public void ForgotPassword(ForgotPasswordDTO dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            throw new Exception("Email không đúng định dạng");

        var user = _db.NguoiDung.FirstOrDefault(x => x.Email == email)
            ?? throw new Exception("Email không tồn tại trong hệ thống");

        // Giới hạn: không gửi lại nếu OTP cũ còn hiệu lực > 3 phút
        var recent = _db.OtpReset
            .Where(x => x.MaNguoiDung == user.MaNguoiDung && !x.DaDung && x.HetHan > DateTime.UtcNow.AddMinutes(3))
            .Any();

        if (recent)
            throw new Exception("Vui lòng chờ ít nhất 2 phút trước khi yêu cầu OTP mới");

        var otp = GenerateOtp();
        var hash = BCrypt.Net.BCrypt.HashPassword(otp);

        _db.OtpReset.Add(new OtpReset
        {
            MaNguoiDung = user.MaNguoiDung,
            MaOtpHash = hash,
            HetHan = DateTime.UtcNow.AddMinutes(5),
            DaDung = false
        });
        _db.SaveChanges();

        _email.SendOtp(email, otp);
    }

    // ─── Đặt lại mật khẩu bằng OTP ───────────────────────────────
    public void ResetPasswordWithOtp(ResetPasswordOtpDTO dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var otp = dto.Otp.Trim();
        var newPwd = dto.NewPassword;
        var confirm = dto.ConfirmPassword;

        if (string.IsNullOrWhiteSpace(otp)) throw new Exception("Vui lòng nhập OTP");
        if (newPwd.Length < 8) throw new Exception("Mật khẩu mới phải ít nhất 8 ký tự");
        if (newPwd != confirm) throw new Exception("Mật khẩu xác nhận không khớp");

        var user = _db.NguoiDung.FirstOrDefault(x => x.Email == email)
            ?? throw new Exception("Email không tồn tại");

        var otpRecord = _db.OtpReset
            .Where(x => x.MaNguoiDung == user.MaNguoiDung && !x.DaDung && x.HetHan > DateTime.UtcNow)
            .OrderByDescending(x => x.NgayTao)
            .FirstOrDefault()
            ?? throw new Exception("OTP không hợp lệ hoặc đã hết hạn");

        if (!BCrypt.Net.BCrypt.Verify(otp, otpRecord.MaOtpHash))
            throw new Exception("OTP không đúng");

        otpRecord.DaDung = true;
        user.MatKhauHash = BCrypt.Net.BCrypt.HashPassword(newPwd);
        user.NgayCapNhat = DateTime.UtcNow;
        _db.SaveChanges();
    }

    // ─── UpdateProfile (legacy cho AdminController) ───────────────
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

    // ─── Helpers ─────────────────────────────────────────────────

    // Trả về họ tên đầy đủ, fallback về username nếu chưa có thông tin.
    private static string BuildHoTen(string? ho, string? ten, string fallback)
    {
        var full = $"{ho} {ten}".Trim();
        return string.IsNullOrWhiteSpace(full) ? fallback : full;
    }

    private string ResolveRoleName(int maVaiTro) =>
        _db.PhanQuyen.FirstOrDefault(x => x.MaVaiTro == maVaiTro)?.TenVaiTro ?? "unknown";

    private string BuildJwt(int userId, string role)
    {
        var jwtSettings = _config.GetSection("Jwt");
        var claims = new[]
        {
            new Claim("user_id", userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateOtp()
    {
        return Random.Shared.Next(100000, 999999).ToString();
    }
}
