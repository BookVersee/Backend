namespace BookManagement.Service.Response;

public class ResponseResponse
{
    public Guid Id { get; set; }
    public Guid FeedbackId { get; set; }
    public Guid ShopId { get; set; }
    public string Content { get; set; } = null!;
    public string? ImageUrl { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
