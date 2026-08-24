using System;

namespace BookManagement.Service.Dtos.Delivery
{
    public class DeliveryDto
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public string? TrackingNumber { get; set; }
        public string? CarrierName { get; set; }
        public decimal ShipFee { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? EstimatedDelivery { get; set; }
        public DateTime? ActualDeliveredAt { get; set; }
    }

    public class CreateDeliveryRequestDto
    {
        public Guid OrderId { get; set; }
        public string CarrierName { get; set; } = "GHN";
        public decimal ShipFee { get; set; }
    }

    public class GhnWebhookDto
    {
        public string OrderCode { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTimeOffset Time { get; set; }
    }
}
