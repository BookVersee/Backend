namespace BookManagement.Service.ReturnRequest;

public class ReturnRequestResponse
{
    public Guid Id { get; set; }
    public Guid OrderDetailId { get; set; }
    public string ReasonType { get; set; } = null!; // WRONG_ITEM, DAMAGED, DEFECTIVE
    public string DetailedReason { get; set; } = null!;
    public string? ImageUrl { get; set; }
    public string Status { get; set; } = null!; // PENDING, APPROVED, REJECTED
    public decimal? RefundAmount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
