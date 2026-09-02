using System;
using System.Threading.Tasks;
using BookManagement.Service.Notification;
using Microsoft.AspNetCore.SignalR;

namespace BookManagement.Api.Hubs;

/// <summary>
/// Infrastructure Notifier: Bắn thông báo Realtime quả chuông qua NotificationHub.
/// </summary>
public class NotificationRealtimeNotifier : INotificationRealtimeNotifier
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationRealtimeNotifier(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendNotificationAsync(Guid userId, NotificationResponse notification)
    {
        // Gửi qua cả UserIdentifier và User Group để đảm bảo nhận được 100%
        await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", notification);
        await _hubContext.Clients.Group($"user_{userId}").SendAsync("ReceiveNotification", notification);
    }
}
