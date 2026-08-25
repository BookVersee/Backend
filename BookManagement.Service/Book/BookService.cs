using System;
using System.Linq;
using System.Threading.Tasks;
using BookStore.BE2.Domain.Entities;
using BookStore.BE2.Domain.Enums;
using BookStore.BE2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BookManagement.Service.Book;

public class BookService : IBookService
{
    private readonly AppDbContext _db;

    public BookService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<BookResponse> CreateBookAsync(int shopId, CreateBookRequest dto)
    {
        var existingIsbn = await _db.Books.AnyAsync(b => b.Isbn == dto.Isbn);
        if (existingIsbn)
        {
            throw new InvalidOperationException("ISBN must be unique.");
        }

        var status = dto.StockQuantity > 0 ? BookStatus.ACTIVE : BookStatus.EMPTY;

        var book = new BookStore.BE2.Domain.Entities.Book
        {
            ShopId = shopId,
            CategoryId = dto.CategoryId,
            Title = dto.Title,
            Isbn = dto.Isbn,
            Author = dto.Author,
            Publisher = dto.Publisher,
            Price = dto.Price,
            StockQuantity = dto.StockQuantity,
            Description = dto.Description,
            ImageUrl = dto.ImageUrl,
            PublishedYear = dto.PublishedYear,
            Status = status,
            Rating = 5.0f
        };

        _db.Books.Add(book);
        await _db.SaveChangesAsync();

        var categoryName = await _db.Categories
            .Where(c => c.CategoryId == book.CategoryId)
            .Select(c => c.CategoryName)
            .FirstOrDefaultAsync();

        return MapToResponse(book, categoryName);
    }

    public async Task<BookResponse> GetBookByIdAsync(int shopId, int bookId)
    {
        var book = await _db.Books
            .Where(b => b.BookId == bookId && b.ShopId == shopId)
            .Select(b => new BookResponse
            {
                BookId = b.BookId,
                ShopId = b.ShopId,
                CategoryId = b.CategoryId,
                CategoryName = b.Category != null ? b.Category.CategoryName : null,
                Title = b.Title,
                Isbn = b.Isbn,
                Author = b.Author,
                Publisher = b.Publisher,
                Price = b.Price,
                StockQuantity = b.StockQuantity,
                Description = b.Description,
                ImageUrl = b.ImageUrl,
                PublishedYear = b.PublishedYear,
                Status = b.Status.ToString(),
                Rating = b.Rating
            })
            .FirstOrDefaultAsync();

        if (book == null)
            throw new KeyNotFoundException("Book not found or unauthorized access.");

        return book;
    }

    public async Task<PagedBookResponse> GetShopBooksAsync(int shopId, BookQueryRequest query)
    {
        var q = _db.Books.Where(b => b.ShopId == shopId);

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim().ToLower();
            q = q.Where(b => b.Title.ToLower().Contains(kw)
                          || b.Isbn.ToLower().Contains(kw)
                          || b.Author.ToLower().Contains(kw));
        }

        if (query.CategoryId.HasValue)
            q = q.Where(b => b.CategoryId == query.CategoryId.Value);

        if (query.Status.HasValue)
            q = q.Where(b => b.Status == query.Status.Value);

        var totalItems = await q.CountAsync();
        var items = await q
            .OrderByDescending(b => b.BookId)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(b => new BookResponse
            {
                BookId = b.BookId,
                ShopId = b.ShopId,
                CategoryId = b.CategoryId,
                CategoryName = b.Category != null ? b.Category.CategoryName : null,
                Title = b.Title,
                Isbn = b.Isbn,
                Author = b.Author,
                Publisher = b.Publisher,
                Price = b.Price,
                StockQuantity = b.StockQuantity,
                Description = b.Description,
                ImageUrl = b.ImageUrl,
                PublishedYear = b.PublishedYear,
                Status = b.Status.ToString(),
                Rating = b.Rating
            })
            .ToListAsync();

        return new PagedBookResponse
        {
            TotalItems = totalItems,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize,
            Items = items
        };
    }

    public async Task<BookResponse> UpdateBookAsync(int shopId, int bookId, UpdateBookRequest dto)
    {
        var book = await _db.Books.FirstOrDefaultAsync(b => b.BookId == bookId && b.ShopId == shopId);
        if (book == null)
            throw new KeyNotFoundException("Book not found or unauthorized access.");

        book.CategoryId = dto.CategoryId;
        book.Title = dto.Title;
        book.Price = dto.Price;
        book.StockQuantity = dto.StockQuantity;
        book.Description = dto.Description;
        book.ImageUrl = dto.ImageUrl;
        book.PublishedYear = dto.PublishedYear;

        if (dto.StockQuantity == 0)
            book.Status = BookStatus.EMPTY;
        else if (!string.IsNullOrEmpty(dto.Status) && Enum.TryParse<BookStatus>(dto.Status, true, out var parsedStatus))
            book.Status = parsedStatus;

        await _db.SaveChangesAsync();

        var categoryName = await _db.Categories
            .Where(c => c.CategoryId == book.CategoryId)
            .Select(c => c.CategoryName)
            .FirstOrDefaultAsync();

        return MapToResponse(book, categoryName);
    }

    public async Task DeleteBookAsync(int shopId, int bookId)
    {
        var book = await _db.Books.FirstOrDefaultAsync(b => b.BookId == bookId && b.ShopId == shopId);
        if (book == null)
            throw new KeyNotFoundException("Book not found or unauthorized access.");

        book.Status = BookStatus.HIDDEN;
        await _db.SaveChangesAsync();
    }

    private static BookResponse MapToResponse(BookStore.BE2.Domain.Entities.Book book, string? categoryName) => new()
    {
        BookId = book.BookId,
        ShopId = book.ShopId,
        CategoryId = book.CategoryId,
        CategoryName = categoryName,
        Title = book.Title,
        Isbn = book.Isbn,
        Author = book.Author,
        Publisher = book.Publisher,
        Price = book.Price,
        StockQuantity = book.StockQuantity,
        Description = book.Description,
        ImageUrl = book.ImageUrl,
        PublishedYear = book.PublishedYear,
        Status = book.Status.ToString(),
        Rating = book.Rating
    };
}
