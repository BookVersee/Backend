using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using BookManagement.Api.Extensions;
using BookManagement.Repository.Entities.Enums;
using BookManagement.Service.Chat;
using BookManagement.Service.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookManagement.Api.Controllers;

/// Vị trí: Api Controller - Tiếp nhận HTTP Request từ Frontend, kiểm tra đầu vào và trả về ApiResponse.
[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    /// Chức năng: Khách hàng xem danh sách hội thoại chat
    [HttpGet("GetUserConversations")]
    [Authorize(Roles = "CUSTOMER,SHOP,ADMIN,SUPER_ADMIN")]
    public async Task<IActionResult> GetUserConversations()
    {
        var (userId, role) = User.GetUserInfo();
        var conversations = await _chatService.GetUserChatThreadsAsync(userId);
        return Ok(ApiResponse.SuccessResponse(conversations));
    }

    /// Chức năng: Shop xem danh sách khách hàng chat với mình
    [HttpGet("GetShopConversations")]
    [Authorize(Roles = "SHOP,ADMIN,SUPER_ADMIN")]
    public async Task<IActionResult> GetShopConversations([FromQuery] Guid? shopId = null)
    {
        var (userId, role) = User.GetUserInfo();
        var targetShopId = (role == UserRole.SHOP) ? userId : (shopId ?? userId);
        var conversations = await _chatService.GetShopChatThreadsAsync(targetShopId);
        return Ok(ApiResponse.SuccessResponse(conversations));
    }

    /// Chức năng: Xem lịch sử tin nhắn của 1 phòng chat
    [HttpGet("GetConversationMessages")]
    [Authorize(Roles = "CUSTOMER,SHOP,ADMIN,SUPER_ADMIN")]
    public async Task<IActionResult> GetConversationMessages(Guid chatId)
    {
        var (userId, role) = User.GetUserInfo();
        var messages = await _chatService.GetChatMessagesAsync(chatId, userId);
        return Ok(ApiResponse.SuccessResponse(messages));
    }

    /// Chức năng: Gửi tin nhắn mới và bắn thông báo SignalR realtime
    [HttpPost("SendMessage")]
    [Authorize(Roles = "CUSTOMER,SHOP,ADMIN,SUPER_ADMIN")]
    public async Task<IActionResult> SendMessage(SendMessageDto dto)
    {
        var (senderId, role) = User.GetUserInfo();
        var messageDto = await _chatService.SendMessageAsync(senderId, dto);
        return Ok(ApiResponse.SuccessResponse(messageDto, "Message sent successfully"));
    }
}
