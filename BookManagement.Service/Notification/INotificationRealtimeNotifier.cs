using System;
using System.Threading.Tasks;

namespace BookManagement.Service.Notification;

public interface INotificationRealtimeNotifier
{
    Task SendNotificationAsync(Guid userId, NotificationResponse notification);
}
