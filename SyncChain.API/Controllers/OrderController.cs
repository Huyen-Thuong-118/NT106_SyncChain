using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using SyncChain.API.Data;
using SyncChain.API.DTOs;
using SyncChain.API.Exceptions;
using SyncChain.API.Models;
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

    // Táº¡o Ä‘Æ¡n hÃ ng má»›i cho ngÆ°á»i dÃ¹ng hiá»‡n táº¡i.
    [Authorize(Policy = "OrderWrite")]
    [HttpPost]
    public async Task<IActionResult> CreateOrder(CreateOrderDTO dto)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized("Token khong hop le");

        if (string.IsNullOrWhiteSpace(dto.IdempotencyKey) &&
            Request.Headers.TryGetValue("Idempotency-Key", out var headerKey))
        {
            dto.IdempotencyKey = headerKey.ToString();
        }

        if (IsInternalRole(GetRole()))
        {
            var channel = dto.SalesChannel?.Trim() ?? string.Empty;
            if (channel.Equals("Online", StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationApiException(
                    "Don Online phai do khach hang tu dat.");
            }
            if (!channel.Equals("Cá»­a hÃ ng trá»±c tiáº¿p", StringComparison.OrdinalIgnoreCase) &&
                !channel.Equals("Facebook", StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationApiException("Kenh ban hang khong hop le.");
            }
            if (channel.Equals("Facebook", StringComparison.OrdinalIgnoreCase) &&
                (string.IsNullOrWhiteSpace(dto.DiaChiGiaoHang) ||
                 string.IsNullOrWhiteSpace(dto.TinhThanh) ||
                 string.IsNullOrWhiteSpace(dto.PhuongXa)))
            {
                throw new ValidationApiException(
                    "Don Facebook phai co day du dia chi giao hang.");
            }
        }

        return Ok(await _service.CreateOrderAsync(userId.Value, dto));
    }

    // Láº¥y danh sÃ¡ch Ä‘Æ¡n, lá»c theo role cá»§a ngÆ°á»i dÃ¹ng.
    [Authorize]
    [HttpGet]
    public IActionResult GetOrders([FromQuery] string? status = null)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized();

        var role = GetRole();

        if (IsInternalRole(role))
        {
            var query = _db.DonHang.AsQueryable();
            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(x => x.TrangThai == status.Trim().ToLowerInvariant());
            return Ok(query
                .OrderByDescending(x => x.NgayTao)
                .Select(x => new
                {
                    x.MaDonHang,
                    x.MaNguoiDung,
                    x.TongTien,
                    ProductTotal = x.ChiTietDonHang.Sum(i => i.SoLuong * i.DonGia),
                    x.NgayTao,
                    x.TrangThai,
                    x.ConcurrencyVersion,
                    CustomerName = x.TenNguoiNhan != "" ? x.TenNguoiNhan :
                        _db.NguoiDung.Where(u => u.MaNguoiDung == x.MaNguoiDung)
                            .Select(u => u.TenDangNhap).FirstOrDefault(),
                    CustomerEmail = x.EmailNguoiNhan != "" ? x.EmailNguoiNhan :
                        _db.NguoiDung.Where(u => u.MaNguoiDung == x.MaNguoiDung)
                            .Select(u => u.Email).FirstOrDefault(),
                    x.SoDienThoai,
                    x.DiaChiGiaoHang,
                    x.TinhThanh,
                    x.PhuongXa,
                    x.LoaiDichVu,
                    x.TrongLuongKg,
                    ProductNames = x.ChiTietDonHang
                        .Select(i => i.SanPham.TenSanPham)
                        .ToList(),
                    ProductCount = x.ChiTietDonHang.Sum(i => i.SoLuong),
                    Shipping = x.VanChuyen == null ? null : new
                    {
                        x.VanChuyen.DonViVanChuyen,
                        x.VanChuyen.MaVanDon,
                        x.VanChuyen.TrangThaiGiaoHang,
                        x.VanChuyen.NgayGiaoDuKien,
                        x.VanChuyen.NgayGiaoThucTe,
                        x.VanChuyen.PhiVanChuyen,
                        x.VanChuyen.ConcurrencyVersion
                    }
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
                x.ConcurrencyVersion
            })
            .ToList();

        return Ok(orders);
    }

    // Láº¥y chi tiáº¿t Ä‘Æ¡n vÃ  kiá»ƒm tra quyá»n xem.
    [Authorize]
    [HttpGet("{id}")]
    public IActionResult GetOrderDetail(int id)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized();

        var order = _db.DonHang
            .Include(x => x.VanChuyen)
            .FirstOrDefault(x => x.MaDonHang == id);
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

        var user = _db.NguoiDung.FirstOrDefault(x => x.MaNguoiDung == order.MaNguoiDung);
        return Ok(new
        {
            order.MaDonHang,
            order.MaNguoiDung,
            order.TongTien,
            order.NgayTao,
            order.TrangThai,
            order.ConcurrencyVersion,
            CustomerName = string.IsNullOrWhiteSpace(order.TenNguoiNhan) ? user?.TenDangNhap : order.TenNguoiNhan,
            CustomerEmail = string.IsNullOrWhiteSpace(order.EmailNguoiNhan) ? user?.Email : order.EmailNguoiNhan,
            order.SoDienThoai,
            order.DiaChiGiaoHang,
            order.TinhThanh,
            order.PhuongXa,
            order.LoaiDichVu,
            order.TrongLuongKg,
            order.GhiChu,
            Details = details,
            Shipping = order.VanChuyen == null ? null : new
            {
                ShippingId = order.VanChuyen.MaVanChuyen,
                Carrier = order.VanChuyen.DonViVanChuyen,
                TrackingNumber = order.VanChuyen.MaVanDon,
                ShippingFee = order.VanChuyen.PhiVanChuyen,
                ShippingStatus = order.VanChuyen.TrangThaiGiaoHang,
                EstimatedDeliveryAt = order.VanChuyen.NgayGiaoDuKien,
                DeliveredAt = order.VanChuyen.NgayGiaoThucTe,
                order.VanChuyen.ConcurrencyVersion
            }
        });
    }

    // Láº¥y toÃ n bá»™ Ä‘Æ¡n cho nhÃ¢n sá»± ná»™i bá»™ quáº£n lÃ½.
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
                x.TrangThai,
                x.ConcurrencyVersion
            })
            .ToList();

        return Ok(orders);
    }

    // Cáº­p nháº­t tráº¡ng thÃ¡i xá»­ lÃ½ Ä‘Æ¡n hÃ ng.
    [Authorize(Policy = "OrderManage")]
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(
        int id,
        [FromQuery] string? status,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] UpdateOrderStatusDTO? request)
    {
        var userId = GetUserId();
        return Ok(await _service.UpdateStatusAsync(id, request, status, userId));
    }

    // Khach hang tu huy don cua chinh minh khi don con cho xu ly.
    [Authorize]
    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> CancelOwnOrder(int id)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized();

        return Ok(await _service.CancelOwnOrderAsync(id, userId.Value));
    }

    // Äá»c mÃ£ ngÆ°á»i dÃ¹ng tá»« JWT.
    // Theo doi don hang: tra ve don + timeline + chi tiet + thanh toan gan nhat.
    // Khach chi xem duoc don cua chinh minh; nhan su noi bo xem duoc tat ca.
    [Authorize]
    [HttpGet("{id}/tracking")]
    public IActionResult GetTracking(int id)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized();

        var order = _db.DonHang.FirstOrDefault(x => x.MaDonHang == id);
        if (order == null)
            return NotFound();

        if (!IsInternalRole(GetRole()) && order.MaNguoiDung != userId.Value)
            return Forbid();

        var chiTiet = _db.ChiTietDonHang
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

        var payment = _db.ThanhToan
            .Where(x => x.MaDonHang == id)
            .OrderByDescending(x => x.NgayTao)
            .Select(x => new
            {
                x.MaThanhToan,
                x.PhuongThuc,
                x.TrangThaiThanhToan,
                x.SoTien,
                x.NgayTao,
                x.NgayCapNhat
            })
            .FirstOrDefault();

        return Ok(new
        {
            order = new
            {
                order.MaDonHang,
                order.MaNguoiDung,
                order.TongTien,
                order.TrangThai,
                order.NgayTao,
                order.PhuongThucThanhToan,
                NguoiNhan = string.IsNullOrWhiteSpace(order.TenNguoiNhan) ? null : order.TenNguoiNhan,
                SoDienThoaiNhan = string.IsNullOrWhiteSpace(order.SoDienThoai) ? null : order.SoDienThoai,
                DiaChiGiao = string.IsNullOrWhiteSpace(order.DiaChiGiaoHang) ? null : order.DiaChiGiaoHang
            },
            payment,
            chiTiet,
            timeline = BuildTrackingTimeline(order.TrangThai)
        });
    }

    // Sinh timeline theo trang thai hien tai (dung bo trang thai lowercase chuan).
    private static object[] BuildTrackingTimeline(string status)
    {
        if (status == OrderStatuses.Cancel)
        {
            return new object[]
            {
                new { step = OrderStatuses.Pending, trangThai = "hoanThanh" },
                new { step = OrderStatuses.Cancel, trangThai = "huyBo" }
            };
        }

        var flow = new[]
        {
            OrderStatuses.Pending,
            OrderStatuses.Processing,
            OrderStatuses.Shipping,
            OrderStatuses.Done
        };
        var currentIndex = Array.IndexOf(flow, status);

        return flow
            .Select((step, index) => (object)new
            {
                step,
                trangThai = index < currentIndex ? "hoanThanh"
                    : index == currentIndex ? "hienTai"
                    : "choDoi"
            })
            .ToArray();
    }

    private int? GetUserId()
    {
        var claim = User.FindFirst("user_id")?.Value;
        return int.TryParse(claim, out var userId) ? userId : null;
    }

    // Äá»c role hiá»‡n táº¡i tá»« JWT.
    private string GetRole()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
    }

    // Kiá»ƒm tra role thuá»™c nhÃ³m nhÃ¢n sá»± ná»™i bá»™.
    private static bool IsInternalRole(string role)
    {
        return role is "admin" or "manager" or "staff";
    }
}
