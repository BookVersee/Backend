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
        Task MarkAsReadAsync(Guid notificationId);
        Task CreateNotificationAsync(Notification notification);
    }
}
