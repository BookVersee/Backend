namespace BookManagement.Service.ReturnRequest;

public interface IReturnRequestService
{
    Task<ReturnRequestResponse> CreateReturnRequestAsync(CreateReturnRequestRequest request);
    Task<ReturnRequestResponse> GetReturnRequestAsync(Guid returnRequestId);
    Task<IEnumerable<ReturnRequestResponse>> GetReturnRequestsByUserAsync(Guid userId);
    Task<IEnumerable<ReturnRequestResponse>> GetReturnRequestsByStatusAsync(string status);
    Task<ReturnRequestResponse> UpdateReturnRequestStatusAsync(Guid returnRequestId, UpdateReturnStatusRequest request);
}
