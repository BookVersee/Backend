using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using BookManagement.Api.Hubs;
using BookManagement.Repository.Data;
using BookManagement.Service.Chat;
using BookManagement.Service.Dtos;
using BookManagement.Service.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BookManagement.Api.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly ChatService _chatService;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly AppDbContext _db;

    public ChatController(ChatService chatService, IHubContext<ChatHub> hubContext, AppDbContext db)
    {
        _chatService = chatService;
        _hubContext = hubContext;
        _db = db;
    }

    private Guid GetUserId()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(userIdStr, out var id) ? id : Guid.Empty;
    }

    /// <summary>
    /// Test Case 2.1: Khách hàng xem danh sách cuộc trò chuyện
    /// </summary>
    [HttpGet("GetUserConversations")]
    public async Task<IActionResult> GetUserConversations()
    {
        var userId = GetUserId();
        var conversations = await _chatService.GetUserChatThreadsAsync(userId);
        return Ok(ApiResponse.SuccessResponse(conversations));
    }

    /// <summary>
    /// Test Case 2.2: Shop xem danh sách khách hàng đang chat
    /// </summary>
    [HttpGet("GetShopConversations")]
    public async Task<IActionResult> GetShopConversations([FromQuery] Guid? shopId)
    {
        var userId = GetUserId();
        var shop = await _db.Shops.FirstOrDefaultAsync(s => s.UserId == userId);
        if (shop == null)
        {
            return NotFound(ApiResponse.ErrorResponse("Shop not found for this user."));
        }

        // Xác minh shopId truyền vào phải thuộc về Shop do userId sở hữu
        if (shopId.HasValue && shopId.Value != Guid.Empty && shopId.Value != shop.Id)
        {
            return StatusCode(403, ApiResponse.ErrorResponse("Forbidden: You do not own this shop."));
        }

        var conversations = await _chatService.GetShopChatThreadsAsync(shop.Id);
        return Ok(ApiResponse.SuccessResponse(conversations));
    }

    /// <summary>
    /// Test Case 2.3: Xem lịch sử tin nhắn trong phòng chat
    /// </summary>
    [HttpGet("GetConversationMessages")]
    public async Task<IActionResult> GetConversationMessages([FromQuery] Guid chatId)
    {
        var userId = GetUserId();
        var messages = await _chatService.GetChatMessagesAsync(chatId, userId);
        return Ok(ApiResponse.SuccessResponse(messages));
    }

    /// <summary>
    /// Test Case 2.4: Gửi tin nhắn mới & Broadcast Real-Time trong Group phòng chat
    /// </summary>
    [HttpPost("SendMessage")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageDto dto)
    {
        var senderId = GetUserId();
        Guid targetUserId = senderId;
        Guid targetShopId = dto.ShopId ?? Guid.Empty;

        if (dto.ChatId.HasValue && dto.ChatId.Value != Guid.Empty)
        {
            var chat = await _db.Chats
                .Include(c => c.Shop)
                .FirstOrDefaultAsync(c => c.Id == dto.ChatId.Value);

            if (chat == null)
            {
                return NotFound(ApiResponse.ErrorResponse("Chat not found."));
            }

            // Xác minh người gửi thuộc cuộc trò chuyện
            if (chat.UserId != senderId && (chat.Shop == null || chat.Shop.UserId != senderId))
            {
                return StatusCode(403, ApiResponse.ErrorResponse("Forbidden: You do not belong to this conversation."));
            }

            targetUserId = chat.UserId;
            targetShopId = chat.ShopId;
        }
        else if (dto.ShopId.HasValue && dto.ShopId.Value != Guid.Empty)
        {
            var senderShop = await _db.Shops.FirstOrDefaultAsync(s => s.UserId == senderId);
            if (senderShop != null && senderShop.Id == dto.ShopId.Value && dto.UserId.HasValue)
            {
                // Shop is replying to a customer
                targetUserId = dto.UserId.Value;
                targetShopId = senderShop.Id;
            }
            else
            {
                // Customer is sending to shop
                targetUserId = senderId;
                targetShopId = dto.ShopId.Value;
            }
        }

        if (targetShopId == Guid.Empty)
        {
            return BadRequest(ApiResponse.ErrorResponse("ShopId or ChatId is required."));
        }

        var messageDto = await _chatService.SendMessageAsync(targetUserId, targetShopId, dto.Content, dto.ImageUrl, senderId);

        // Broadcast real-time CHỈ trong group phòng chat cụ thể (loại bỏ broadcast toàn hệ thống Clients.All)
        string roomName = $"chat_{messageDto.ChatId}";
        await _hubContext.Clients.Group(roomName).SendAsync("ReceiveMessage", messageDto);

        return Ok(ApiResponse.SuccessResponse(messageDto, "Message sent successfully"));
    }
}
