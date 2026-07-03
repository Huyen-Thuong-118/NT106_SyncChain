using Microsoft.EntityFrameworkCore;
using SyncChain.API.Data;
using SyncChain.API.DTOs.Cart;
using SyncChain.API.Models;

namespace SyncChain.API.Services;

public class CartService
{
    private readonly AppDbContext _db;

    public CartService(AppDbContext db) => _db = db;

    public object GetCart(int userId)
    {
        var cart = GetOrCreateCart(userId);
        var items = _db.ChiTietGioHang
            .Include(x => x.SanPham)
            .Where(x => x.MaGioHang == cart.MaGioHang)
            .Select(x => new
            {
                x.MaChiTietGio,
                x.MaSanPham,
                x.SanPham.TenSanPham,
                x.SanPham.GiaBan,
                x.SanPham.HinhAnhUrl,
                x.SanPham.SoLuongTon,
                x.SanPham.TrangThai,
                x.SoLuong,
                ThanhTien = x.SanPham.GiaBan * x.SoLuong
            })
            .ToList();

        var total = items.Sum(x => x.ThanhTien);
        return new { cart.MaGioHang, Items = items, TongTien = total, SoLuong = items.Count };
    }

    public object AddItem(int userId, CartItemDTO dto)
    {
        if (dto.SoLuong <= 0)
            throw new Exception("Số lượng phải lớn hơn 0");

        var product = _db.SanPham.Find(dto.MaSanPham)
            ?? throw new KeyNotFoundException("Sản phẩm không tồn tại");

        if (product.TrangThai == "Ngung ban")
            throw new Exception("Sản phẩm đã ngừng kinh doanh");

        var cart = GetOrCreateCart(userId);

        var existing = _db.ChiTietGioHang
            .FirstOrDefault(x => x.MaGioHang == cart.MaGioHang && x.MaSanPham == dto.MaSanPham);

        var newQty = (existing?.SoLuong ?? 0) + dto.SoLuong;
        if (newQty > product.SoLuongTon)
            throw new Exception($"Chỉ còn {product.SoLuongTon} sản phẩm trong kho");

        if (existing != null)
        {
            existing.SoLuong = newQty;
        }
        else
        {
            _db.ChiTietGioHang.Add(new ChiTietGioHang
            {
                MaGioHang = cart.MaGioHang,
                MaSanPham = dto.MaSanPham,
                SoLuong = dto.SoLuong
            });
        }

        cart.NgayCapNhat = DateTime.UtcNow;
        _db.SaveChanges();
        return GetCart(userId);
    }

    public object UpdateItem(int userId, int maSanPham, int soLuong)
    {
        if (soLuong <= 0)
            throw new Exception("Số lượng phải lớn hơn 0");

        var cart = GetOrCreateCart(userId);
        var item = _db.ChiTietGioHang
            .FirstOrDefault(x => x.MaGioHang == cart.MaGioHang && x.MaSanPham == maSanPham)
            ?? throw new KeyNotFoundException("Sản phẩm không có trong giỏ");

        var product = _db.SanPham.Find(maSanPham)!;
        if (soLuong > product.SoLuongTon)
            throw new Exception($"Chỉ còn {product.SoLuongTon} sản phẩm trong kho");

        item.SoLuong = soLuong;
        cart.NgayCapNhat = DateTime.UtcNow;
        _db.SaveChanges();
        return GetCart(userId);
    }

    public object RemoveItem(int userId, int maSanPham)
    {
        var cart = GetOrCreateCart(userId);
        var item = _db.ChiTietGioHang
            .FirstOrDefault(x => x.MaGioHang == cart.MaGioHang && x.MaSanPham == maSanPham)
            ?? throw new KeyNotFoundException("Sản phẩm không có trong giỏ");

        _db.ChiTietGioHang.Remove(item);
        cart.NgayCapNhat = DateTime.UtcNow;
        _db.SaveChanges();
        return GetCart(userId);
    }

    public void ClearCart(int userId)
    {
        var cart = _db.GioHang.FirstOrDefault(x => x.MaNguoiDung == userId);
        if (cart == null) return;

        var items = _db.ChiTietGioHang.Where(x => x.MaGioHang == cart.MaGioHang).ToList();
        _db.ChiTietGioHang.RemoveRange(items);
        _db.SaveChanges();
    }

    private GioHang GetOrCreateCart(int userId)
    {
        var cart = _db.GioHang.FirstOrDefault(x => x.MaNguoiDung == userId);
        if (cart != null) return cart;

        cart = new GioHang { MaNguoiDung = userId };
        _db.GioHang.Add(cart);
        _db.SaveChanges();
        return cart;
    }
}
