using System;
using BookManagement.Repository.Abstractions;
using BookManagement.Repository.Entities.Enums;

namespace BookManagement.Repository.Entities
{
    public class Escrow : BaseEntity<Guid>, IAuditableEntity
    {
        public Guid OrderId { get; set; }
        public Guid ShopId { get; set; }
        public decimal Amount { get; set; }
        public decimal PlatformFee { get; set; }
        public decimal NetShopEarnings { get; set; }
        public DateTimeOffset ReleaseDate { get; set; }
        public EscrowStatus Status { get; set; } = EscrowStatus.HOLDING;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }

        // Navigation properties
        public Order Order { get; set; } = null!;
        public Shop Shop { get; set; } = null!;
    }
}
