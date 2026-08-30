using System;
using BookManagement.Repository.Entities.Enums;

namespace BookManagement.Service.Book
{
    public class CreateBookRequest
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
    }

    public class UpdateBookRequest
    {
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
        public BookStatus Status { get; set; }
    }

    public class CreateBookRequestDto
    {
        public Guid CategoryId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Isbn { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Publisher { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public int PublishedYear { get; set; }
    }

    public class UpdateBookRequestDto
    {
        public Guid CategoryId { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public int PublishedYear { get; set; }
        public string? Status { get; set; }
    }

    public class BookQueryDto
    {
        public string? Keyword { get; set; }
        public Guid? CategoryId { get; set; }
        public BookStatus? Status { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
