using System;
using BookManagement.Repository.Abstractions;

namespace BookManagement.Repository.Entities
{
    public class Response : BaseEntity<Guid>, IAuditableEntity
    {
        public Guid FeedbackId { get; set; }
        public Guid ShopId { get; set; }
        public string Content { get; set; } = null!;
        public string? ImageUrl { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }

        // Navigation Properties
        public Feedback Feedback { get; set; } = null!;
        public Shop Shop { get; set; } = null!;
    }
}
