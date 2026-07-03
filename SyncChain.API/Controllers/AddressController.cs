using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncChain.API.DTOs.Address;
using SyncChain.API.Services;

namespace SyncChain.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AddressController : ControllerBase
{
    private readonly AddressService _service;

    public AddressController(AddressService service) => _service = service;

    [HttpGet]
    public IActionResult GetAll() =>
        Ok(_service.GetAll(GetUserId()));

    [HttpPost]
    public IActionResult Create([FromBody] CreateAddressDTO dto)
    {
        try { return Ok(_service.Create(GetUserId(), dto)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("{id}")]
    public IActionResult Update(int id, [FromBody] UpdateAddressDTO dto)
    {
        try { return Ok(_service.Update(GetUserId(), id, dto)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        try
        {
            _service.Delete(GetUserId(), id);
            return Ok(new { message = "Xóa địa chỉ thành công" });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("{id}/default")]
    public IActionResult SetDefault(int id)
    {
        try { return Ok(_service.SetDefault(GetUserId(), id)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    private int GetUserId()
    {
        var value = User.FindFirst("user_id")?.Value;
        if (!int.TryParse(value, out var id)) throw new Exception("Token không hợp lệ");
        return id;
    }
}
