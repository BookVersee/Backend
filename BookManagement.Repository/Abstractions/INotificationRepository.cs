using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BookManagement.Repository.Entities;

namespace BookManagement.Repository.Abstractions
{
    public interface INotificationRepository
    {
        Task<IEnumerable<Notification>> GetNotificationsByUserIdAsync(Guid userId);
        Task<Notification?> GetByIdAsync(Guid id);
        Task<bool> MarkAsReadAsync(Guid userId, Guid notificationId);
        Task MarkAllAsReadByUserIdAsync(Guid userId);
        Task CreateNotificationAsync(Notification notification);
    }
}
