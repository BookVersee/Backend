using System;
using System.Collections.Generic;
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
    [Route("api/orders")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        /// Chức năng: Lấy danh sách lịch sử đơn hàng. Trả về: Danh sách đơn hàng theo trạng thái.
        [HttpGet("GetUserOrders")]
        [HttpGet("/api/shop/orders")]
        public async Task<IActionResult> GetUserOrders([FromQuery] OrderStatus? status)
        {
            var userId = GetCurrentUserId();
            var orders = await _orderService.GetUserOrdersAsync(userId, status);
            return Ok(ApiResponse<IEnumerable<OrderResponse>>.SuccessResponse(orders));
        }

        /// Chức năng: Đặt hàng thanh toán từ giỏ hàng. Trả về: Thông tin đơn hàng mới tạo.
        [Authorize(Roles = "CUSTOMER")]
        [HttpPost("CreateOrder")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            var userId = GetCurrentUserId();
            var order = await _orderService.CreateOrderAsync(userId, request);
            return Ok(ApiResponse<OrderResponse>.SuccessResponse(order, "Order created successfully."));
        }

        /// Chức năng: Xem thông tin chi tiết đơn hàng. Trả về: Dữ liệu chi tiết sản phẩm và thanh toán của đơn hàng.
        [HttpGet("GetOrderDetail")]
        public async Task<IActionResult> GetOrderDetail([FromQuery] Guid id)
        {
            var userId = GetCurrentUserId();
            var order = await _orderService.GetOrderDetailAsync(userId, id);
            return Ok(ApiResponse<OrderResponse>.SuccessResponse(order));
        }

        /// Chức năng: Hủy đơn hàng đang chờ xử lý. Trả về: Thông báo xác nhận hủy đơn thành công.
        [HttpPost("CancelOrder")]
        public async Task<IActionResult> CancelOrder([FromQuery] Guid id)
        {
            var userId = GetCurrentUserId();
            await _orderService.CancelOrderAsync(userId, id);
            return Ok(ApiResponse<string>.SuccessResponse("Order cancelled successfully."));
        }

        /// Chức năng: Gửi yêu cầu khiếu nại trả hàng / hoàn tiền. Trả về: Dữ liệu đơn yêu cầu trả hàng.
        [HttpPost("SendRequestReturn")]
        public async Task<IActionResult> SendRequestReturn([FromQuery] Guid orderDetailId, [FromBody] CreateReturnRequest input)
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
