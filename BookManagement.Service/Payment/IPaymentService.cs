namespace BookManagement.Service.Payment;

public interface IPaymentService
{
    Task<PaymentResponse> GetPaymentAsync(Guid paymentId);
    Task<IEnumerable<PaymentResponse>> GetPaymentsByOrderIdAsync(Guid orderId);
    Task<IEnumerable<PaymentResponse>> GetPaymentsByUserIdAsync(Guid userId);
    Task<IEnumerable<PaymentResponse>> GetPendingPaymentsAsync();
}
