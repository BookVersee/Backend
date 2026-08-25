using System.Text.Json.Serialization;

namespace BookManagement.Service.Shop;

// === REQUEST DTOs ===
public class ShopRegisterRequest
{
    [JsonPropertyName("shop_name")]
    public string ShopName { get; set; } = string.Empty;

    [JsonPropertyName("address")]
    public string Address { get; set; } = string.Empty;

    [JsonPropertyName("qr_image_url")]
    public string? QrImageUrl { get; set; }
}

public class UpdateShopProfileRequest
{
    [JsonPropertyName("shop_name")]
    public string? ShopName { get; set; }

    [JsonPropertyName("condition")]
    public string? Condition { get; set; }
}
