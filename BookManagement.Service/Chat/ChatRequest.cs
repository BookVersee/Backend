namespace BookManagement.Service.Chat;

public class SendMessageRequest
{
    public Guid ChatId { get; set; }
    public Guid SenderId { get; set; }
    public required string Content { get; set; }
    public string? ImageUrl { get; set; }
}
