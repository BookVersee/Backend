using System;
using System.Security.Claims;
using System.Threading.Tasks;
using BookManagement.Api.Extensions;
using BookManagement.Service.Common;
using BookManagement.Service.Delivery;
using BookManagement.Service.Shipping;
using BookManagement.Service.Shop;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookManagement.Api.Controllers;

/// Vị trí: Api Controller - Tiếp nhận HTTP Request từ Frontend, kiểm tra đầu vào và trả về ApiResponse.
[ApiController]
[Route("api/shipping")]
public class ShippingController : ControllerBase
{
    private readonly IShippingService _shippingService;

    public ShippingController(IShippingService shippingService)
    {
        _shippingService = shippingService;
    }

    /// Chức năng: Tạo vận đơn giao hàng qua đơn vị Giao Hàng Nhanh (GHN)
    [HttpPost("CreateGhnOrder")]
    [Authorize(Roles = "SHOP,ADMIN,SUPER_ADMIN")]
    public async Task<IActionResult> CreateGhnOrder([FromBody] CreateGhnOrderDto dto)
    {
        var (userId, role) = User.GetUserInfo();
        var result = await _shippingService.CreateGhnOrderAsync(userId, dto);
        return Ok(ApiResponse.SuccessResponse(result, "GHN shipping order created successfully"));
    }

    /// Chức năng: Webhook tiếp nhận tự động cập nhật trạng thái vận đơn từ GHN
    [HttpPost("GhnWebhook")]
    [AllowAnonymous]
    public async Task<IActionResult> GhnWebhook([FromBody] GhnWebhookPayload payload)
    {
        await _shippingService.ProcessGhnWebhookAsync(payload);
        return Ok(new { message = "Webhook processed successfully." });
    }
}
