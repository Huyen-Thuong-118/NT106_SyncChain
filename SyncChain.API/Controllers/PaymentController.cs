using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SyncChain.API.Data;
using SyncChain.API.DTOs.Payment;
using SyncChain.API.Models;
using SyncChain.API.Services;
using System.Security.Claims;
using System.Text.Json;

namespace SyncChain.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly VnPayService _vnpay;
    private readonly MoMoService _momo;
    private readonly INotificationService _notify;
    private readonly EmailService _email;
    private readonly OrderService _orders;

    public PaymentController(AppDbContext db, VnPayService vnpay, MoMoService momo,
        INotificationService notify, EmailService email, OrderService orders)
    {
        _db     = db;
        _vnpay  = vnpay;
        _momo   = momo;
        _notify = notify;
        _email  = email;
        _orders = orders;
    }

    private int? GetUserId()
    {
        var claim = User.FindFirst("user_id")?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    private string GetRole() => User.FindFirst(ClaimTypes.Role)?.Value ?? "";

    // POST /api/payment/initiate
    [Authorize(Policy = "OrderWrite")]
    [HttpPost("initiate")]
    public async Task<IActionResult> Initiate([FromBody] InitiatePaymentDTO dto)
    {
        var method = dto.PhuongThuc.ToLowerInvariant();
        if (method is not ("vnpay" or "momo" or "cod"))
            return BadRequest(new { message = "Phương thức không hợp lệ. Dùng: vnpay, momo, cod" });

        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var order = await _db.DonHang.FindAsync(dto.MaDonHang);
        if (order == null) return NotFound(new { message = "Đơn hàng không tồn tại" });

        // Customer chỉ được thanh toán đơn của mình
        if (GetRole() == "customer" && order.MaNguoiDung != userId.Value)
            return Forbid();

        if (order.TrangThai != OrderStatuses.Pending)
            return BadRequest(new { message = "Chỉ đơn ở trạng thái chờ xử lý mới có thể thanh toán" });

        // Chặn thanh toán lặp: đơn đã có giao dịch hoàn tất thì không cho khởi tạo lại.
        var alreadyPaid = await _db.ThanhToan
            .AnyAsync(x => x.MaDonHang == order.MaDonHang && x.TrangThaiThanhToan == "Completed");
        if (alreadyPaid)
            return BadRequest(new { message = "Đơn hàng đã được thanh toán" });

        order.PhuongThucThanhToan = method;

        var payment = new ThanhToan
        {
            MaDonHang          = order.MaDonHang,
            PhuongThuc         = method,
            TrangThaiThanhToan = "Pending",
            SoTien             = order.TongTien,
            NgayTao            = DateTime.UtcNow
        };

        if (method == "cod")
        {
            payment.TrangThaiThanhToan = "Completed";
            payment.NgayCapNhat        = DateTime.UtcNow;

            _db.ThanhToan.Add(payment);
            await _db.SaveChangesAsync();

            await _orders.ClearPurchasedItemsAsync(order.MaDonHang);

            var owner = await _db.NguoiDung.FindAsync(order.MaNguoiDung);
            var name  = $"{owner?.Ho} {owner?.Ten}".Trim();
            if (string.IsNullOrEmpty(name)) name = owner?.TenDangNhap ?? "Khách hàng";

            await _notify.PushPaymentResultAsync(order.MaNguoiDung, order.MaDonHang, true, "COD");

            if (owner != null)
            {
                _email.SendOrderConfirmation(owner.Email, name, order.MaDonHang, order.TongTien, "COD");
                _email.SendPaymentResult(owner.Email, name, order.MaDonHang, true, "COD");
            }

            return Ok(new { message = "Đặt hàng COD thành công", orderId = order.MaDonHang });
        }

        //vnpay
        if (method == "vnpay")
        {
            var ip      = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var info    = $"Thanh toan don hang #{order.MaDonHang}";
            var (url, txnRef) = _vnpay.CreatePaymentUrl(order.MaDonHang, order.TongTien, info, ip);
            payment.MaGiaoDich = txnRef;
            _db.ThanhToan.Add(payment);
            await _db.SaveChangesAsync();
            return Ok(new { paymentUrl = url, phuongThuc = "vnpay" });
        }

        // momo
        var (success, payUrl, momoOrderId, msg) =
            await _momo.CreatePaymentAsync(order.MaDonHang, order.TongTien,
                $"Thanh toan don #{order.MaDonHang}");

        if (!success)
            return StatusCode(500, new { message = $"MoMo: {msg}" });

        payment.MaGiaoDich = momoOrderId;
        _db.ThanhToan.Add(payment);
        await _db.SaveChangesAsync();
        return Ok(new { paymentUrl = payUrl, phuongThuc = "momo" });
    }

    // GET /api/payment/vnpay/return  — trình duyệt redirect về đây
    [HttpGet("vnpay/return")]
    public async Task<IActionResult> VnPayReturn()
    {
        if (!_vnpay.ValidateCallback(Request.Query))
            return Content(PaymentHtml("Lỗi xác thực", false,
                "Chữ ký không hợp lệ"), "text/html");

        var (txnRef, success, _) = _vnpay.ParseCallback(Request.Query);
        await HandleVnPayResult(txnRef, success, Request.Query.ToString() ?? "");

        return Content(PaymentHtml(
            success ? "Thanh toán thành công" : "Thanh toán thất bại",
            success,
            success ? "Đơn hàng của bạn đã được xác nhận." : "Vui lòng thử lại hoặc chọn phương thức khác."),
            "text/html");
    }

    // GET /api/payment/vnpay/ipn  — VNPay server gọi (thực tế là GET)
    [HttpGet("vnpay/ipn")]
    [HttpPost("vnpay/ipn")]
    public async Task<IActionResult> VnPayIpn()
    {
        if (!_vnpay.ValidateCallback(Request.Query))
            return Ok(new { RspCode = "97", Message = "Invalid signature" });

        var (txnRef, success, _) = _vnpay.ParseCallback(Request.Query);
        var updated = await HandleVnPayResult(txnRef, success, Request.Query.ToString() ?? "");

        if (!updated)
            return Ok(new { RspCode = "02", Message = "Order not found or already processed" });

        return Ok(new { RspCode = "00", Message = "Confirm success" });
    }

    private async Task<bool> HandleVnPayResult(string txnRef, bool success, string rawCallback)
    {
        var payment = await _db.ThanhToan
            .Where(x => x.MaGiaoDich == txnRef && x.TrangThaiThanhToan == "Pending")
            .OrderByDescending(x => x.NgayTao)
            .FirstOrDefaultAsync();

        if (payment == null) return false;

        payment.TrangThaiThanhToan = success ? "Completed" : "Failed";
        payment.NgayCapNhat        = DateTime.UtcNow;
        payment.DuLieuCallback     = rawCallback;

        // Thanh toán online thành công chỉ ghi nhận giao dịch; đơn giữ nguyên
        // pending để nhân sự xử lý (giống COD). Trạng thái đơn do luồng xử lý
        // của nhân sự điều khiển qua OrderService.
        var order = await _db.DonHang.FindAsync(payment.MaDonHang);
        await _db.SaveChangesAsync();

        if (order != null)
        {
            var owner = await _db.NguoiDung.FindAsync(order.MaNguoiDung);
            var name  = $"{owner?.Ho} {owner?.Ten}".Trim();
            if (string.IsNullOrEmpty(name)) name = owner?.TenDangNhap ?? "Khách hàng";

            await _notify.PushPaymentResultAsync(order.MaNguoiDung, order.MaDonHang, success, "VNPay");
            if (success)
            {
                // Thanh toan online thanh cong: tu dong chuyen don sang "dang xu ly"
                // va xoa cac san pham cua don khoi gio hang (chi khi da thanh toan).
                await _orders.AdvanceToProcessingAfterPaymentAsync(order.MaDonHang);
                await _orders.ClearPurchasedItemsAsync(order.MaDonHang);
                if (owner != null)
                {
                    _email.SendOrderConfirmation(owner.Email, name, order.MaDonHang, order.TongTien, "VNPay");
                    _email.SendPaymentResult(owner.Email, name, order.MaDonHang, true, "VNPay");
                }
            }
        }

        return true;
    }

    // GET /api/payment/momo/return — MoMo redirect về đây
    [HttpGet("momo/return")]
    public async Task<IActionResult> MoMoReturn()
    {
        // MoMo redirect params are in query string
        var resultCode = Request.Query["resultCode"].ToString();
        var momoOrderId = Request.Query["orderId"].ToString();
        var success = resultCode == "0";

        await HandleMoMoResult(momoOrderId, success, Request.QueryString.ToString());

        return Content(PaymentHtml(
            success ? "Thanh toán thành công" : "Thanh toán thất bại",
            success,
            success ? "Đơn hàng của bạn đã được xác nhận." : "Vui lòng thử lại."),
            "text/html");
    }

    // POST /api/payment/momo/ipn — MoMo server-to-server
    [HttpPost("momo/ipn")]
    public async Task<IActionResult> MoMoIpn([FromBody] JsonElement body)
    {
        if (!_momo.ValidateCallback(body))
            return Ok(new { message = "Invalid signature" });

        var (momoOrderId, success) = _momo.ParseCallback(body);
        await HandleMoMoResult(momoOrderId, success, body.GetRawText());

        return Ok(new { message = "ok" });
    }

    private async Task<bool> HandleMoMoResult(string momoOrderId, bool success, string rawCallback)
    {
        var payment = await _db.ThanhToan
            .Where(x => x.MaGiaoDich == momoOrderId && x.TrangThaiThanhToan == "Pending")
            .OrderByDescending(x => x.NgayTao)
            .FirstOrDefaultAsync();

        if (payment == null) return false;

        payment.TrangThaiThanhToan = success ? "Completed" : "Failed";
        payment.NgayCapNhat        = DateTime.UtcNow;
        payment.DuLieuCallback     = rawCallback;

        // Thanh toán online thành công chỉ ghi nhận giao dịch; đơn giữ nguyên
        // pending để nhân sự xử lý (giống COD). Trạng thái đơn do luồng xử lý
        // của nhân sự điều khiển qua OrderService.
        var order = await _db.DonHang.FindAsync(payment.MaDonHang);
        await _db.SaveChangesAsync();

        if (order != null)
        {
            var owner = await _db.NguoiDung.FindAsync(order.MaNguoiDung);
            var name  = $"{owner?.Ho} {owner?.Ten}".Trim();
            if (string.IsNullOrEmpty(name)) name = owner?.TenDangNhap ?? "Khách hàng";

            await _notify.PushPaymentResultAsync(order.MaNguoiDung, order.MaDonHang, success, "MoMo");
            if (success)
            {
                // Thanh toan online thanh cong: tu dong chuyen don sang "dang xu ly"
                // va xoa cac san pham cua don khoi gio hang (chi khi da thanh toan).
                await _orders.AdvanceToProcessingAfterPaymentAsync(order.MaDonHang);
                await _orders.ClearPurchasedItemsAsync(order.MaDonHang);
                if (owner != null)
                {
                    _email.SendOrderConfirmation(owner.Email, name, order.MaDonHang, order.TongTien, "MoMo");
                    _email.SendPaymentResult(owner.Email, name, order.MaDonHang, true, "MoMo");
                }
            }
        }

        return true;
    }

    // GET /api/payment/status/{orderId}
    [Authorize]
    [HttpGet("status/{orderId}")]
    public async Task<IActionResult> GetStatus(int orderId)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var order = await _db.DonHang.FindAsync(orderId);
        if (order == null) return NotFound();

        var role = GetRole();
        if (role == "customer" && order.MaNguoiDung != userId.Value)
            return Forbid();

        var payment = await _db.ThanhToan
            .Where(x => x.MaDonHang == orderId)
            .OrderByDescending(x => x.NgayTao)
            .Select(x => new
            {
                x.MaThanhToan, x.PhuongThuc, x.TrangThaiThanhToan,
                x.SoTien, x.NgayTao, x.NgayCapNhat, x.MaGiaoDich
            })
            .FirstOrDefaultAsync();

        if (payment == null)
            return NotFound(new { message = "Đơn hàng chưa có giao dịch thanh toán" });

        return Ok(payment);
    }

    private static string PaymentHtml(string title, bool success, string message) =>
        $$"""
        <!DOCTYPE html>
        <html lang="vi">
        <head><meta charset="UTF-8"><title>{{title}} — SyncChain</title>
        <style>
          body{font-family:Arial,sans-serif;display:flex;align-items:center;justify-content:center;min-height:100vh;margin:0;background:#f3f4f6}
          .card{background:#fff;border-radius:16px;padding:40px;max-width:400px;text-align:center;box-shadow:0 4px 24px rgba(0,0,0,.08)}
          h1{color:{{(success ? "#16a34a" : "#dc2626")}};font-size:24px}
          p{color:#374151}
          .icon{font-size:48px}
        </style></head>
        <body>
          <div class="card">
            <div class="icon">{{(success ? "✅" : "❌")}}</div>
            <h1>{{title}}</h1>
            <p>{{message}}</p>
            <p style="color:#9ca3af;font-size:13px">Bạn có thể đóng cửa sổ này và quay lại ứng dụng.</p>
          </div>
        </body></html>
        """;
}
