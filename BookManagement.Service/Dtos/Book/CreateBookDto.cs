using System;
using System.ComponentModel.DataAnnotations;

namespace BookManagement.Service.Dtos.Book
{
    public class CreateBookDto
    {
        [Required(ErrorMessage = "Category ID is required")]
        public Guid CategoryId { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(255, ErrorMessage = "Title cannot exceed 255 characters")]
        public string Title { get; set; } = null!;

        [StringLength(20)]
        public string? Isbn { get; set; }

        [StringLength(150)]
        public string? Author { get; set; }

        [StringLength(150)]
        public string? Publisher { get; set; }

        [Range(0, 999999999.99, ErrorMessage = "Price must be non-negative")]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity must be non-negative")]
        public int StockQuantity { get; set; }

        public string? Description { get; set; }

        [StringLength(255)]
        public string? ImageUrl { get; set; }

        public int? PublishedYear { get; set; }
    }
}
