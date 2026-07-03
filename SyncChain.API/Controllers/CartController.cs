using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncChain.API.DTOs.Cart;
using SyncChain.API.Services;

namespace SyncChain.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly CartService _service;

    public CartController(CartService service) => _service = service;

    [HttpGet]
    public IActionResult GetCart() => Ok(_service.GetCart(GetUserId()));

    [HttpPost("items")]
    public IActionResult AddItem([FromBody] CartItemDTO dto)
    {
        try { return Ok(_service.AddItem(GetUserId(), dto)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("items/{maSanPham}")]
    public IActionResult UpdateItem(int maSanPham, [FromQuery] int soLuong)
    {
        try { return Ok(_service.UpdateItem(GetUserId(), maSanPham, soLuong)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("items/{maSanPham}")]
    public IActionResult RemoveItem(int maSanPham)
    {
        try { return Ok(_service.RemoveItem(GetUserId(), maSanPham)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete]
    public IActionResult ClearCart()
    {
        _service.ClearCart(GetUserId());
        return Ok(new { message = "Đã xóa toàn bộ giỏ hàng" });
    }

    private int GetUserId()
    {
        var value = User.FindFirst("user_id")?.Value;
        if (!int.TryParse(value, out var id)) throw new Exception("Token không hợp lệ");
        return id;
    }
}
