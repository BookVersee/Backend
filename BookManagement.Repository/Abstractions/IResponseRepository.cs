using BookManagement.Repository.Entities;

namespace BookManagement.Repository.Abstractions;

public interface IResponseRepository
{
    Task<Response?> GetByIdAsync(Guid responseId);
    Task<Response?> GetByFeedbackIdAsync(Guid feedbackId);
    Task<IEnumerable<Response>> GetByShopIdAsync(Guid shopId);
    Task AddAsync(Response response);
    Task UpdateAsync(Response response);
}
