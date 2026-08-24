namespace BookManagement.Service.Payment;

public class PaymentResponse
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string PaymentType { get; set; } = null!; // PAYMENT, REFUND
    public string Method { get; set; } = null!; // COD, ONLINE, BANK_TRANSFER
    public decimal Amount { get; set; }
    public string Status { get; set; } = null!; // PENDING, COMPLETED, FAILED
    public DateTimeOffset CreatedAt { get; set; }
}
