using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncChain.API.DTOs.WarehouseIssue;
using SyncChain.API.Services;

namespace SyncChain.API.Controllers;

[ApiController]
[Route("api/warehouse-issues")]
public class WarehouseIssuesController : ControllerBase
{
    private readonly WarehouseIssueService _service;

    public WarehouseIssuesController(WarehouseIssueService service)
    {
        _service = service;
    }

    [Authorize(Policy = "InventoryRead")]
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? reason,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        return await HandleAsync(() =>
            _service.GetAllAsync(status, reason, fromDate, toDate));
    }

    [Authorize(Policy = "InventoryRead")]
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory()
    {
        return Ok(await _service.GetHistoryAsync());
    }

    [Authorize(Policy = "InventoryRead")]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        return await HandleAsync(() => _service.GetByIdAsync(id));
    }

    [Authorize(Policy = "InventoryWrite")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateWarehouseIssueDTO dto)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(new { message = "Token khong co thong tin nguoi dung" });

        return await HandleAsync(
            () => _service.CreateAsync(dto, userId),
            result => CreatedAtAction(nameof(GetById), new { id = result.MaPhieuXuat }, result));
    }

    [Authorize(Policy = "InventoryWrite")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateWarehouseIssueDTO dto)
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
    [HttpPut("{id}/complete")]
    public async Task<IActionResult> Complete(int id)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(new { message = "Token khong co thong tin nguoi dung" });

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
            return Ok(new { message = "Da xoa phieu xuat" });
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
