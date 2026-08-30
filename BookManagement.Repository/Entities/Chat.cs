using System;
using System.Collections.Generic;
using BookManagement.Repository.Abstractions;

namespace BookManagement.Repository.Entities
{
    public class Chat : BaseEntity<Guid>, IAuditableEntity
    {
        public Guid UserId { get; set; }
        public Guid ShopId { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }

        // Navigation Properties
        public User User { get; set; } = null!;
        public Shop Shop { get; set; } = null!;
        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}
