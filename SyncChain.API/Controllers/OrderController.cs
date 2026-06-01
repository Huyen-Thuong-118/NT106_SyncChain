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

    public OrderController(OrderService service, AppDbContext db)
    {
        _service = service;
        _db = db;
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
                    x.TrangThai
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
                x.TrangThai
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
    public IActionResult UpdateStatus(int id, string status)
    {
        var order = _db.DonHang.Find(id);

        if (order == null)
            return NotFound();

        var validStatus = new[] { "Draft", "Approved", "Processing", "Done", "Cancelled" };

        if (!validStatus.Contains(status))
            return BadRequest("Trang thai khong hop le");

        if (order.TrangThai == "Done")
            return BadRequest("Don da hoan thanh");

        order.TrangThai = status;
        _db.SaveChanges();

        return Ok("Cap nhat thanh cong");
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
