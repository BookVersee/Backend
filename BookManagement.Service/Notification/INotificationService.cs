using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BookManagement.Repository.Entities.Enums;

namespace BookManagement.Service.Notification;

public interface INotificationService
{
    Task<IEnumerable<NotificationResponse>> GetUserNotificationsAsync(Guid userId);
    Task<IEnumerable<NotificationResponse>> GetUnreadNotificationsAsync(Guid userId);
    Task<bool> MarkNotificationAsReadAsync(Guid userId, Guid notificationId);
    Task MarkAllNotificationsAsReadAsync(Guid userId);
    Task<NotificationResponse> CreateAndSendNotificationAsync(Guid userId, NotificationType type, Guid? referenceId, string content, string? imageUrl = null);
}
