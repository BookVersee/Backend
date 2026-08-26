using System;
using System.Threading.Tasks;
using BookManagement.Service.Delivery;
using BookManagement.Service.Dtos;
using BookManagement.Service.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookManagement.Api.Controllers;

[ApiController]
[Route("api/delivery")]
public class DeliveryController : ControllerBase
{
    private readonly DeliveryService _deliveryService;

    public DeliveryController(DeliveryService deliveryService)
    {
        _deliveryService = deliveryService;
    }

    /// <summary>
    /// Test Case 4.1: Tạo vận đơn giao hàng mới
    /// </summary>
    [HttpPost("CreateDelivery")]
    [Authorize(Roles = "SHOP,ADMIN,DELIVER,SHIPPER")]
    public async Task<IActionResult> CreateDelivery([FromBody] CreateDeliveryDto dto)
    {
        var result = await _deliveryService.CreateDeliveryAsync(dto);
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
