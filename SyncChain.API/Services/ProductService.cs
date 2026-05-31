using Microsoft.EntityFrameworkCore;
using SyncChain.API.Data;
using SyncChain.API.DTOs.Product;
using SyncChain.API.Models;

namespace SyncChain.API.Services;

public class ProductService
{
    private readonly AppDbContext _db;

    public ProductService(AppDbContext db)
    {
        _db = db;
    }

    // Lấy tất cả sản phẩm trong kho.
    public List<SanPham> GetAll()
    {
        return _db.SanPham.ToList();
    }

    // Tìm sản phẩm theo mã, báo lỗi nếu không có.
    public SanPham GetById(int id)
    {
        var sp = _db.SanPham.Find(id);
        if (sp == null) throw new Exception("Khong tim thay san pham");

        return sp;
    }

    // Tạo sản phẩm và tự tính trạng thái theo tồn kho.
    public SanPham Create(CreateProductDTO dto)
    {
        ValidatePricing(dto.GiaBan, dto.GiaNhap);

        var sp = new SanPham
        {
            TenSanPham = dto.TenSanPham,
            GiaBan = dto.GiaBan,
            GiaNhap = dto.GiaNhap,
            SoLuongTon = dto.SoLuongTon,
            HinhAnhUrl = dto.HinhAnhUrl,
            MoTa = dto.MoTa,
            TrangThai = BuildStatus(dto.SoLuongTon)
        };

        _db.SanPham.Add(sp);
        _db.SaveChanges();

        return sp;
    }

    // Cập nhật sản phẩm, giá và trạng thái bán.
    public SanPham Update(int id, UpdateProductDTO dto)
    {
        var sp = _db.SanPham.Find(id);
        if (sp == null) throw new Exception("Khong tim thay san pham");

        ValidatePricing(dto.GiaBan, dto.GiaNhap);

        sp.TenSanPham = dto.TenSanPham;
        sp.GiaBan = dto.GiaBan;
        sp.GiaNhap = dto.GiaNhap;
        sp.SoLuongTon = dto.SoLuongTon;
        sp.HinhAnhUrl = dto.HinhAnhUrl;
        sp.MoTa = dto.MoTa;
        sp.TrangThai = BuildStatus(dto.SoLuongTon, dto.TrangThai ?? sp.TrangThai);

        _db.SaveChanges();

        return sp;
    }

    // Xóa sản phẩm khỏi database.
    public void Delete(int id)
    {
        var sp = _db.SanPham.Find(id);
        if (sp == null) throw new Exception("Khong tim thay san pham");

        _db.SanPham.Remove(sp);
        _db.SaveChanges();
    }

    // Nhập thêm hàng và ghi lịch sử nhập kho.
    public SanPham ImportStock(int id, int quantity, int? userId, string note)
    {
        if (quantity <= 0)
            throw new InvalidOperationException("So luong nhap phai lon hon 0");

        var sp = _db.SanPham.Find(id);
        if (sp == null) throw new Exception("Khong tim thay san pham");

        sp.SoLuongTon += quantity;
        if (sp.SoLuongTon > 0 && sp.TrangThai == "Ngung ban")
            sp.TrangThai = "Hoat dong";

        _db.GiaoDichKho.Add(new GiaoDichKho
        {
            MaSanPham = id,
            Loai = "Nhap kho",
            SoLuong = quantity,
            MaNguoiDung = userId,
            GhiChu = string.IsNullOrWhiteSpace(note) ? "Nhap them hang" : note
        });

        _db.SaveChanges();
        return sp;
    }

    // Lấy các giao dịch nhập kho gần đây cho trang nhập hàng.
    public List<object> GetImportHistory()
    {
        return _db.GiaoDichKho
            .Include(x => x.SanPham)
            .Where(x => x.Loai == "Nhap kho")
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

        sp.TrangThai = status;
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
                Loai = "Xuat kho",
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
    private static void ValidatePricing(decimal salePrice, decimal importPrice)
    {
        if (salePrice < 0)
            throw new InvalidOperationException("Gia ban khong hop le");

        if (importPrice < 0)
            throw new InvalidOperationException("Gia nhap khong hop le");
    }
}
