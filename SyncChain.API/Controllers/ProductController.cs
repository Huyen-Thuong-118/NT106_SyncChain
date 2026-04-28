using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SyncChain.API.Services;
using SyncChain.API.DTOs.Product;

namespace SyncChain.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private readonly ProductService _service;

    public ProductController(ProductService service)
    {
        _service = service;
    }

    // 📦 GET ALL (ai login cũng xem được)
    [Authorize]
    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok(_service.GetAll());
    }

    // ➕ CREATE (admin)
    [Authorize(Roles = "admin")]
    [HttpPost]
    public IActionResult Create(CreateProductDTO dto)
    {
        var result = _service.Create(dto);
        return Ok(result);
    }

    // ✏️ UPDATE (admin + staff)
    [Authorize(Roles = "admin,staff")]
    [HttpPut("{id}")]
    public IActionResult Update(int id, UpdateProductDTO dto)
    {
        var result = _service.Update(id, dto);
        return Ok(result);
    }

    // ❌ DELETE (admin)
    [Authorize(Roles = "admin")]
    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        _service.Delete(id);
        return Ok("Đã xóa");
    }
}