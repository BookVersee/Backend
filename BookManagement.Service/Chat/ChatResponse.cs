using System;

namespace BookManagement.Service.Chat
{
    public class ChatThreadDto
    {
        public Guid ChatId { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public Guid ShopId { get; set; }
        public string? LastMessage { get; set; }
        public int UnreadCount { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }

    public class MessageDto
    {
        public Guid MessageId { get; set; }
        public Guid ChatId { get; set; }
        public Guid SenderId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public bool IsRead { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
