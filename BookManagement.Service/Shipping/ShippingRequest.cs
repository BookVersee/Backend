using System;
using System.Text.Json.Serialization;

namespace BookManagement.Service.Shipping;

public class CreateGhnOrderRequest
{
    [JsonPropertyName("order_id")]
    public int OrderId { get; set; }
}

public class GhnWebhookRequest
{
    [JsonPropertyName("OrderCode")]
    public string OrderCode { get; set; } = string.Empty;

    [JsonPropertyName("Status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("Time")]
    public DateTime? Time { get; set; }
}
