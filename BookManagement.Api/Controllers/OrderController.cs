using System;
using System.Security.Claims;
using System.Threading.Tasks;
using BookManagement.Repository.Entities.Enums;
using BookManagement.Service.Models;
using BookManagement.Service.Order;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookManagement.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpGet]
        public async Task<IActionResult> GetUserOrders([FromQuery] OrderStatus? status)
        {
            var userId = GetCurrentUserId();
            var orders = await _orderService.GetUserOrdersAsync(userId, status);
            return Ok(ApiResponse<object>.SuccessResponse(orders));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderDetail(Guid id)
        {
            var userId = GetCurrentUserId();
            var order = await _orderService.GetOrderDetailAsync(userId, id);
            return Ok(ApiResponse<OrderResponse>.SuccessResponse(order));
        }

        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelOrder(Guid id)
        {
            var userId = GetCurrentUserId();
            await _orderService.CancelOrderAsync(userId, id);
            return Ok(ApiResponse<string>.SuccessResponse("Order cancelled successfully."));
        }

        [HttpPost("details/{orderDetailId}/return")]
        public async Task<IActionResult> SendRequestReturn(Guid orderDetailId, [FromBody] CreateReturnRequest input)
        {
            var userId = GetCurrentUserId();
            var returnRequest = await _orderService.CreateReturnRequestAsync(userId, orderDetailId, input);
            return Ok(ApiResponse<ReturnRequestResponse>.SuccessResponse(returnRequest, "Return request submitted."));
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
