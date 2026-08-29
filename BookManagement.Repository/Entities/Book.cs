using System;
using System.Collections.Generic;
using BookManagement.Repository.Abstractions;
using BookManagement.Repository.Entities.Enums;

namespace BookManagement.Repository.Entities
{
    public class Book : BaseEntity<Guid>, IAuditableEntity
    {
        public Guid ShopId { get; set; }
        public Guid CategoryId { get; set; }
        public string Title { get; set; } = null!;
        public string? Isbn { get; set; }
        public string? Author { get; set; }
        public string? Publisher { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public int? PublishedYear { get; set; }
        public BookStatus Status { get; set; } = BookStatus.ACTIVE;
        public float Rating { get; set; } = 0;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }

        // Navigation Properties
        public Shop Shop { get; set; } = null!;
        public Category Category { get; set; } = null!;
        public ICollection<CartBookDetail> CartBookDetails { get; set; } = new List<CartBookDetail>();
        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}
