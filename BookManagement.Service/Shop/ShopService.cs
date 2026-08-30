using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookManagement.Service.Book;
using BookManagement.Service.Feedback;
using BookManagement.Service.Order;
using BookManagement.Service.Common;
using BookManagement.Repository.Data;
using BookManagement.Repository.Entities;
using ShopEntity = BookManagement.Repository.Entities.Shop;
using BookManagement.Repository.Entities.Enums;
using BookEntity = BookManagement.Repository.Entities.Book;
using Microsoft.EntityFrameworkCore;

namespace BookManagement.Service.Shop
{
    /// Vị trí: Domain Service - Thực thi logic nghiệp vụ hệ thống, quản lý Cửa hàng, gian hàng và kho hàng trong DbContext.
    public class ShopService : IShopService
    {
        private readonly AppDbContext _db;

        public ShopService(AppDbContext db)
        {
            _db = db;
        }

        private async Task<Guid> ResolveShopIdAsync(Guid userIdOrShopId)
        {
            var shopIds = await _db.Database
                .SqlQueryRaw<Guid>("SELECT Id FROM Shops WHERE UserId = {0} OR Id = {0}", userIdOrShopId)
                .ToListAsync();
            if (!shopIds.Any())
            {
                throw new KeyNotFoundException("Shop not found for the specified user or shop id.");
            }
            return shopIds.First();
        }

        /// Chức năng: Đăng ký tạo thông tin Cửa hàng bán sách mới
        public async Task<ShopRegisterResponseDto> RegisterShopAsync(Guid userId, ShopRegisterDto dto)
        {
            var existingShop = await _db.Shops.FirstOrDefaultAsync(s => s.Id == userId);
            if (existingShop != null)
            {
                throw new InvalidOperationException("User already registered a shop.");
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user != null)
            {
                user.Role = UserRole.SHOP;
                user.Address = dto.Address ?? user.Address;
                user.QrImageUrl = dto.QrImageUrl ?? user.QrImageUrl;
                user.UpdatedAt = DateTimeOffset.UtcNow;
            }

            var shopName = dto.ShopName.Trim();
            var createdAt = DateTimeOffset.UtcNow;

            await _db.Database.ExecuteSqlInterpolatedAsync(
                $"IF NOT EXISTS (SELECT 1 FROM Shops WHERE Id = {userId}) INSERT INTO Shops (Id, ShopName, Condition, Rating, ViolationCount, CreatedAt) VALUES ({userId}, {shopName}, 'OPEN', 0, 0, {createdAt});");

            _db.Notifications.Add(new BookManagement.Repository.Entities.Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = NotificationType.SYSTEM,
                ReferenceId = userId,
                Content = $"Chúc mừng! Cửa hàng '{shopName}' của bạn đã được đăng ký thành công và gian hàng đã đi vào hoạt động. Bạn có thể bắt đầu đăng bán sách ngay!",
                IsRead = false,
                CreatedAt = createdAt
            });

            await _db.SaveChangesAsync();

            return new ShopRegisterResponseDto
            {
                ShopId = userId,
                ShopName = shopName,
                Condition = ShopCondition.OPEN.ToString(),
                CreatedAt = createdAt
            };
        }

        /// Chức năng: Xem thông tin hồ sơ lý lịch Cửa hàng
        public async Task<ShopProfileDto> GetShopProfileAsync(Guid userIdOrShopId)
        {
            var shopId = await ResolveShopIdAsync(userIdOrShopId);
            var shop = await _db.Shops
                .Include(s => s.Books)
                .FirstOrDefaultAsync(s => s.Id == shopId);

            if (shop == null)
            {
                throw new KeyNotFoundException("Shop not found.");
            }

            return new ShopProfileDto
            {
                ShopId = shop.Id,
                ShopName = shop.ShopName,
                Condition = shop.Condition.ToString(),
                Rating = shop.Rating,
                TotalBooks = shop.Books.Count(b => b.Status != BookStatus.HIDDEN),
                CreatedAt = shop.CreatedAt
            };
        }

        /// Chức năng: Đăng bán sản phẩm sách mới cho Cửa hàng
        public async Task<BookResponseDto> CreateBookAsync(Guid userIdOrShopId, CreateBookRequestDto dto)
        {
            var shopId = await ResolveShopIdAsync(userIdOrShopId);

            if (string.IsNullOrWhiteSpace(dto.Title))
            {
                throw new ArgumentException("Tựa đề sách không được để trống.");
            }

            if (string.IsNullOrWhiteSpace(dto.Author))
            {
                throw new ArgumentException("Tên tác giả không được để trống.");
            }

            if (dto.CategoryId == Guid.Empty || !await _db.Categories.AnyAsync(c => c.Id == dto.CategoryId))
            {
                throw new ArgumentException("Thể loại sách đã chọn không tồn tại trong hệ thống.");
            }

            if (dto.Price < 1000)
            {
                throw new ArgumentException("Giá sản phẩm phải từ 1.000 VNĐ trở lên.");
            }

            if (dto.StockQuantity < 0)
            {
                throw new ArgumentException("Số lượng tồn kho không được là số âm.");
            }

            if (dto.PublishedYear > DateTime.UtcNow.Year + 1)
            {
                throw new ArgumentException($"Năm xuất bản không được lớn hơn {DateTime.UtcNow.Year + 1}.");
            }

            string isbn;
            if (!string.IsNullOrWhiteSpace(dto.Isbn))
            {
                isbn = dto.Isbn.Trim();
                var existingIsbn = await _db.Books.AnyAsync(b => b.Isbn == isbn);
                if (existingIsbn)
                {
                    throw new InvalidOperationException($"Mã ISBN '{isbn}' đã tồn tại trong hệ thống. Vui lòng nhập mã ISBN khác.");
                }
            }
            else
            {
                do
                {
                    isbn = "978-" + Random.Shared.Next(1000, 9999) + "-" + Random.Shared.Next(1000, 9999);
                } while (await _db.Books.AnyAsync(b => b.Isbn == isbn));
            }

            var status = dto.StockQuantity > 0 ? BookStatus.ACTIVE : BookStatus.EMPTY;

            var book = new BookEntity
            {
                Id = Guid.NewGuid(),
                ShopId = shopId,
                CategoryId = dto.CategoryId,
                Title = dto.Title.Trim(),
                Isbn = isbn,
                Author = dto.Author.Trim(),
                Publisher = dto.Publisher?.Trim(),
                Price = dto.Price,
                StockQuantity = dto.StockQuantity,
                Description = dto.Description?.Trim(),
                ImageUrl = dto.ImageUrl?.Trim(),
                PublishedYear = dto.PublishedYear > 0 ? dto.PublishedYear : DateTime.UtcNow.Year,
                Status = status,
                Rating = 5.0f
            };

            _db.Books.Add(book);

            var imagesToInsert = new List<BookImage>();
            int imgOrder = 0;

            if (dto.Images != null && dto.Images.Count > 0)
            {
                foreach (var img in dto.Images.Where(i => !string.IsNullOrWhiteSpace(i.ImageUrl)))
                {
                    imagesToInsert.Add(new BookImage
                    {
                        Id = Guid.NewGuid(),
                        BookId = book.Id,
                        ImageUrl = img.ImageUrl.Trim(),
                        PublicId = img.PublicId?.Trim(),
                        IsCover = img.IsCover || imgOrder == 0,
                        DisplayOrder = img.DisplayOrder > 0 ? img.DisplayOrder : imgOrder++,
                        CreatedAt = DateTimeOffset.UtcNow
                    });
                }
            }
            else if (dto.ImageUrls != null && dto.ImageUrls.Count > 0)
            {
                foreach (var url in dto.ImageUrls.Where(u => !string.IsNullOrWhiteSpace(u)))
                {
                    imagesToInsert.Add(new BookImage
                    {
                        Id = Guid.NewGuid(),
                        BookId = book.Id,
                        ImageUrl = url.Trim(),
                        IsCover = imgOrder == 0,
                        DisplayOrder = imgOrder++,
                        CreatedAt = DateTimeOffset.UtcNow
                    });
                }
            }
            else if (!string.IsNullOrWhiteSpace(dto.ImageUrl))
            {
                imagesToInsert.Add(new BookImage
                {
                    Id = Guid.NewGuid(),
                    BookId = book.Id,
                    ImageUrl = dto.ImageUrl.Trim(),
                    IsCover = true,
                    DisplayOrder = 0,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }

            if (imagesToInsert.Count > 0)
            {
                if (string.IsNullOrWhiteSpace(book.ImageUrl))
                {
                    var coverImg = imagesToInsert.FirstOrDefault(i => i.IsCover) ?? imagesToInsert[0];
                    book.ImageUrl = coverImg.ImageUrl;
                }
                _db.BookImages.AddRange(imagesToInsert);
                book.Images = imagesToInsert;
            }

            var shop = await _db.Shops.FirstOrDefaultAsync(s => s.Id == shopId);
            var shopName = shop?.ShopName ?? "Cửa hàng";

            var activeCustomers = await _db.Users
                .Where(u => u.Role == UserRole.CUSTOMER && u.Status == UserStatus.ACTIVE)
                .Take(100)
                .ToListAsync();

            foreach (var customer in activeCustomers)
            {
                _db.Notifications.Add(new BookManagement.Repository.Entities.Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = customer.Id,
                    Type = NotificationType.PROMOTION,
                    ReferenceId = book.Id,
                    Content = $"[Sách Mới Về] Sách mới '{book.Title}' vừa được cửa hàng '{shopName}' đăng bán với giá {book.Price:N0} VNĐ. Xem ngay!",
                    ImageUrl = book.ImageUrl,
                    IsRead = false,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }

            await _db.SaveChangesAsync();

            var categoryName = await _db.Categories
                .Where(c => c.Id == book.CategoryId)
                .Select(c => c.CategoryName)
                .FirstOrDefaultAsync();

            return MapToBookDto(book, categoryName);
        }

        /// Chức năng: Xem thông tin chi tiết 1 sản phẩm sách của Cửa hàng
        public async Task<BookResponseDto> GetBookByIdAsync(Guid userIdOrShopId, Guid bookId)
        {
            var shopId = await ResolveShopIdAsync(userIdOrShopId);
            var book = await _db.Books
                .Include(b => b.Category)
                .Include(b => b.Images)
                .FirstOrDefaultAsync(b => b.Id == bookId && b.ShopId == shopId);

            if (book == null)
            {
                throw new KeyNotFoundException("Book not found or unauthorized access.");
            }

            return MapToBookDto(book, book.Category?.CategoryName);
        }

        /// Chức năng: Lấy danh sách sản phẩm kho sách của Cửa hàng phân trang
        public async Task<PagedResultDto<BookResponseDto>> GetShopBooksAsync(Guid userIdOrShopId, BookQueryDto query)
        {
            var shopId = await ResolveShopIdAsync(userIdOrShopId);
            var q = _db.Books.Where(b => b.ShopId == shopId);

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                var kw = query.Keyword.Trim().ToLower();
                q = q.Where(b => b.Title.ToLower().Contains(kw) || (b.Isbn != null && b.Isbn.ToLower().Contains(kw)) || (b.Author != null && b.Author.ToLower().Contains(kw)));
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
                .OrderBy(b => b.Title)
                .Skip((query.PageIndex - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(b => new BookResponseDto
                {
                    BookId = b.Id,
                    ShopId = b.ShopId,
                    CategoryId = b.CategoryId,
                    CategoryName = b.Category != null ? b.Category.CategoryName : null,
                    Title = b.Title,
                    Isbn = b.Isbn ?? string.Empty,
                    Author = b.Author ?? string.Empty,
                    Publisher = b.Publisher ?? string.Empty,
                    Price = b.Price,
                    StockQuantity = b.StockQuantity,
                    Description = b.Description,
                    ImageUrl = b.ImageUrl,
                    PublishedYear = b.PublishedYear,
                    Status = b.Status.ToString(),
                    Rating = b.Rating,
                    Images = b.Images.OrderBy(i => i.DisplayOrder).Select(i => new BookImageDto
                    {
                        Id = i.Id,
                        ImageUrl = i.ImageUrl,
                        PublicId = i.PublicId,
                        IsCover = i.IsCover,
                        DisplayOrder = i.DisplayOrder
                    }).ToList(),
                    CreatedAt = b.CreatedAt,
                    UpdatedAt = b.UpdatedAt
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

        /// Chức năng: Cập nhật thông tin và giá bán sản phẩm sách
        public async Task<BookResponseDto> UpdateBookAsync(Guid userIdOrShopId, Guid bookId, UpdateBookRequestDto dto)
        {
            var shopId = await ResolveShopIdAsync(userIdOrShopId);
            var book = await _db.Books
                .Include(b => b.Images)
                .FirstOrDefaultAsync(b => b.Id == bookId && b.ShopId == shopId);
            if (book == null)
            {
                throw new KeyNotFoundException("Book not found or unauthorized access.");
            }

            if (dto.CategoryId != Guid.Empty && await _db.Categories.AnyAsync(c => c.Id == dto.CategoryId))
            {
                book.CategoryId = dto.CategoryId;
            }

            if (!string.IsNullOrWhiteSpace(dto.Title))
            {
                book.Title = dto.Title;
            }

            if (dto.Price > 0)
            {
                book.Price = dto.Price;
            }

            if (dto.StockQuantity >= 0)
            {
                book.StockQuantity = dto.StockQuantity;
            }

            if (dto.Description != null)
            {
                book.Description = dto.Description;
            }

            if (dto.ImageUrl != null)
            {
                book.ImageUrl = dto.ImageUrl;
            }

            if (dto.PublishedYear > 0)
            {
                book.PublishedYear = dto.PublishedYear;
            }

            if (dto.StockQuantity == 0)
            {
                book.Status = BookStatus.EMPTY;
            }
            else if (!string.IsNullOrEmpty(dto.Status) && Enum.TryParse<BookStatus>(dto.Status, true, out var parsedStatus))
            {
                book.Status = parsedStatus;
            }

            // Cập nhật danh sách ảnh nếu có
            if (dto.Images != null && dto.Images.Count > 0)
            {
                _db.BookImages.RemoveRange(book.Images);
                var newImages = dto.Images.Where(i => !string.IsNullOrWhiteSpace(i.ImageUrl)).Select((img, idx) => new BookImage
                {
                    Id = Guid.NewGuid(),
                    BookId = book.Id,
                    ImageUrl = img.ImageUrl.Trim(),
                    PublicId = img.PublicId?.Trim(),
                    IsCover = img.IsCover || idx == 0,
                    DisplayOrder = img.DisplayOrder > 0 ? img.DisplayOrder : idx,
                    CreatedAt = DateTimeOffset.UtcNow
                }).ToList();

                _db.BookImages.AddRange(newImages);
                book.Images = newImages;

                if (newImages.Count > 0 && string.IsNullOrWhiteSpace(dto.ImageUrl))
                {
                    book.ImageUrl = (newImages.FirstOrDefault(i => i.IsCover) ?? newImages[0]).ImageUrl;
                }
            }
            else if (dto.ImageUrls != null && dto.ImageUrls.Count > 0)
            {
                _db.BookImages.RemoveRange(book.Images);
                var newImages = dto.ImageUrls.Where(u => !string.IsNullOrWhiteSpace(u)).Select((url, idx) => new BookImage
                {
                    Id = Guid.NewGuid(),
                    BookId = book.Id,
                    ImageUrl = url.Trim(),
                    IsCover = idx == 0,
                    DisplayOrder = idx,
                    CreatedAt = DateTimeOffset.UtcNow
                }).ToList();

                _db.BookImages.AddRange(newImages);
                book.Images = newImages;

                if (newImages.Count > 0 && string.IsNullOrWhiteSpace(dto.ImageUrl))
                {
                    book.ImageUrl = newImages[0].ImageUrl;
                }
            }

            await _db.SaveChangesAsync();

            var categoryName = await _db.Categories
                .Where(c => c.Id == book.CategoryId)
                .Select(c => c.CategoryName)
                .FirstOrDefaultAsync();

            return MapToBookDto(book, categoryName);
        }

        private static BookResponseDto MapToBookDto(BookEntity book, string? categoryName) => new BookResponseDto
        {
            BookId = book.Id,
            ShopId = book.ShopId,
            CategoryId = book.CategoryId,
            CategoryName = categoryName,
            Title = book.Title,
            Isbn = book.Isbn ?? string.Empty,
            Author = book.Author ?? string.Empty,
            Publisher = book.Publisher ?? string.Empty,
            Price = book.Price,
            StockQuantity = book.StockQuantity,
            Description = book.Description,
            ImageUrl = book.ImageUrl,
            PublishedYear = book.PublishedYear,
            Status = book.Status.ToString(),
            Rating = book.Rating,
            Images = book.Images != null
                ? book.Images.OrderBy(i => i.DisplayOrder).Select(i => new BookImageDto
                {
                    Id = i.Id,
                    ImageUrl = i.ImageUrl,
                    PublicId = i.PublicId,
                    IsCover = i.IsCover,
                    DisplayOrder = i.DisplayOrder
                }).ToList()
                : new List<BookImageDto>(),
            CreatedAt = book.CreatedAt,
            UpdatedAt = book.UpdatedAt
        };

        /// Chức năng: Ẩn sản phẩm sách khỏi gian hàng Cửa hàng
        public async Task DeleteBookAsync(Guid userIdOrShopId, Guid bookId)
        {
            var shopId = await ResolveShopIdAsync(userIdOrShopId);
            var book = await _db.Books.FirstOrDefaultAsync(b => b.Id == bookId && b.ShopId == shopId);
            if (book == null)
            {
                throw new KeyNotFoundException("Book not found or unauthorized access.");
            }

            book.Status = BookStatus.HIDDEN;
            await _db.SaveChangesAsync();
        }

        /// Chức năng: Xem chi tiết đơn hàng bán của Cửa hàng
        public async Task<ShopOrderDetailDto> GetShopOrderDetailAsync(Guid userIdOrShopId, Guid orderId)
        {
            var shopId = await ResolveShopIdAsync(userIdOrShopId);
            var order = await _db.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Book)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.OrderDetails.Any(od => od.Book.ShopId == shopId));

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
                OrderId = order.Id,
                OrderStatus = order.OrderStatus.ToString(),
                ShippingAddress = order.ShippingAddress,
                Weight = order.Weight ?? 0m,
                Note = order.Note,
                Items = shopItems
            };
        }

        /// Chức năng: Cập nhật trạng thái xử lý đơn hàng của Cửa hàng
        public async Task UpdateOrderStatusAsync(Guid userIdOrShopId, Guid orderId, UpdateOrderStatusDto dto)
        {
            var shopId = await ResolveShopIdAsync(userIdOrShopId);
            var strategy = _db.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var tx = await _db.Database.BeginTransactionAsync();

                var order = await _db.Orders
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Book)
                    .FirstOrDefaultAsync(o => o.Id == orderId && o.OrderDetails.Any(od => od.Book.ShopId == shopId));

                if (order == null)
                {
                    throw new KeyNotFoundException("Order not found.");
                }

                var rawStatus = !string.IsNullOrEmpty(dto.OrderStatus) ? dto.OrderStatus : dto.NewStatus;
                if (string.IsNullOrEmpty(rawStatus) || !Enum.TryParse<OrderStatus>(rawStatus, true, out var targetStatus))
                {
                    throw new ArgumentException($"Invalid order status: {rawStatus}");
                }

                if (dto.Weight.HasValue && dto.Weight.Value > 0)
                {
                    order.Weight = dto.Weight.Value;
                }

                if (!string.IsNullOrEmpty(dto.Note))
                {
                    order.Note = dto.Note;
                }

                if (targetStatus == OrderStatus.CANCELLED)
                {
                    if (order.OrderStatus != OrderStatus.CANCELLED)
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
                }

                order.OrderStatus = targetStatus;
                order.UpdatedAt = DateTimeOffset.UtcNow;

                await _db.SaveChangesAsync();
                await tx.CommitAsync();
            });
        }

        /// Chức năng: Thống kê doanh thu Cửa hàng theo mốc thời gian
        public async Task<RevenueResponseDto> GetShopRevenueAsync(Guid userIdOrShopId, RevenueQueryRequest query)
        {
            var shopId = await ResolveShopIdAsync(userIdOrShopId);
            var q = _db.OrderDetails
                .Include(od => od.Order)
                .Where(od => od.Book.ShopId == shopId
                    && od.Order.OrderStatus == OrderStatus.DELIVERED
                    && od.ReturnStatus != ReturnStatus.REFUNDED);

            if (query.FromDate.HasValue)
            {
                var fromDt = new DateTimeOffset(query.FromDate.Value);
                q = q.Where(od => od.Order.CreatedAt >= fromDt);
            }

            if (query.ToDate.HasValue)
            {
                var toDt = new DateTimeOffset(query.ToDate.Value);
                q = q.Where(od => od.Order.CreatedAt <= toDt);
            }

            var items = await q.ToListAsync();

            var totalRevenue = items.Sum(i => i.Quantity * i.UnitPrice);
            var totalCompletedOrders = items.Select(i => i.OrderId).Distinct().Count();

            var details = items
                .GroupBy(i => FormatPeriod(i.Order.CreatedAt.DateTime, query.PeriodType))
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

        /// Chức năng: Lấy danh sách đánh giá từ khách hàng dành cho Cửa hàng
        public async Task<PagedResultDto<FeedbackDto>> GetShopFeedbacksAsync(Guid userIdOrShopId, ShopFeedbackQueryRequest query)
        {
            var shopId = await ResolveShopIdAsync(userIdOrShopId);
            var q = _db.Feedbacks.Where(f => f.ShopId == shopId);

            if (query.Rating.HasValue)
            {
                q = q.Where(f => f.Rating == query.Rating.Value);
            }

            if (query.HasResponse.HasValue)
            {
                q = query.HasResponse.Value ? q.Where(f => f.Response != null) : q.Where(f => f.Response == null);
            }

            var pageIndex = query.PageIndex < 1 ? 1 : query.PageIndex;
            var pageSize = query.PageSize < 1 ? 10 : query.PageSize;

            var totalItems = await q.CountAsync();
            var items = await q
                .OrderByDescending(f => f.CreatedAt)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .Select(f => new FeedbackDto
                {
                    FeedbackId = f.Id,
                    OrderDetailId = f.OrderDetailId,
                    ShopId = f.ShopId,
                    Rating = f.Rating,
                    Content = f.Content ?? string.Empty,
                    Type = f.Type.ToString(),
                    ImageUrl = f.ImageUrl,
                    CreatedAt = f.CreatedAt,
                    BookTitle = f.OrderDetail != null && f.OrderDetail.Book != null ? f.OrderDetail.Book.Title : null,
                    Response = (f.Response != null && !f.Response.IsDeleted) ? new FeedbackResponseDataDto
                    {
                        ResponseId = f.Response.Id,
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

        /// Chức năng: Phản hồi bình luận đánh giá của khách hàng
        public async Task<ResponseCreatedDto> CreateFeedbackResponseAsync(Guid userIdOrShopId, Guid feedbackId, FeedbackResponseRequestDto dto)
        {
            var shopId = await ResolveShopIdAsync(userIdOrShopId);
            var feedback = await _db.Feedbacks
                .Include(f => f.Response)
                .FirstOrDefaultAsync(f => f.Id == feedbackId && f.ShopId == shopId);

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
                CreatedAt = DateTimeOffset.UtcNow
            };

            _db.Responses.Add(response);
            await _db.SaveChangesAsync();

            return new ResponseCreatedDto
            {
                ResponseId = response.Id,
                FeedbackId = response.FeedbackId,
                ShopId = response.ShopId,
                Content = response.Content,
                ImageUrl = response.ImageUrl,
                CreatedAt = response.CreatedAt
            };
        }

        /// Chức năng: Xử lý chấp nhận hoặc từ chối yêu cầu trả hàng của khách
        public async Task ProcessReturnRequestAsync(Guid userIdOrShopId, Guid returnRequestId, ProcessReturnRequestDto dto)
        {
            var shopId = await ResolveShopIdAsync(userIdOrShopId);
            var returnReq = await _db.ReturnRequests
                .Include(r => r.OrderDetail)
                    .ThenInclude(od => od.Book)
                .Include(r => r.OrderDetail)
                    .ThenInclude(od => od.Order)
                .FirstOrDefaultAsync(r => r.Id == returnRequestId && r.OrderDetail.Book.ShopId == shopId);

            if (returnReq == null)
            {
                throw new KeyNotFoundException("Return request not found or unauthorized.");
            }

            bool isApprove = (dto.IsApproved.HasValue && dto.IsApproved.Value)
                || (dto.Status != null && dto.Status.Equals("APPROVED", StringComparison.OrdinalIgnoreCase));

            string notificationContent;

            if (isApprove)
            {
                returnReq.Status = ReturnRequestStatus.APPROVED;
                returnReq.OrderDetail.ReturnStatus = ReturnStatus.PROCESSING;
                notificationContent = $"Yêu cầu trả hàng cho cuốn '{returnReq.OrderDetail.Book?.Title}' đã được Shop chấp nhận. Đang xử lý hoàn tiền.";
            }
            else
            {
                returnReq.Status = ReturnRequestStatus.REJECTED;
                returnReq.OrderDetail.ReturnStatus = ReturnStatus.REJECTED;
                notificationContent = $"Yêu cầu trả hàng cho cuốn '{returnReq.OrderDetail.Book?.Title}' đã bị Shop từ chối. Bạn có thể gửi Khiếu nại lên Admin nếu không đồng ý.";
            }

            returnReq.UpdatedAt = DateTimeOffset.UtcNow;

            if (returnReq.OrderDetail?.Order != null)
            {
                var notification = new BookManagement.Repository.Entities.Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = returnReq.OrderDetail.Order.UserId,
                    Type = NotificationType.ORDER_UPDATE,
                    ReferenceId = returnReq.Id,
                    Content = notificationContent,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                await _db.Notifications.AddAsync(notification);
            }

            await _db.SaveChangesAsync();
        }
    }
}
