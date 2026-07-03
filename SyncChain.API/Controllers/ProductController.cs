using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SyncChain.API.Data;
using SyncChain.API.DTOs.Product;
using SyncChain.API.Services;

namespace SyncChain.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private readonly ProductService _service;
    private readonly IWebHostEnvironment _environment;
    private readonly AppDbContext _db;

    public ProductController(ProductService service, IWebHostEnvironment environment, AppDbContext db)
    {
        _service = service;
        _environment = environment;
        _db = db;
    }

    // Trả về sản phẩm, hỗ trợ search và filter.
    [Authorize]
    [HttpGet]
    public IActionResult GetAll(
        [FromQuery] string? search = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] bool? inStockOnly = null,
        [FromQuery] string? sort = null)
    {
        var query = _db.SanPham.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => EF.Functions.ILike(x.TenSanPham, $"%{search}%"));

        if (minPrice.HasValue)
            query = query.Where(x => x.GiaBan >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(x => x.GiaBan <= maxPrice.Value);

        if (inStockOnly == true)
            query = query.Where(x => x.SoLuongTon > 0 && x.TrangThai != "Ngung ban");

        query = sort switch
        {
            "price_asc"  => query.OrderBy(x => x.GiaBan),
            "price_desc" => query.OrderByDescending(x => x.GiaBan),
            "newest"     => query.OrderByDescending(x => x.MaSanPham),
            _            => query.OrderBy(x => x.MaSanPham)
        };

        // Nhân sự nội bộ thấy đầy đủ (kèm giá nhập); khách hàng chỉ thấy dữ liệu công khai.
        if (IsInternalRole(GetRole()))
            return Ok(query.ToList());

        return Ok(query.Select(x => new
        {
            x.MaSanPham, x.TenSanPham, x.GiaBan, x.SoLuongTon,
            x.MucTonThap, x.TrangThai, x.HinhAnhUrl, x.MoTa
        }).ToList());
    }

    // Lấy thông tin cơ bản của một sản phẩm.
    [Authorize]
    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        var sp = _service.GetById(id);
        if (IsInternalRole(GetRole()))
            return Ok(sp);

        return Ok(new
        {
            sp.MaSanPham, sp.TenSanPham, sp.GiaBan, sp.SoLuongTon,
            sp.MucTonThap, sp.TrangThai, sp.HinhAnhUrl, sp.MoTa
        });
    }

    // Lấy chi tiết sản phẩm kèm thống kê và lịch sử kho.
    [Authorize]
    [HttpGet("{id}/detail")]
    public IActionResult GetDetail(int id)
    {
        if (IsInternalRole(GetRole()))
            return Ok(_service.GetDetail(id));

        // Khách hàng: ẩn giá nhập, doanh thu và lịch sử kho nội bộ.
        var sp = _service.GetById(id);
        var soldCount = _db.ChiTietDonHang
            .Where(x => x.MaSanPham == id && x.DonHang != null && x.DonHang.TrangThai != "Cancelled")
            .Sum(x => (int?)x.SoLuong) ?? 0;

        return Ok(new
        {
            Product = new
            {
                sp.MaSanPham, sp.TenSanPham, sp.GiaBan, sp.SoLuongTon,
                sp.MucTonThap, sp.TrangThai, sp.HinhAnhUrl, sp.MoTa
            },
            SoldCount = soldCount,
            Revenue = 0m,
            StockHistory = Array.Empty<object>()
        });
    }

    // Tạo sản phẩm mới.
    [Authorize(Policy = "ProductWrite")]
    [HttpPost]
    public IActionResult Create(CreateProductDTO dto)
    {
        return Ok(_service.Create(dto));
    }

    // Cập nhật thông tin sản phẩm.
    [Authorize(Policy = "ProductWrite")]
    [HttpPut("{id}")]
    public IActionResult Update(int id, UpdateProductDTO dto)
    {
        return Ok(_service.Update(id, dto));
    }

    // Nhập thêm tồn kho và ghi lịch sử giao dịch kho.
    [Authorize(Policy = "ProductWrite")]
    [HttpPost("{id}/import")]
    public IActionResult ImportStock(int id, ImportStockDTO dto)
    {
        return Ok(_service.ImportStock(id, dto.SoLuong, GetUserId(), dto.GhiChu));
    }

    // Lấy danh sách giao dịch nhập kho gần đây.
    [Authorize(Policy = "ProductWrite")]
    [HttpGet("imports")]
    public IActionResult GetImportHistory()
    {
        return Ok(_service.GetImportHistory());
    }

    // Cập nhật trạng thái bán/ngừng bán của sản phẩm.
    [Authorize(Policy = "ProductWrite")]
    [HttpPut("{id}/status")]
    public IActionResult UpdateStatus(int id, string status)
    {
        return Ok(_service.UpdateStatus(id, status));
    }

    // Nhận file ảnh, lưu vào wwwroot và trả đường dẫn ảnh.
    [Authorize(Policy = "ProductWrite")]
    [HttpPost("upload-image")]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File anh khong hop le");

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
            return BadRequest("Chi ho tro file anh jpg, jpeg, png, webp, gif");

        var webRoot = _environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = Path.Combine(_environment.ContentRootPath, "wwwroot");
        }

        var uploadRoot = Path.Combine(webRoot, "uploads", "products");
        Directory.CreateDirectory(uploadRoot);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(uploadRoot, fileName);

        await using (var stream = System.IO.File.Create(filePath))
        {
            await file.CopyToAsync(stream);
        }

        return Ok(new { imageUrl = $"/uploads/products/{fileName}" });
    }

    // Xóa sản phẩm khỏi hệ thống.
    [Authorize(Policy = "ProductWrite")]
    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        _service.Delete(id);
        return Ok("Da xoa");
    }

    // Lấy user id từ token để gắn vào giao dịch kho.
    private int? GetUserId()
    {
        var claim = User.FindFirst("user_id")?.Value;
        return int.TryParse(claim, out var userId) ? userId : null;
    }

    // Đọc role hiện tại từ JWT.
    private string GetRole() => User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

    // Kiểm tra role thuộc nhóm nhân sự nội bộ (được xem dữ liệu nội bộ như giá nhập).
    private static bool IsInternalRole(string role) => role is "admin" or "manager" or "staff";
}
