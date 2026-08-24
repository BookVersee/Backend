namespace BookManagement.Service.Chat;

public class MessageResponse
{
    public Guid Id { get; set; }
    public Guid ChatId { get; set; }
    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = null!;
    public string Content { get; set; } = null!;
    public string? ImageUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class ChatResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ShopId { get; set; }
    public string ShopName { get; set; } = null!;
    public DateTimeOffset UpdatedAt { get; set; }
    public List<MessageResponse> Messages { get; set; } = new();
}
