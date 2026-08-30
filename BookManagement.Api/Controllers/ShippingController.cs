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

[ApiController]
[Route("api/shipping")]
public class ShippingController : ControllerBase
{
    private readonly IShippingService _shippingService;

    public ShippingController(IShippingService shippingService)
    {
        _shippingService = shippingService;
    }

    /// <summary>
    /// Test Case 5.1: Tạo đơn vận chuyển qua GHN
    /// </summary>
    [HttpPost("CreateGhnOrder")]
    [Authorize(Roles = "SHOP,ADMIN,SUPER_ADMIN")]
    public async Task<IActionResult> CreateGhnOrder(CreateGhnOrderDto dto)
    {
        var userId = User.GetUserId();
        var result = await _shippingService.CreateGhnOrderAsync(userId, dto);
        return Ok(ApiResponse.SuccessResponse(result, "GHN shipping order created successfully"));
    }

    /// <summary>
    /// Test Case 5.2: Tiếp nhận Webhook trạng thái từ GHN
    /// </summary>
    [HttpPost("GhnWebhook")]
    [AllowAnonymous]
    public async Task<IActionResult> GhnWebhook(GhnWebhookPayload payload)
    {
        await _shippingService.ProcessGhnWebhookAsync(payload);
        return Ok(new { message = "Webhook processed successfully." });
    }
}
