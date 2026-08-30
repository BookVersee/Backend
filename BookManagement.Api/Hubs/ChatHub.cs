using System;
using System.Security.Claims;
using System.Threading.Tasks;
using BookManagement.Service.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BookManagement.Api.Hubs;

/// Vị trí: Presentation/Hub - Quản lý luồng kết nối Websocket thời gian thực giữa Khách hàng và Chủ Shop.
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

    /// Chức năng: Tham gia vào phòng chat cụ thể
    public async Task JoinRoom(string roomName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
    }

    /// Chức năng: Rời khỏi phòng chat
    public async Task LeaveRoom(string roomName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomName);
    }

    /// Chức năng: Khách hàng gửi tin nhắn cho Cửa hàng qua Websocket
    public async Task SendMessageToShop(Guid shopId, string content, string? imageUrl = null)
    {
        var senderId = GetUserId();
        var messageDto = await _chatService.SendMessageAsync(senderId, shopId, content, imageUrl, senderId);

        string roomName = $"chat_{messageDto.ChatId}";
        await Clients.Group(roomName).SendAsync("ReceiveMessage", messageDto);
    }

    /// Chức năng: Chủ Shop phản hồi tin nhắn cho Khách hàng qua Websocket
    public async Task SendMessageToUser(Guid userId, Guid shopId, string content, string? imageUrl = null)
    {
        var senderId = GetUserId();
        var messageDto = await _chatService.SendMessageAsync(userId, shopId, content, imageUrl, senderId);

        string roomName = $"chat_{messageDto.ChatId}";
        await Clients.Group(roomName).SendAsync("ReceiveMessage", messageDto);
    }
}
