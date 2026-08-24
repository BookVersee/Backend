namespace BookManagement.Service.Notification;

public class NotificationResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Type { get; set; } = null!; // ORDER_UPDATE, PROMOTION, SYSTEM
    public Guid? ReferenceId { get; set; }
    public string Content { get; set; } = null!;
    public string? ImageUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
