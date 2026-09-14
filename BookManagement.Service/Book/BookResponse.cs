using System;
using System.Collections.Generic;
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
        public List<BookImageDto> Images { get; set; } = new List<BookImageDto>();
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }

    public class BookImageDto
    {
        public Guid Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string? PublicId { get; set; }
        public bool IsCover { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class BookResponseDto
    {
        public Guid BookId { get; set; }
        public Guid ShopId { get; set; }
        public Guid CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Isbn { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Publisher { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public int? PublishedYear { get; set; }
        public string Status { get; set; } = string.Empty;
        public float Rating { get; set; }
        public List<BookImageDto> Images { get; set; } = new List<BookImageDto>();
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }

    public class PagedResponse<T>
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / (PageSize < 1 ? 10 : PageSize));
    }
}
