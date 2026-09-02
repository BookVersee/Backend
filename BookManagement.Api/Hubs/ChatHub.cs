using System;
using System.Security.Claims;
using System.Threading.Tasks;
using BookManagement.Repository.Data;
using BookManagement.Service.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

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

    private readonly AppDbContext _db;

    public ChatHub(ChatService chatService, AppDbContext db)
    {
        _chatService = chatService;
        _db = db;
    }

    private Guid GetUserId()
    {
        var userIdStr = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? Context.User?.FindFirstValue("sub");
        return Guid.TryParse(userIdStr, out var id) ? id : Guid.Empty;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId != Guid.Empty)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            
            // Nếu là Shop hoặc User sở hữu Shop, tự động join vào Group shop_{id}
            var shop = await _db.Shops.AsNoTracking().FirstOrDefaultAsync(s => s.Id == userId || s.UserId == userId);
            if (shop != null)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"shop_{shop.Id}");
                await Groups.AddToGroupAsync(Context.ConnectionId, $"shop_{userId}");
            }
        }
        await base.OnConnectedAsync();
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
    /// Chủ Shop đăng ký lắng nghe group của Shop mình
    /// </summary>
    public async Task JoinShop(Guid shopId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"shop_{shopId}");
    }

    /// <summary>
    /// Khách hàng gửi tin nhắn cho Cửa hàng qua Websocket
    /// </summary>
    [Authorize(Roles = "CUSTOMER,SHOP,ADMIN,SUPER_ADMIN")]
    public async Task SendMessageToShop(Guid shopId, string content, string? imageUrl = null)
    {
        await SendMessageToShopAsync(shopId, content, imageUrl);
    }

    [Authorize(Roles = "CUSTOMER,SHOP,ADMIN,SUPER_ADMIN")]
    public async Task SendMessageToShopAsync(Guid shopId, string content, string? imageUrl = null)
    {
        var senderId = GetUserId();
        var messageDto = await _chatService.SendMessageAsync(senderId, shopId, content, imageUrl, senderId);

        string roomName = $"chat_{messageDto.ChatId}";
        await Clients.Group(roomName).SendAsync("ReceiveMessage", messageDto);

        // Lấy tên người gửi và số tin nhắn chưa đọc
        var sender = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == senderId);
        string senderName = sender?.FullName ?? sender?.Username ?? "Khách hàng";
        string messagePreview = !string.IsNullOrWhiteSpace(content)
            ? (content.Length > 60 ? content.Substring(0, 60) + "..." : content)
            : "[Hình ảnh]";

        int unreadCount = await _db.Messages
            .CountAsync(m => m.ChatId == messageDto.ChatId && !m.IsRead && m.SenderId == senderId);

        var notificationPayload = new
        {
            chatId = messageDto.ChatId,
            senderId = senderId,
            senderName = senderName,
            messagePreview = messagePreview,
            timestamp = messageDto.CreatedAt,
            unreadCount = unreadCount
        };

        // Bắn thông báo ngoài phòng chat tới group của Shop và đích danh User chủ shop
        await Clients.Group($"shop_{shopId}").SendAsync("ReceiveNewMessageNotification", notificationPayload);
        await Clients.User(shopId.ToString()).SendAsync("ReceiveNewMessageNotification", notificationPayload);
    }

    /// <summary>
    /// Chủ Shop phản hồi tin nhắn cho Khách hàng qua Websocket
    /// </summary>
    [Authorize(Roles = "SHOP,ADMIN,SUPER_ADMIN")]
    public async Task SendMessageToUser(Guid userId, Guid shopId, string content, string? imageUrl = null)
    {
        await SendMessageToUserAsync(userId, shopId, content, imageUrl);
    }

    [Authorize(Roles = "SHOP,ADMIN,SUPER_ADMIN")]
    public async Task SendMessageToUserAsync(Guid userId, Guid shopId, string content, string? imageUrl = null)
    {
        var senderId = GetUserId();
        var messageDto = await _chatService.SendMessageAsync(userId, shopId, content, imageUrl, senderId);

        string roomName = $"chat_{messageDto.ChatId}";
        await Clients.Group(roomName).SendAsync("ReceiveMessage", messageDto);

        // Lấy tên Shop gửi
        var shop = await _db.Shops.AsNoTracking().FirstOrDefaultAsync(s => s.Id == shopId);
        string senderName = shop?.ShopName ?? "Cửa hàng";
        string messagePreview = !string.IsNullOrWhiteSpace(content)
            ? (content.Length > 60 ? content.Substring(0, 60) + "..." : content)
            : "[Hình ảnh]";

        int unreadCount = await _db.Messages
            .CountAsync(m => m.ChatId == messageDto.ChatId && !m.IsRead && m.SenderId == senderId);

        var notificationPayload = new
        {
            chatId = messageDto.ChatId,
            senderId = senderId,
            senderName = senderName,
            messagePreview = messagePreview,
            timestamp = messageDto.CreatedAt,
            unreadCount = unreadCount
        };

        // Bắn thông báo ngoài phòng chat tới đích danh khách hàng
        await Clients.User(userId.ToString()).SendAsync("ReceiveNewMessageNotification", notificationPayload);
        await Clients.Group($"user_{userId}").SendAsync("ReceiveNewMessageNotification", notificationPayload);
    }
}
