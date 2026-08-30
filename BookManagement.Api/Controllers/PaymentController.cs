using System;
using BookManagement.Api.Filters;
using BookManagement.Api.Extensions;
using System.Security.Claims;
using System.Threading.Tasks;
using BookManagement.Service.Common;
using BookManagement.Service.Payment;
using BookManagement.Service.Shop;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace BookManagement.Api.Controllers;

/// Vị trí: Api Controller - Tiếp nhận HTTP Request từ Frontend, kiểm tra đầu vào và trả về ApiResponse.
[ApiController]
[Route("api/payment")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly IShopService _shopService;
    private readonly IConfiguration _configuration;

    public PaymentController(IPaymentService paymentService, IShopService shopService, IConfiguration configuration)
    {
        _paymentService = paymentService;
        _shopService = shopService;
        _configuration = configuration;
    }

    /// Chức năng: Tạo URL và mã QR thanh toán MoMo Sandbox
    [HttpPost("CreatePaymentUrl")]
    [Authorize]
    public async Task<IActionResult> CreatePaymentUrl(CreatePaymentUrlDto dto)
    {
        var (userId, role) = User.GetUserInfo();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        var (paymentUrl, qrCodeUrl, deeplink) = await _paymentService.CreateMomoUrlAsync(userId, dto, ipAddress);
        return Ok(ApiResponse.SuccessResponse(new 
        { 
            payment_url = paymentUrl, 
            qr_code_url = qrCodeUrl,
            deeplink = deeplink
        }));
    }

    /// Chức năng: Callback chuyển hướng trình duyệt sau khi thanh toán MoMo
    [HttpGet("Callback")]
    [AllowAnonymous]
    public IActionResult Callback(string? orderId, int? resultCode, string? message)
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

    /// Chức năng: Webhook IPN nhận kết quả thanh toán từ Server MoMo
    [HttpPost("Ipn")]
    [AllowAnonymous]
    public async Task<IActionResult> Ipn(MomoIpnRequest req)
    {
        var (resultCode, message) = await _paymentService.ProcessMomoIpnAsync(req);
        return Ok(new { resultCode, message });
    }

    /// Chức năng: Hoàn tiền đơn hàng MoMo (Chống trùng lặp Idempotent)
    [HttpPost("ProcessRefund")]
    [Authorize(Roles = "SHOP")]
    [Idempotent]
    public async Task<IActionResult> ProcessRefund(ProcessRefundDto dto)
    {
        var (userId, role) = User.GetUserInfo();
        var profile = await _shopService.GetShopProfileAsync(userId);
        await _paymentService.ProcessRefundAsync(profile.ShopId, dto);
        return Ok(ApiResponse.SuccessResponse(null, "MoMo refund processed successfully."));
    }

    /// Chức năng: Chủ động truy vấn đồng bộ trạng thái thanh toán từ MoMo
    [HttpPost("QueryPaymentStatus")]
    [Authorize]
    public async Task<IActionResult> QueryPaymentStatus(Guid orderId)
    {
        var (isPaid, message, transCode) = await _paymentService.SyncPaymentStatusAsync(orderId);
        return Ok(ApiResponse.SuccessResponse(new
        {
            order_id = orderId,
            is_paid = isPaid,
            transaction_code = transCode
        }, message));
    }

    /// Chức năng: Quét và hủy các đơn hàng PENDING quá hạn 15 phút
    [HttpPost("ExpirePendingOrders")]
    [Authorize(Roles = "ADMIN,SUPER_ADMIN")]
    public async Task<IActionResult> ExpirePendingOrders(int expiryMinutes = 15)
    {
        int count = await _paymentService.ExpirePendingOrdersAsync(expiryMinutes);
        return Ok(ApiResponse.SuccessResponse(new { expired_count = count }, $"Đã xử lý hủy và hoàn kho cho {count} đơn hàng quá hạn."));
    }
}
