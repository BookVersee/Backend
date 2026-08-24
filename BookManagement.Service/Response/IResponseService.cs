namespace BookManagement.Service.Response;

public interface IResponseService
{
    Task<ResponseResponse> GetResponseByFeedbackIdAsync(Guid feedbackId);
    Task<IEnumerable<ResponseResponse>> GetResponsesByShopAsync(Guid shopId);
}
