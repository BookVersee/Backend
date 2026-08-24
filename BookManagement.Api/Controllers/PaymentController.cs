using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BookManagement.Service.Dtos;
using BookManagement.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookManagement.Api.Controllers;

[ApiController]
[Route("api/payment/vnpay")]
public class PaymentController : ControllerBase
{
    private readonly PaymentService _paymentService;
    private readonly ShopService _shopService;

    public PaymentController(PaymentService paymentService, ShopService shopService)
    {
        _paymentService = paymentService;
        _shopService = shopService;
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("user_id")?.Value
            ?? User.FindFirst("sub")?.Value;
        return int.TryParse(claim, out var id) ? id : 1;
    }

    [HttpPost("create-url")]
    [Authorize]
    public async Task<IActionResult> CreatePaymentUrl([FromBody] CreateVnpayUrlDto dto)
    {
        var userId = GetUserId();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        var paymentUrl = await _paymentService.CreateVnpayUrlAsync(userId, dto, ipAddress);
        return Ok(new { payment_url = paymentUrl });
    }

    [HttpGet("ipn")]
    [AllowAnonymous]
    public async Task<IActionResult> VnpayIpn()
    {
        var queryDict = HttpContext.Request.Query
            .ToDictionary(q => q.Key, q => q.Value.ToString());

        var (rspCode, message) = await _paymentService.ProcessVnpayIpnAsync(queryDict);
        return Ok(new { RspCode = rspCode, Message = message });
    }

    [HttpPost("refund")]
    [Authorize(Roles = "SHOP")]
    public async Task<IActionResult> ProcessRefund([FromBody] VnpayRefundDto dto)
    {
        var userId = GetUserId();
        var shopProfile = await _shopService.GetShopProfileAsync(userId);
        await _paymentService.ProcessVnpayRefundAsync(shopProfile.ShopId, dto);
        return Ok(new { message = "Refund processed successfully" });
    }
}
