using System;
using BookManagement.Repository.Abstractions;
using BookManagement.Repository.Entities.Enums;

namespace BookManagement.Repository.Entities
{
    public class TransactionHistory : BaseEntity<Guid>, IAuditableEntity
    {
        public Guid UserId { get; set; }
        public ReferenceType ReferenceType { get; set; }
        public Guid? ReferenceId { get; set; }
        public TransactionType TransactionType { get; set; }
        public decimal Amount { get; set; }
        public string? TransactionCode { get; set; }
        public string? Description { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }

        // Navigation Properties
        public User User { get; set; } = null!;
    }
}
