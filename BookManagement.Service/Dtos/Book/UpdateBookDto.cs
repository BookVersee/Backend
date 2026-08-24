using System;
using System.ComponentModel.DataAnnotations;
using BookManagement.Repository.Entities.Enums;

namespace BookManagement.Service.Dtos.Book
{
    public class UpdateBookDto
    {
        public Guid? CategoryId { get; set; }

        [StringLength(255)]
        public string? Title { get; set; }

        [StringLength(20)]
        public string? Isbn { get; set; }

        [StringLength(150)]
        public string? Author { get; set; }

        [StringLength(150)]
        public string? Publisher { get; set; }

        [Range(0, 999999999.99)]
        public decimal? Price { get; set; }

        [Range(0, int.MaxValue)]
        public int? StockQuantity { get; set; }

        public string? Description { get; set; }

        [StringLength(255)]
        public string? ImageUrl { get; set; }

        public int? PublishedYear { get; set; }

        public BookStatus? Status { get; set; }
    }
}
