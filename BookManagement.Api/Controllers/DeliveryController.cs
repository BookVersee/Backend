using System;
using System.Threading.Tasks;
using BookManagement.Api.Extensions;
using BookManagement.Service.Delivery;
using BookManagement.Service.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookManagement.Api.Controllers;

/// Vị trí: Api Controller - Tiếp nhận HTTP Request từ Frontend, kiểm tra đầu vào và trả về ApiResponse.
[ApiController]
[Route("api/delivery")]
public class DeliveryController : ControllerBase
{
    private readonly DeliveryService _deliveryService;

    public DeliveryController(DeliveryService deliveryService)
    {
        _deliveryService = deliveryService;
    }

    /// Chức năng: Khởi tạo vận đơn giao hàng mới
    [HttpPost("CreateDelivery")]
    [Authorize(Roles = "SHOP,ADMIN,SUPER_ADMIN,DELIVER,SHIPPER")]
    public async Task<IActionResult> CreateDelivery(CreateDeliveryDto dto)
    {
        var (userId, role) = User.GetUserInfo();
        var result = await _deliveryService.CreateDeliveryAsync(dto);
        return Ok(ApiResponse.SuccessResponse(result, "Delivery created successfully"));
    }

    /// Chức năng: Shipper xem danh sách các đơn hàng cần giao
    [HttpGet("GetDeliveryOrders")]
    [Authorize(Roles = "SHIPPER,DELIVER,ADMIN,SUPER_ADMIN,SHOP")]
    public async Task<IActionResult> GetDeliveryOrders(string? status)
    {
        var (userId, role) = User.GetUserInfo();
        var result = await _deliveryService.GetDeliveryOrdersAsync(status);
        return Ok(ApiResponse.SuccessResponse(result));
    }

    /// Chức năng: Cập nhật trạng thái hành trình giao hàng (Shipper/Carrier)
    [HttpPost("UpdateDeliveryStatus")]
    [Authorize(Roles = "SHIPPER,DELIVER,ADMIN,SUPER_ADMIN,SHOP")]
    public async Task<IActionResult> UpdateDeliveryStatus(Guid deliveryId, UpdateDeliveryStatusDto dto)
    {
        var (userId, role) = User.GetUserInfo();
        if (deliveryId == Guid.Empty)
        {
            return BadRequest(ApiResponse.ErrorResponse("deliveryId is required."));
        }
        await _deliveryService.UpdateDeliveryStatusAsync(deliveryId, dto);
        return Ok(ApiResponse.SuccessResponse(null, "Delivery status updated successfully"));
    }
}
