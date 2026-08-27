using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookManagement.Repository.Abstractions;
using BookManagement.Repository.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace BookManagement.Service.Book
{
    public class BookService : IBookService
    {
        private readonly IBookRepository _bookRepository;

        public BookService(IBookRepository bookRepository)
        {
            _bookRepository = bookRepository;
        }

        public async Task<PagedResponse<BookResponse>> FindBooksAsync(FilterRequest filter)
        {
            var query = await _bookRepository.GetQueryableAsync();
            query = query.Where(b => b.Status == BookStatus.ACTIVE && b.Shop != null && (b.Shop.Condition == ShopCondition.ACTIVE || b.Shop.Condition == ShopCondition.OPEN));

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var kw = filter.Keyword.Trim().ToLower();
                query = query.Where(b => b.Title.ToLower().Contains(kw) ||
                                         (b.Author != null && b.Author.ToLower().Contains(kw)) ||
                                         (b.Isbn != null && b.Isbn.ToLower().Contains(kw)));
            }

            if (filter.CategoryId.HasValue)
                query = query.Where(b => b.CategoryId == filter.CategoryId.Value);

            if (filter.ShopId.HasValue)
                query = query.Where(b => b.ShopId == filter.ShopId.Value);

            if (filter.MinPrice.HasValue)
                query = query.Where(b => b.Price >= filter.MinPrice.Value);

            if (filter.MaxPrice.HasValue)
                query = query.Where(b => b.Price <= filter.MaxPrice.Value);

            if (!string.IsNullOrWhiteSpace(filter.Author))
                query = query.Where(b => b.Author != null && b.Author.ToLower().Contains(filter.Author.Trim().ToLower()));

            query = filter.SortBy?.ToLower() switch
            {
                "price_asc" => query.OrderBy(b => b.Price),
                "price_desc" => query.OrderByDescending(b => b.Price),
                "rating" => query.OrderByDescending(b => b.Rating),
                "newest" => query.OrderByDescending(b => b.CreatedAt),
                _ => query.OrderByDescending(b => b.CreatedAt)
            };

            var totalCount = await query.CountAsync();
            var page = filter.Page < 1 ? 1 : filter.Page;
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

        public async Task<BookResponse> GetBookDetailAsync(Guid bookId)
        {
            var book = await _bookRepository.GetByIdAsync(bookId);
            if (book == null) throw new KeyNotFoundException("Book not found.");
            return MapToResponse(book);
        }

        public async Task<ShopResponse> GetShopProfileAsync(Guid shopId)
        {
            var shop = await _bookRepository.GetShopByIdAsync(shopId);
            if (shop == null) throw new KeyNotFoundException("Shop not found.");
            return new ShopResponse
            {
                Id = shop.Id,
                UserId = shop.UserId,
                ShopName = shop.ShopName,
                Condition = shop.Condition,
                Rating = shop.Rating,
                OwnerName = shop.User?.FullName ?? shop.User?.Username,
                Phone = shop.User?.Phone,
                Address = shop.User?.Address,
                CreatedAt = shop.CreatedAt
            };
        }

        public async Task<IEnumerable<BookResponse>> GetBooksByShopAsync(Guid shopId)
        {
            var books = await _bookRepository.GetBooksByShopIdAsync(shopId);
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
            CreatedAt = b.CreatedAt
        };
    }
}
