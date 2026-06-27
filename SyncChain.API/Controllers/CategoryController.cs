using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SyncChain.API.Data;
using SyncChain.API.DTOs.Category;
using SyncChain.API.Models;

namespace SyncChain.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoryController : ControllerBase
{
    private readonly AppDbContext _db;

    public CategoryController(AppDbContext db)
    {
        _db = db;
    }

    // Lay danh sach danh muc san pham.
    [Authorize]
    [HttpGet]
    public IActionResult GetAll()
    {
        var categories = _db.DanhMucSanPham
            .OrderBy(x => x.TenDanhMuc)
            .Select(x => new
            {
                x.MaDanhMuc,
                x.TenDanhMuc,
                x.MoTa,
                x.IsActive,
                ProductCount = _db.SanPham.Count(sp => sp.MaDanhMuc == x.MaDanhMuc)
            })
            .ToList();

        return Ok(categories);
    }

    // Lay chi tiet mot danh muc.
    [Authorize]
    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        var category = _db.DanhMucSanPham
            .Where(x => x.MaDanhMuc == id)
            .Select(x => new
            {
                x.MaDanhMuc,
                x.TenDanhMuc,
                x.MoTa,
                x.IsActive,
                ProductCount = _db.SanPham.Count(sp => sp.MaDanhMuc == x.MaDanhMuc)
            })
            .FirstOrDefault();

        if (category == null)
            return NotFound("Danh muc khong ton tai");

        return Ok(category);
    }

    // Tao danh muc moi.
    [Authorize(Policy = "ManagerOrAdmin")]
    [HttpPost]
    public IActionResult Create(CreateCategoryDTO dto)
    {
        var name = NormalizeName(dto.TenDanhMuc);
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Ten danh muc khong duoc de trong");

        if (CategoryNameExists(name))
            return BadRequest("Ten danh muc da ton tai");

        var category = new DanhMucSanPham
        {
            TenDanhMuc = name,
            MoTa = dto.MoTa?.Trim() ?? string.Empty,
            IsActive = true
        };

        _db.DanhMucSanPham.Add(category);
        _db.SaveChanges();

        return Ok(category);
    }

    // Cap nhat danh muc.
    [Authorize(Policy = "ManagerOrAdmin")]
    [HttpPut("{id}")]
    public IActionResult Update(int id, UpdateCategoryDTO dto)
    {
        var category = _db.DanhMucSanPham.Find(id);
        if (category == null)
            return NotFound("Danh muc khong ton tai");

        var name = NormalizeName(dto.TenDanhMuc);
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Ten danh muc khong duoc de trong");

        if (CategoryNameExists(name, id))
            return BadRequest("Ten danh muc da ton tai");

        category.TenDanhMuc = name;
        category.MoTa = dto.MoTa?.Trim() ?? string.Empty;
        category.IsActive = dto.IsActive;
        _db.SaveChanges();

        return Ok(category);
    }

    // Bat/tat danh muc.
    [Authorize(Policy = "ManagerOrAdmin")]
    [HttpPut("{id}/active")]
    public IActionResult SetActive(int id, SetCategoryActiveDTO dto)
    {
        var category = _db.DanhMucSanPham.Find(id);
        if (category == null)
            return NotFound("Danh muc khong ton tai");

        category.IsActive = dto.IsActive;
        _db.SaveChanges();

        return Ok(category);
    }

    // Xoa mem danh muc de khong lam mat lien ket san pham cu.
    [Authorize(Policy = "ManagerOrAdmin")]
    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        var category = _db.DanhMucSanPham.Find(id);
        if (category == null)
            return NotFound("Danh muc khong ton tai");

        if (_db.SanPham.Any(x => x.MaDanhMuc == id))
        {
            category.IsActive = false;
            _db.SaveChanges();
            return Ok(new
            {
                message = "Danh muc dang co san pham nen da duoc vo hieu hoa",
                category.MaDanhMuc,
                category.TenDanhMuc,
                category.IsActive
            });
        }

        _db.DanhMucSanPham.Remove(category);
        _db.SaveChanges();
        return Ok("Da xoa danh muc");
    }

    // Lay san pham thuoc danh muc.
    [Authorize(Policy = "ProductRead")]
    [HttpGet("{id}/products")]
    public IActionResult GetProducts(int id)
    {
        var category = _db.DanhMucSanPham.Find(id);
        if (category == null)
            return NotFound("Danh muc khong ton tai");

        var products = _db.SanPham
            .Include(x => x.DanhMuc)
            .Where(x => x.MaDanhMuc == id)
            .OrderBy(x => x.TenSanPham)
            .Select(x => new
            {
                x.MaSanPham,
                x.TenSanPham,
                x.GiaBan,
                x.GiaNhap,
                x.SoLuongTon,
                x.MucTonThap,
                x.TrangThai,
                x.HinhAnhUrl,
                x.MoTa,
                x.MaDanhMuc,
                DanhMuc = new
                {
                    category.MaDanhMuc,
                    category.TenDanhMuc,
                    category.MoTa,
                    category.IsActive
                }
            })
            .ToList();

        return Ok(products);
    }

    private bool CategoryNameExists(string name, int? exceptId = null)
    {
        return _db.DanhMucSanPham.Any(x =>
            x.TenDanhMuc.ToLower() == name.ToLower() &&
            (!exceptId.HasValue || x.MaDanhMuc != exceptId.Value));
    }

    private static string NormalizeName(string? name)
    {
        return name?.Trim() ?? string.Empty;
    }
}
