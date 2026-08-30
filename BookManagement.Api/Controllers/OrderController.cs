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
        public async Task<IActionResult> GetUserOrders(OrderStatus? status)
        {
            var userId = User.GetUserId();
            var orders = await _orderService.GetUserOrdersAsync(userId, status);
            return Ok(ApiResponse<IEnumerable<OrderResponse>>.SuccessResponse(orders));
        }

        /// Chức năng: Đặt hàng thanh toán từ giỏ hàng. Trả về: Thông tin đơn hàng mới tạo.
        [Authorize(Roles = "CUSTOMER,SHOP")]
        [HttpPost("CreateOrder")]
        [Idempotent]
        public async Task<IActionResult> CreateOrder(CreateOrderRequest request)
        {
            var userId = User.GetUserId();
            var order = await _orderService.CreateOrderAsync(userId, request);
            return Ok(ApiResponse<OrderResponse>.SuccessResponse(order, "Order created successfully."));
        }

        /// Chức năng: Xem thông tin chi tiết đơn hàng. Trả về: Dữ liệu chi tiết sản phẩm và thanh toán của đơn hàng.
        [HttpGet("GetOrderDetail")]
        public async Task<IActionResult> GetOrderDetail(Guid id)
        {
            var userId = User.GetUserId();
            var order = await _orderService.GetOrderDetailAsync(userId, id);
            return Ok(ApiResponse<OrderResponse>.SuccessResponse(order));
        }

        /// Chức năng: Hủy đơn hàng đang chờ xử lý. Trả về: Thông báo xác nhận hủy đơn thành công.
        [HttpPost("CancelOrder")]
        public async Task<IActionResult> CancelOrder(Guid id)
        {
            var userId = User.GetUserId();
            await _orderService.CancelOrderAsync(userId, id);
            return Ok(ApiResponse<string>.SuccessResponse("Order cancelled successfully."));
        }

        /// Chức năng: Gửi yêu cầu khiếu nại trả hàng / hoàn tiền. Trả về: Dữ liệu đơn yêu cầu trả hàng.
        [HttpPost("SendRequestReturn")]
        public async Task<IActionResult> SendRequestReturn(Guid orderDetailId, CreateReturnRequest input)
        {
            var userId = User.GetUserId();
            var returnRequest = await _orderService.CreateReturnRequestAsync(userId, orderDetailId, input);
            return Ok(ApiResponse<ReturnRequestResponse>.SuccessResponse(returnRequest, "Return request submitted."));
        }

        /// Chức năng: Gửi khiếu nại lên Admin khi yêu cầu trả hàng bị Shop từ chối.
        [HttpPost("EscalateDispute")]
        public async Task<IActionResult> EscalateDispute(Guid returnRequestId, string? reason)
        {
            var userId = User.GetUserId();
            await _orderService.EscalateReturnRequestAsync(userId, returnRequestId, reason);
            return Ok(ApiResponse<string>.SuccessResponse("Khiếu nại của bạn đã được gửi tới Admin xử lý.", "Escalation submitted."));
        }
    }
}
