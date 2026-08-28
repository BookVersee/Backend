using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using BookManagement.Api.Filters;
using BookManagement.Repository.Entities.Enums;
using BookManagement.Service.Models;
using BookManagement.Service.Order;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookManagement.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/orders")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        /// Chá»©c nÄƒng: Láº¥y danh sÃ¡ch lá»‹ch sá»­ Ä‘Æ¡n hÃ ng. Tráº£ vá» : Danh sÃ¡ch Ä‘Æ¡n hÃ ng theo tráº¡ng thÃ¡i.
        [HttpGet("GetUserOrders")]
        public async Task<IActionResult> GetUserOrders([FromQuery] OrderStatus? status)
        {
            var userId = GetCurrentUserId();
            var orders = await _orderService.GetUserOrdersAsync(userId, status);
            return Ok(ApiResponse<IEnumerable<OrderResponse>>.SuccessResponse(orders));
        }

        /// Chá»©c nÄƒng: Ä áº·t hÃ ng thanh toÃ¡n tá»« giá»  hÃ ng. Tráº£ vá» : ThÃ´ng tin Ä‘Æ¡n hÃ ng má»›i táº¡o.
        [Authorize(Roles = "CUSTOMER")]
        [HttpPost("CreateOrder")]
        [Idempotent]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            var userId = GetCurrentUserId();
            var order = await _orderService.CreateOrderAsync(userId, request);
            return Ok(ApiResponse<OrderResponse>.SuccessResponse(order, "Order created successfully."));
        }

        /// Chá»©c nÄƒng: Xem thÃ´ng tin chi tiáº¿t Ä‘Æ¡n hÃ ng. Tráº£ vá»: Dá»¯ liá»‡u chi tiáº¿t sáº£n pháº©m vÃ  thanh toÃ¡n cá»§a Ä‘Æ¡n hÃ ng.
        [HttpGet("GetOrderDetail")]
        public async Task<IActionResult> GetOrderDetail([FromQuery] Guid id)
        {
            var userId = GetCurrentUserId();
            var order = await _orderService.GetOrderDetailAsync(userId, id);
            return Ok(ApiResponse<OrderResponse>.SuccessResponse(order));
        }

        /// Chá»©c nÄƒng: Há»§y Ä‘Æ¡n hÃ ng Ä‘ang chá» xá»­ lÃ½. Tráº£ vá»: ThÃ´ng bÃ¡o xÃ¡c nháº­n há»§y Ä‘Æ¡n thÃ nh cÃ´ng.
        [HttpPost("CancelOrder")]
        public async Task<IActionResult> CancelOrder([FromQuery] Guid id)
        {
            var userId = GetCurrentUserId();
            await _orderService.CancelOrderAsync(userId, id);
            return Ok(ApiResponse<string>.SuccessResponse("Order cancelled successfully."));
        }

        /// Chá»©c nÄƒng: Gá»­i yÃªu cáº§u khiáº¿u náº¡i tráº£ hÃ ng / hoÃ n tiá»n. Tráº£ vá»: Dá»¯ liá»‡u Ä‘Æ¡n yÃªu cáº§u tráº£ hÃ ng.
        [HttpPost("SendRequestReturn")]
        public async Task<IActionResult> SendRequestReturn([FromQuery] Guid orderDetailId, [FromBody] CreateReturnRequest input)
        {
            var userId = GetCurrentUserId();
            var returnRequest = await _orderService.CreateReturnRequestAsync(userId, orderDetailId, input);
            return Ok(ApiResponse<ReturnRequestResponse>.SuccessResponse(returnRequest, "Return request submitted."));
        }

        /// Chá»©c nÄƒng: Gá»­i khiáº¿u náº¡i lÃªn Admin khi yÃªu cáº§u tráº£ hÃ ng bá»‹ Shop tá»« chá»‘i.
        [HttpPost("EscalateDispute")]
        public async Task<IActionResult> EscalateDispute([FromQuery] Guid returnRequestId, [FromQuery] string? reason)
        {
            var userId = GetCurrentUserId();
            await _orderService.EscalateReturnRequestAsync(userId, returnRequestId, reason);
            return Ok(ApiResponse<string>.SuccessResponse("Khiáº¿u náº¡i cá»§a báº¡n Ä‘Ã£ Ä‘Æ°á»£c gá»­i tá»›i Admin xá»­ lÃ½.", "Escalation submitted."));
        }

        private Guid GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(claim) || !Guid.TryParse(claim, out var userId))
            {
                throw new UnauthorizedAccessException("Invalid authentication claims.");
            }
            return userId;
        }
    }
}

