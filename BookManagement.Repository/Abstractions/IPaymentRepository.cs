using BookManagement.Repository.Entities;

namespace BookManagement.Repository.Abstractions;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid paymentId);
    Task<IEnumerable<Payment>> GetByOrderIdAsync(Guid orderId);
    Task<IEnumerable<Payment>> GetByUserIdAsync(Guid userId);
    Task AddAsync(Payment payment);
    Task UpdateAsync(Payment payment);
    Task<IEnumerable<Payment>> GetPendingPaymentsAsync();
}
