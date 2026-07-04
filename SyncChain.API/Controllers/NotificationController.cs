using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SyncChain.API.Data;
using System.Security.Claims;

namespace SyncChain.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly AppDbContext _db;

    public NotificationController(AppDbContext db) => _db = db;

    private int GetUserId()
    {
        // Đọc user_id an toàn từ JWT — tránh NullReferenceException/FormatException
        // nếu token thiếu hoặc sai claim.
        var value = User.FindFirst("user_id")?.Value;
        if (!int.TryParse(value, out var id))
            throw new UnauthorizedAccessException("Token không hợp lệ");
        return id;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetUserId();
        var list = await _db.ThongBao
            .Where(x => x.MaNguoiDung == userId)
            .OrderByDescending(x => x.NgayTao)
            .Select(x => new
            {
                x.MaThongBao, x.LoaiThongBao, x.TieuDe,
                x.NoiDung, x.DaDoc, x.NgayTao, x.MaDonHang
            })
            .ToListAsync();
        return Ok(list);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount()
    {
        var userId = GetUserId();
        var count = await _db.ThongBao
            .CountAsync(x => x.MaNguoiDung == userId && !x.DaDoc);
        return Ok(new { count });
    }

    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkRead(int id)
    {
        var userId = GetUserId();
        var n = await _db.ThongBao.FirstOrDefaultAsync(x => x.MaThongBao == id && x.MaNguoiDung == userId);
        if (n == null) return NotFound();
        n.DaDoc = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = GetUserId();
        var unread = await _db.ThongBao
            .Where(x => x.MaNguoiDung == userId && !x.DaDoc)
            .ToListAsync();
        unread.ForEach(x => x.DaDoc = true);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
