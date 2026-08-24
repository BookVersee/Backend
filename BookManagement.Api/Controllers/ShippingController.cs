using System.Security.Claims;
using System.Threading.Tasks;
using BookManagement.Service.Dtos;
using BookManagement.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookManagement.Api.Controllers;

[ApiController]
[Route("api/shipping/ghn")]
public class ShippingController : ControllerBase
{
    private readonly ShippingService _shippingService;
    private readonly ShopService _shopService;

    public ShippingController(ShippingService shippingService, ShopService shopService)
    {
        _shippingService = shippingService;
        _shopService = shopService;
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("user_id")?.Value
            ?? User.FindFirst("sub")?.Value;
        return int.TryParse(claim, out var id) ? id : 1;
    }

    [HttpPost("create-order")]
    [Authorize(Roles = "SHOP")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateGhnOrderDto dto)
    {
        var userId = GetUserId();
        var shopProfile = await _shopService.GetShopProfileAsync(userId);
        var result = await _shippingService.CreateGhnOrderAsync(shopProfile.ShopId, dto);
        return StatusCode(201, result);
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook([FromBody] GhnWebhookPayload payload)
    {
        await _shippingService.ProcessGhnWebhookAsync(payload);
        return Ok(new { message = "Webhook processed" });
    }
}
