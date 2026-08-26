using System;
using BookManagement.Repository.Entities.Enums;

namespace BookManagement.Service.Dtos.Book
{
    public class BookDto
    {
        public Guid Id { get; set; }
        public Guid ShopId { get; set; }
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
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
        public float Rating { get; set; }
    }
}
