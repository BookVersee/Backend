using System;
using System.Threading.Tasks;
using BookManagement.Service.Chat;
using Microsoft.AspNetCore.SignalR;

namespace BookManagement.Api.Hubs;

/// <summary>
/// Service Notifier Real-time (Triển khai giao diện IChatRealtimeNotifier).
/// Chức năng chính:
/// - Đóng vai trò cầu nối giúp tầng Service (ChatService) phát tín hiệu Real-time cho SignalR ChatHub.
/// - Đảm bảo tuân thủ 3-Layer Architecture (Tầng Service không dính cứng vào thư viện SignalR).
/// Vị trí: Presentation/Infrastructure Layer (BookManagement.Api/Hubs).
/// </summary>
public class ChatRealtimeNotifier : IChatRealtimeNotifier
{
    private readonly IHubContext<ChatHub> _hubContext;

    public ChatRealtimeNotifier(IHubContext<ChatHub> hubContext)
    {
        _hubContext = hubContext;
    }

    /// <summary>
    /// Bắn thông báo tin nhắn mới tới tất cả Client đang ở trong phòng chat (Group SignalR)
    /// </summary>
    public async Task BroadcastMessageAsync(Guid chatId, MessageDto message)
    {
        string roomName = $"chat_{chatId}";
        await _hubContext.Clients.Group(roomName).SendAsync("ReceiveMessage", message);
    }
}
