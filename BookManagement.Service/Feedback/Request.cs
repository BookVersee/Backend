using System;
using BookManagement.Repository.Entities.Enums;

namespace BookManagement.Service.Feedback
{
    public class CreateFeedbackRequest
    {
        public Guid ShopId { get; set; }
        public Guid OrderDetailId { get; set; }
        public int Rating { get; set; }
        public string? Content { get; set; }
        public FeedbackType Type { get; set; } = FeedbackType.BOOK;
        public string? ImageUrl { get; set; }
    }

    public class ReportResponseRequest
    {
        public string Reason { get; set; } = null!;
    }
}
