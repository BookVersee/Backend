using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using BookManagement.Service.Dtos;
using BookManagement.Service.Models;
using BookManagement.Service.Payment;
using BookManagement.Service.Shop;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace BookManagement.Api.Controllers;

[ApiController]
[Route("api/payment")]
public class PaymentController : ControllerBase
{
    private readonly PaymentService _paymentService;
    private readonly ShopService _shopService;
    private readonly IConfiguration _configuration;

    public PaymentController(PaymentService paymentService, ShopService shopService, IConfiguration configuration)
    {
        _paymentService = paymentService;
        _shopService = shopService;
        _configuration = configuration;
    }

    private Guid GetUserId()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(userIdStr, out var id) ? id : Guid.Empty;
    }

    /// <summary>
    /// Test Case 6.1: Tạo URL thanh toán VNPAY Sandbox
    /// </summary>
    [HttpPost("CreateVnpayUrl")]
    [Authorize]
    public async Task<IActionResult> CreateVnpayUrl([FromBody] CreateVnpayUrlDto dto)
    {
        var userId = GetUserId();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        var paymentUrl = await _paymentService.CreateVnpayUrlAsync(userId, dto, ipAddress);
        return Ok(ApiResponse.SuccessResponse(new { payment_url = paymentUrl }));
    }

    /// <summary>
    /// Callback VNPAY: Tự động chuyển hướng trình duyệt về trang Frontend UI (/payment-result)
    /// </summary>
    [HttpGet("vnpay/callback")]
    [HttpGet("callback")]
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

        var frontendReturnUrl = _configuration["VnPay:ReturnUrl"] 
            ?? _configuration["Vnpay:FrontendReturnUrl"] 
            ?? "http://localhost:3000/payment-result";

        queryParams.TryGetValue("vnp_TxnRef", out var txnRef);
        queryParams.TryGetValue("vnp_ResponseCode", out var responseCode);

        var redirectUrl = $"{frontendReturnUrl}?vnp_ResponseCode={responseCode ?? rspCode}&vnp_TxnRef={txnRef}&message={Uri.EscapeDataString(message)}";
        return Redirect(redirectUrl);
    }

    /// <summary>
    /// IPN VNPAY: Webhook Server-to-Server ngầm từ VNPAY
    /// </summary>
    [HttpGet("vnpay/ipn")]
    [HttpGet("ipn")]
    [AllowAnonymous]
    public async Task<IActionResult> VnpayIpn()
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

    /// <summary>
    /// Test Case 6.2: Hoàn tiền đơn hàng qua VNPAY
    /// </summary>
    [HttpPost("ProcessVnpayRefund")]
    [Authorize(Roles = "SHOP")]
    public async Task<IActionResult> ProcessVnpayRefund([FromBody] VnpayRefundDto dto)
    {
        var userId = GetUserId();
        var profile = await _shopService.GetShopProfileAsync(userId);
        await _paymentService.ProcessVnpayRefundAsync(profile.ShopId, dto);
        return Ok(ApiResponse.SuccessResponse(null, "VNPAY refund processed successfully."));
    }
}
