using System;
using System.Security.Claims;
using System.Threading.Tasks;
using BookManagement.Service.Dtos;
using BookManagement.Service.Models;
using BookManagement.Service.Shipping;
using BookManagement.Service.Shop;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookManagement.Api.Controllers;

[ApiController]
[Route("api/shipping")]
public class ShippingController : ControllerBase
{
    private readonly ShippingService _shippingService;
    private readonly ShopService _shopService;

    public ShippingController(ShippingService shippingService, ShopService shopService)
    {
        _shippingService = shippingService;
        _shopService = shopService;
    }

    private Guid GetUserId()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(userIdStr, out var id) ? id : Guid.Empty;
    }

    /// <summary>
    /// Test Case 5.1: Tạo đơn vận chuyển qua GHN
    /// </summary>
    [HttpPost("CreateGhnOrder")]
    [Authorize(Roles = "SHOP,ADMIN,SUPER_ADMIN")]
    public async Task<IActionResult> CreateGhnOrder([FromBody] CreateGhnOrderDto dto)
    {
        var userId = GetUserId();
        var profile = await _shopService.GetShopProfileAsync(userId);
        var result = await _shippingService.CreateGhnOrderAsync(profile.ShopId, dto);
        return Ok(ApiResponse.SuccessResponse(result, "GHN shipping order created successfully"));
    }

    /// <summary>
    /// Test Case 5.2: Tiếp nhận Webhook trạng thái từ GHN
    /// </summary>
    [HttpPost("GhnWebhook")]
    [AllowAnonymous]
    public async Task<IActionResult> GhnWebhook([FromBody] GhnWebhookPayload payload)
    {
        await _shippingService.ProcessGhnWebhookAsync(payload);
        return Ok(new { message = "Webhook processed successfully." });
    }
}
