using System;
using System.Text.Json.Serialization;

namespace BookManagement.Service.Shipping;

public class GhnOrderCreatedResponse
{
    [JsonPropertyName("delivery_id")]
    public int DeliveryId { get; set; }

    [JsonPropertyName("order_id")]
    public int OrderId { get; set; }

    [JsonPropertyName("tracking_number")]
    public string TrackingNumber { get; set; } = string.Empty;

    [JsonPropertyName("ship_fee")]
    public decimal ShipFee { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("estimated_delivery")]
    public DateTime EstimatedDelivery { get; set; }
}
