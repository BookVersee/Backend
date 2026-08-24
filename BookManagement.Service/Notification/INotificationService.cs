namespace BookManagement.Service.Notification;

public interface INotificationService
{
    Task<IEnumerable<NotificationResponse>> GetUserNotificationsAsync(Guid userId);
    Task<IEnumerable<NotificationResponse>> GetUnreadNotificationsAsync(Guid userId);
    Task MarkNotificationAsReadAsync(Guid notificationId);
    Task MarkAllNotificationsAsReadAsync(Guid userId);
}
