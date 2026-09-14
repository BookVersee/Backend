using System;
using System.Text.Json.Serialization;

namespace BookManagement.Service.Delivery
{
    public class CreateDeliveryDto
    {
        public Guid OrderId { get; set; }
        public string TrackingNumber { get; set; } = string.Empty;
        public string CarrierName { get; set; } = string.Empty;
        public decimal ShipFee { get; set; }
        public DateTime? EstimatedDelivery { get; set; }
    }

    public class UpdateDeliveryDto
    {
        public string TrackingNumber { get; set; } = string.Empty;
        public string CarrierName { get; set; } = string.Empty;
        public decimal ShipFee { get; set; }
        public DateTime? EstimatedDelivery { get; set; }
    }

    public class UpdateDeliveryStatusDto
    {
        public string Status { get; set; } = string.Empty;
        public string? Note { get; set; }
        public string? FailedReason { get; set; }
    }

    public class CreateGhnOrderDto
    {
        public Guid OrderId { get; set; }
        public string? RequiredNote { get; set; }
    }

    public class GhnWebhookPayload
    {
        [JsonPropertyName("OrderCode")]
        public string OrderCode { get; set; } = string.Empty;

        [JsonPropertyName("Status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("Time")]
        public DateTime? Time { get; set; }

        [JsonPropertyName("Description")]
        public string? Description { get; set; }
    }
}
