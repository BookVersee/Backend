using System;
using BookManagement.Repository.Abstractions;
using BookManagement.Repository.Entities.Enums;

namespace BookManagement.Repository.Entities
{
    public class Notification : BaseEntity<Guid>, IAuditableEntity
    {
        public Guid UserId { get; set; }
        public NotificationType Type { get; set; } = NotificationType.SYSTEM;
        public Guid? ReferenceId { get; set; }
        public string? Content { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }

        // Navigation Properties
        public User User { get; set; } = null!;
    }
}
