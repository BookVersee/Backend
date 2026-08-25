using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookManagement.Service.Dtos;
using BookStore.BE2.Domain.Entities;
using BookStore.BE2.Domain.Enums;
using BookStore.BE2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using BookEntity = BookStore.BE2.Domain.Entities.Book;
using ShopEntity = BookStore.BE2.Domain.Entities.Shop;

namespace BookManagement.Service.Services;

public class ShopService
{
    private readonly AppDbContext _db;

    public ShopService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ShopRegisterResponseDto> RegisterShopAsync(int userId, ShopRegisterDto dto)
    {
        var existingShop = await _db.Shops.FirstOrDefaultAsync(s => s.UserId == userId);
        if (existingShop != null)
        {
            throw new InvalidOperationException("User already registered a shop.");
        }

        var shop = new ShopEntity
        {
            UserId = userId,
            ShopName = dto.ShopName,
            Condition = ShopCondition.OPEN,
            Rating = 5.0f,
            CreatedAt = DateTime.UtcNow
        };

        _db.Shops.Add(shop);

        var user = await _db.Users.FindAsync(userId);
        if (user != null)
        {
            user.Address = dto.Address ?? user.Address;
            user.QrImageUrl = dto.QrImageUrl ?? user.QrImageUrl;
            user.Role = UserRole.SHOP;
        }

        await _db.SaveChangesAsync();

        return new ShopRegisterResponseDto
        {
            ShopId = shop.ShopId,
            ShopName = shop.ShopName,
            Condition = shop.Condition.ToString(),
            CreatedAt = shop.CreatedAt
        };
    }

    public async Task<ShopProfileDto> GetShopProfileAsync(int userId)
    {
        var shop = await _db.Shops
            .Include(s => s.Books)
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (shop == null)
        {
            throw new KeyNotFoundException("Shop not found for user.");
        }

        return new ShopProfileDto
        {
            ShopId = shop.ShopId,
            ShopName = shop.ShopName,
            Condition = shop.Condition.ToString(),
            Rating = shop.Rating,
            TotalBooks = shop.Books.Count(b => b.Status != BookStatus.HIDDEN),
            CreatedAt = shop.CreatedAt
        };
    }

    public async Task<BookResponseDto> CreateBookAsync(int shopId, CreateBookRequestDto dto)
    {
        var existingIsbn = await _db.Books.AnyAsync(b => b.Isbn == dto.Isbn);
        if (existingIsbn)
        {
            throw new InvalidOperationException("ISBN must be unique.");
        }

        var status = dto.StockQuantity > 0 ? BookStatus.ACTIVE : BookStatus.EMPTY;

        var book = new BookEntity
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

        return MapToBookDto(book, categoryName);
    }

    public async Task<BookResponseDto> GetBookByIdAsync(int shopId, int bookId)
    {
        var book = await _db.Books
            .Where(b => b.BookId == bookId && b.ShopId == shopId)
            .Select(b => new BookResponseDto
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
        {
            throw new KeyNotFoundException("Book not found or unauthorized access.");
        }

        return book;
    }

    public async Task<PagedResultDto<BookResponseDto>> GetShopBooksAsync(int shopId, BookQueryDto query)
    {
        var q = _db.Books.Where(b => b.ShopId == shopId);

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim().ToLower();
            q = q.Where(b => b.Title.ToLower().Contains(kw) || b.Isbn.ToLower().Contains(kw) || b.Author.ToLower().Contains(kw));
        }

        if (query.CategoryId.HasValue)
        {
            q = q.Where(b => b.CategoryId == query.CategoryId.Value);
        }

        if (query.Status.HasValue)
        {
            q = q.Where(b => b.Status == query.Status.Value);
        }

        var totalItems = await q.CountAsync();
        var items = await q
            .OrderByDescending(b => b.BookId)
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(b => new BookResponseDto
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

        return new PagedResultDto<BookResponseDto>
        {
            TotalItems = totalItems,
            PageIndex = query.PageIndex,
            PageSize = query.PageSize,
            Items = items
        };
    }

    public async Task<BookResponseDto> UpdateBookAsync(int shopId, int bookId, UpdateBookRequestDto dto)
    {
        var book = await _db.Books.FirstOrDefaultAsync(b => b.BookId == bookId && b.ShopId == shopId);
        if (book == null)
        {
            throw new KeyNotFoundException("Book not found or unauthorized access.");
        }

        book.CategoryId = dto.CategoryId;
        book.Title = dto.Title;
        book.Price = dto.Price;
        book.StockQuantity = dto.StockQuantity;
        book.Description = dto.Description;
        book.ImageUrl = dto.ImageUrl;
        book.PublishedYear = dto.PublishedYear;

        if (dto.StockQuantity == 0)
        {
            book.Status = BookStatus.EMPTY;
        }
        else if (!string.IsNullOrEmpty(dto.Status) && Enum.TryParse<BookStatus>(dto.Status, true, out var parsedStatus))
        {
            book.Status = parsedStatus;
        }

        await _db.SaveChangesAsync();

        var categoryName = await _db.Categories
            .Where(c => c.CategoryId == book.CategoryId)
            .Select(c => c.CategoryName)
            .FirstOrDefaultAsync();

        return MapToBookDto(book, categoryName);
    }

    private static BookResponseDto MapToBookDto(BookEntity book, string? categoryName) => new BookResponseDto
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

    public async Task DeleteBookAsync(int shopId, int bookId)
    {
        var book = await _db.Books.FirstOrDefaultAsync(b => b.BookId == bookId && b.ShopId == shopId);
        if (book == null)
        {
            throw new KeyNotFoundException("Book not found or unauthorized access.");
        }

        book.Status = BookStatus.HIDDEN;
        await _db.SaveChangesAsync();
    }

    public async Task<ShopOrderDetailDto> GetShopOrderDetailAsync(int shopId, int orderId)
    {
        var order = await _db.Orders
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Book)
            .FirstOrDefaultAsync(o => o.OrderId == orderId && o.OrderDetails.Any(od => od.Book.ShopId == shopId));

        if (order == null)
        {
            throw new KeyNotFoundException("Order not found for shop.");
        }

        var shopItems = order.OrderDetails
            .Where(od => od.Book.ShopId == shopId)
            .Select(od => new ShopOrderItemDto
            {
                BookId = od.BookId,
                Title = od.Book.Title,
                Quantity = od.Quantity,
                UnitPrice = od.UnitPrice,
                ReturnStatus = od.ReturnStatus.ToString()
            }).ToList();

        return new ShopOrderDetailDto
        {
            OrderId = order.OrderId,
            OrderStatus = order.OrderStatus.ToString(),
            ShippingAddress = order.ShippingAddress,
            Weight = order.Weight,
            Note = order.Note,
            Items = shopItems
        };
    }

    public async Task UpdateOrderStatusAsync(int shopId, int orderId, UpdateOrderStatusDto dto)
    {
        using var tx = await _db.Database.BeginTransactionAsync();

        var order = await _db.Orders
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Book)
            .FirstOrDefaultAsync(o => o.OrderId == orderId && o.OrderDetails.Any(od => od.Book.ShopId == shopId));

        if (order == null)
        {
            throw new KeyNotFoundException("Order not found.");
        }

        if (!Enum.TryParse<OrderStatus>(dto.NewStatus, true, out var targetStatus))
        {
            throw new ArgumentException($"Invalid order status: {dto.NewStatus}");
        }

        if (targetStatus == OrderStatus.CANCELLED)
        {
            foreach (var item in order.OrderDetails)
            {
                var book = item.Book;
                if (book != null)
                {
                    book.StockQuantity += item.Quantity;
                    if (book.Status == BookStatus.EMPTY && book.StockQuantity > 0)
                    {
                        book.Status = BookStatus.ACTIVE;
                    }
                }
            }
        }

        order.OrderStatus = targetStatus;
        order.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public async Task<RevenueResponseDto> GetShopRevenueAsync(int shopId, DateTime? fromDate, DateTime? toDate, string? periodType)
    {
        var query = _db.OrderDetails
            .Include(od => od.Order)
            .Where(od => od.Book.ShopId == shopId && od.Order.OrderStatus == OrderStatus.DELIVERED);

        if (fromDate.HasValue)
        {
            query = query.Where(od => od.Order.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(od => od.Order.CreatedAt <= toDate.Value);
        }

        var items = await query.ToListAsync();

        var totalRevenue = items.Sum(i => i.Quantity * i.UnitPrice);
        var totalCompletedOrders = items.Select(i => i.OrderId).Distinct().Count();

        var details = items
            .GroupBy(i => FormatPeriod(i.Order.CreatedAt, periodType))
            .Select(g => new RevenueDetailDto
            {
                Period = g.Key,
                Amount = g.Sum(x => x.Quantity * x.UnitPrice),
                OrderCount = g.Select(x => x.OrderId).Distinct().Count()
            })
            .OrderBy(d => d.Period)
            .ToList();

        return new RevenueResponseDto
        {
            TotalRevenue = totalRevenue,
            TotalOrdersCompleted = totalCompletedOrders,
            Details = details
        };
    }

    private static string FormatPeriod(DateTime dt, string? periodType)
    {
        return (periodType?.ToUpperInvariant()) switch
        {
            "MONTH" => dt.ToString("yyyy-MM"),
            "YEAR" => dt.ToString("yyyy"),
            _ => dt.ToString("yyyy-MM-dd")
        };
    }

    public async Task<PagedResultDto<FeedbackDto>> GetShopFeedbacksAsync(int shopId, int? rating, bool? hasResponse, int pageIndex, int pageSize)
    {
        var q = _db.Feedbacks.Where(f => f.ShopId == shopId);

        if (rating.HasValue)
        {
            q = q.Where(f => f.Rating == rating.Value);
        }

        if (hasResponse.HasValue)
        {
            q = hasResponse.Value ? q.Where(f => f.Response != null) : q.Where(f => f.Response == null);
        }

        var totalItems = await q.CountAsync();
        var items = await q
            .OrderByDescending(f => f.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new FeedbackDto
            {
                FeedbackId = f.FeedbackId,
                OrderDetailId = f.OrderDetailId,
                ShopId = f.ShopId,
                Rating = f.Rating,
                Content = f.Content,
                Type = f.Type.ToString(),
                ImageUrl = f.ImageUrl,
                CreatedAt = f.CreatedAt,
                BookTitle = f.OrderDetail != null && f.OrderDetail.Book != null ? f.OrderDetail.Book.Title : null,
                Response = f.Response != null ? new FeedbackResponseDataDto
                {
                    ResponseId = f.Response.ResponseId,
                    Content = f.Response.Content,
                    ImageUrl = f.Response.ImageUrl,
                    CreatedAt = f.Response.CreatedAt
                } : null
            })
            .ToListAsync();

        return new PagedResultDto<FeedbackDto>
        {
            TotalItems = totalItems,
            PageIndex = pageIndex,
            PageSize = pageSize,
            Items = items
        };
    }

    public async Task<ResponseCreatedDto> CreateFeedbackResponseAsync(int shopId, int feedbackId, FeedbackResponseRequestDto dto)
    {
        var feedback = await _db.Feedbacks
            .Include(f => f.Response)
            .FirstOrDefaultAsync(f => f.FeedbackId == feedbackId && f.ShopId == shopId);

        if (feedback == null)
        {
            throw new KeyNotFoundException("Feedback not found or unauthorized access.");
        }

        if (feedback.Response != null)
        {
            throw new InvalidOperationException("Response already exists for this feedback.");
        }

        var response = new Response
        {
            FeedbackId = feedbackId,
            ShopId = shopId,
            Content = dto.Content,
            ImageUrl = dto.ImageUrl,
            CreatedAt = DateTime.UtcNow
        };

        _db.Responses.Add(response);
        await _db.SaveChangesAsync();

        return new ResponseCreatedDto
        {
            ResponseId = response.ResponseId,
            FeedbackId = response.FeedbackId,
            ShopId = response.ShopId,
            Content = response.Content,
            ImageUrl = response.ImageUrl,
            CreatedAt = response.CreatedAt
        };
    }

    public async Task ProcessReturnRequestAsync(int shopId, int returnRequestId, ProcessReturnRequestDto dto)
    {
        var returnReq = await _db.ReturnRequests
            .Include(r => r.OrderDetail)
                .ThenInclude(od => od.Book)
            .FirstOrDefaultAsync(r => r.ReturnRequestId == returnRequestId && r.OrderDetail.Book.ShopId == shopId);

        if (returnReq == null)
        {
            throw new KeyNotFoundException("Return request not found or unauthorized.");
        }

        if (dto.Status.Equals("APPROVED", StringComparison.OrdinalIgnoreCase))
        {
            returnReq.Status = ReturnRequestStatus.APPROVED;
            returnReq.OrderDetail.ReturnStatus = ReturnStatus.PROCESSING;
        }
        else if (dto.Status.Equals("REJECTED", StringComparison.OrdinalIgnoreCase))
        {
            returnReq.Status = ReturnRequestStatus.REJECTED;
            returnReq.OrderDetail.ReturnStatus = ReturnStatus.REJECTED;
        }
        else
        {
            throw new ArgumentException("Status must be APPROVED or REJECTED.");
        }

        returnReq.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}
