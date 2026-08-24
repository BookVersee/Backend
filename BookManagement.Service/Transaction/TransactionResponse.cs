namespace BookManagement.Service.Transaction;

public class TransactionHistoryResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string ReferenceType { get; set; } = null!; // ORDER_PAYMENT, REFUND, SHIPPING_FEE, SHOP_REVENUE, WITHDRAWAL
    public Guid? ReferenceId { get; set; }
    public string TransactionType { get; set; } = null!; // IN, OUT
    public decimal Amount { get; set; }
    public string TransactionCode { get; set; } = null!;
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class BalanceResponse
{
    public decimal Balance { get; set; }
}
