using System;
using System.Security.Claims;
using System.Threading.Tasks;
using BookManagement.Service.Dtos;
using BookManagement.Service.Services;
using BookStore.BE2.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookManagement.Api.Controllers;

[ApiController]
[Route("api/shop")]
public class ShopController : ControllerBase
{
    private readonly ShopService _shopService;
    private readonly ChatService _chatService;

    public ShopController(ShopService shopService, ChatService chatService)
    {
        _shopService = shopService;
        _chatService = chatService;
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("user_id")?.Value
            ?? User.FindFirst("sub")?.Value;
        return int.TryParse(claim, out var id) ? id : 1;
    }

    private async Task<int> GetShopIdAsync()
    {
        var userId = GetUserId();
        var profile = await _shopService.GetShopProfileAsync(userId);
        return profile.ShopId;
    }

    // 1. Shop & Inventory Management
    [HttpPost("register")]
    [Authorize]
    public async Task<IActionResult> RegisterShop([FromBody] ShopRegisterDto dto)
    {
        var userId = GetUserId();
        var result = await _shopService.RegisterShopAsync(userId, dto);
        return StatusCode(201, result);
    }

    [HttpGet("profile")]
    [Authorize(Roles = "SHOP")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetUserId();
        var result = await _shopService.GetShopProfileAsync(userId);
        return Ok(result);
    }

    [HttpPost("books")]
    [Authorize(Roles = "SHOP")]
    public async Task<IActionResult> CreateBook([FromBody] CreateBookRequestDto dto)
    {
        var shopId = await GetShopIdAsync();
        var result = await _shopService.CreateBookAsync(shopId, dto);
        return StatusCode(201, result);
    }

    [HttpGet("books/{book_id}")]
    [Authorize(Roles = "SHOP")]
    public async Task<IActionResult> GetBookById([FromRoute(Name = "book_id")] int bookId)
    {
        var shopId = await GetShopIdAsync();
        var result = await _shopService.GetBookByIdAsync(shopId, bookId);
        return Ok(result);
    }

    [HttpGet("books")]
    [Authorize(Roles = "SHOP")]
    public async Task<IActionResult> GetShopBooks([FromQuery] BookQueryDto query)
    {
        var shopId = await GetShopIdAsync();
        var result = await _shopService.GetShopBooksAsync(shopId, query);
        return Ok(result);
    }

    [HttpPut("books/{book_id}")]
    [Authorize(Roles = "SHOP")]
    public async Task<IActionResult> UpdateBook([FromRoute(Name = "book_id")] int bookId, [FromBody] UpdateBookRequestDto dto)
    {
        var shopId = await GetShopIdAsync();
        var result = await _shopService.UpdateBookAsync(shopId, bookId, dto);
        return Ok(result);
    }

    [HttpDelete("books/{book_id}")]
    [Authorize(Roles = "SHOP")]
    public async Task<IActionResult> DeleteBook([FromRoute(Name = "book_id")] int bookId)
    {
        var shopId = await GetShopIdAsync();
        await _shopService.DeleteBookAsync(shopId, bookId);
        return Ok(new { message = "Book marked as HIDDEN" });
    }

    // 2. Shop Order Processing, Revenue & Feedback
    [HttpGet("orders/{order_id}")]
    [Authorize(Roles = "SHOP")]
    public async Task<IActionResult> GetOrder([FromRoute(Name = "order_id")] int orderId)
    {
        var shopId = await GetShopIdAsync();
        var result = await _shopService.GetShopOrderDetailAsync(shopId, orderId);
        return Ok(result);
    }

    [HttpPatch("orders/{order_id}/status")]
    [Authorize(Roles = "SHOP")]
    public async Task<IActionResult> UpdateOrderStatus([FromRoute(Name = "order_id")] int orderId, [FromBody] UpdateOrderStatusDto dto)
    {
        var shopId = await GetShopIdAsync();
        await _shopService.UpdateOrderStatusAsync(shopId, orderId, dto);
        return Ok(new { message = "Order status updated successfully" });
    }

    [HttpGet("revenue")]
    [Authorize(Roles = "SHOP")]
    public async Task<IActionResult> GetRevenue(
        [FromQuery(Name = "from_date")] DateTime? fromDate,
        [FromQuery(Name = "to_date")] DateTime? toDate,
        [FromQuery(Name = "period_type")] string? periodType)
    {
        var shopId = await GetShopIdAsync();
        var result = await _shopService.GetShopRevenueAsync(shopId, fromDate, toDate, periodType);
        return Ok(result);
    }

    [HttpGet("feedbacks")]
    [Authorize(Roles = "SHOP")]
    public async Task<IActionResult> GetFeedbacks(
        [FromQuery] int? rating,
        [FromQuery(Name = "has_response")] bool? hasResponse,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10)
    {
        var shopId = await GetShopIdAsync();
        var result = await _shopService.GetShopFeedbacksAsync(shopId, rating, hasResponse, pageIndex, pageSize);
        return Ok(result);
    }

    [HttpPost("feedbacks/{feedback_id}/response")]
    [Authorize(Roles = "SHOP")]
    public async Task<IActionResult> CreateFeedbackResponse([FromRoute(Name = "feedback_id")] int feedbackId, [FromBody] FeedbackResponseRequestDto dto)
    {
        var shopId = await GetShopIdAsync();
        var result = await _shopService.CreateFeedbackResponseAsync(shopId, feedbackId, dto);
        return StatusCode(201, result);
    }

    [HttpPatch("return-requests/{return_request_id}")]
    [Authorize(Roles = "SHOP")]
    public async Task<IActionResult> ProcessReturnRequest([FromRoute(Name = "return_request_id")] int returnRequestId, [FromBody] ProcessReturnRequestDto dto)
    {
        var shopId = await GetShopIdAsync();
        await _shopService.ProcessReturnRequestAsync(shopId, returnRequestId, dto);
        return Ok(new { message = "Return request processed" });
    }

    // 6. Realtime Communication (Shop Chat REST endpoints)
    [HttpGet("chats")]
    [Authorize(Roles = "SHOP")]
    public async Task<IActionResult> GetShopChats()
    {
        var shopId = await GetShopIdAsync();
        var result = await _chatService.GetShopChatsAsync(shopId);
        return Ok(result);
    }

    [HttpGet("chats/{chat_id}/messages")]
    [Authorize(Roles = "SHOP")]
    public async Task<IActionResult> GetChatMessages(
        [FromRoute(Name = "chat_id")] int chatId,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 30)
    {
        var userId = GetUserId();
        var shopId = await GetShopIdAsync();
        var result = await _chatService.GetChatMessagesAsync(shopId, chatId, pageIndex, pageSize, userId);
        return Ok(result);
    }
}
