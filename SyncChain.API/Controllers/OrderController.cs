using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SyncChain.API.Data;
using SyncChain.API.DTOs;
using SyncChain.API.Services;

namespace SyncChain.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private readonly OrderService _service;
    private readonly AppDbContext _db;
    private readonly INotificationService _notify;
    private readonly EmailService _email;

    public OrderController(OrderService service, AppDbContext db,
        INotificationService notify, EmailService email)
    {
        _service = service;
        _db      = db;
        _notify  = notify;
        _email   = email;
    }

    // Tạo đơn hàng mới cho người dùng hiện tại.
    [Authorize(Policy = "OrderWrite")]
    [HttpPost]
    public IActionResult CreateOrder(CreateOrderDTO dto)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized("Token khong hop le");

        var result = _service.CreateOrder(userId.Value, dto);

        return Ok(result);
    }

    // Lấy danh sách đơn, lọc theo role của người dùng.
    [Authorize]
    [HttpGet]
    public IActionResult GetOrders()
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized();

        var role = GetRole();

        if (IsInternalRole(role))
        {
            return Ok(_db.DonHang
                .OrderByDescending(x => x.NgayTao)
                .Select(x => new
                {
                    x.MaDonHang,
                    x.MaNguoiDung,
                    x.TongTien,
                    x.NgayTao,
                    x.TrangThai,
                    x.NguoiNhan,
                    x.SoDienThoaiNhan,
                    x.DiaChiGiao
                })
                .ToList());
        }

        var orders = _db.DonHang
            .Where(x => x.MaNguoiDung == userId.Value)
            .OrderByDescending(x => x.NgayTao)
            .Select(x => new
            {
                x.MaDonHang,
                x.MaNguoiDung,
                x.TongTien,
                x.NgayTao,
                x.TrangThai,
                x.NguoiNhan,
                x.SoDienThoaiNhan,
                x.DiaChiGiao
            })
            .ToList();

        return Ok(orders);
    }

    // Lấy chi tiết đơn và kiểm tra quyền xem.
    [Authorize]
    [HttpGet("{id}")]
    public IActionResult GetOrderDetail(int id)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized();

        var order = _db.DonHang.Find(id);
        if (order == null)
            return NotFound();

        if (!IsInternalRole(GetRole()) && order.MaNguoiDung != userId.Value)
            return Forbid();

        var details = _db.ChiTietDonHang
            .Include(x => x.SanPham)
            .Where(x => x.MaDonHang == id)
            .Select(x => new
            {
                x.MaSanPham,
                x.SoLuong,
                x.DonGia,
                SanPham = new
                {
                    x.SanPham.MaSanPham,
                    x.SanPham.TenSanPham,
                    x.SanPham.GiaBan,
                    x.SanPham.SoLuongTon,
                    x.SanPham.MucTonThap,
                    x.SanPham.TrangThai
                }
            })
            .ToList();

        return Ok(details);
    }

    // Lấy toàn bộ đơn cho nhân sự nội bộ quản lý.
    [Authorize(Policy = "OrderManage")]
    [HttpGet("full")]
    public IActionResult GetFullOrders()
    {
        var orders = _db.DonHang
            .OrderByDescending(o => o.NgayTao)
            .Select(x => new
            {
                x.MaDonHang,
                x.MaNguoiDung,
                x.TongTien,
                x.NgayTao,
                x.TrangThai
            })
            .ToList();

        return Ok(orders);
    }

    // Cập nhật trạng thái xử lý đơn hàng.
    [Authorize(Policy = "OrderManage")]
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, string status)
    {
        var order = await _db.DonHang.FindAsync(id);

        if (order == null)
            return NotFound();

        var validStatus = new[] { "Draft", "Approved", "Processing", "Done", "Cancelled" };

        if (!validStatus.Contains(status))
            return BadRequest("Trang thai khong hop le");

        if (order.TrangThai is "Done" or "Cancelled")
            return BadRequest("Don da o trang thai cuoi, khong the cap nhat");

        // Không đổi trạng thái → bỏ qua để tránh gửi thông báo/email trùng lặp.
        if (order.TrangThai == status)
            return Ok("Trang thai khong thay doi");

        order.TrangThai = status;
        await _db.SaveChangesAsync();

        // Push thông báo realtime + email khi trạng thái thay đổi
        await _notify.PushOrderStatusAsync(order.MaNguoiDung, order.MaDonHang, status);

        var owner = await _db.NguoiDung.FindAsync(order.MaNguoiDung);
        if (owner != null)
        {
            var name = $"{owner.Ho} {owner.Ten}".Trim();
            if (string.IsNullOrEmpty(name)) name = owner.TenDangNhap;
            _email.SendOrderStatusChanged(owner.Email, name, order.MaDonHang, status);
        }

        return Ok("Cap nhat thanh cong");
    }

    // GET /api/order/{id}/tracking — lịch sử & timeline đơn hàng
    [Authorize]
    [HttpGet("{id}/tracking")]
    public async Task<IActionResult> GetTracking(int id)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var order = await _db.DonHang.FindAsync(id);
        if (order == null) return NotFound();

        if (!IsInternalRole(GetRole()) && order.MaNguoiDung != userId.Value)
            return Forbid();

        var payment = await _db.ThanhToan
            .Where(x => x.MaDonHang == id)
            .OrderByDescending(x => x.NgayTao)
            .Select(x => new
            {
                x.MaThanhToan, x.PhuongThuc, x.TrangThaiThanhToan,
                x.SoTien, x.NgayTao, x.NgayCapNhat
            })
            .FirstOrDefaultAsync();

        var details = await _db.ChiTietDonHang
            .Include(x => x.SanPham)
            .Where(x => x.MaDonHang == id)
            .Select(x => new
            {
                x.MaSanPham, x.SoLuong, x.DonGia,
                SanPham = new
                {
                    x.SanPham.MaSanPham, x.SanPham.TenSanPham,
                    x.SanPham.GiaBan,    x.SanPham.SoLuongTon,
                    x.SanPham.MucTonThap, x.SanPham.TrangThai
                }
            })
            .ToListAsync();

        var steps = new[] { "Draft", "Approved", "Processing", "Done" };
        bool cancelled = order.TrangThai == "Cancelled";
        int currentIdx = cancelled ? -1 : Array.IndexOf(steps, order.TrangThai);

        var timeline = steps.Select((s, i) =>
        {
            string trangThai;
            if (cancelled && s == order.TrangThai) trangThai = "huyBo";
            else if (i < currentIdx)   trangThai = "hoanThanh";
            else if (i == currentIdx)  trangThai = "hienTai";
            else                       trangThai = "choDoi";
            return new { step = s, trangThai };
        }).ToList();

        if (cancelled)
            timeline.Add(new { step = "Cancelled", trangThai = "huyBo" });

        return Ok(new
        {
            order = new
            {
                order.MaDonHang, order.MaNguoiDung, order.TongTien,
                order.NgayTao,   order.TrangThai,   order.PhuongThucThanhToan,
                order.NguoiNhan, order.SoDienThoaiNhan, order.DiaChiGiao
            },
            payment,
            chiTiet = details,
            timeline
        });
    }

    // Đọc mã người dùng từ JWT.
    private int? GetUserId()
    {
        var claim = User.FindFirst("user_id")?.Value;
        return int.TryParse(claim, out var userId) ? userId : null;
    }

    // Đọc role hiện tại từ JWT.
    private string GetRole()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
    }

    // Kiểm tra role thuộc nhóm nhân sự nội bộ.
    private static bool IsInternalRole(string role)
    {
        return role is "admin" or "manager" or "staff";
    }
}
