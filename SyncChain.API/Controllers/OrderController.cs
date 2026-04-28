using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SyncChain.API.Services;
using SyncChain.API.DTOs;
using SyncChain.API.Data; 
using Microsoft.EntityFrameworkCore;

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

    // 🔥 TẠO ĐƠN
    [Authorize]
    [HttpPost]
    public IActionResult CreateOrder(CreateOrderDTO dto)
    {
        var claim = User.FindFirst("user_id");

        if (claim == null)
            return Unauthorized("Token không hợp lệ");

        var userId = int.Parse(claim.Value);

        var result = _service.CreateOrder(userId, dto);

        return Ok(result);
    }

    [Authorize]
    [HttpGet]
    public IActionResult GetOrders()
    {
        var claim = User.FindFirst("user_id");

        if (claim == null)
            return Unauthorized();

        var userId = int.Parse(claim.Value);
        var role = User.FindFirst("role")?.Value;

        // 🔥 admin xem tất cả
        if (role == "4")
        {
            return Ok(_db.DonHang.ToList());
        }

        // 🔥 user chỉ xem đơn của mình
        var orders = _db.DonHang
            .Where(x => x.MaNguoiDung == userId)
            .ToList();

        return Ok(orders);
    }

    [Authorize]
    [HttpGet("{id}")]
    public IActionResult GetOrderDetail(int id)
    {
        var details = _db.ChiTietDonHang
            .Where(x => x.MaDonHang == id)
            .ToList();

        return Ok(details);
    }

    [Authorize]
    [HttpGet("full")]
    public IActionResult GetFullOrders()
    {
        var orders = _db.DonHang
            .Include(o => o.ChiTietDonHang) // 🔥 load luôn items
            .OrderByDescending(o => o.NgayTao)
            .ToList();

        return Ok(orders);
    }

    [Authorize]
    [HttpPut("{id}/status")]
    public IActionResult UpdateStatus(int id, string status)
    {
        var order = _db.DonHang.Find(id);

        if (order == null)
            return NotFound();

        var validStatus = new[] { "pending", "processing", "done", "cancel" };

        if (!validStatus.Contains(status))
            return BadRequest("Trạng thái không hợp lệ");

        if (order.TrangThai == "done")
        return BadRequest("Đơn đã hoàn thành");

        order.TrangThai = status;
        _db.SaveChanges();

        return Ok("Cập nhật thành công");


    }


}