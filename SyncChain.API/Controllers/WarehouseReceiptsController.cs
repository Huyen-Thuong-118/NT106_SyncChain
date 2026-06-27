using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncChain.API.DTOs.WarehouseReceipt;
using SyncChain.API.Services;

namespace SyncChain.API.Controllers;

[ApiController]
[Route("api/warehouse-receipts")]
public class WarehouseReceiptsController : ControllerBase
{
    private readonly WarehouseReceiptService _service;

    public WarehouseReceiptsController(WarehouseReceiptService service)
    {
        _service = service;
    }

    [Authorize(Policy = "InventoryRead")]
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? source,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        return await HandleAsync(() =>
            _service.GetAllAsync(status, source, fromDate, toDate));
    }

    [Authorize(Policy = "InventoryRead")]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        return await HandleAsync(() => _service.GetByIdAsync(id));
    }

    [Authorize(Policy = "InventoryWrite")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateWarehouseReceiptDTO dto)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized("Token khong co thong tin nguoi dung");

        return await HandleAsync(
            () => _service.CreateAsync(dto, userId),
            result => CreatedAtAction(nameof(GetById), new { id = result.MaPhieuNhap }, result));
    }

    [Authorize(Policy = "InventoryWrite")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateWarehouseReceiptDTO dto)
    {
        return await HandleAsync(() => _service.UpdateAsync(id, dto));
    }

    [Authorize(Policy = "InventoryWrite")]
    [HttpPut("{id}/submit")]
    public async Task<IActionResult> Submit(int id)
    {
        return await HandleAsync(() => _service.SubmitAsync(id));
    }

    [Authorize(Policy = "InventoryApprove")]
    [HttpPut("{id}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized("Token khong co thong tin nguoi dung");

        return await HandleAsync(() => _service.ApproveAsync(id, userId));
    }

    [Authorize(Policy = "InventoryApprove")]
    [HttpPut("{id}/complete")]
    public async Task<IActionResult> Complete(int id)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized("Token khong co thong tin nguoi dung");

        return await HandleAsync(() => _service.CompleteAsync(id, userId));
    }

    [Authorize(Policy = "InventoryWrite")]
    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        return await HandleAsync(() => _service.CancelAsync(id));
    }

    [Authorize(Policy = "InventoryWrite")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _service.DeleteAsync(id);
            return Ok(new { message = "Da xoa phieu nhap" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private async Task<IActionResult> HandleAsync<T>(
        Func<Task<T>> action,
        Func<T, IActionResult>? success = null)
    {
        try
        {
            var result = await action();
            return success?.Invoke(result) ?? Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private bool TryGetUserId(out int userId)
    {
        var claim = User.FindFirst("user_id")?.Value;
        return int.TryParse(claim, out userId);
    }
}
