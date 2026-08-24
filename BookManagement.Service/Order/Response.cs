using System;
using System.Collections.Generic;
using BookManagement.Repository.Entities.Enums;

namespace BookManagement.Service.Order
{
    public class OrderResponse
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string UserFullName { get; set; } = null!;
        public decimal TotalAmount { get; set; }
        public OrderStatus OrderStatus { get; set; }
        public string ShippingAddress { get; set; } = null!;
        public decimal? Weight { get; set; }
        public string? Note { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public List<OrderDetailResponse> OrderDetails { get; set; } = new();
        public List<DeliveryResponse> Deliveries { get; set; } = new();
    }

    public class OrderDetailResponse
    {
        public Guid OrderDetailId { get; set; }
        public Guid BookId { get; set; }
        public string BookTitle { get; set; } = null!;
        public string? BookImage { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public ReturnStatus ReturnStatus { get; set; }
        public ReturnRequestResponse? ReturnRequest { get; set; }
    }

    public class ReturnRequestResponse
    {
        public Guid Id { get; set; }
        public Guid OrderDetailId { get; set; }
        public ReasonType ReasonType { get; set; }
        public string? DetailedReason { get; set; }
        public string? ImageUrl { get; set; }
        public ReturnRequestStatus Status { get; set; }
        public decimal RefundAmount { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class DeliveryResponse
    {
        public Guid Id { get; set; }
        public string? TrackingNumber { get; set; }
        public string? CarrierName { get; set; }
        public decimal? ShipFee { get; set; }
        public DeliveryStatus Status { get; set; }
        public DateTime? EstimatedDelivery { get; set; }
        public DateTime? ActualDeliveredAt { get; set; }
    }
}
