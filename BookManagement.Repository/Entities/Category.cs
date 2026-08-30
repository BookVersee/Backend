using System;
using System.Collections.Generic;
using BookManagement.Repository.Abstractions;

namespace BookManagement.Repository.Entities
{
    public class Category : BaseEntity<Guid>, IAuditableEntity
    {
        public string CategoryName { get; set; } = null!;
        public string? Description { get; set; }
        public bool Status { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }

        // Navigation Properties
        public ICollection<Book> Books { get; set; } = new List<Book>();
    }
}
