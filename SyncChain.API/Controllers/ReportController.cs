using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SyncChain.API.Data;
using Microsoft.EntityFrameworkCore;

namespace SyncChain.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportController : ControllerBase
{
    private readonly AppDbContext _db;

    public ReportController(AppDbContext db)
    {
        _db = db;
    }

    // 🔥 STEP 2 — DOANH THU TỔNG
    [Authorize(Policy = "AdminOnly")]
    [HttpGet("revenue")]
    public IActionResult GetRevenue()
    {
        var total = _db.DonHang.Sum(x => x.TongTien);

        return Ok(new
        {
            totalRevenue = total
        });
    }

    // 🔥 STEP 3 — DOANH THU THEO NGÀY
    [Authorize(Policy = "AdminOnly")]
    [HttpGet("revenue-by-date")]
    public IActionResult RevenueByDate()
    {
        var result = _db.DonHang
            .GroupBy(x => x.NgayTao.Date)
            .Select(g => new
            {
                Date = g.Key,
                Total = g.Sum(x => x.TongTien)
            })
            .OrderByDescending(x => x.Date)
            .ToList();

        return Ok(result);
    }

    // 🔥 STEP 4 — TOP PRODUCT
    [Authorize(Policy = "AdminOnly")]
    [HttpGet("top-products")]
    public IActionResult TopProducts()
    {
        var result = _db.ChiTietDonHang
            
            .Include(x => x.SanPham)
            .GroupBy(x => x.MaSanPham)
            .Select(g => new
            {
                TenSanPham = g.First().SanPham.TenSanPham,
                SoLuongBan = g.Sum(x => x.SoLuong)
            })
            .OrderByDescending(x => x.SoLuongBan)
            .Take(5)
            .ToList();

        return Ok(result);
    }
}