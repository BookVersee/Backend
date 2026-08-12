using System;
using System.Collections.Generic;
using BookManagement.Repository.Abstractions;

namespace BookManagement.Repository.Entities
{
    public class Cart : BaseEntity<Guid>, IAuditableEntity
    {
        public Guid UserId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }

        // Navigation Properties
        public User User { get; set; } = null!;
        public ICollection<CartBookDetail> CartBookDetails { get; set; } = new List<CartBookDetail>();
    }
}
