using System;
using BookManagement.Repository.Abstractions;
using BookManagement.Repository.Entities.Enums;

namespace BookManagement.Repository.Entities
{
    public class Delivery : BaseEntity<Guid>
    {
        public Guid OrderId { get; set; }
        public string? TrackingNumber { get; set; }
        public string? CarrierName { get; set; }
        public decimal? ShipFee { get; set; }
        public DeliveryStatus Status { get; set; } = DeliveryStatus.PENDING;
        public DateTime? EstimatedDelivery { get; set; }
        public DateTime? ActualDeliveredAt { get; set; }

        // Navigation Properties
        public Order Order { get; set; } = null!;
    }
}
