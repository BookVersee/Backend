using System;
using BookManagement.Repository.Abstractions;
using BookManagement.Repository.Entities.Enums;

namespace BookManagement.Repository.Entities
{
    public class Payment : BaseEntity<Guid>, IAuditableEntity
    {
        public Guid OrderId { get; set; }
        public Guid? ReturnRequestId { get; set; }
        public PaymentType PaymentType { get; set; } = PaymentType.PAYMENT;
        public PaymentMethod Method { get; set; } = PaymentMethod.COD;
        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; } = PaymentStatus.PENDING;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }

        // Navigation Properties
        public Order Order { get; set; } = null!;
        public ReturnRequest? ReturnRequest { get; set; }
    }
}
