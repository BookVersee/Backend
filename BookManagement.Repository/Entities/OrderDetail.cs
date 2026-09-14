using System;
using BookManagement.Repository.Abstractions;
using BookManagement.Repository.Entities.Enums;

namespace BookManagement.Repository.Entities
{
    public class OrderDetail : BaseEntity<Guid>, IAuditableEntity
    {
        public Guid OrderId { get; set; }
        public Guid BookId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public ReturnStatus ReturnStatus { get; set; } = ReturnStatus.NONE;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }

        // Navigation Properties
        public Order Order { get; set; } = null!;
        public Book Book { get; set; } = null!;
        public ReturnRequest? ReturnRequest { get; set; }
        public Feedback? Feedback { get; set; }
    }
}
