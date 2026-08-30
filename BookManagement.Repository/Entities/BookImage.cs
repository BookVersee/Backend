using System;
using BookManagement.Repository.Abstractions;

namespace BookManagement.Repository.Entities
{
    public class BookImage : BaseEntity<Guid>, IAuditableEntity
    {
        public Guid BookId { get; set; }
        public string ImageUrl { get; set; } = null!;
        public string? PublicId { get; set; }
        public bool IsCover { get; set; } = false;
        public int DisplayOrder { get; set; } = 0;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }

        // Navigation Property
        public Book Book { get; set; } = null!;
    }
}
