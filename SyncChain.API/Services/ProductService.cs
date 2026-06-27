using Microsoft.EntityFrameworkCore;
using SyncChain.API.Data;
using SyncChain.API.DTOs.Inventory;
using SyncChain.API.DTOs.Product;
using SyncChain.API.Models;

namespace SyncChain.API.Services;

public class ProductService
{
    private readonly AppDbContext _db;
    private readonly InventoryService _inventory;
    private readonly IAuditService _audit;

    public ProductService(AppDbContext db, InventoryService inventory, IAuditService audit)
    {
        _db = db;
        _inventory = inventory;
        _audit = audit;
    }

    // Lấy tất cả sản phẩm trong kho.
    public List<ProductResponseDTO> GetAll(int? categoryId = null)
    {
        if (categoryId.HasValue &&
            !_db.DanhMucSanPham.Any(x => x.MaDanhMuc == categoryId.Value))
        {
            throw new InvalidOperationException("Danh muc khong ton tai");
        }

        var query = _db.SanPham
            .Include(x => x.DanhMuc)
            .AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(x => x.MaDanhMuc == categoryId.Value);

        var now = DateTime.UtcNow;
        var currentMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var nextMonthStart = currentMonthStart.AddMonths(1);
        var previousMonthStart = currentMonthStart.AddMonths(-1);
        var sales = _db.ChiTietDonHang
            .AsNoTracking()
            .Where(x => x.DonHang != null &&
                        x.DonHang.TrangThai != "cancel" &&
                        x.DonHang.NgayTao >= previousMonthStart &&
                        x.DonHang.NgayTao < nextMonthStart)
            .GroupBy(x => x.MaSanPham)
            .Select(g => new
            {
                ProductId = g.Key,
                CurrentMonth = g.Where(x => x.DonHang!.NgayTao >= currentMonthStart)
                    .Sum(x => x.SoLuong),
                PreviousMonth = g.Where(x => x.DonHang!.NgayTao < currentMonthStart)
                    .Sum(x => x.SoLuong)
            })
            .ToDictionary(x => x.ProductId);

        return query
            .OrderBy(x => x.MaSanPham)
            .AsEnumerable()
            .Select(x =>
            {
                sales.TryGetValue(x.MaSanPham, out var performance);
                return ToProductResponse(
                    x,
                    performance?.CurrentMonth ?? 0,
                    performance?.PreviousMonth ?? 0);
            })
            .ToList();
    }

    // Tìm sản phẩm theo mã, báo lỗi nếu không có.
    public ProductResponseDTO GetById(int id)
    {
        var sp = _db.SanPham
            .Include(x => x.DanhMuc)
            .FirstOrDefault(x => x.MaSanPham == id);
        if (sp == null) throw new KeyNotFoundException("Khong tim thay san pham");

        return ToProductResponse(sp);
    }

    // Tạo sản phẩm và tự tính trạng thái theo tồn kho.
    public ProductResponseDTO Create(CreateProductDTO dto)
    {
        ValidateProduct(dto.TenSanPham, dto.GiaBan, dto.GiaNhap, dto.SoLuongTon);
        ValidateCategory(dto.MaDanhMuc);

        var sp = new SanPham
        {
            TenSanPham = dto.TenSanPham,
            GiaBan = dto.GiaBan,
            GiaNhap = dto.GiaNhap,
            SoLuongTon = dto.SoLuongTon,
            TonKhoBanDau = dto.SoLuongTon,
            HinhAnhUrl = dto.HinhAnhUrl,
            MoTa = dto.MoTa,
            MaDanhMuc = dto.MaDanhMuc,
            TrangThai = BuildStatus(dto.SoLuongTon)
        };

        using var transaction = _db.Database.BeginTransaction();
        _db.SanPham.Add(sp);
        _db.SaveChanges();
        _audit.AddSuccess(
            AuditActions.Create,
            "SanPham",
            sp.MaSanPham.ToString(),
            after: ProductAuditValue(sp));
        _db.SaveChanges();
        transaction.Commit();

        return GetById(sp.MaSanPham);
    }

    // Cập nhật sản phẩm, giá và trạng thái bán.
    public ProductResponseDTO Update(int id, UpdateProductDTO dto)
    {
        var sp = _db.SanPham.Find(id);
        if (sp == null) throw new KeyNotFoundException("Khong tim thay san pham");

        ValidateProduct(dto.TenSanPham, dto.GiaBan, dto.GiaNhap, dto.SoLuongTon);
        ValidateCategory(dto.MaDanhMuc);

        if (dto.SoLuongTon != sp.SoLuongTon)
        {
            throw new InvalidOperationException(
                "Khong duoc sua ton kho truc tiep. Hay dung API dieu chinh ton kho");
        }

        var before = ProductAuditValue(sp);
        sp.TenSanPham = dto.TenSanPham;
        sp.GiaBan = dto.GiaBan;
        sp.GiaNhap = dto.GiaNhap;
        sp.HinhAnhUrl = dto.HinhAnhUrl;
        sp.MoTa = dto.MoTa;
        sp.MaDanhMuc = dto.MaDanhMuc;
        sp.TrangThai = BuildStatus(dto.SoLuongTon, dto.TrangThai ?? sp.TrangThai);

        _audit.AddSuccess(
            AuditActions.Update,
            "SanPham",
            id.ToString(),
            before,
            ProductAuditValue(sp));
        _db.SaveChanges();

        return GetById(sp.MaSanPham);
    }

    // Xóa sản phẩm khỏi database.
    public void Delete(int id)
    {
        var sp = _db.SanPham.Find(id);
        if (sp == null) throw new KeyNotFoundException("Khong tim thay san pham");

        if (_db.GiaoDichKho.Any(x => x.MaSanPham == id))
        {
            throw new InvalidOperationException(
                "Khong the xoa san pham da co lich su giao dich kho");
        }

        var before = ProductAuditValue(sp);
        _db.SanPham.Remove(sp);
        _audit.AddSuccess(
            AuditActions.Delete,
            "SanPham",
            id.ToString(),
            before: before);
        _db.SaveChanges();
    }

    // Nhập thêm hàng và ghi lịch sử nhập kho.
    public async Task<InventoryChangeResultDTO> ImportStockAsync(
        int id,
        int quantity,
        int? userId,
        string note)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();
        var result = await _inventory.IncreaseStockAsync(
            id,
            quantity,
            InventoryTransactionTypes.Receipt,
            userId,
            string.IsNullOrWhiteSpace(note) ? "Nhap kho nhanh" : note);
        _audit.AddSuccess(
            AuditActions.InventoryAdjustment,
            "SanPham",
            id.ToString(),
            before: new { stock = result.TonTruoc },
            after: new { stock = result.TonSau },
            metadata: new { operation = "QUICK_RECEIPT", quantity });
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
        return result;
    }

    // Lấy các giao dịch nhập kho gần đây cho trang nhập hàng.
    public List<object> GetImportHistory()
    {
        return _db.GiaoDichKho
            .Include(x => x.SanPham)
            .Where(x => x.Loai == InventoryTransactionTypes.Receipt)
            .OrderByDescending(x => x.ThoiGian)
            .Take(50)
            .Select(x => new
            {
                x.MaGiaoDich,
                x.MaSanPham,
                TenSanPham = x.SanPham.TenSanPham,
                x.SoLuong,
                x.ThoiGian,
                x.MaNguoiDung,
                x.GhiChu,
                x.MaPhieuNhap,
                x.NguonNhap,
                DonGiaNhap = x.SanPham.GiaNhap,
                ThanhTien = x.SanPham.GiaNhap * x.SoLuong
            })
            .ToList<object>();
    }

    // Đổi trạng thái sản phẩm, ép ngừng bán nếu hết hàng.
    public SanPham UpdateStatus(int id, string status)
    {
        var sp = _db.SanPham.Find(id);
        if (sp == null) throw new Exception("Khong tim thay san pham");

        if (status != "Hoat dong" && status != "Ngung ban")
            throw new InvalidOperationException("Trang thai san pham khong hop le");

        if (sp.SoLuongTon <= 0)
            status = "Ngung ban";

        var oldStatus = sp.TrangThai;
        sp.TrangThai = status;
        _audit.AddSuccess(
            AuditActions.StatusChange,
            "SanPham",
            id.ToString(),
            before: new { status = oldStatus },
            after: new { status });
        _db.SaveChanges();
        return sp;
    }

    // Tổng hợp chi tiết sản phẩm, doanh thu và lịch sử kho/bán.
    public object GetDetail(int id)
    {
        var sp = GetById(id);
        var soldLines = _db.ChiTietDonHang
            .Include(x => x.DonHang)
            .Where(x => x.MaSanPham == id && x.DonHang != null && x.DonHang.TrangThai != "cancel");
        var soldCount = soldLines.Sum(x => (int?)x.SoLuong) ?? 0;
        var revenue = soldLines.Sum(x => (decimal?)(x.SoLuong * x.DonGia)) ?? 0;
        var now = DateTime.UtcNow;
        var currentMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var previousMonthStart = currentMonthStart.AddMonths(-1);
        var nextMonthStart = currentMonthStart.AddMonths(1);
        var currentMonthSold = soldLines
            .Where(x => x.DonHang!.NgayTao >= currentMonthStart &&
                        x.DonHang.NgayTao < nextMonthStart)
            .Sum(x => (int?)x.SoLuong) ?? 0;
        var previousMonthSold = soldLines
            .Where(x => x.DonHang!.NgayTao >= previousMonthStart &&
                        x.DonHang.NgayTao < currentMonthStart)
            .Sum(x => (int?)x.SoLuong) ?? 0;
        var performancePercent = previousMonthSold == 0
            ? currentMonthSold > 0 ? 100m : 0m
            : Math.Round((currentMonthSold - previousMonthSold) * 100m / previousMonthSold, 1);

        var stockHistory = _db.GiaoDichKho
            .Where(x => x.MaSanPham == id)
            .OrderByDescending(x => x.ThoiGian)
            .Take(20)
            .Select(x => new
            {
                x.ThoiGian,
                x.Loai,
                x.SoLuong,
                x.MaNguoiDung,
                x.GhiChu
            })
            .ToList();

        var salesHistory = _db.ChiTietDonHang
            .Include(x => x.DonHang)
            .Where(x => x.MaSanPham == id && x.DonHang != null && x.DonHang.TrangThai != "cancel")
            .OrderByDescending(x => x.DonHang!.NgayTao)
            .Take(20)
            .Select(x => new
            {
                ThoiGian = x.DonHang!.NgayTao,
                Loai = InventoryTransactionTypes.OrderIssue,
                SoLuong = -x.SoLuong,
                MaNguoiDung = (int?)x.DonHang.MaNguoiDung,
                GhiChu = $"Don hang #{x.MaDonHang}"
            })
            .ToList();

        return new
        {
            Product = sp,
            SoldCount = soldCount,
            Revenue = revenue,
            CurrentMonthSold = currentMonthSold,
            PreviousMonthSold = previousMonthSold,
            PerformancePercent = performancePercent,
            ReviewCount = 0,
            AverageRating = 0m,
            StockHistory = stockHistory.Concat(salesHistory)
                .OrderByDescending(x => x.ThoiGian)
                .Take(20)
                .ToList()
        };
    }

    // Tính trạng thái sản phẩm theo số lượng tồn.
    private static string BuildStatus(int stockQuantity, string? requestedStatus = null)
    {
        if (stockQuantity <= 0)
            return "Ngung ban";

        return requestedStatus == "Ngung ban" ? "Ngung ban" : "Hoat dong";
    }

    // Kiểm tra giá bán và giá nhập hợp lệ.
    private static void ValidateProduct(
        string productName,
        decimal salePrice,
        decimal importPrice,
        int stockQuantity)
    {
        if (string.IsNullOrWhiteSpace(productName))
            throw new InvalidOperationException("Ten san pham khong duoc de trong");

        if (salePrice < 0)
            throw new InvalidOperationException("Gia ban khong hop le");

        if (importPrice < 0)
            throw new InvalidOperationException("Gia nhap khong hop le");

        if (stockQuantity < 0)
            throw new InvalidOperationException("So luong ton khong hop le");
    }

    private void ValidateCategory(int? categoryId)
    {
        if (!categoryId.HasValue)
            return;

        var category = _db.DanhMucSanPham.FirstOrDefault(x => x.MaDanhMuc == categoryId.Value);
        if (category == null)
            throw new InvalidOperationException("Danh muc khong ton tai");

        if (!category.IsActive)
            throw new InvalidOperationException("Danh muc da bi tat");
    }

    private static ProductResponseDTO ToProductResponse(
        SanPham sp,
        int currentMonthSold = 0,
        int previousMonthSold = 0)
    {
        var performancePercent = previousMonthSold == 0
            ? currentMonthSold > 0 ? 100m : 0m
            : Math.Round((currentMonthSold - previousMonthSold) * 100m / previousMonthSold, 1);

        return new ProductResponseDTO
        {
            MaSanPham = sp.MaSanPham,
            TenSanPham = sp.TenSanPham,
            GiaBan = sp.GiaBan,
            GiaNhap = sp.GiaNhap,
            SoLuongTon = sp.SoLuongTon,
            TonKhoBanDau = sp.TonKhoBanDau,
            MucTonThap = sp.MucTonThap,
            TrangThai = sp.TrangThai,
            HinhAnhUrl = sp.HinhAnhUrl,
            MoTa = sp.MoTa,
            MaDanhMuc = sp.MaDanhMuc,
            DaBanThangNay = currentMonthSold,
            DaBanThangTruoc = previousMonthSold,
            HieuSuatPhanTram = performancePercent,
            DanhMuc = sp.DanhMuc == null ? null : new ProductCategoryResponseDTO
            {
                MaDanhMuc = sp.DanhMuc.MaDanhMuc,
                TenDanhMuc = sp.DanhMuc.TenDanhMuc,
                MoTa = sp.DanhMuc.MoTa,
                IsActive = sp.DanhMuc.IsActive
            }
        };
    }

    private static object ProductAuditValue(SanPham sp) => new
    {
        sp.TenSanPham,
        sp.GiaBan,
        sp.GiaNhap,
        sp.SoLuongTon,
        sp.TrangThai,
        sp.MaDanhMuc,
        sp.HinhAnhUrl,
        sp.MoTa
    };
}
