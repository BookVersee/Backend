using System;

namespace BookManagement.Repository.Abstractions
{
    public abstract class BaseEntity<TKey>
    {
        public TKey Id { get; set; } = default!;
        public bool IsDeleted { get; set; } = false;
    }
}
