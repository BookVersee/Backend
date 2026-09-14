using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookManagement.Repository.Data;
using BookManagement.Repository.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using NotificationEntity = BookManagement.Repository.Entities.Notification;

namespace BookManagement.Service.Notification;

/// Vị trí: Domain Service - Thực thi logic nghiệp vụ hệ thống, xử lý danh sách thông báo và lưu DbContext.
public class NotificationService : INotificationService
{
    private readonly AppDbContext _context;
    private readonly INotificationRealtimeNotifier? _realtimeNotifier;

    public NotificationService(AppDbContext context, INotificationRealtimeNotifier? realtimeNotifier = null)
    {
        _context = context;
        _realtimeNotifier = realtimeNotifier;
    }

    /// Chức năng: Tạo mới thông báo trong DB và bắn Realtime tức thì tới quả chuông của người dùng
    public async Task<NotificationResponse> CreateAndSendNotificationAsync(Guid userId, NotificationType type, Guid? referenceId, string content, string? imageUrl = null)
    {
        var entity = new NotificationEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            ReferenceId = referenceId,
            Content = content,
            ImageUrl = imageUrl,
            IsRead = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _context.Notifications.Add(entity);
        await _context.SaveChangesAsync();

        var response = MapToResponse(entity);

        if (_realtimeNotifier != null)
        {
            try
            {
                await _realtimeNotifier.SendNotificationAsync(userId, response);
            }
            catch
            {
                // Bỏ qua lỗi kết nối realtime nếu client không online
            }
        }

        return response;
    }

    /// Chức năng: Lấy danh sách toàn bộ thông báo cá nhân của người dùng
    public async Task<IEnumerable<NotificationResponse>> GetUserNotificationsAsync(Guid userId)
    {
        var notifications = await _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return notifications.Select(MapToResponse).ToList();
    }

    /// Chức năng: Lấy danh sách các thông báo chưa đọc của người dùng
    public async Task<IEnumerable<NotificationResponse>> GetUnreadNotificationsAsync(Guid userId)
    {
        var notifications = await _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return notifications.Select(MapToResponse).ToList();
    }

    /// Chức năng: Đánh dấu 1 thông báo cụ thể là đã đọc
    public async Task<bool> MarkNotificationAsReadAsync(Guid userId, Guid notificationId)
    {
        var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
        if (notification == null) return false;

        notification.IsRead = true;
        notification.UpdatedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    /// Chức năng: Đánh dấu tất cả thông báo của người dùng là đã đọc
    public async Task MarkAllNotificationsAsReadAsync(Guid userId)
    {
        var unreadNotifications = await _context.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToListAsync();
        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
            notification.UpdatedAt = DateTimeOffset.UtcNow;
        }
        await _context.SaveChangesAsync();
    }

    private static NotificationResponse MapToResponse(NotificationEntity notification)
    {
        return new NotificationResponse
        {
            Id = notification.Id,
            UserId = notification.UserId,
            Type = notification.Type.ToString(),
            ReferenceId = notification.ReferenceId,
            Content = notification.Content ?? string.Empty,
            ImageUrl = notification.ImageUrl,
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt
        };
    }
}
