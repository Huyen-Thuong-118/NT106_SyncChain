using SyncChain.API.Data;
using SyncChain.API.Models;
using SyncChain.API.DTOs;

public class OrderService
{
    private readonly AppDbContext _db;

    public OrderService(AppDbContext db)
    {
        _db = db;
    }

    public object CreateOrder(int userId, CreateOrderDTO dto)
    {
        decimal tongTien = 0;

        var order = new DonHang
        {
            MaNguoiDung = userId,
            NgayTao = DateTime.Now
        };

        _db.DonHang.Add(order);
        _db.SaveChanges(); // để có MaDonHang

        foreach (var item in dto.Items)
        {
            var product = _db.SanPham.Find(item.MaSanPham);

            if (product == null)
                throw new Exception("Sản phẩm không tồn tại");

            if (product.SoLuongTon < item.SoLuong)
                throw new Exception("Không đủ hàng");

            // 🔥 TRỪ KHO
            product.SoLuongTon -= item.SoLuong;

            var detail = new ChiTietDonHang
            {
                MaDonHang = order.MaDonHang,
                MaSanPham = product.MaSanPham,
                SoLuong = item.SoLuong,
                DonGia = product.GiaBan
            };

            tongTien += product.GiaBan * item.SoLuong;

            _db.ChiTietDonHang.Add(detail);
        }

        order.TongTien = tongTien;

        _db.SaveChanges();

        return new
        {
            message = "Tạo đơn thành công",
            order.MaDonHang,
            tongTien
        };
    }
}