using SyncChain.API.Data;
using SyncChain.API.Models;
using SyncChain.API.DTOs.Product;

namespace SyncChain.API.Services;

public class ProductService
{
    private readonly AppDbContext _db;

    public ProductService(AppDbContext db)
    {
        _db = db;
    }

    public List<SanPham> GetAll()
    {
        return _db.SanPham.ToList();
    }

    public SanPham Create(CreateProductDTO dto)
    {
        var sp = new SanPham
        {
            TenSanPham = dto.TenSanPham,
            GiaBan = dto.GiaBan,
            SoLuongTon = dto.SoLuongTon
        };

        _db.SanPham.Add(sp);
        _db.SaveChanges();

        return sp;
    }

    public SanPham Update(int id, UpdateProductDTO dto)
    {
        var sp = _db.SanPham.Find(id);
        if (sp == null) throw new Exception("Không tìm thấy sản phẩm");

        sp.TenSanPham = dto.TenSanPham;
        sp.GiaBan = dto.GiaBan;
        sp.SoLuongTon = dto.SoLuongTon;

        _db.SaveChanges();

        return sp;
    }

    public void Delete(int id)
    {
        var sp = _db.SanPham.Find(id);
        if (sp == null) throw new Exception("Không tìm thấy sản phẩm");

        _db.SanPham.Remove(sp);
        _db.SaveChanges();
    }
}