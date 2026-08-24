using BookManagement.Repository.Abstractions;
using BookManagement.Repository.Entities;

namespace BookManagement.Service.Payment;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;

    public PaymentService(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<PaymentResponse> GetPaymentAsync(Guid paymentId)
    {
        var payment = await _paymentRepository.GetByIdAsync(paymentId);
        if (payment == null)
            throw new Exception("Payment not found");
        return MapToResponse(payment);
    }

    public async Task<IEnumerable<PaymentResponse>> GetPaymentsByOrderIdAsync(Guid orderId)
    {
        var payments = await _paymentRepository.GetByOrderIdAsync(orderId);
        return payments.Select(MapToResponse).ToList();
    }

    public async Task<IEnumerable<PaymentResponse>> GetPaymentsByUserIdAsync(Guid userId)
    {
        var payments = await _paymentRepository.GetByUserIdAsync(userId);
        return payments.Select(MapToResponse).ToList();
    }

    public async Task<IEnumerable<PaymentResponse>> GetPendingPaymentsAsync()
    {
        var payments = await _paymentRepository.GetPendingPaymentsAsync();
        return payments.Select(MapToResponse).ToList();
    }

    private static PaymentResponse MapToResponse(BookManagement.Repository.Entities.Payment payment)
    {
        return new PaymentResponse
        {
            Id = payment.Id,
            OrderId = payment.OrderId,
            PaymentType = payment.PaymentType.ToString(),
            Method = payment.Method.ToString(),
            Amount = payment.Amount,
            Status = payment.Status.ToString(),
            CreatedAt = payment.CreatedAt
        };
    }
}
