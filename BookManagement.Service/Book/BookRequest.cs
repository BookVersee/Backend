using System;
using System.ComponentModel.DataAnnotations;
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

    public class BookImageInputDto
    {
        public string ImageUrl { get; set; } = string.Empty;
        public string? PublicId { get; set; }
        public bool IsCover { get; set; } = false;
        public int DisplayOrder { get; set; } = 0;
    }

    public class CreateBookRequestDto
    {
        [Required(ErrorMessage = "Thể loại sách không được để trống.")]
        public Guid CategoryId { get; set; }

        [Required(ErrorMessage = "Tựa đề sách không được để trống.", AllowEmptyStrings = false)]
        [StringLength(255, MinimumLength = 1, ErrorMessage = "Tựa đề sách phải từ 1 đến 255 ký tự.")]
        public string Title { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Mã ISBN không được vượt quá 50 ký tự.")]
        public string? Isbn { get; set; }

        [Required(ErrorMessage = "Tên tác giả không được để trống.", AllowEmptyStrings = false)]
        [StringLength(150, ErrorMessage = "Tên tác giả không được vượt quá 150 ký tự.")]
        public string Author { get; set; } = string.Empty;

        [StringLength(150, ErrorMessage = "Nhà xuất bản không được vượt quá 150 ký tự.")]
        public string? Publisher { get; set; }

        [Range(1000, 100000000, ErrorMessage = "Giá sản phẩm phải từ 1.000 VNĐ đến 100.000.000 VNĐ.")]
        public decimal Price { get; set; }

        [Range(0, 100000, ErrorMessage = "Số lượng tồn kho phải từ 0 đến 100.000 cuốn.")]
        public int StockQuantity { get; set; }

        [StringLength(4000, ErrorMessage = "Mô tả không được vượt quá 4000 ký tự.")]
        public string? Description { get; set; }

        /// Ảnh bìa chính
        public string? ImageUrl { get; set; }

        /// Danh sách đường dẫn các ảnh (Ảnh bìa + Trang đọc thử / góc chụp)
        public List<string>? ImageUrls { get; set; }

        /// Danh sách đối tượng ảnh chi tiết (nếu có kèm PublicId / IsCover / DisplayOrder)
        public List<BookImageInputDto>? Images { get; set; }

        [Range(1000, 2100, ErrorMessage = "Năm xuất bản không hợp lệ (từ 1000 đến 2100).")]
        public int PublishedYear { get; set; }
    }

    public class UpdateBookRequestDto
    {
        public Guid CategoryId { get; set; }

        [StringLength(255, MinimumLength = 1, ErrorMessage = "Tựa đề sách phải từ 1 đến 255 ký tự.")]
        public string? Title { get; set; }

        [Range(1000, 100000000, ErrorMessage = "Giá sản phẩm phải từ 1.000 VNĐ đến 100.000.000 VNĐ.")]
        public decimal Price { get; set; }

        [Range(0, 100000, ErrorMessage = "Số lượng tồn kho phải từ 0 đến 100.000 cuốn.")]
        public int StockQuantity { get; set; }

        [StringLength(4000, ErrorMessage = "Mô tả không được vượt quá 4000 ký tự.")]
        public string? Description { get; set; }

        public string? ImageUrl { get; set; }

        /// Danh sách đường dẫn các ảnh cập nhật
        public List<string>? ImageUrls { get; set; }

        /// Danh sách đối tượng ảnh chi tiết cập nhật
        public List<BookImageInputDto>? Images { get; set; }

        [Range(1000, 2100, ErrorMessage = "Năm xuất bản không hợp lệ.")]
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
