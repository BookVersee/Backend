using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using BookManagement.Api.Extensions;
using BookManagement.Service.Chat;
using BookManagement.Service.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookManagement.Api.Controllers;

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

    /// <summary>
    /// Test Case 2.1: Khách hàng xem danh sách cuộc trò chuyện
    /// </summary>
    [HttpGet("GetUserConversations")]
    public async Task<IActionResult> GetUserConversations()
    {
        var userId = User.GetUserId();
        var conversations = await _chatService.GetUserChatThreadsAsync(userId);
        return Ok(ApiResponse.SuccessResponse(conversations));
    }

    /// <summary>
    /// Test Case 2.2: Shop xem danh sách khách hàng đang chat
    /// </summary>
    [HttpGet("GetShopConversations")]
    public async Task<IActionResult> GetShopConversations(Guid shopId)
    {
        var conversations = await _chatService.GetShopChatThreadsAsync(shopId);
        return Ok(ApiResponse.SuccessResponse(conversations));
    }

    /// <summary>
    /// Test Case 2.3: Xem lịch sử tin nhắn trong phòng chat
    /// </summary>
    [HttpGet("GetConversationMessages")]
    public async Task<IActionResult> GetConversationMessages(Guid chatId)
    {
        var userId = User.GetUserId();
        var messages = await _chatService.GetChatMessagesAsync(chatId, userId);
        return Ok(ApiResponse.SuccessResponse(messages));
    }

    /// <summary>
    /// Test Case 2.4: Gửi tin nhắn mới & Broadcast Real-Time
    /// </summary>
    [HttpPost("SendMessage")]
    public async Task<IActionResult> SendMessage(SendMessageDto dto)
    {
        var senderId = User.GetUserId();
        var messageDto = await _chatService.SendMessageAsync(senderId, dto);
        return Ok(ApiResponse.SuccessResponse(messageDto, "Message sent successfully"));
    }
}
