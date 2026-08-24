using BookManagement.Repository.Abstractions;
using BookManagement.Repository.Entities.Enums;

namespace BookManagement.Service.ReturnRequest;

public class ReturnRequestService : IReturnRequestService
{
    private readonly IReturnRequestRepository _returnRequestRepository;

    public ReturnRequestService(IReturnRequestRepository returnRequestRepository)
    {
        _returnRequestRepository = returnRequestRepository;
    }

    public async Task<ReturnRequestResponse> CreateReturnRequestAsync(CreateReturnRequestRequest request)
    {
        var returnRequest = new BookManagement.Repository.Entities.ReturnRequest
        {
            OrderDetailId = request.OrderDetailId,
            ReasonType = (ReasonType)Enum.Parse(typeof(ReasonType), request.ReasonType),
            DetailedReason = request.DetailedReason,
            ImageUrl = request.ImageUrl,
            Status = ReturnRequestStatus.PENDING,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _returnRequestRepository.AddAsync(returnRequest);
        return MapToResponse(returnRequest);
    }

    public async Task<ReturnRequestResponse> GetReturnRequestAsync(Guid returnRequestId)
    {
        var returnRequest = await _returnRequestRepository.GetByIdAsync(returnRequestId);
        if (returnRequest == null)
            throw new Exception("Return request not found");
        return MapToResponse(returnRequest);
    }

    public async Task<IEnumerable<ReturnRequestResponse>> GetReturnRequestsByUserAsync(Guid userId)
    {
        var returnRequests = await _returnRequestRepository.GetByUserIdAsync(userId);
        return returnRequests.Select(MapToResponse).ToList();
    }

    public async Task<IEnumerable<ReturnRequestResponse>> GetReturnRequestsByStatusAsync(string status)
    {
        var returnRequests = await _returnRequestRepository.GetByStatusAsync(status);
        return returnRequests.Select(MapToResponse).ToList();
    }

    public async Task<ReturnRequestResponse> UpdateReturnRequestStatusAsync(Guid returnRequestId, UpdateReturnStatusRequest request)
    {
        var returnRequest = await _returnRequestRepository.GetByIdAsync(returnRequestId);
        if (returnRequest == null)
            throw new Exception("Return request not found");

        returnRequest.Status = (ReturnRequestStatus)Enum.Parse(typeof(ReturnRequestStatus), request.Status);
        if (request.RefundAmount.HasValue)
            returnRequest.RefundAmount = request.RefundAmount.Value;

        await _returnRequestRepository.UpdateAsync(returnRequest);
        return MapToResponse(returnRequest);
    }

    private static ReturnRequestResponse MapToResponse(BookManagement.Repository.Entities.ReturnRequest returnRequest)
    {
        return new ReturnRequestResponse
        {
            Id = returnRequest.Id,
            OrderDetailId = returnRequest.OrderDetailId,
            ReasonType = returnRequest.ReasonType.ToString(),
            DetailedReason = returnRequest.DetailedReason,
            ImageUrl = returnRequest.ImageUrl,
            Status = returnRequest.Status.ToString(),
            RefundAmount = returnRequest.RefundAmount,
            CreatedAt = returnRequest.CreatedAt
        };
    }
}
