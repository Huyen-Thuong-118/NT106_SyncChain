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

    // Tổng hợp số liệu dashboard cho quản lý đơn và kho.
    [Authorize(Policy = "OrderManage")]
    [HttpGet("dashboard")]
    public IActionResult Dashboard()
    {
        var now = DateTime.Now;
        var fromDate = now.Date.AddDays(-6);

        var orders = _db.DonHang
            .Where(x => x.NgayTao.Date >= fromDate)
            .ToList();

        var allOrders = _db.DonHang.ToList();
        var products = _db.SanPham.ToList();

        var totalRevenue = allOrders
            .Where(x => x.TrangThai != "cancel")
            .Sum(x => x.TongTien);
        var todayRevenue = allOrders
            .Where(x => x.TrangThai != "cancel" && x.NgayTao.Date == now.Date)
            .Sum(x => x.TongTien);

        var lowStock = products
            .Where(x => x.SoLuongTon > 0 && x.SoLuongTon <= x.MucTonThap)
            .OrderBy(x => x.SoLuongTon)
            .Take(5)
            .Select(x => new
            {
                x.MaSanPham,
                x.TenSanPham,
                x.SoLuongTon,
                x.MucTonThap,
                x.TrangThai
            })
            .ToList();

        var topProducts = _db.ChiTietDonHang
            .Include(x => x.SanPham)
            .Include(x => x.DonHang)
            .Where(x => x.DonHang != null && x.DonHang.TrangThai != "cancel")
            .GroupBy(x => x.MaSanPham)
            .Select(g => new
            {
                MaSanPham = g.Key,
                TenSanPham = g.First().SanPham.TenSanPham,
                SoLuongBan = g.Sum(x => x.SoLuong),
                DoanhThu = g.Sum(x => x.SoLuong * x.DonGia)
            })
            .OrderByDescending(x => x.SoLuongBan)
            .Take(5)
            .ToList();

        var trend = Enumerable.Range(0, 7)
            .Select(offset =>
            {
                var date = fromDate.AddDays(offset);
                var dateOrders = orders.Where(x => x.NgayTao.Date == date).ToList();

                return new
                {
                    Date = date,
                    Label = date.ToString("dd/MM"),
                    TotalOrders = dateOrders.Count,
                    CompletedOrders = dateOrders.Count(x => x.TrangThai == "done"),
                    ProcessingOrders = dateOrders.Count(x => x.TrangThai is "pending" or "processing"),
                    Revenue = dateOrders.Where(x => x.TrangThai != "cancel").Sum(x => x.TongTien)
                };
            })
            .ToList();

        var recentActivities = allOrders
            .OrderByDescending(x => x.NgayTao)
            .Take(5)
            .Select(x => new
            {
                Title = $"Don hang DH-{x.MaDonHang:0000} - {x.TrangThai}",
                Time = x.NgayTao,
                Type = "order"
            })
            .ToList<object>();

        recentActivities.AddRange(_db.GiaoDichKho
            .OrderByDescending(x => x.ThoiGian)
            .Take(5)
            .Select(x => new
            {
                Title = $"{x.Loai} SP-{x.MaSanPham:0000}: {x.SoLuong}",
                Time = x.ThoiGian,
                Type = "stock"
            }));

        return Ok(new
        {
            TotalProducts = products.Count,
            ActiveProducts = products.Count(x => x.SoLuongTon > 0 && x.TrangThai != "Ngung ban"),
            LowStockProducts = products.Count(x => x.SoLuongTon > 0 && x.SoLuongTon <= x.MucTonThap),
            OutOfStockProducts = products.Count(x => x.SoLuongTon <= 0 || x.TrangThai == "Ngung ban"),
            TotalOrders = allOrders.Count,
            PendingOrders = allOrders.Count(x => x.TrangThai is "pending" or "processing"),
            CompletedOrders = allOrders.Count(x => x.TrangThai == "done"),
            CancelledOrders = allOrders.Count(x => x.TrangThai == "cancel"),
            TotalRevenue = totalRevenue,
            TodayRevenue = todayRevenue,
            Trend = trend,
            TopProducts = topProducts,
            LowStock = lowStock,
            RecentActivities = recentActivities
                .OrderByDescending(x => (DateTime)x.GetType().GetProperty("Time")!.GetValue(x)!)
                .Take(6)
                .ToList()
        });
    }

    // Tính tổng doanh thu toàn hệ thống.
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

    // Lấy nhật ký hoạt động gần đây từ đơn hàng, kho và tài khoản.
    [Authorize(Policy = "OrderManage")]
    [HttpGet("logs")]
    public IActionResult GetLogs()
    {
        var orderLogs = _db.DonHang
            .OrderByDescending(x => x.NgayTao)
            .Take(40)
            .Select(x => new
            {
                Title = $"Đơn hàng DH-{x.MaDonHang:0000}",
                Description = $"Trạng thái hiện tại: {x.TrangThai}, tổng tiền {x.TongTien:N0} VND.",
                Time = x.NgayTao,
                Tag = "Đơn hàng",
                Icon = "ĐH",
                Level = x.TrangThai == "cancel" ? "danger" : x.TrangThai == "done" ? "success" : "info"
            })
            .ToList();

        var stockLogs = _db.GiaoDichKho
            .Include(x => x.SanPham)
            .OrderByDescending(x => x.ThoiGian)
            .Take(40)
            .Select(x => new
            {
                Title = $"{x.Loai} SP-{x.MaSanPham:0000}",
                Description = $"{x.SanPham.TenSanPham}: {(x.SoLuong > 0 ? "+" : string.Empty)}{x.SoLuong} sản phẩm. {x.GhiChu}",
                Time = x.ThoiGian,
                Tag = "Kho hàng",
                Icon = "K",
                Level = x.SoLuong < 0 ? "warning" : "success"
            })
            .ToList();

        var userLogs = _db.NguoiDung
            .OrderByDescending(x => x.MaNguoiDung)
            .Take(20)
            .Select(x => new
            {
                Title = $"Tài khoản ND-{x.MaNguoiDung:0000}",
                Description = $"{x.Email} - {(x.IsActive ? "đang hoạt động" : "đã khóa")}.",
                Time = DateTime.Now.AddMinutes(-x.MaNguoiDung),
                Tag = "Người dùng",
                Icon = "ND",
                Level = x.IsActive ? "info" : "danger"
            })
            .ToList();

        return Ok(orderLogs
            .Concat(stockLogs)
            .Concat(userLogs)
            .OrderByDescending(x => x.Time)
            .Take(100)
            .ToList());
    }

    // Gom doanh thu theo từng ngày.
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

    // Lấy top sản phẩm bán chạy.
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
