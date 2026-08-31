using System;
using System.Security.Claims;
using System.Threading.Tasks;
using BookManagement.Api.Extensions;
using BookManagement.Repository.Entities.Enums;
using BookManagement.Service.Common;
using BookManagement.Service.Book;
using BookManagement.Service.Feedback;
using BookManagement.Service.Order;
using BookManagement.Service.Shop;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookManagement.Api.Controllers;

/// Vị trí: Api Controller - Tiếp nhận HTTP Request từ Frontend, kiểm tra đầu vào và trả về ApiResponse.
[ApiController]
[Route("api/shop")]
[Authorize]
public class ShopController : ControllerBase
{
    private readonly IShopService _shopService;

    public ShopController(IShopService shopService)
    {
        _shopService = shopService;
    }

    /// Chức năng: Xem thông tin hồ sơ Cửa hàng cá nhân
    [HttpGet("GetMyProfile")]
    [Authorize(Roles = "SHOP")]
    public async Task<IActionResult> GetShopProfile()
    {
        var (userId, role) = User.GetUserInfo();
        var result = await _shopService.GetShopProfileAsync(userId);
        return Ok(ApiResponse.SuccessResponse(result));
    }

    /// Chức năng: Đăng bán sản phẩm sách mới cho Cửa hàng
    [HttpPost("CreateShopBook")]
    [Authorize(Roles = "SHOP")]
    public async Task<IActionResult> CreateBook(CreateBookRequestDto dto)
    {
        var (userId, role) = User.GetUserInfo();
        var result = await _shopService.CreateBookAsync(userId, dto);
        return Ok(ApiResponse.SuccessResponse(result, "Book created successfully"));
    }

    /// Chức năng: Lấy danh sách tồn kho sách của Cửa hàng
    [HttpGet("GetShopInventory")]
    [Authorize(Roles = "SHOP")]
    public async Task<IActionResult> GetShopInventory([FromQuery] BookQueryDto query)
    {
        var (userId, role) = User.GetUserInfo();
        var result = await _shopService.GetShopBooksAsync(userId, query);
        return Ok(ApiResponse.SuccessResponse(result));
    }

    /// Chức năng: Xem thông tin chi tiết 1 sản phẩm sách của Cửa hàng
    [HttpGet("GetShopBookDetail")]
    [Authorize(Roles = "SHOP")]
    public async Task<IActionResult> GetShopBookDetail([FromQuery] Guid bookId)
    {
        var (userId, role) = User.GetUserInfo();
        var result = await _shopService.GetBookByIdAsync(userId, bookId);
        return Ok(ApiResponse.SuccessResponse(result));
    }

    /// Chức năng: Cập nhật thông tin và giá sản phẩm sách
    [HttpPost("UpdateShopBook")]
    [Authorize(Roles = "SHOP")]
    public async Task<IActionResult> UpdateShopBook(Guid bookId, UpdateBookRequestDto dto)
    {
        var (userId, role) = User.GetUserInfo();
        var result = await _shopService.UpdateBookAsync(userId, bookId, dto);
        return Ok(ApiResponse.SuccessResponse(result, "Book updated successfully"));
    }

    /// Chức năng: Ẩn sản phẩm sách khỏi gian hàng
    [HttpPost("DeleteShopBook")]
    [Authorize(Roles = "SHOP")]
    public async Task<IActionResult> DeleteShopBook(Guid bookId)
    {
        var (userId, role) = User.GetUserInfo();
        await _shopService.DeleteBookAsync(userId, bookId);
        return Ok(ApiResponse.SuccessResponse(null, "Book status updated to HIDDEN successfully"));
    }

    /// Chức năng: Xem thông tin chi tiết đơn hàng của Cửa hàng
    [HttpGet("GetShopOrderDetail")]
    [Authorize(Roles = "SHOP")]
    public async Task<IActionResult> GetShopOrderDetail(Guid orderId)
    {
        var (userId, role) = User.GetUserInfo();
        var result = await _shopService.GetShopOrderDetailAsync(userId, orderId);
        return Ok(ApiResponse.SuccessResponse(result));
    }

    /// Chức năng: Cập nhật trạng thái xử lý đơn hàng của Cửa hàng
    [HttpPost("UpdateOrderStatus")]
    [Authorize(Roles = "SHOP")]
    public async Task<IActionResult> UpdateOrderStatus(Guid orderId, UpdateOrderStatusDto dto)
    {
        var (userId, role) = User.GetUserInfo();
        await _shopService.UpdateOrderStatusAsync(userId, orderId, dto);
        return Ok(ApiResponse.SuccessResponse(null, "Order status updated successfully"));
    }

    /// Chức năng: Thống kê doanh thu Cửa hàng theo mốc thời gian
    [HttpGet("GetRevenueStatistics")]
    [Authorize(Roles = "SHOP")]
    public async Task<IActionResult> GetRevenueStatistics([FromQuery] RevenueQueryRequest query)
    {
        var (userId, role) = User.GetUserInfo();
        var result = await _shopService.GetShopRevenueAsync(userId, query);
        return Ok(ApiResponse.SuccessResponse(result));
    }

    /// Chức năng: Lấy danh sách đánh giá từ khách hàng dành cho Cửa hàng
    [HttpGet("GetShopFeedbacks")]
    [Authorize(Roles = "SHOP")]
    public async Task<IActionResult> GetShopFeedbacks([FromQuery] ShopFeedbackQueryRequest query)
    {
        var (userId, role) = User.GetUserInfo();
        var result = await _shopService.GetShopFeedbacksAsync(userId, query);
        return Ok(ApiResponse.SuccessResponse(result));
    }

    /// Chức năng: Phản hồi bình luận đánh giá của khách hàng
    [HttpPost("ReplyFeedback")]
    [Authorize(Roles = "SHOP")]
    public async Task<IActionResult> ReplyFeedback(Guid feedbackId, FeedbackResponseRequestDto dto)
    {
        var (userId, role) = User.GetUserInfo();
        var targetFeedbackId = dto.FeedbackId ?? feedbackId;
        var result = await _shopService.CreateFeedbackResponseAsync(userId, targetFeedbackId, dto);
        return Ok(ApiResponse.SuccessResponse(result, "Feedback response created successfully"));
    }

    /// Chức năng: Xử lý chấp nhận hoặc từ chối yêu cầu trả hàng
    [HttpPost("ProcessReturnRequest")]
    [Authorize(Roles = "SHOP")]
    public async Task<IActionResult> ProcessReturnRequest(Guid returnRequestId, ProcessReturnRequestDto dto)
    {
        var (userId, role) = User.GetUserInfo();
        await _shopService.ProcessReturnRequestAsync(userId, returnRequestId, dto);
        return Ok(ApiResponse.SuccessResponse(null, "Return request processed successfully"));
    }

    /// Chức năng: Shop tạm ngừng kinh doanh (CLOSED) hoặc mở bán lại (OPEN)
    [HttpPost("UpdateShopCondition")]
    [Authorize(Roles = "SHOP")]
    public async Task<IActionResult> UpdateShopCondition([FromBody] UpdateShopConditionDto dto)
    {
        var (userId, role) = User.GetUserInfo();
        var result = await _shopService.UpdateShopConditionAsync(userId, dto);
        var message = dto.Condition == ShopCondition.OPEN
            ? "Cửa hàng đã mở bán hoạt động trở lại thành công."
            : "Cửa hàng đã tạm ngừng kinh doanh thành công.";
        return Ok(ApiResponse.SuccessResponse(result, message));
    }
}
