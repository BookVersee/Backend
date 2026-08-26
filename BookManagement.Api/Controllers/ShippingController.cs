using System;
using System.Security.Claims;
using System.Threading.Tasks;
using BookManagement.Service.Dtos;
using BookManagement.Service.Models;
using BookManagement.Service.Services;
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

    [HttpPost("ghn/create")]
    [Authorize(Roles = "SHOP,ADMIN")]
    public async Task<IActionResult> CreateGhnOrder([FromBody] CreateGhnOrderDto dto)
    {
        var userId = GetUserId();
        var profile = await _shopService.GetShopProfileAsync(userId);
        var result = await _shippingService.CreateGhnOrderAsync(profile.ShopId, dto);
        return StatusCode(201, ApiResponse.SuccessResponse(result, "GHN shipping order created successfully"));
    }

    [HttpPost("ghn/webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> GhnWebhook([FromBody] GhnWebhookPayload payload)
    {
        await _shippingService.ProcessGhnWebhookAsync(payload);
        return Ok(new { message = "Webhook processed" });
    }
}
