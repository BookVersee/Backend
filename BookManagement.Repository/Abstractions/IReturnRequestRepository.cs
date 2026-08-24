using BookManagement.Repository.Entities;

namespace BookManagement.Repository.Abstractions;

public interface IReturnRequestRepository
{
    Task<ReturnRequest?> GetByIdAsync(Guid returnRequestId);
    Task<ReturnRequest?> GetByOrderDetailIdAsync(Guid orderDetailId);
    Task<IEnumerable<ReturnRequest>> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<ReturnRequest>> GetByStatusAsync(string status);
    Task AddAsync(ReturnRequest returnRequest);
    Task UpdateAsync(ReturnRequest returnRequest);
}
