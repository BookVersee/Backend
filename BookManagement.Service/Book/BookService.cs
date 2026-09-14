using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookManagement.Repository.Data;
using BookManagement.Repository.Entities.Enums;
using BookManagement.Service.Shop;
using Microsoft.EntityFrameworkCore;

namespace BookManagement.Service.Book
{
    /// Vị trí: Domain Service - Thực thi logic nghiệp vụ hệ thống, tính toán và truy vấn trực tiếp DbContext.
    public class BookService : IBookService
    {
        private readonly AppDbContext _context;

        public BookService(AppDbContext context)
        {
            _context = context;
        }

        /// Chức năng: Tìm kiếm và lọc danh sách sản phẩm sách phân trang
        public async Task<PagedResponse<BookResponse>> FindBooksAsync(BookQueryDto filter)
        {
            var query = _context.Books
                .Include(b => b.Shop)
                .Include(b => b.Category)
                .AsNoTracking()
                .Where(b => b.Status == BookStatus.ACTIVE && b.Shop != null && b.Shop.Condition == ShopCondition.OPEN);

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var kw = filter.Keyword.Trim().ToLower();
                query = query.Where(b => b.Title.ToLower().Contains(kw) ||
                                         (b.Author != null && b.Author.ToLower().Contains(kw)) ||
                                         (b.Isbn != null && b.Isbn.ToLower().Contains(kw)));
            }

            if (filter.CategoryId.HasValue)
                query = query.Where(b => b.CategoryId == filter.CategoryId.Value);

            var totalCount = await query.CountAsync();
            var page = filter.PageIndex < 1 ? 1 : filter.PageIndex;
            var pageSize = filter.PageSize < 1 ? 10 : filter.PageSize;
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PagedResponse<BookResponse>
            {
                Items = items.Select(MapToResponse),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        /// Chức năng: Xem thông tin chi tiết của 1 cuốn sách
        public async Task<BookResponse> GetBookDetailAsync(Guid bookId)
        {
            var book = await _context.Books
                .Include(b => b.Category)
                .Include(b => b.Images)
                .FirstOrDefaultAsync(b => b.Id == bookId);

            if (book == null) throw new KeyNotFoundException("Book not found.");

            var res = MapToResponse(book);
            var shop = await _context.Shops.FirstOrDefaultAsync(s => s.Id == book.ShopId);
            if (shop != null)
            {
                res.ShopName = shop.ShopName;
            }
            return res;
        }

        /// Chức năng: Xem thông tin công khai của Cửa hàng
        public async Task<ShopProfileDto> GetShopProfileAsync(Guid shopId)
        {
            var shop = await _context.Shops
                .Include(s => s.Books)
                .FirstOrDefaultAsync(s => s.Id == shopId);

            if (shop == null) throw new KeyNotFoundException("Shop not found.");
            return new ShopProfileDto
            {
                ShopId = shop.Id,
                ShopName = shop.ShopName,
                Condition = shop.Condition.ToString(),
                Rating = shop.Rating,
                TotalBooks = shop.Books.Count,
                CreatedAt = shop.CreatedAt
            };
        }

        /// Chức năng: Lấy danh sách toàn bộ sách do Cửa hàng bán
        public async Task<IEnumerable<BookResponse>> GetBooksByShopAsync(Guid shopId)
        {
            var books = await _context.Books
                .Include(b => b.Category)
                .Include(b => b.Images)
                .Where(b => b.ShopId == shopId)
                .AsNoTracking()
                .ToListAsync();

            return books.Select(MapToResponse);
        }

        private static BookResponse MapToResponse(BookManagement.Repository.Entities.Book b) => new BookResponse
        {
            Id = b.Id,
            ShopId = b.ShopId,
            ShopName = b.Shop?.ShopName ?? "Shop",
            CategoryId = b.CategoryId,
            CategoryName = b.Category?.CategoryName ?? "Category",
            Title = b.Title,
            Isbn = b.Isbn,
            Author = b.Author,
            Publisher = b.Publisher,
            Price = b.Price,
            StockQuantity = b.StockQuantity,
            Description = b.Description,
            ImageUrl = b.ImageUrl,
            PublishedYear = b.PublishedYear,
            Status = b.Status,
            Rating = b.Rating,
            Images = b.Images != null
                ? b.Images.OrderBy(i => i.DisplayOrder).Select(i => new BookImageDto
                {
                    Id = i.Id,
                    ImageUrl = i.ImageUrl,
                    PublicId = i.PublicId,
                    IsCover = i.IsCover,
                    DisplayOrder = i.DisplayOrder
                }).ToList()
                : new List<BookImageDto>(),
            CreatedAt = b.CreatedAt,
            UpdatedAt = b.UpdatedAt
        };
    }
}
