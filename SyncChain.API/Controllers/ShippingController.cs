using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SyncChain.API.Data;
using SyncChain.API.DTOs.Shipping;
using SyncChain.API.Exceptions;
using SyncChain.API.Services;

namespace SyncChain.API.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class ShippingController : ControllerBase
{
    private readonly ShippingService _service;
    private readonly DeliveryEstimateService _estimateService;
    private readonly AppDbContext _db;

    public ShippingController(
        ShippingService service,
        DeliveryEstimateService estimateService,
        AppDbContext db)
    {
        _service = service;
        _estimateService = estimateService;
        _db = db;
    }

    [HttpPost("shipping/estimate")]
    public IActionResult Estimate(EstimateDeliveryDTO dto) =>
        Ok(_estimateService.Estimate(dto));

    [Authorize(Policy = "OrderManage")]
    [HttpPost("orders/{orderId:int}/shipping")]
    public async Task<IActionResult> Create(int orderId, CreateShippingDTO dto)
    {
        return Ok(await _service.CreateAsync(
            orderId, dto, GetRequiredUserId()));
    }

    [Authorize(Policy = "OrderManage")]
    [HttpPut("orders/{orderId:int}/shipping")]
    public async Task<IActionResult> Update(int orderId, UpdateShippingDTO dto)
    {
        return Ok(await _service.UpdateAsync(
            orderId, dto, GetRequiredUserId()));
    }

    [Authorize(Policy = "OrderManage")]
    [HttpPut("orders/{orderId:int}/shipping/status")]
    public async Task<IActionResult> UpdateStatus(int orderId, UpdateShippingStatusDTO dto)
    {
        return Ok(await _service.UpdateStatusAsync(
            orderId, dto, GetRequiredUserId()));
    }

    [HttpGet("orders/{orderId:int}/shipping")]
    public async Task<IActionResult> GetByOrder(int orderId)
    {
        if (!await CanViewOrderAsync(orderId))
            return Forbid();
        return Ok(await _service.GetByOrderAsync(orderId));
    }

    [HttpGet("shipping/tracking/{trackingNumber}")]
    public async Task<IActionResult> GetByTracking(string trackingNumber)
    {
        var normalizedTrackingNumber = trackingNumber.Trim();
        if (!IsInternalRole())
        {
            var ownerId = await _db.VanChuyen.AsNoTracking()
                .Where(x => x.MaVanDon == normalizedTrackingNumber)
                .Select(x => (int?)x.DonHang.MaNguoiDung)
                .FirstOrDefaultAsync();
            if (ownerId.HasValue && ownerId.Value != GetRequiredUserId())
                return Forbid();
        }
        return Ok(await _service.GetByTrackingAsync(normalizedTrackingNumber));
    }

    [HttpGet("orders/{orderId:int}/shipping/history")]
    public async Task<IActionResult> GetHistory(int orderId)
    {
        if (!await CanViewOrderAsync(orderId))
            return Forbid();
        return Ok(await _service.GetHistoryAsync(orderId));
    }

    private async Task<bool> CanViewOrderAsync(int orderId)
    {
        if (IsInternalRole())
            return true;
        return await _db.DonHang.AsNoTracking()
            .AnyAsync(x => x.MaDonHang == orderId && x.MaNguoiDung == GetRequiredUserId());
    }

    private int GetRequiredUserId()
    {
        var claim = User.FindFirst("user_id")?.Value;
        if (!int.TryParse(claim, out var userId))
            throw new AuthenticationApiException("Token khong co user_id hop le.");
        return userId;
    }

    private bool IsInternalRole()
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        return role is "staff" or "manager" or "admin";
    }
}
