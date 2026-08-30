using System;
using BookManagement.Api.Filters;
using BookManagement.Api.Extensions;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using BookManagement.Repository.Entities.Enums;
using BookManagement.Service.Common;
using BookManagement.Service.Order;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookManagement.Api.Controllers
{
    /// Vị trí: Api Controller - Tiếp nhận HTTP Request từ Frontend, kiểm tra đầu vào và trả về ApiResponse.
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

        /// Chức năng: Lấy danh sách lịch sử đơn hàng
        [HttpGet("GetUserOrders")]
        public async Task<IActionResult> GetUserOrders(OrderStatus? status)
        {
            var userId = User.GetUserId();
            var orders = await _orderService.GetUserOrdersAsync(userId, status);
            return Ok(ApiResponse<IEnumerable<OrderResponse>>.SuccessResponse(orders));
        }

        /// Chức năng: Đặt hàng thanh toán từ giỏ hàng (Chống trùng lặp Idempotent)
        [Authorize(Roles = "CUSTOMER,SHOP")]
        [HttpPost("CreateOrder")]
        [Idempotent]
        public async Task<IActionResult> CreateOrder(CreateOrderRequest request)
        {
            var userId = User.GetUserId();
            var order = await _orderService.CreateOrderAsync(userId, request);
            return Ok(ApiResponse<OrderResponse>.SuccessResponse(order, "Order created successfully."));
        }

        /// Chức năng: Xem thông tin chi tiết đơn hàng
        [HttpGet("GetOrderDetail")]
        public async Task<IActionResult> GetOrderDetail(Guid id)
        {
            var userId = User.GetUserId();
            var order = await _orderService.GetOrderDetailAsync(userId, id);
            return Ok(ApiResponse<OrderResponse>.SuccessResponse(order));
        }

        /// Chức năng: Hủy đơn hàng đang ở trạng thái PENDING
        [HttpPost("CancelOrder")]
        public async Task<IActionResult> CancelOrder(Guid id)
        {
            var userId = User.GetUserId();
            await _orderService.CancelOrderAsync(userId, id);
            return Ok(ApiResponse<string>.SuccessResponse("Order cancelled successfully."));
        }

        /// Chức năng: Gửi yêu cầu khiếu nại trả hàng / hoàn tiền
        [HttpPost("SendRequestReturn")]
        public async Task<IActionResult> SendRequestReturn(Guid orderDetailId, CreateReturnRequest input)
        {
            var userId = User.GetUserId();
            var returnRequest = await _orderService.CreateReturnRequestAsync(userId, orderDetailId, input);
            return Ok(ApiResponse<ReturnRequestResponse>.SuccessResponse(returnRequest, "Return request submitted."));
        }

        /// Chức năng: Gửi khiếu nại lên Admin khi Shop từ chối trả hàng
        [HttpPost("EscalateDispute")]
        public async Task<IActionResult> EscalateDispute(Guid returnRequestId, string? reason)
        {
            var userId = User.GetUserId();
            await _orderService.EscalateReturnRequestAsync(userId, returnRequestId, reason);
            return Ok(ApiResponse<string>.SuccessResponse("Khiếu nại của bạn đã được gửi tới Admin xử lý.", "Escalation submitted."));
        }
    }
}
