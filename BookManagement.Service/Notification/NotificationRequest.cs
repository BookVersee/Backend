using System;
using BookManagement.Repository.Entities.Enums;

namespace BookManagement.Service.Notification
{
    public class CreateNotificationRequest
    {
        public Guid UserId { get; set; }
        public NotificationType Type { get; set; }
        public Guid? ReferenceId { get; set; }
        public string Content { get; set; } = null!;
    }
}
