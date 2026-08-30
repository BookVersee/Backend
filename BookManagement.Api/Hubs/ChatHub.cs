using System;
using System.Security.Claims;
using System.Threading.Tasks;
using BookManagement.Service.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BookManagement.Api.Hubs;

/// <summary>
///  thu vien HTTP/Websocket
/// SignalR Real-time Hub: Quản lý luồng kết nối Websocket trực tiếp giữa Khách hàng và Chủ Shop.
/// Chức năng chính:
/// - Quản lý tham gia/rời phòng chat (JoinRoom / LeaveRoom).
/// - Nhận và phát tin nhắn thời gian thực mà không cần reload trang.
/// Vị trí: Presentation Layer (BookManagement.Api/Hubs).
/// </summary>
[Authorize]
public class ChatHub : Hub
{
    private readonly ChatService _chatService;

    public ChatHub(ChatService chatService)
    {
        _chatService = chatService;
    }

    private Guid GetUserId()
    {
        var userIdStr = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? Context.User?.FindFirstValue("sub");
        return Guid.TryParse(userIdStr, out var id) ? id : Guid.Empty;
    }

    /// <summary>
    /// Tham gia vào phòng chat cụ thể (roomName = "chat_{chatId}")
    /// </summary>
    public async Task JoinRoom(string roomName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
    }

    /// <summary>
    /// Rời khỏi phòng chat
    /// </summary>
    public async Task LeaveRoom(string roomName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomName);
    }

    /// <summary>
    /// Khách hàng gửi tin nhắn cho Cửa hàng qua Websocket
    /// </summary>
    public async Task SendMessageToShop(Guid shopId, string content, string? imageUrl = null)
    {
        var senderId = GetUserId();
        var messageDto = await _chatService.SendMessageAsync(senderId, shopId, content, imageUrl, senderId);

        string roomName = $"chat_{messageDto.ChatId}";
        await Clients.Group(roomName).SendAsync("ReceiveMessage", messageDto);
    }

    /// <summary>
    /// Chủ Shop phản hồi tin nhắn cho Khách hàng qua Websocket
    /// </summary>
    public async Task SendMessageToUser(Guid userId, Guid shopId, string content, string? imageUrl = null)
    {
        var senderId = GetUserId();
        var messageDto = await _chatService.SendMessageAsync(userId, shopId, content, imageUrl, senderId);

        string roomName = $"chat_{messageDto.ChatId}";
        await Clients.Group(roomName).SendAsync("ReceiveMessage", messageDto);
    }
}
