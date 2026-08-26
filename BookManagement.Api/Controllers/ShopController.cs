using System;
using System.Security.Claims;
using System.Threading.Tasks;
using BookManagement.Repository.Entities.Enums;
using BookManagement.Service.Dtos;
using BookManagement.Service.Models;
using BookManagement.Service.Shop;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookManagement.Api.Controllers;

[ApiController]
[Route("api/shop")]
[Authorize]
public class ShopController : ControllerBase
{
    private readonly ShopService _shopService;

    public ShopController(ShopService shopService)
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
    [Authorize(Roles = "SHOP,ADMIN")]
    public async Task<IActionResult> CreateBook([FromBody] CreateBookRequestDto dto)
    {
        var userId = GetUserId();
        var profile = await _shopService.GetShopProfileAsync(userId);
        var result = await _shopService.CreateBookAsync(profile.ShopId, dto);
        return Ok(ApiResponse.SuccessResponse(result, "Book created successfully"));
    }

    /// <summary>
    /// Test Case 1.3: Lấy danh sách kho sách & Lọc sản phẩm
    /// </summary>
    [HttpGet("GetShopInventory")]
    [Authorize(Roles = "SHOP,ADMIN")]
    public async Task<IActionResult> GetShopInventory(
        [FromQuery] string? searchTerm,
        [FromQuery] string? keyword,
        [FromQuery] Guid? categoryId,
        [FromQuery] BookStatus? status,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10)
    {
        var userId = GetUserId();
        var profile = await _shopService.GetShopProfileAsync(userId);
        var query = new BookQueryDto
        {
            Keyword = searchTerm ?? keyword,
            CategoryId = categoryId,
            Status = status,
            PageIndex = pageIndex > 0 ? pageIndex : 1,
            PageSize = pageSize > 0 ? pageSize : 10
        };
        var result = await _shopService.GetShopBooksAsync(profile.ShopId, query);
        return Ok(ApiResponse.SuccessResponse(result));
    }

    /// <summary>
    /// Test Case 1.4: Cập nhật thông tin & Giá sách
    /// </summary>
    [HttpPost("UpdateShopBook")]
    [Authorize(Roles = "SHOP,ADMIN")]
    public async Task<IActionResult> UpdateShopBook(
        [FromQuery] Guid bookId,
        [FromBody] UpdateBookRequestDto dto)
    {
        if (bookId == Guid.Empty)
        {
            return BadRequest(ApiResponse.ErrorResponse("bookId is required."));
        }
        var userId = GetUserId();
        var profile = await _shopService.GetShopProfileAsync(userId);
        var result = await _shopService.UpdateBookAsync(profile.ShopId, bookId, dto);
        return Ok(ApiResponse.SuccessResponse(result, "Book updated successfully"));
    }

    /// <summary>
    /// Test Case 1.5: Ẩn sách khỏi gian hàng
    /// </summary>
    [HttpPost("DeleteShopBook")]
    [Authorize(Roles = "SHOP,ADMIN")]
    public async Task<IActionResult> DeleteShopBook([FromQuery] Guid bookId)
    {
        if (bookId == Guid.Empty)
        {
            return BadRequest(ApiResponse.ErrorResponse("bookId is required."));
        }
        var userId = GetUserId();
        var profile = await _shopService.GetShopProfileAsync(userId);
        await _shopService.DeleteBookAsync(profile.ShopId, bookId);
        return Ok(ApiResponse.SuccessResponse(null, "Book status updated to HIDDEN successfully"));
    }

    /// <summary>
    /// Test Case 3.1: Xem chi tiết đơn hàng của Shop
    /// </summary>
    [HttpGet("GetShopOrderDetail")]
    [Authorize(Roles = "SHOP,ADMIN")]
    public async Task<IActionResult> GetShopOrderDetail([FromQuery] Guid orderId)
    {
        if (orderId == Guid.Empty)
        {
            return BadRequest(ApiResponse.ErrorResponse("orderId is required."));
        }
        var userId = GetUserId();
        var profile = await _shopService.GetShopProfileAsync(userId);
        var result = await _shopService.GetShopOrderDetailAsync(profile.ShopId, orderId);
        return Ok(ApiResponse.SuccessResponse(result));
    }

    /// <summary>
    /// Test Case 3.2: Cập nhật trạng thái đơn hàng
    /// </summary>
    [HttpPost("UpdateOrderStatus")]
    [Authorize(Roles = "SHOP,ADMIN")]
    public async Task<IActionResult> UpdateOrderStatus(
        [FromQuery] Guid orderId,
        [FromBody] UpdateOrderStatusDto dto)
    {
        if (orderId == Guid.Empty)
        {
            return BadRequest(ApiResponse.ErrorResponse("orderId is required."));
        }
        var userId = GetUserId();
        var profile = await _shopService.GetShopProfileAsync(userId);
        await _shopService.UpdateOrderStatusAsync(profile.ShopId, orderId, dto);
        return Ok(ApiResponse.SuccessResponse(null, "Order status updated successfully"));
    }

    /// <summary>
    /// Test Case 3.3: Thống kê doanh thu Shop
    /// </summary>
    [HttpGet("GetRevenueStatistics")]
    [Authorize(Roles = "SHOP,ADMIN")]
    public async Task<IActionResult> GetRevenueStatistics(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? periodType)
    {
        var userId = GetUserId();
        var profile = await _shopService.GetShopProfileAsync(userId);
        var result = await _shopService.GetShopRevenueAsync(profile.ShopId, fromDate, toDate, periodType);
        return Ok(ApiResponse.SuccessResponse(result));
    }

    /// <summary>
    /// Test Case 3.4: Xem & Trả lời đánh giá của khách
    /// </summary>
    [HttpGet("GetShopFeedbacks")]
    [Authorize(Roles = "SHOP,ADMIN")]
    public async Task<IActionResult> GetShopFeedbacks(
        [FromQuery] int? rating,
        [FromQuery] bool? hasResponse,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10)
    {
        var userId = GetUserId();
        var profile = await _shopService.GetShopProfileAsync(userId);
        var result = await _shopService.GetShopFeedbacksAsync(profile.ShopId, rating, hasResponse, pageIndex, pageSize);
        return Ok(ApiResponse.SuccessResponse(result));
    }

    [HttpPost("ReplyFeedback")]
    [Authorize(Roles = "SHOP,ADMIN")]
    public async Task<IActionResult> ReplyFeedback(
        [FromQuery] Guid feedbackId,
        [FromBody] FeedbackResponseRequestDto dto)
    {
        if (feedbackId == Guid.Empty)
        {
            return BadRequest(ApiResponse.ErrorResponse("feedbackId is required."));
        }
        var userId = GetUserId();
        var profile = await _shopService.GetShopProfileAsync(userId);
        var result = await _shopService.CreateFeedbackResponseAsync(profile.ShopId, feedbackId, dto);
        return Ok(ApiResponse.SuccessResponse(result, "Feedback response created successfully"));
    }

    /// <summary>
    /// Test Case 3.5: Xử lý yêu cầu hoàn trả hàng
    /// </summary>
    [HttpPost("ProcessReturnRequest")]
    [Authorize(Roles = "SHOP,ADMIN")]
    public async Task<IActionResult> ProcessReturnRequest(
        [FromQuery] Guid returnRequestId,
        [FromBody] ProcessReturnRequestDto dto)
    {
        if (returnRequestId == Guid.Empty)
        {
            return BadRequest(ApiResponse.ErrorResponse("returnRequestId is required."));
        }
        var userId = GetUserId();
        var profile = await _shopService.GetShopProfileAsync(userId);
        await _shopService.ProcessReturnRequestAsync(profile.ShopId, returnRequestId, dto);
        return Ok(ApiResponse.SuccessResponse(null, "Return request processed successfully"));
    }
}
