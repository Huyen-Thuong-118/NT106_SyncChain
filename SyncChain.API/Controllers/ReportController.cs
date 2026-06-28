using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SyncChain.API.Data;
using SyncChain.API.DTOs.Report;
using SyncChain.API.Exceptions;
using SyncChain.API.Models;

namespace SyncChain.API.Controllers;

[ApiController]
[Route("api/reports")]
[Route("api/[controller]")]
public class ReportController : ControllerBase
{
    private const string Uncategorized = "Uncategorized";
    private readonly AppDbContext _db;

    public ReportController(AppDbContext db)
    {
        _db = db;
    }

    [Authorize(Policy = "ReportView")]
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard([FromQuery] ReportDateRangeQueryDTO query)
    {
        var (from, to) = NormalizeRange(query.From, query.To);
        var ordersQuery = FilterOrdersByDate(_db.DonHang.AsNoTracking(), from, to);
        var shippingQuery = FilterShippingByDate(_db.VanChuyen.AsNoTracking(), from, to);

        var orderCounts = await ordersQuery
            .GroupBy(x => x.TrangThai)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();
        var shippingCounts = await shippingQuery
            .GroupBy(x => x.TrangThaiGiaoHang)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var netRevenue = await _db.ChiTietDonHang.AsNoTracking()
            .Where(x => x.DonHang != null &&
                        x.DonHang.TrangThai == OrderStatuses.Done &&
                        x.DonHang.NgayTao >= from &&
                        x.DonHang.NgayTao <= to)
            .SumAsync(x => (decimal?)(x.SoLuong * x.DonGia)) ?? 0;
        var cancelledValue = await _db.ChiTietDonHang.AsNoTracking()
            .Where(x => x.DonHang != null &&
                        x.DonHang.TrangThai == OrderStatuses.Cancel &&
                        x.DonHang.NgayTao >= from &&
                        x.DonHang.NgayTao <= to)
            .SumAsync(x => (decimal?)(x.SoLuong * x.DonGia)) ?? 0;
        var shippingFee = await _db.VanChuyen.AsNoTracking()
            .Where(x => x.DonHang.TrangThai == OrderStatuses.Done &&
                        x.DonHang.NgayTao >= from && x.DonHang.NgayTao <= to)
            .SumAsync(x => (decimal?)x.PhiVanChuyen) ?? 0;

        var inventory = await _db.SanPham.AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new InventorySummaryDTO
            {
                TotalProducts = g.Count(),
                ActiveProducts = g.Count(x => x.TrangThai != "Ngung ban"),
                LowStockProducts = g.Count(x => x.SoLuongTon > 0 && x.SoLuongTon <= x.MucTonThap),
                OutOfStockProducts = g.Count(x => x.SoLuongTon <= 0),
                TotalInventoryValue = g.Sum(x => x.SoLuongTon * x.GiaNhap)
            })
            .FirstOrDefaultAsync() ?? new InventorySummaryDTO();

        return Ok(new DashboardSummaryDTO
        {
            From = from,
            To = to,
            Orders = new OrderSummaryDTO
            {
                Total = orderCounts.Sum(x => x.Count),
                Pending = CountStatus(orderCounts, OrderStatuses.Pending),
                Processing = CountStatus(orderCounts, OrderStatuses.Processing),
                Done = CountStatus(orderCounts, OrderStatuses.Done),
                Cancel = CountStatus(orderCounts, OrderStatuses.Cancel)
            },
            Revenue = new RevenueSummaryDTO
            {
                Gross = netRevenue,
                Net = netRevenue,
                ShippingFee = shippingFee,
                CancelledValue = cancelledValue
            },
            Inventory = inventory,
            Shipping = new ShippingSummaryDTO
            {
                Total = shippingCounts.Sum(x => x.Count),
                Pending = CountShippingStatus(shippingCounts, ShippingStatuses.Pending),
                Ready = CountShippingStatus(shippingCounts, ShippingStatuses.Ready),
                PickedUp = CountShippingStatus(shippingCounts, ShippingStatuses.PickedUp),
                InTransit = CountShippingStatus(shippingCounts, ShippingStatuses.InTransit),
                Delivered = CountShippingStatus(shippingCounts, ShippingStatuses.Delivered),
                Failed = CountShippingStatus(shippingCounts, ShippingStatuses.Failed),
                Returned = CountShippingStatus(shippingCounts, ShippingStatuses.Returned),
                Cancelled = CountShippingStatus(shippingCounts, ShippingStatuses.Cancelled)
            }
        });
    }

    [Authorize(Policy = "ReportView")]
    [HttpGet("categories")]
    public async Task<IActionResult> Categories([FromQuery] ReportDateRangeQueryDTO query)
    {
        var (from, to) = NormalizeRange(query.From, query.To);

        var productStats = await _db.SanPham.AsNoTracking()
            .Include(x => x.DanhMuc)
            .GroupBy(x => new
            {
                x.MaDanhMuc,
                CategoryName = x.DanhMuc == null ? Uncategorized : x.DanhMuc.TenDanhMuc
            })
            .Select(g => new CategoryReportItemDTO
            {
                CategoryId = g.Key.MaDanhMuc,
                CategoryName = g.Key.CategoryName,
                ProductCount = g.Count(),
                ActiveProductCount = g.Count(x => x.TrangThai != "Ngung ban"),
                StockQuantity = g.Sum(x => x.SoLuongTon)
            })
            .ToListAsync();

        var salesStats = await _db.ChiTietDonHang.AsNoTracking()
            .Where(x => x.DonHang != null &&
                        x.DonHang.TrangThai == OrderStatuses.Done &&
                        x.DonHang.NgayTao >= from &&
                        x.DonHang.NgayTao <= to)
            .GroupBy(x => new
            {
                x.SanPham.MaDanhMuc,
                CategoryName = x.SanPham.DanhMuc == null ? Uncategorized : x.SanPham.DanhMuc.TenDanhMuc
            })
            .Select(g => new
            {
                g.Key.MaDanhMuc,
                g.Key.CategoryName,
                SoldQuantity = g.Sum(x => x.SoLuong),
                Revenue = g.Sum(x => x.SoLuong * x.DonGia)
            })
            .ToListAsync();

        foreach (var sale in salesStats)
        {
            var item = productStats.FirstOrDefault(x =>
                x.CategoryId == sale.MaDanhMuc && x.CategoryName == sale.CategoryName);
            if (item == null)
            {
                item = new CategoryReportItemDTO
                {
                    CategoryId = sale.MaDanhMuc,
                    CategoryName = sale.CategoryName
                };
                productStats.Add(item);
            }

            item.SoldQuantity = sale.SoldQuantity;
            item.Revenue = sale.Revenue;
        }

        return Ok(new CategoryReportPageDTO
        {
            Items = productStats
                .OrderByDescending(x => x.Revenue)
                .ThenBy(x => x.CategoryName)
                .ToList()
        });
    }

    [Authorize(Policy = "ReportView")]
    [HttpGet("inventory")]
    public async Task<IActionResult> Inventory([FromQuery] InventoryReportQueryDTO query)
    {
        if (query.LowStockThreshold < 0)
            throw new ValidationApiException("Nguong ton kho thap khong duoc am.");

        var products = await _db.SanPham.AsNoTracking()
            .Include(x => x.DanhMuc)
            .Select(x => new InventoryReportProductDTO
            {
                ProductId = x.MaSanPham,
                ProductName = x.TenSanPham,
                Quantity = x.SoLuongTon,
                CategoryName = x.DanhMuc == null ? Uncategorized : x.DanhMuc.TenDanhMuc,
                UnitCost = x.GiaNhap,
                InventoryValue = x.SoLuongTon * x.GiaNhap
            })
            .ToListAsync();

        return Ok(new InventoryReportDTO
        {
            TotalProducts = products.Count,
            TotalQuantity = products.Sum(x => x.Quantity),
            TotalInventoryValue = products.Sum(x => x.InventoryValue),
            LowStockThreshold = query.LowStockThreshold,
            LowStockProducts = products
                .Where(x => x.Quantity > 0 && x.Quantity <= query.LowStockThreshold)
                .OrderBy(x => x.Quantity)
                .ThenBy(x => x.ProductName)
                .ToList(),
            OutOfStockProducts = products
                .Where(x => x.Quantity <= 0)
                .OrderBy(x => x.ProductName)
                .ToList()
        });
    }

    [Authorize(Policy = "ReportView")]
    [HttpGet("shipping")]
    public async Task<IActionResult> Shipping([FromQuery] ReportDateRangeQueryDTO query)
    {
        var (from, to) = NormalizeRange(query.From, query.To);
        var shippingQuery = FilterShippingByDate(_db.VanChuyen.AsNoTracking(), from, to);

        var byStatus = await shippingQuery
            .GroupBy(x => x.TrangThaiGiaoHang)
            .Select(g => new ShippingStatusReportItemDTO
            {
                Status = g.Key,
                Count = g.Count()
            })
            .OrderBy(x => x.Status)
            .ToListAsync();

        var byCarrier = await shippingQuery
            .GroupBy(x => x.DonViVanChuyen == "" ? "Unknown" : x.DonViVanChuyen)
            .Select(g => new ShippingCarrierReportItemDTO
            {
                Carrier = g.Key,
                Count = g.Count(),
                Delivered = g.Count(x => x.TrangThaiGiaoHang == ShippingStatuses.Delivered),
                Failed = g.Count(x => x.TrangThaiGiaoHang == ShippingStatuses.Failed),
                Returned = g.Count(x => x.TrangThaiGiaoHang == ShippingStatuses.Returned),
                ShippingFee = g.Sum(x => x.PhiVanChuyen)
            })
            .OrderByDescending(x => x.Count)
            .ToListAsync();

        return Ok(new ShippingReportDTO
        {
            TotalShipments = byStatus.Sum(x => x.Count),
            TotalShippingFee = byCarrier.Sum(x => x.ShippingFee),
            ByStatus = byStatus,
            ByCarrier = byCarrier
        });
    }

    [Authorize(Policy = "RevenueView")]
    [HttpGet("revenue")]
    public async Task<IActionResult> Revenue([FromQuery] RevenueReportQueryDTO query)
    {
        var groupBy = NormalizeGroupBy(query.GroupBy);
        var (from, to) = NormalizeRange(query.From, query.To);

        var orders = await _db.DonHang.AsNoTracking()
            .Where(x => x.NgayTao >= from && x.NgayTao <= to)
            .Select(x => new
            {
                x.MaDonHang,
                x.NgayTao,
                x.TrangThai,
                ProductTotal = x.ChiTietDonHang.Sum(i => i.SoLuong * i.DonGia),
                ShippingFee = x.VanChuyen == null ? 0 : x.VanChuyen.PhiVanChuyen
            })
            .ToListAsync();

        var doneOrders = orders.Where(x => x.TrangThai == OrderStatuses.Done).ToList();
        var items = doneOrders
            .GroupBy(x => BuildPeriod(x.NgayTao, groupBy))
            .Select(g =>
            {
                var net = g.Sum(x => x.ProductTotal);
                var shippingFee = g.Sum(x => x.ShippingFee);
                return new RevenueReportItemDTO
                {
                    Period = g.Key,
                    OrderCount = g.Count(),
                    NetRevenue = net,
                    ShippingFee = shippingFee,
                    GrossRevenue = net
                };
            })
            .OrderBy(x => x.Period)
            .ToList();

        var totalNet = doneOrders.Sum(x => x.ProductTotal);
        var totalShipping = doneOrders.Sum(x => x.ShippingFee);

        return Ok(new RevenueReportDTO
        {
            From = from,
            To = to,
            GroupBy = groupBy,
            NetRevenue = totalNet,
            ShippingFee = totalShipping,
            GrossRevenue = totalNet,
            CancelledValue = orders
                .Where(x => x.TrangThai == OrderStatuses.Cancel)
                .Sum(x => x.ProductTotal),
            Items = items
        });
    }

    [Authorize(Policy = "ReportView")]
    [HttpGet("orders")]
    public async Task<IActionResult> Orders([FromQuery] ReportDateRangeQueryDTO query)
    {
        var (from, to) = NormalizeRange(query.From, query.To);
        var orders = await FilterOrdersByDate(_db.DonHang.AsNoTracking(), from, to)
            .Select(x => new { x.NgayTao, x.TrangThai })
            .ToListAsync();

        return Ok(new OrderReportDTO
        {
            Total = orders.Count,
            ByStatus = OrderStatuses.All
                .Select(status => new OrderStatusReportItemDTO
                {
                    Status = status,
                    Count = orders.Count(x => x.TrangThai == status)
                })
                .ToList(),
            ByDay = orders
                .GroupBy(x => x.NgayTao.Date)
                .Select(g => new OrderDayReportItemDTO
                {
                    Date = g.Key,
                    Total = g.Count(),
                    Pending = g.Count(x => x.TrangThai == OrderStatuses.Pending),
                    Processing = g.Count(x => x.TrangThai == OrderStatuses.Processing),
                    Done = g.Count(x => x.TrangThai == OrderStatuses.Done),
                    Cancel = g.Count(x => x.TrangThai == OrderStatuses.Cancel)
                })
                .OrderBy(x => x.Date)
                .ToList()
        });
    }

    [Authorize(Policy = "ReportView")]
    [HttpGet("top-products")]
    public async Task<IActionResult> TopProducts([FromQuery] TopProductReportQueryDTO query)
    {
        var sortBy = NormalizeSortBy(query.SortBy);
        var (from, to) = NormalizeRange(query.From, query.To);

        var itemsQuery = _db.ChiTietDonHang.AsNoTracking()
            .Where(x => x.DonHang != null &&
                        x.DonHang.TrangThai == OrderStatuses.Done &&
                        x.DonHang.NgayTao >= from &&
                        x.DonHang.NgayTao <= to)
            .GroupBy(x => new
            {
                x.MaSanPham,
                x.SanPham.TenSanPham,
                CategoryName = x.SanPham.DanhMuc == null ? Uncategorized : x.SanPham.DanhMuc.TenDanhMuc
            })
            .Select(g => new TopProductReportItemDTO
            {
                ProductId = g.Key.MaSanPham,
                ProductName = g.Key.TenSanPham,
                CategoryName = g.Key.CategoryName,
                SoldQuantity = g.Sum(x => x.SoLuong),
                Revenue = g.Sum(x => x.SoLuong * x.DonGia)
            });

        var items = sortBy == "quantity"
            ? await itemsQuery.OrderByDescending(x => x.SoldQuantity).ThenByDescending(x => x.Revenue).Take(query.Take).ToListAsync()
            : await itemsQuery.OrderByDescending(x => x.Revenue).ThenByDescending(x => x.SoldQuantity).Take(query.Take).ToListAsync();

        return Ok(new TopProductReportDTO { Items = items });
    }

    [Authorize(Policy = "ReportView")]
    [HttpGet("logs")]
    public async Task<IActionResult> GetLogs()
    {
        var orderLogs = await _db.DonHang.AsNoTracking()
            .OrderByDescending(x => x.NgayTao)
            .Take(40)
            .Select(x => new
            {
                Title = $"Don hang DH-{x.MaDonHang:0000}",
                Description = $"Trang thai hien tai: {x.TrangThai}, tong tien {x.TongTien:N0} VND.",
                Time = x.NgayTao,
                Tag = "Don hang",
                Icon = "DH",
                Level = x.TrangThai == OrderStatuses.Cancel ? "danger" : x.TrangThai == OrderStatuses.Done ? "success" : "info"
            })
            .ToListAsync();

        var stockLogs = await _db.GiaoDichKho.AsNoTracking()
            .Include(x => x.SanPham)
            .OrderByDescending(x => x.ThoiGian)
            .Take(40)
            .Select(x => new
            {
                Title = $"{x.Loai} SP-{x.MaSanPham:0000}",
                Description = $"{x.SanPham.TenSanPham}: {(x.SoLuong > 0 ? "+" : string.Empty)}{x.SoLuong} san pham. {x.GhiChu}",
                Time = x.ThoiGian,
                Tag = "Kho hang",
                Icon = "K",
                Level = x.SoLuong < 0 ? "warning" : "success"
            })
            .ToListAsync();

        return Ok(orderLogs
            .Concat(stockLogs)
            .OrderByDescending(x => x.Time)
            .Take(100)
            .ToList());
    }

    [Authorize(Policy = "RevenueView")]
    [HttpGet("revenue-by-date")]
    public async Task<IActionResult> RevenueByDate([FromQuery] ReportDateRangeQueryDTO query)
    {
        var (from, to) = NormalizeRange(query.From, query.To);
        var result = await _db.DonHang.AsNoTracking()
            .Where(x => x.TrangThai == OrderStatuses.Done &&
                        x.NgayTao >= from &&
                        x.NgayTao <= to)
            .GroupBy(x => x.NgayTao.Date)
            .Select(g => new
            {
                Date = g.Key,
                Total = g.Sum(x => x.ChiTietDonHang.Sum(i => i.SoLuong * i.DonGia))
            })
            .OrderByDescending(x => x.Date)
            .ToListAsync();

        return Ok(result);
    }

    private static (DateTime From, DateTime To) NormalizeRange(DateTime? from, DateTime? to)
    {
        var normalizedFrom = NormalizeUtc(from) ?? DateTime.UtcNow.Date.AddDays(-29);
        var normalizedTo = NormalizeUtc(to) ?? DateTime.UtcNow;
        if (normalizedFrom > normalizedTo)
            throw new ValidationApiException("Khoang thoi gian bao cao khong hop le.");
        return (normalizedFrom, normalizedTo);
    }

    private static DateTime? NormalizeUtc(DateTime? value)
    {
        if (!value.HasValue)
            return null;
        return value.Value.Kind switch
        {
            DateTimeKind.Utc => value.Value,
            DateTimeKind.Local => value.Value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
        };
    }

    private static IQueryable<DonHang> FilterOrdersByDate(IQueryable<DonHang> query, DateTime from, DateTime to) =>
        query.Where(x => x.NgayTao >= from && x.NgayTao <= to);

    private static IQueryable<VanChuyen> FilterShippingByDate(IQueryable<VanChuyen> query, DateTime from, DateTime to) =>
        query.Where(x => x.NgayTao >= from && x.NgayTao <= to);

    private static string NormalizeGroupBy(string? groupBy)
    {
        var normalized = string.IsNullOrWhiteSpace(groupBy)
            ? "day"
            : groupBy.Trim().ToLowerInvariant();
        return normalized is "day" or "month" or "year"
            ? normalized
            : throw new ValidationApiException("groupBy chi ho tro day, month hoac year.");
    }

    private static string NormalizeSortBy(string? sortBy)
    {
        var normalized = string.IsNullOrWhiteSpace(sortBy)
            ? "revenue"
            : sortBy.Trim().ToLowerInvariant();
        return normalized is "revenue" or "quantity"
            ? normalized
            : throw new ValidationApiException("sortBy chi ho tro revenue hoac quantity.");
    }

    private static string BuildPeriod(DateTime value, string groupBy) =>
        groupBy switch
        {
            "year" => value.ToString("yyyy"),
            "month" => value.ToString("yyyy-MM"),
            _ => value.ToString("yyyy-MM-dd")
        };

    private static int CountStatus(IEnumerable<dynamic> items, string status) =>
        items.FirstOrDefault(x => x.Status == status)?.Count ?? 0;

    private static int CountShippingStatus(IEnumerable<dynamic> items, string status) =>
        items.FirstOrDefault(x => x.Status == status)?.Count ?? 0;
}
