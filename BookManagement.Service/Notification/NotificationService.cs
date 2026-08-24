using BookManagement.Repository.Abstractions;
using BookManagement.Repository.Entities;
using NotificationEntity = BookManagement.Repository.Entities.Notification;
namespace BookManagement.Service.Notification;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;

    public NotificationService(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<IEnumerable<NotificationResponse>> GetUserNotificationsAsync(Guid userId)
    {
        var notifications = await _notificationRepository.GetNotificationsByUserIdAsync(userId);
        return notifications.Select(MapToResponse).ToList();
    }

    public async Task<IEnumerable<NotificationResponse>> GetUnreadNotificationsAsync(Guid userId)
    {
        var notifications = await _notificationRepository.GetNotificationsByUserIdAsync(userId);
        return notifications.Where(n => !n.IsRead).Select(MapToResponse).ToList();
    }

    public async Task MarkNotificationAsReadAsync(Guid notificationId)
    {
        await _notificationRepository.MarkAsReadAsync(notificationId);
    }

    public async Task MarkAllNotificationsAsReadAsync(Guid userId)
    {
        var notifications = await _notificationRepository.GetNotificationsByUserIdAsync(userId);
        foreach (var notification in notifications.Where(n => !n.IsRead))
        {
            await _notificationRepository.MarkAsReadAsync(notification.Id);
        }
    }

    private static NotificationResponse MapToResponse(NotificationEntity notification)
    {
        return new NotificationResponse
        {
            Id = notification.Id,
            UserId = notification.UserId,
            Type = notification.Type.ToString(),
            ReferenceId = notification.ReferenceId,
            Content = notification.Content,
            ImageUrl = notification.ImageUrl,
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt
        };
    }
}
