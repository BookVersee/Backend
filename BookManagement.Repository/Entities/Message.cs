using System;
using BookManagement.Repository.Abstractions;

namespace BookManagement.Repository.Entities
{
    public class Message : BaseEntity<Guid>, IAuditableEntity
    {
        public Guid ChatId { get; set; }
        public Guid SenderId { get; set; }
        public string? Content { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }

        // Navigation Properties
        public Chat Chat { get; set; } = null!;
        public User Sender { get; set; } = null!;
    }
}
