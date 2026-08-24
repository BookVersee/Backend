using BookManagement.Repository.Abstractions;
using BookManagement.Repository.Entities;

namespace BookManagement.Service.Response;

public class ResponseService : IResponseService
{
    private readonly IResponseRepository _responseRepository;

    public ResponseService(IResponseRepository responseRepository)
    {
        _responseRepository = responseRepository;
    }

    public async Task<ResponseResponse> GetResponseByFeedbackIdAsync(Guid feedbackId)
    {
        var response = await _responseRepository.GetByFeedbackIdAsync(feedbackId);
        if (response == null)
            throw new Exception("Response not found");
        return MapToResponse(response);
    }

    public async Task<IEnumerable<ResponseResponse>> GetResponsesByShopAsync(Guid shopId)
    {
        var responses = await _responseRepository.GetByShopIdAsync(shopId);
        return responses.Select(MapToResponse).ToList();
    }

    private static ResponseResponse MapToResponse(BookManagement.Repository.Entities.Response response)
    {
        return new ResponseResponse
        {
            Id = response.Id,
            FeedbackId = response.FeedbackId,
            ShopId = response.ShopId,
            Content = response.Content,
            ImageUrl = response.ImageUrl,
            CreatedAt = response.CreatedAt
        };
    }
}
