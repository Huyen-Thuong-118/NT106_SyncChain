using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncChain.API.DTOs.Product;
using SyncChain.API.Services;

namespace SyncChain.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private readonly ProductService _service;
    private readonly IWebHostEnvironment _environment;

    public ProductController(ProductService service, IWebHostEnvironment environment)
    {
        _service = service;
        _environment = environment;
    }

    // Trả về toàn bộ sản phẩm cho màn hình quản lý/bán hàng.
    [Authorize]
    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok(_service.GetAll());
    }

    // Lấy thông tin cơ bản của một sản phẩm.
    [Authorize]
    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        return Ok(_service.GetById(id));
    }

    // Lấy chi tiết sản phẩm kèm thống kê và lịch sử kho.
    [Authorize]
    [HttpGet("{id}/detail")]
    public IActionResult GetDetail(int id)
    {
        return Ok(_service.GetDetail(id));
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
}
