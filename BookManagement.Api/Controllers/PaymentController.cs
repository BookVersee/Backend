using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using BookManagement.Service.Dtos;
using BookManagement.Service.Models;
using BookManagement.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookManagement.Api.Controllers;

[ApiController]
[Route("api/payment")]
public class PaymentController : ControllerBase
{
    private readonly PaymentService _paymentService;
    private readonly ShopService _shopService;

    public PaymentController(PaymentService paymentService, ShopService shopService)
    {
        _paymentService = paymentService;
        _shopService = shopService;
    }

    private Guid GetUserId()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(userIdStr, out var id) ? id : Guid.Empty;
    }

    [HttpPost("vnpay/create-url")]
    [Authorize]
    public async Task<IActionResult> CreateVnpayUrl([FromBody] CreateVnpayUrlDto dto)
    {
        var userId = GetUserId();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        var paymentUrl = await _paymentService.CreateVnpayUrlAsync(userId, dto, ipAddress);
        return Ok(ApiResponse.SuccessResponse(new { payment_url = paymentUrl }));
    }

    [HttpGet("vnpay/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> VnpayCallback()
    {
        var queryParams = new Dictionary<string, string>();
        foreach (var key in Request.Query.Keys)
        {
            if (!string.IsNullOrEmpty(key))
            {
                queryParams[key] = Request.Query[key].ToString();
            }
        }

        var (rspCode, message) = await _paymentService.ProcessVnpayIpnAsync(queryParams);
        return Ok(new { RspCode = rspCode, Message = message });
    }

    [HttpPost("vnpay/refund")]
    [Authorize(Roles = "SHOP,ADMIN")]
    public async Task<IActionResult> ProcessVnpayRefund([FromBody] VnpayRefundDto dto)
    {
        var userId = GetUserId();
        var profile = await _shopService.GetShopProfileAsync(userId);
        await _paymentService.ProcessVnpayRefundAsync(profile.ShopId, dto);
        return Ok(ApiResponse.SuccessResponse(null, "VNPAY refund processed successfully"));
    }
}
