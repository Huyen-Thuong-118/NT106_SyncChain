using SyncChain.API.Data;
using SyncChain.API.DTOs;
using SyncChain.API.Models;

namespace SyncChain.API.Services;

public class OrderService
{
    private readonly AppDbContext _db;

    public OrderService(AppDbContext db)
    {
        _db = db;
    }

    // Tạo đơn, trừ kho và ghi giao dịch xuất kho trong một transaction.
    public object CreateOrder(int userId, CreateOrderDTO dto)
    {
        if (dto.Items.Count == 0)
            throw new InvalidOperationException("Don hang phai co it nhat mot san pham");

        // Gộp các dòng trùng sản phẩm để kiểm tra tồn kho chính xác.
        var requestedItems = dto.Items
            .GroupBy(x => x.MaSanPham)
            .Select(g => new
            {
                MaSanPham = g.Key,
                SoLuong = g.Sum(x => x.SoLuong)
            })
            .ToList();

        if (requestedItems.Any(x => x.SoLuong <= 0))
            throw new InvalidOperationException("So luong san pham phai lon hon 0");

        var productIds = requestedItems.Select(x => x.MaSanPham).ToList();
        var products = _db.SanPham
            .Where(x => productIds.Contains(x.MaSanPham))
            .ToDictionary(x => x.MaSanPham);

        // Kiểm tra sản phẩm tồn tại, còn bán và đủ số lượng.
        foreach (var item in requestedItems)
        {
            if (!products.TryGetValue(item.MaSanPham, out var product))
                throw new InvalidOperationException($"San pham #{item.MaSanPham} khong ton tai");

            if (product.SoLuongTon <= 0 || product.TrangThai == "Ngung ban")
                throw new InvalidOperationException($"{product.TenSanPham} hien dang ngung ban hoac het hang");

            if (product.SoLuongTon < item.SoLuong)
                throw new InvalidOperationException($"{product.TenSanPham} chi con {product.SoLuongTon} trong kho");
        }

        // Dùng transaction để đơn hàng và tồn kho luôn cập nhật cùng nhau.
        using var transaction = _db.Database.BeginTransaction();

        decimal total = 0;
        var order = new DonHang
        {
            MaNguoiDung = userId,
            NgayTao = DateTime.Now,
            TrangThai = "pending"
        };

        _db.DonHang.Add(order);
        _db.SaveChanges();

        // Tạo chi tiết đơn hàng và ghi lịch sử xuất kho.
        foreach (var item in requestedItems)
        {
            var product = products[item.MaSanPham];
            product.SoLuongTon -= item.SoLuong;
            if (product.SoLuongTon <= 0)
            {
                product.SoLuongTon = 0;
                product.TrangThai = "Ngung ban";
            }

            var detail = new ChiTietDonHang
            {
                MaDonHang = order.MaDonHang,
                MaSanPham = product.MaSanPham,
                SoLuong = item.SoLuong,
                DonGia = product.GiaBan
            };

            total += product.GiaBan * item.SoLuong;
            _db.ChiTietDonHang.Add(detail);
            _db.GiaoDichKho.Add(new SyncChain.API.Models.GiaoDichKho
            {
                MaSanPham = product.MaSanPham,
                Loai = "Xuat kho",
                SoLuong = -item.SoLuong,
                MaNguoiDung = userId,
                GhiChu = $"Tao don hang #{order.MaDonHang}"
            });
        }

        order.TongTien = total;

        _db.SaveChanges();
        transaction.Commit();

        return new
        {
            message = "Tao don thanh cong",
            order.MaDonHang,
            tongTien = total
        };
    }
}
