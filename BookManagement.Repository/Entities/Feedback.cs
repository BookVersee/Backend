using System;
using BookManagement.Repository.Abstractions;
using BookManagement.Repository.Entities.Enums;

namespace BookManagement.Repository.Entities
{
    public class Feedback : BaseEntity<Guid>, IAuditableEntity
    {
        public Guid ShopId { get; set; }
        public Guid OrderDetailId { get; set; }
        public int Rating { get; set; }
        public string? Content { get; set; }
        public FeedbackType Type { get; set; } = FeedbackType.BOOK;
        public string? ImageUrl { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }

        // Navigation Properties
        public Shop Shop { get; set; } = null!;
        public OrderDetail OrderDetail { get; set; } = null!;
        public Response? Response { get; set; }
    }
}
