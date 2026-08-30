using System;
using System.Collections.Generic;
using BookManagement.Repository.Entities.Enums;
using BookManagement.Service.Shop;

namespace BookManagement.Service.Delivery
{
    public class DeliveryResponse
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public string? TrackingNumber { get; set; }
        public string? CarrierName { get; set; }
        public decimal? ShipFee { get; set; }
        public string Status { get; set; } = null!;
        public DateTime? EstimatedDelivery { get; set; }
        public DateTime? ActualDeliveredAt { get; set; }
    }

    public class DeliveryManifestDetailDto
    {
        public Guid DeliveryId { get; set; }
        public Guid OrderId { get; set; }
        public string TrackingNumber { get; set; } = string.Empty;
        public string CarrierName { get; set; } = string.Empty;
        public decimal ShipFee { get; set; }
        public string Status { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
        public string RecipientPhone { get; set; } = string.Empty;
        public string RecipientAddress { get; set; } = string.Empty;
        public decimal Weight { get; set; }
        public decimal CodAmount { get; set; }
        public List<ShopOrderItemDto> Items { get; set; } = new();
    }
}
