using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookManagement.Service.Feedback
{
    public interface IFeedbackService
    {
        Task<IEnumerable<FeedbackResponse>> GetBookFeedbacksAsync(Guid bookId);
        Task<FeedbackResponse> CreateFeedbackAsync(Guid userId, CreateFeedbackRequest request);
        Task ReportResponseAsync(Guid userId, Guid responseId, ReportResponseRequest request);
    }
}
