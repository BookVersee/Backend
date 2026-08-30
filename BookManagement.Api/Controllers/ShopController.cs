using System;
using System.Security.Claims;
using System.Threading.Tasks;
using BookManagement.Repository.Entities.Enums;
using BookManagement.Service.Common;
using BookManagement.Service.Book;
using BookManagement.Service.Feedback;
using BookManagement.Service.Order;
using BookManagement.Service.Shop;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookManagement.Api.Controllers;

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

    private Guid GetUserId()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(userIdStr, out var id) ? id : Guid.Empty;
    }

    /// <summary>
    /// Test Case 1.1: Xem hồ sơ Shop hiện tại
    /// </summary>
    [HttpGet("GetMyProfile")]
    public async Task<IActionResult> GetShopProfile()
    {
        var userId = GetUserId();
        var result = await _shopService.GetShopProfileAsync(userId);
        return Ok(ApiResponse.SuccessResponse(result));
    }

    /// <summary>
    /// Test Case 1.2: Đăng bán sách mới
    /// </summary>
    [HttpPost("CreateShopBook")]
    [Authorize(Roles = "SHOP")]
    public async Task<IActionResult> CreateBook(CreateBookRequestDto dto)
    {
        var userId = GetUserId();
        var result = await _shopService.CreateBookAsync(userId, dto);
        return Ok(ApiResponse.SuccessResponse(result, "Book created successfully"));
    }

    /// <summary>
    /// Test Case 1.3: Lấy danh sách kho sách & Lọc sản phẩm
    /// </summary>
    [HttpGet("GetShopInventory")]
    [Authorize(Roles = "SHOP")]
    public async Task<IActionResult> GetShopInventory(BookQueryDto query)
    {
        var userId = GetUserId();
        var result = await _shopService.GetShopBooksAsync(userId, query);
        return Ok(ApiResponse.SuccessResponse(result));
    }

    /// <summary>
    /// Test Case 1.4: Cập nhật thông tin & Giá sách
    /// </summary>
    [HttpPost("UpdateShopBook")]
    [Authorize(Roles = "SHOP")]
    public async Task<IActionResult> UpdateShopBook(Guid bookId, UpdateBookRequestDto dto)
    {
        var userId = GetUserId();
        var result = await _shopService.UpdateBookAsync(userId, bookId, dto);
        return Ok(ApiResponse.SuccessResponse(result, "Book updated successfully"));
    }

    /// <summary>
    /// Test Case 1.5: Ẩn sách khỏi gian hàng
    /// </summary>
    [HttpPost("DeleteShopBook")]
    [Authorize(Roles = "SHOP")]
    public async Task<IActionResult> DeleteShopBook(Guid bookId)
    {
        var userId = GetUserId();
        await _shopService.DeleteBookAsync(userId, bookId);
        return Ok(ApiResponse.SuccessResponse(null, "Book status updated to HIDDEN successfully"));
    }

    /// <summary>
    /// Test Case 3.1: Xem chi tiết đơn hàng của Shop
    /// </summary>
    [HttpGet("GetShopOrderDetail")]
    [Authorize(Roles = "SHOP")]
    public async Task<IActionResult> GetShopOrderDetail(Guid orderId)
    {
        var userId = GetUserId();
        var result = await _shopService.GetShopOrderDetailAsync(userId, orderId);
        return Ok(ApiResponse.SuccessResponse(result));
    }

    /// <summary>
    /// Test Case 3.2: Cập nhật trạng thái đơn hàng
    /// </summary>
    [HttpPost("UpdateOrderStatus")]
    [Authorize(Roles = "SHOP")]
    public async Task<IActionResult> UpdateOrderStatus(Guid orderId, UpdateOrderStatusDto dto)
    {
        var userId = GetUserId();
        await _shopService.UpdateOrderStatusAsync(userId, orderId, dto);
        return Ok(ApiResponse.SuccessResponse(null, "Order status updated successfully"));
    }

    /// <summary>
    /// Test Case 3.3: Thống kê doanh thu Shop
    /// </summary>
    [HttpGet("GetRevenueStatistics")]
    [Authorize(Roles = "SHOP")]
    public async Task<IActionResult> GetRevenueStatistics(RevenueQueryRequest query)
    {
        var userId = GetUserId();
        var result = await _shopService.GetShopRevenueAsync(userId, query);
        return Ok(ApiResponse.SuccessResponse(result));
    }

    /// <summary>
    /// Test Case 3.4: Xem & Trả lời đánh giá của khách
    /// </summary>
    [HttpGet("GetShopFeedbacks")]
    [Authorize(Roles = "SHOP")]
    public async Task<IActionResult> GetShopFeedbacks(ShopFeedbackQueryRequest query)
    {
        var userId = GetUserId();
        var result = await _shopService.GetShopFeedbacksAsync(userId, query);
        return Ok(ApiResponse.SuccessResponse(result));
    }

    [HttpPost("ReplyFeedback")]
    [Authorize(Roles = "SHOP")]
    public async Task<IActionResult> ReplyFeedback(Guid feedbackId, FeedbackResponseRequestDto dto)
    {
        var userId = GetUserId();
        var targetFeedbackId = dto.FeedbackId ?? feedbackId;
        var result = await _shopService.CreateFeedbackResponseAsync(userId, targetFeedbackId, dto);
        return Ok(ApiResponse.SuccessResponse(result, "Feedback response created successfully"));
    }

    /// <summary>
    /// Test Case 3.5: Xử lý yêu cầu hoàn trả hàng
    /// </summary>
    [HttpPost("ProcessReturnRequest")]
    [Authorize(Roles = "SHOP")]
    public async Task<IActionResult> ProcessReturnRequest(Guid returnRequestId, ProcessReturnRequestDto dto)
    {
        var userId = GetUserId();
        await _shopService.ProcessReturnRequestAsync(userId, returnRequestId, dto);
        return Ok(ApiResponse.SuccessResponse(null, "Return request processed successfully"));
    }
}
