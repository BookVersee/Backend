using System;
using System.Security.Claims;
using System.Threading.Tasks;
using BookManagement.Repository.Data;
using BookManagement.Service.Delivery;
using BookManagement.Service.Dtos;
using BookManagement.Service.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookManagement.Api.Controllers;

[ApiController]
[Route("api/delivery")]
public class DeliveryController : ControllerBase
{
    private readonly DeliveryService _deliveryService;
    private readonly AppDbContext _db;

    public DeliveryController(DeliveryService deliveryService, AppDbContext db)
    {
        _deliveryService = deliveryService;
        _db = db;
    }

    /// <summary>
    /// Test Case 4.1: Tạo vận đơn giao hàng mới
    /// </summary>
    [HttpPost("CreateDelivery")]
    [Authorize(Roles = "SHOP,ADMIN,DELIVER,SHIPPER")]
    public async Task<IActionResult> CreateDelivery([FromBody] CreateDeliveryDto dto)
    {
        Guid? shopId = null;
        if (User.IsInRole("SHOP"))
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (Guid.TryParse(userIdStr, out var userId))
            {
                var shop = await _db.Shops.FirstOrDefaultAsync(s => s.UserId == userId);
                if (shop != null) shopId = shop.Id;
            }
        }

        var result = await _deliveryService.CreateDeliveryAsync(dto, shopId);
        return Ok(ApiResponse.SuccessResponse(result, "Delivery created successfully"));
    }

    /// <summary>
    /// Test Case 4.2: Shipper xem danh sách đơn cần giao
    /// </summary>
    [HttpGet("GetDeliveryOrders")]
    [Authorize(Roles = "SHIPPER,DELIVER,ADMIN,SHOP")]
    public async Task<IActionResult> GetDeliveryOrders([FromQuery] string? status)
    {
        var result = await _deliveryService.GetDeliveryOrdersAsync(status);
        return Ok(ApiResponse.SuccessResponse(result));
    }

    /// <summary>
    /// Test Case 4.3: Shipper cập nhật giao hàng thành công
    /// </summary>
    [HttpPost("UpdateDeliveryStatus")]
    [Authorize(Roles = "SHIPPER,DELIVER,ADMIN,SHOP")]
    public async Task<IActionResult> UpdateDeliveryStatus(
        [FromQuery] Guid deliveryId,
        [FromBody] UpdateDeliveryStatusDto dto)
    {
        if (deliveryId == Guid.Empty)
        {
            return BadRequest(ApiResponse.ErrorResponse("deliveryId is required."));
        }
        await _deliveryService.UpdateDeliveryStatusAsync(deliveryId, dto);
        return Ok(ApiResponse.SuccessResponse(null, "Delivery status updated successfully"));
    }
}
