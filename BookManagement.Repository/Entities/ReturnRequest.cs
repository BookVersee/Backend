using System;
using System.Collections.Generic;
using BookManagement.Repository.Abstractions;
using BookManagement.Repository.Entities.Enums;

namespace BookManagement.Repository.Entities
{
    public class ReturnRequest : BaseEntity<Guid>, IAuditableEntity
    {
        public Guid OrderDetailId { get; set; }
        public ReasonType ReasonType { get; set; }
        public string? DetailedReason { get; set; }
        public string? ImageUrl { get; set; }
        public ReturnRequestStatus Status { get; set; } = ReturnRequestStatus.PENDING;
        public decimal RefundAmount { get; set; }
        public Guid? AdminId { get; set; }
        public string? AdminNote { get; set; }
        public DateTimeOffset? ShopRespondedAt { get; set; }
        public DateTimeOffset? EscalatedAt { get; set; }
        public DateTimeOffset? ResolvedAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }

        // Navigation Properties
        public OrderDetail OrderDetail { get; set; } = null!;
        public User? Admin { get; set; }
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
