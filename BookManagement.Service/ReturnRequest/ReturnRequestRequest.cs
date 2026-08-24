namespace BookManagement.Service.ReturnRequest;

public class CreateReturnRequestRequest
{
    public Guid OrderDetailId { get; set; }
    public required string ReasonType { get; set; } // WRONG_ITEM, DAMAGED, DEFECTIVE
    public required string DetailedReason { get; set; }
    public string? ImageUrl { get; set; }
}

public class UpdateReturnStatusRequest
{
    public required string Status { get; set; }
    public decimal? RefundAmount { get; set; }
}
