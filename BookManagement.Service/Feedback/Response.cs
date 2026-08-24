using System;
using BookManagement.Repository.Entities.Enums;

namespace BookManagement.Service.Feedback
{
    public class FeedbackResponse
    {
        public Guid Id { get; set; }
        public Guid ShopId { get; set; }
        public string ShopName { get; set; } = null!;
        public Guid OrderDetailId { get; set; }
        public int Rating { get; set; }
        public string? Content { get; set; }
        public FeedbackType Type { get; set; }
        public string? ImageUrl { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public ShopResponseResponse? Response { get; set; }
    }

    public class ShopResponseResponse
    {
        public Guid Id { get; set; }
        public Guid FeedbackId { get; set; }
        public Guid ShopId { get; set; }
        public string Content { get; set; } = null!;
        public string? ImageUrl { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
