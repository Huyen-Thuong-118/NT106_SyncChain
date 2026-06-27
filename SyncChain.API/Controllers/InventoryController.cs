using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SyncChain.API.DTOs.Inventory;
using SyncChain.API.Services;

namespace SyncChain.API.Controllers;

[ApiController]
[Route("api/inventory")]
public class InventoryController : ControllerBase
{
    private readonly InventoryService _service;
    private readonly IAuthorizationService _authorization;

    public InventoryController(
        InventoryService service,
        IAuthorizationService authorization)
    {
        _service = service;
        _authorization = authorization;
    }

    [Authorize(Policy = "InventoryRead")]
    [HttpGet("products/{productId}")]
    public async Task<IActionResult> GetCurrentStock(int productId)
    {
        return await HandleAsync(() => _service.GetCurrentStockAsync(productId));
    }

    [Authorize(Policy = "InventoryRead")]
    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] int? productId,
        [FromQuery] string? transactionType,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        return await HandleAsync(() => _service.GetTransactionHistoryAsync(
            productId,
            transactionType,
            fromDate,
            toDate));
    }

    [Authorize(Policy = "InventoryWrite")]
    [HttpPost("adjustments")]
    public async Task<IActionResult> Adjust(InventoryAdjustmentDTO dto)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized(new { message = "Token khong co thong tin nguoi dung" });

        return await HandleAsync(() => _service.AdjustStockAsync(dto, userId));
    }

    [Authorize(Policy = "InventoryRead")]
    [HttpPost("reconcile")]
    public async Task<IActionResult> Reconcile(ReconcileInventoryDTO dto)
    {
        int? userId = null;
        if (dto.ApplyFix)
        {
            var authorization = await _authorization.AuthorizeAsync(
                User,
                resource: null,
                policyName: "InventoryApprove");
            if (!authorization.Succeeded)
                return Forbid();

            if (!TryGetUserId(out var parsedUserId))
                return Unauthorized(new { message = "Token khong co thong tin nguoi dung" });

            userId = parsedUserId;
        }

        return await HandleAsync(() => _service.ReconcileStockAsync(dto.ApplyFix, userId));
    }

    private async Task<IActionResult> HandleAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return Ok(await action());
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
