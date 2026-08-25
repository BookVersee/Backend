using System;
using System.Text.Json.Serialization;

namespace BookManagement.Service.Shop;

// === RESPONSE DTOs ===
public class ShopProfileResponse
{
    [JsonPropertyName("shop_id")]
    public int ShopId { get; set; }

    [JsonPropertyName("shop_name")]
    public string ShopName { get; set; } = string.Empty;

    [JsonPropertyName("condition")]
    public string Condition { get; set; } = string.Empty;

    [JsonPropertyName("rating")]
    public float Rating { get; set; }

    [JsonPropertyName("total_books")]
    public int TotalBooks { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
}

public class ShopRegisterResponse
{
    [JsonPropertyName("shop_id")]
    public int ShopId { get; set; }

    [JsonPropertyName("shop_name")]
    public string ShopName { get; set; } = string.Empty;

    [JsonPropertyName("condition")]
    public string Condition { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
}
