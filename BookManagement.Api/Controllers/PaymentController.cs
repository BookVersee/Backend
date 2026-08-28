using System;
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
    /// Tạo URL thanh toán MoMo Sandbox (Quét mã QR Code / Ví MoMo)
    /// </summary>
    [HttpPost("CreatePaymentUrl")]
    [Authorize]
    public async Task<IActionResult> CreatePaymentUrl([FromBody] CreatePaymentUrlDto dto)
    {
        var userId = GetUserId();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        var (paymentUrl, qrCodeUrl, deeplink) = await _paymentService.CreateMomoUrlAsync(userId, dto, ipAddress);
        return Ok(ApiResponse.SuccessResponse(new 
        { 
            payment_url = paymentUrl, 
            qr_code_url = qrCodeUrl,
            deeplink = deeplink
        }));
    }

    /// <summary>
    /// Callback MoMo: Nhận kết quả và chuyển hướng trình duyệt sau khi khách thanh toán
    /// </summary>
    [HttpGet("Callback")]
    [AllowAnonymous]
    public IActionResult Callback([FromQuery] string? orderId, [FromQuery] int? resultCode, [FromQuery] string? message)
    {
        var status = (resultCode == 0) ? "SUCCESS" : "FAILED";
        var msg = message ?? (resultCode == 0 ? "Giao dịch MoMo thành công." : "Giao dịch MoMo thất bại.");

        return Ok(ApiResponse.SuccessResponse(new 
        { 
            order_id = orderId, 
            result_code = resultCode ?? 0, 
            status = status,
            message = msg
        }, msg));
    }

    /// <summary>
    /// IPN Webhook MoMo: Server-to-Server tự động cập nhật đơn hàng thành PAID
    /// </summary>
    [HttpPost("Ipn")]
    [AllowAnonymous]
    public async Task<IActionResult> Ipn([FromBody] MomoIpnRequest req)
    {
        var (resultCode, message) = await _paymentService.ProcessMomoIpnAsync(req);
        return Ok(new { resultCode, message });
    }

    /// <summary>
    /// Hoàn tiền đơn hàng qua MoMo
    /// </summary>
    [HttpPost("ProcessRefund")]
    [Authorize(Roles = "SHOP")]
    public async Task<IActionResult> ProcessRefund([FromBody] ProcessRefundDto dto)
    {
        var userId = GetUserId();
        var profile = await _shopService.GetShopProfileAsync(userId);
        await _paymentService.ProcessRefundAsync(profile.ShopId, dto);
        return Ok(ApiResponse.SuccessResponse(null, "MoMo refund processed successfully."));
    }
}
