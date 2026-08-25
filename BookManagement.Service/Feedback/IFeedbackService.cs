using System.Threading.Tasks;

namespace BookManagement.Service.Feedback;

public interface IFeedbackService
{
    Task<PagedFeedbackResponse> GetShopFeedbacksAsync(int shopId, int? rating, bool? hasResponse, int pageIndex, int pageSize);
    Task<FeedbackReplyCreatedResponse> CreateFeedbackResponseAsync(int shopId, int feedbackId, CreateFeedbackResponseRequest dto);
    Task ProcessReturnRequestAsync(int shopId, int returnRequestId, ProcessReturnRequest dto);
}
