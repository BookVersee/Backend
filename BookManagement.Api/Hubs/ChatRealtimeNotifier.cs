using System;
using System.Threading.Tasks;
using BookManagement.Service.Chat;
using Microsoft.AspNetCore.SignalR;

namespace BookManagement.Api.Hubs;

/// Vị trí: Infrastructure Notifier - Cầu nối giúp tầng Service phát tín hiệu Realtime qua SignalR ChatHub.
public class ChatRealtimeNotifier : IChatRealtimeNotifier
{
    private readonly IHubContext<ChatHub> _hubContext;

    public ChatRealtimeNotifier(IHubContext<ChatHub> hubContext)
    {
        _hubContext = hubContext;
    }

    /// Chức năng: Phát tín hiệu tin nhắn mới tới phòng chat Websocket
    public async Task BroadcastMessageAsync(Guid chatId, MessageDto message)
    {
        string roomName = $"chat_{chatId}";
        await _hubContext.Clients.Group(roomName).SendAsync("ReceiveMessage", message);
    }
}
