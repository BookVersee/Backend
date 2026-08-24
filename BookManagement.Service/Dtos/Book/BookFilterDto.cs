using System;
using BookManagement.Repository.Entities.Enums;

namespace BookManagement.Service.Dtos.Book
{
    public class BookFilterDto
    {
        public string? Keyword { get; set; }
        public Guid? CategoryId { get; set; }
        public BookStatus? Status { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
