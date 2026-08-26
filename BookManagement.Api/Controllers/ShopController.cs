using System;
using System.Security.Claims;
using System.Threading.Tasks;
using BookManagement.Service.Dtos;
using BookManagement.Service.Models;
using BookManagement.Service.Services;
using BookManagement.Repository.Entities.Enums;
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

    [HttpPost("register")]
    public async Task<IActionResult> RegisterShop([FromBody] ShopRegisterDto dto)
    {
        var userId = GetUserId();
        var result = await _shopService.RegisterShopAsync(userId, dto);
        return Ok(ApiResponse.SuccessResponse(result, "Shop registered successfully"));
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetShopProfile()
    {
        var userId = GetUserId();
        var result = await _shopService.GetShopProfileAsync(userId);
        return Ok(ApiResponse.SuccessResponse(result));
    }

    [HttpPost("books")]
    [Authorize(Roles = "SHOP,ADMIN")]
    public async Task<IActionResult> CreateBook([FromBody] CreateBookRequestDto dto)
    {
        var userId = GetUserId();
        var profile = await _shopService.GetShopProfileAsync(userId);
        var result = await _shopService.CreateBookAsync(profile.ShopId, dto);
        return StatusCode(201, ApiResponse.SuccessResponse(result, "Book created successfully"));
    }

    [HttpGet("books/{book_id}")]
    [Authorize(Roles = "SHOP,ADMIN")]
    public async Task<IActionResult> GetBookById([FromRoute(Name = "book_id")] Guid bookId)
    {
        var userId = GetUserId();
        var profile = await _shopService.GetShopProfileAsync(userId);
        var result = await _shopService.GetBookByIdAsync(profile.ShopId, bookId);
        return Ok(ApiResponse.SuccessResponse(result));
    }

    [HttpGet("books")]
    [Authorize(Roles = "SHOP,ADMIN")]
    public async Task<IActionResult> GetShopBooks([FromQuery] BookQueryDto query)
    {
        var userId = GetUserId();
        var profile = await _shopService.GetShopProfileAsync(userId);
        var result = await _shopService.GetShopBooksAsync(profile.ShopId, query);
        return Ok(ApiResponse.SuccessResponse(result));
    }

    [HttpPut("books/{book_id}")]
    [Authorize(Roles = "SHOP,ADMIN")]
    public async Task<IActionResult> UpdateBook([FromRoute(Name = "book_id")] Guid bookId, [FromBody] UpdateBookRequestDto dto)
    {
        var userId = GetUserId();
        var profile = await _shopService.GetShopProfileAsync(userId);
        var result = await _shopService.UpdateBookAsync(profile.ShopId, bookId, dto);
        return Ok(ApiResponse.SuccessResponse(result, "Book updated successfully"));
    }

    [HttpDelete("books/{book_id}")]
    [Authorize(Roles = "SHOP,ADMIN")]
    public async Task<IActionResult> DeleteBook([FromRoute(Name = "book_id")] Guid bookId)
    {
        var userId = GetUserId();
        var profile = await _shopService.GetShopProfileAsync(userId);
        await _shopService.DeleteBookAsync(profile.ShopId, bookId);
        return Ok(ApiResponse.SuccessResponse(null, "Book status updated to HIDDEN successfully"));
    }

    [HttpGet("orders/{order_id}")]
    [Authorize(Roles = "SHOP,ADMIN")]
    public async Task<IActionResult> GetShopOrderDetail([FromRoute(Name = "order_id")] Guid orderId)
    {
        var userId = GetUserId();
        var profile = await _shopService.GetShopProfileAsync(userId);
        var result = await _shopService.GetShopOrderDetailAsync(profile.ShopId, orderId);
        return Ok(ApiResponse.SuccessResponse(result));
    }

    [HttpPut("orders/{order_id}/status")]
    [Authorize(Roles = "SHOP,ADMIN")]
    public async Task<IActionResult> UpdateOrderStatus([FromRoute(Name = "order_id")] Guid orderId, [FromBody] UpdateOrderStatusDto dto)
    {
        var userId = GetUserId();
        var profile = await _shopService.GetShopProfileAsync(userId);
        await _shopService.UpdateOrderStatusAsync(profile.ShopId, orderId, dto);
        return Ok(ApiResponse.SuccessResponse(null, "Order status updated successfully"));
    }

    [HttpGet("revenue")]
    [Authorize(Roles = "SHOP,ADMIN")]
    public async Task<IActionResult> GetShopRevenue([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, [FromQuery] string? periodType)
    {
        var userId = GetUserId();
        var profile = await _shopService.GetShopProfileAsync(userId);
        var result = await _shopService.GetShopRevenueAsync(profile.ShopId, fromDate, toDate, periodType);
        return Ok(ApiResponse.SuccessResponse(result));
    }

    [HttpGet("feedbacks")]
    [Authorize(Roles = "SHOP,ADMIN")]
    public async Task<IActionResult> GetShopFeedbacks([FromQuery] int? rating, [FromQuery] bool? hasResponse, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
    {
        var userId = GetUserId();
        var profile = await _shopService.GetShopProfileAsync(userId);
        var result = await _shopService.GetShopFeedbacksAsync(profile.ShopId, rating, hasResponse, pageIndex, pageSize);
        return Ok(ApiResponse.SuccessResponse(result));
    }

    [HttpPost("feedbacks/{feedback_id}/response")]
    [Authorize(Roles = "SHOP,ADMIN")]
    public async Task<IActionResult> CreateFeedbackResponse([FromRoute(Name = "feedback_id")] Guid feedbackId, [FromBody] FeedbackResponseRequestDto dto)
    {
        var userId = GetUserId();
        var profile = await _shopService.GetShopProfileAsync(userId);
        var result = await _shopService.CreateFeedbackResponseAsync(profile.ShopId, feedbackId, dto);
        return StatusCode(201, ApiResponse.SuccessResponse(result, "Feedback response created successfully"));
    }

    [HttpPut("return-requests/{return_request_id}")]
    [Authorize(Roles = "SHOP,ADMIN")]
    public async Task<IActionResult> ProcessReturnRequest([FromRoute(Name = "return_request_id")] Guid returnRequestId, [FromBody] ProcessReturnRequestDto dto)
    {
        var userId = GetUserId();
        var profile = await _shopService.GetShopProfileAsync(userId);
        await _shopService.ProcessReturnRequestAsync(profile.ShopId, returnRequestId, dto);
        return Ok(ApiResponse.SuccessResponse(null, "Return request processed successfully"));
    }
}
