using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BookManagement.Service.Chat;

public class ChatThreadResponse
{
    [JsonPropertyName("chat_id")]
    public int ChatId { get; set; }

    [JsonPropertyName("user_id")]
    public int UserId { get; set; }

    [JsonPropertyName("user_name")]
    public string UserName { get; set; } = string.Empty;

    [JsonPropertyName("shop_id")]
    public int ShopId { get; set; }

    [JsonPropertyName("last_message")]
    public string? LastMessage { get; set; }

    [JsonPropertyName("unread_count")]
    public int UnreadCount { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

public class MessageResponse
{
    [JsonPropertyName("message_id")]
    public int MessageId { get; set; }

    [JsonPropertyName("chat_id")]
    public int ChatId { get; set; }

    [JsonPropertyName("sender_id")]
    public int SenderId { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("image_url")]
    public string? ImageUrl { get; set; }

    [JsonPropertyName("is_read")]
    public bool IsRead { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
}
