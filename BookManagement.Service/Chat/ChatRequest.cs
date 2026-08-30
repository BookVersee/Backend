using System;

namespace BookManagement.Service.Chat
{
    public class SendMessageDto
    {
        public Guid? ChatId { get; set; }
        public Guid? ShopId { get; set; }
        public Guid? UserId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
    }
}
