using System;
using BookManagement.Repository.Abstractions;

namespace BookManagement.Repository.Entities
{
    public class Wallet : BaseEntity<Guid>, IAuditableEntity
    {
        public Guid UserId { get; set; }
        public decimal Balance { get; set; } = 0;
        public decimal HeldBalance { get; set; } = 0;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }

        // Navigation property
        public User User { get; set; } = null!;
    }
}
