using System;
using System.Collections.Generic;
using BookManagement.Repository.Abstractions;
using BookManagement.Repository.Entities.Enums;

namespace BookManagement.Repository.Entities
{
    public class Order : BaseEntity<Guid>, IAuditableEntity
    {
        public Guid UserId { get; set; }
        public decimal TotalAmount { get; set; }
        public OrderStatus OrderStatus { get; set; } = OrderStatus.PENDING;
        public string ShippingAddress { get; set; } = null!;
        public decimal? Weight { get; set; }
        public string? Note { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }

        // Navigation Properties
        public User User { get; set; } = null!;
        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        public ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
