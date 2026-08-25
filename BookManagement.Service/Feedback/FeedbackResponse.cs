using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BookManagement.Service.Feedback;

public class FeedbackResponseData
{
    [JsonPropertyName("response_id")]
    public int ResponseId { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("image_url")]
    public string? ImageUrl { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
}

public class FeedbackResponse
{
    [JsonPropertyName("feedback_id")]
    public int FeedbackId { get; set; }

    [JsonPropertyName("order_detail_id")]
    public int OrderDetailId { get; set; }

    [JsonPropertyName("shop_id")]
    public int ShopId { get; set; }

    [JsonPropertyName("rating")]
    public int Rating { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("image_url")]
    public string? ImageUrl { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("book_title")]
    public string? BookTitle { get; set; }

    [JsonPropertyName("response")]
    public FeedbackResponseData? Response { get; set; }
}

public class PagedFeedbackResponse
{
    [JsonPropertyName("total_items")]
    public int TotalItems { get; set; }

    [JsonPropertyName("page_index")]
    public int PageIndex { get; set; }

    [JsonPropertyName("page_size")]
    public int PageSize { get; set; }

    [JsonPropertyName("items")]
    public IEnumerable<FeedbackResponse> Items { get; set; } = new List<FeedbackResponse>();
}

public class FeedbackReplyCreatedResponse
{
    [JsonPropertyName("response_id")]
    public int ResponseId { get; set; }

    [JsonPropertyName("feedback_id")]
    public int FeedbackId { get; set; }

    [JsonPropertyName("shop_id")]
    public int ShopId { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("image_url")]
    public string? ImageUrl { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
}
