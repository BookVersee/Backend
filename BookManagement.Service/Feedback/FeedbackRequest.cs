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

    public class FeedbackResponseRequestDto
    {
        public Guid? FeedbackId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
    }

    public class ShopFeedbackQueryRequest
    {
        public int? Rating { get; set; }
        public bool? HasResponse { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
