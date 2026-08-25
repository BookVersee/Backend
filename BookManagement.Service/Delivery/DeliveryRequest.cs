using System;
using System.Text.Json.Serialization;

namespace BookManagement.Service.Delivery;

public class CreateDeliveryRequest
{
    [JsonPropertyName("order_id")]
    public int OrderId { get; set; }

    [JsonPropertyName("tracking_number")]
    public string TrackingNumber { get; set; } = string.Empty;

    [JsonPropertyName("carrier_name")]
    public string CarrierName { get; set; } = string.Empty;

    [JsonPropertyName("shipfee")]
    public decimal ShipFee { get; set; }

    [JsonPropertyName("estimated_delivery")]
    public DateTime? EstimatedDelivery { get; set; }
}

public class UpdateDeliveryRequest
{
    [JsonPropertyName("tracking_number")]
    public string TrackingNumber { get; set; } = string.Empty;

    [JsonPropertyName("carrier_name")]
    public string CarrierName { get; set; } = string.Empty;

    [JsonPropertyName("shipfee")]
    public decimal ShipFee { get; set; }

    [JsonPropertyName("estimated_delivery")]
    public DateTime? EstimatedDelivery { get; set; }
}

public class UpdateDeliveryStatusRequest
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("failed_reason")]
    public string? FailedReason { get; set; }
}
