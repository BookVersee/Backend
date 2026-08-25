using System;
using System.Collections.Generic;
using System;
using BookManagement.Repository.Entities.Enums;

namespace BookManagement.Service.Book
{
    public class BookResponse
    {
        public Guid Id { get; set; }
        public Guid ShopId { get; set; }
        public string ShopName { get; set; } = null!;
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string? Isbn { get; set; }
        public string? Author { get; set; }
        public string? Publisher { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public int? PublishedYear { get; set; }
        public BookStatus Status { get; set; }
        public double? Rating { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class ShopResponse
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string ShopName { get; set; } = null!;
        public ShopCondition Condition { get; set; }
        public double? Rating { get; set; }
        public string? OwnerName { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class PagedResponse<T>
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}
