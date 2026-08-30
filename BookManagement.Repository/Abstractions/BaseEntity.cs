using System;

namespace BookManagement.Repository.Abstractions
{
    public abstract class BaseEntity<TKey> : IAuditableEntity
    {
        public TKey Id { get; set; } = default!;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
