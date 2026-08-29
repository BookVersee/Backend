using BookManagement.Repository.Abstractions;
using BookManagement.Repository.Data;
using BookManagement.Repository.Entities;
using BookManagement.Repository.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace BookManagement.Service.Admin;

public class AdminService : IAdminService
{
    private readonly AppDbContext _context;
    private readonly IUserRepository _userRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IUserSessionRepository _sessionRepository;
    private readonly IBookRepository _bookRepository;

    public AdminService(
        AppDbContext context,
        IUserRepository userRepository,
        IOrderRepository orderRepository,
        IUserSessionRepository sessionRepository,
        IBookRepository bookRepository)
    {
        _context = context;
        _userRepository = userRepository;
        _orderRepository = orderRepository;
        _sessionRepository = sessionRepository;
        _bookRepository = bookRepository;
    }

    // ===== USER MANAGEMENT =====
    public async Task<PagedResult<UserResponse>> GetUsersAsync(UserFilterRequest filter)
    {
        var query = _context.Users.AsNoTracking().Include(u => u.Shop).AsQueryable();

        if (filter.Role.HasValue)
            query = query.Where(u => u.Role == filter.Role.Value);

        if (filter.Status.HasValue)
            query = query.Where(u => u.Status == filter.Status.Value);

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var kw = filter.Keyword.Trim().ToLower();
            query = query.Where(u => u.Username.ToLower().Contains(kw) ||
                                     u.Email.ToLower().Contains(kw) ||
                                     (u.FullName != null && u.FullName.ToLower().Contains(kw)) ||
                                     (u.Shop != null && u.Shop.ShopName.ToLower().Contains(kw)));
        }

        var totalCount = await query.CountAsync();
        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize < 1 ? 10 : filter.PageSize;

        var items = await query.OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<UserResponse>
        {
            Items = items.Select(MapToUserResponse),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<UserDetailResponse> GetUserDetailAsync(Guid id)
    {
        var user = await _context.Users.AsNoTracking().Include(u => u.Shop).FirstOrDefaultAsync(u => u.Id == id)
                   ?? await _context.Users.AsNoTracking().Include(u => u.Shop).FirstOrDefaultAsync(u => u.Shop != null && u.Shop.Id == id);

        if (user == null)
            throw new KeyNotFoundException("User or Shop not found.");

        var orders = await _orderRepository.GetOrdersByUserIdAsync(user.Id);
        var transactions = await _context.TransactionHistories.AsNoTracking().Where(t => t.UserId == user.Id).ToListAsync();

        return new UserDetailResponse
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            Phone = user.Phone,
            Address = user.Address,
            Role = user.Role.ToString(),
            Status = user.Status.ToString(),
            ShopId = user.Shop?.Id,
            ShopName = user.Shop?.ShopName,
            ShopStatus = user.Shop?.Condition.ToString(),
            CreatedAt = user.CreatedAt,
            RecentOrders = orders.Select(o => new OrderSummaryResponse
            {
                Id = o.Id,
                UserId = o.UserId,
                TotalAmount = o.TotalAmount,
                Status = o.OrderStatus.ToString(),
                ShippingAddress = o.ShippingAddress,
                CreatedAt = o.CreatedAt
            }).ToList(),
            FinancialTransactions = transactions.Select(t => new TransactionSummaryResponse
            {
                Id = t.Id,
                UserId = t.UserId,
                ReferenceType = t.ReferenceType.ToString(),
                ReferenceId = t.ReferenceId,
                TransactionType = t.TransactionType.ToString(),
                Amount = t.Amount,
                TransactionCode = t.TransactionCode ?? string.Empty,
                Description = t.Description,
                CreatedAt = t.CreatedAt
            }).ToList()
        };
    }

    public async Task UpdateUserStatusAsync(Guid userId, string status)
    {
        var user = await _context.Users.Include(u => u.Shop).FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            throw new KeyNotFoundException("User not found.");

        var newStatus = (UserStatus)Enum.Parse(typeof(UserStatus), status);
        user.Status = newStatus;

        if (user.Shop != null)
        {
            if (newStatus == UserStatus.LOCKED)
            {
                user.Shop.Condition = ShopCondition.CLOSED;
            }
            else if (newStatus == UserStatus.ACTIVE && user.Shop.Condition == ShopCondition.CLOSED)
            {
                user.Shop.Condition = ShopCondition.OPEN;
            }
        }

        if (user.Status == UserStatus.LOCKED)
        {
            await _sessionRepository.RevokeAllUserSessionsAsync(userId);
        }

        await _context.SaveChangesAsync();
    }

    // ===== DISPUTE MANAGEMENT =====
    public async Task<IEnumerable<DisputeResponse>> GetDisputesAsync(string? status = null)
    {
        IQueryable<BookManagement.Repository.Entities.ReturnRequest> query = _context.ReturnRequests.AsNoTracking()
            .Include(rr => rr.OrderDetail)
            .ThenInclude(od => od.Order)
            .ThenInclude(o => o.User)
            .Include(rr => rr.OrderDetail)
            .ThenInclude(od => od.Book)
            .ThenInclude(b => b.Shop);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(rr => rr.Status == (ReturnRequestStatus)Enum.Parse(typeof(ReturnRequestStatus), status));

        var disputes = await query.OrderByDescending(rr => rr.CreatedAt).ToListAsync();
        return disputes.Select(MapToDisputeResponse);
    }

    public async Task<DisputeResponse> GetDisputeDetailAsync(Guid disputeId)
    {
        var dispute = await _context.ReturnRequests
            .AsNoTracking()
            .Include(rr => rr.OrderDetail)
            .ThenInclude(od => od.Order)
            .ThenInclude(o => o.User)
            .Include(rr => rr.OrderDetail)
            .ThenInclude(od => od.Book)
            .ThenInclude(b => b.Shop)
            .FirstOrDefaultAsync(rr => rr.Id == disputeId);

        if (dispute == null)
            throw new Exception("Dispute not found");

        return MapToDisputeResponse(dispute);
    }

    public async Task ResolveDisputeAsync(Guid disputeId, ResolveDisputeRequest request)
    {
        var dispute = await _context.ReturnRequests
            .Include(rr => rr.OrderDetail)
                .ThenInclude(od => od.Order)
            .Include(rr => rr.OrderDetail)
                .ThenInclude(od => od.Book)
            .FirstOrDefaultAsync(rr => rr.Id == disputeId);

        if (dispute == null)
            throw new KeyNotFoundException("Dispute not found.");

        dispute.Status = request.ApproveRefund ? ReturnRequestStatus.APPROVED : ReturnRequestStatus.REJECTED;
        dispute.UpdatedAt = DateTimeOffset.UtcNow;

        if (dispute.OrderDetail != null)
        {
            dispute.OrderDetail.ReturnStatus = request.ApproveRefund ? ReturnStatus.PROCESSING : ReturnStatus.REJECTED;

            if (dispute.OrderDetail.Order != null)
            {
                var buyerNotification = new BookManagement.Repository.Entities.Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = dispute.OrderDetail.Order.UserId,
                    Type = NotificationType.ORDER_UPDATE,
                    ReferenceId = dispute.Id,
                    Content = request.ApproveRefund
                        ? $"Ban quản trị (Admin) đã chấp nhận khiếu nại trả hàng cuốn '{dispute.OrderDetail.Book?.Title}'. Yêu cầu hoàn tiền đang được xử lý."
                        : $"Ban quản trị (Admin) đã từ chối khiếu nại trả hàng cuốn '{dispute.OrderDetail.Book?.Title}'. Ghi chú: {request.AdminResolutionNote ?? "Không đủ bằng chứng"}.",
                    CreatedAt = DateTimeOffset.UtcNow
                };
                await _context.Notifications.AddAsync(buyerNotification);
            }

            // Nếu Admin phán quyết Khách thắng (ApproveRefund = true), tính 1 lần vi phạm cho Shop
            if (request.ApproveRefund && dispute.OrderDetail.Book != null && dispute.OrderDetail.Book.ShopId != Guid.Empty)
            {
                var shop = await _context.Shops.FirstOrDefaultAsync(s => s.Id == dispute.OrderDetail.Book.ShopId);
                if (shop != null)
                {
                    await HandleShopViolationAsync(shop, "Bị Admin phán quyết thua khiếu nại trả hàng");
                }
            }
        }

        await _context.SaveChangesAsync();
    }

    // ===== ORDER MONITORING =====
    public async Task<PagedResult<OrderResponse>> GetAllOrdersAsync(int page = 1, int pageSize = 10)
    {
        var query = _context.Orders.AsNoTracking();
        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<OrderResponse>
        {
            Items = items.Select(MapToOrderResponse),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<OrderResponse>> GetOrdersByStatusAsync(string status, int page = 1, int pageSize = 10)
    {
        var orderStatus = (OrderStatus)Enum.Parse(typeof(OrderStatus), status);
        var query = _context.Orders.AsNoTracking().Where(o => o.OrderStatus == orderStatus);
        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<OrderResponse>
        {
            Items = items.Select(MapToOrderResponse),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<OrderResponse> GetOrderDetailAsync(Guid orderId)
    {
        var order = await _context.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null)
            throw new Exception("Order not found");

        return MapToOrderResponse(order);
    }

    // ===== BOOK MANAGEMENT =====
    public async Task<PagedResult<BookResponse>> GetAllBooksAsync(int page = 1, int pageSize = 10)
    {
        var query = _context.Books.AsNoTracking();
        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(b => b.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<BookResponse>
        {
            Items = items.Select(MapToBookResponse),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<BookResponse>> GetBooksByStatusAsync(string status, int page = 1, int pageSize = 10)
    {
        var bookStatus = (BookStatus)Enum.Parse(typeof(BookStatus), status);
        var query = _context.Books.AsNoTracking().Where(b => b.Status == bookStatus);
        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(b => b.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<BookResponse>
        {
            Items = items.Select(MapToBookResponse),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task HideBookAsync(Guid bookId)
    {
        var book = await _bookRepository.GetByIdAsync(bookId);
        if (book == null)
            throw new Exception("Book not found");

        book.Status = BookStatus.HIDDEN;
        await _bookRepository.UpdateAsync(book);
    }

    // ===== SHOP MANAGEMENT =====
    public async Task<IEnumerable<ShopResponse>> GetPendingShopsAsync()
    {
        var shops = await _context.Shops
            .AsNoTracking()
            .Where(s => s.Condition == ShopCondition.PENDING)
            .ToListAsync();

        return shops.Select(MapToShopResponse);
    }

    public async Task<PagedResult<ShopResponse>> GetAllShopsAsync(int page = 1, int pageSize = 10)
    {
        var query = _context.Shops.AsNoTracking();
        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<ShopResponse>
        {
            Items = items.Select(MapToShopResponse),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task ApproveShopAsync(Guid shopId)
    {
        var shop = await _context.Shops.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == shopId);
        if (shop == null)
            throw new KeyNotFoundException("Shop not found.");

        shop.Condition = ShopCondition.OPEN;
        if (shop.User != null)
        {
            if (shop.User.Role != BookManagement.Repository.Entities.Enums.UserRole.ADMIN)
            {
                shop.User.Role = BookManagement.Repository.Entities.Enums.UserRole.SHOP;
            }
            shop.User.Status = UserStatus.ACTIVE;

            // Automated notification to shop owner about shop reopening
            var notification = new BookManagement.Repository.Entities.Notification
            {
                Id = Guid.NewGuid(),
                UserId = shop.UserId,
                Type = NotificationType.SYSTEM,
                ReferenceId = shop.Id,
                Content = $"Cửa hàng '{shop.ShopName}' của bạn đã được Ban quản trị (Admin) mở khóa / kích hoạt hoạt động trở lại.",
                CreatedAt = DateTimeOffset.UtcNow
            };
            await _context.Notifications.AddAsync(notification);
        }

        await _context.SaveChangesAsync();
    }

    public async Task LockShopAsync(Guid shopId, LockShopRequest request)
    {
        var shop = await _context.Shops.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == shopId);
        if (shop == null)
            throw new KeyNotFoundException("Shop not found.");

        shop.Condition = ShopCondition.CLOSED;
        if (shop.User != null)
        {
            shop.User.Status = UserStatus.LOCKED;
            await _sessionRepository.RevokeAllUserSessionsAsync(shop.UserId);

            var notification = new BookManagement.Repository.Entities.Notification
            {
                Id = Guid.NewGuid(),
                UserId = shop.UserId,
                Type = NotificationType.SYSTEM,
                ReferenceId = shop.Id,
                Content = $"Cửa hàng '{shop.ShopName}' của bạn đã bị Ban quản trị (Admin) tạm khóa. Ghi chú: {request.Reason ?? "Vi phạm tiêu chuẩn cộng đồng"}.",
                CreatedAt = DateTimeOffset.UtcNow
            };
            await _context.Notifications.AddAsync(notification);
        }

        await _context.SaveChangesAsync();
    }

    // ===== DASHBOARD & STATISTICS =====
    public async Task<DashboardStatisticsResponse> GetDashboardStatisticsAsync(string period = "month")
    {
        var validStatuses = new[] { OrderStatus.PAID, OrderStatus.SHIPPING, OrderStatus.DELIVERING, OrderStatus.DELIVERED };
        var totalOrders = await _context.Orders.CountAsync();
        var totalUsers = await _context.Users.CountAsync();
        var activeShops = await _context.Shops.CountAsync(s => s.Condition == ShopCondition.OPEN);
        var totalRevenue = await _context.Orders
            .Where(o => validStatuses.Contains(o.OrderStatus))
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;
        var disputesCount = await _context.ReturnRequests.CountAsync(rr => rr.Status == ReturnRequestStatus.PENDING);

        return new DashboardStatisticsResponse
        {
            TotalOrders = totalOrders,
            TotalUsers = totalUsers,
            ActiveShops = activeShops,
            TotalRevenue = totalRevenue,
            DisputesCount = disputesCount
        };
    }

    public async Task<RevenueReportResponse> GetRevenueReportAsync(string period = "month")
    {
        var validStatuses = new[] { OrderStatus.PAID, OrderStatus.SHIPPING, OrderStatus.DELIVERING, OrderStatus.DELIVERED };
        var orders = await _context.Orders
            .AsNoTracking()
            .Where(o => validStatuses.Contains(o.OrderStatus))
            .ToListAsync();
        var totalRevenue = orders.Sum(o => o.TotalAmount);
        var avgOrderValue = orders.Count > 0 ? totalRevenue / orders.Count : 0;

        return new RevenueReportResponse
        {
            Period = period,
            TotalRevenue = totalRevenue,
            AvgOrderValue = avgOrderValue,
            OrderCount = orders.Count
        };
    }

    public async Task<IEnumerable<TopSellingBooksResponse>> GetTopSellingBooksAsync(int limit = 10)
    {
        var topBooks = await _context.OrderDetails
            .AsNoTracking()
            .GroupBy(od => od.BookId)
            .Select(g => new
            {
                BookId = g.Key,
                SoldCount = g.Sum(od => od.Quantity),
                TotalRevenue = g.Sum(od => od.UnitPrice * od.Quantity)
            })
            .OrderByDescending(x => x.SoldCount)
            .Take(limit)
            .ToListAsync();

        var bookIds = topBooks.Select(tb => tb.BookId).ToList();
        var books = await _context.Books
            .AsNoTracking()
            .Where(b => bookIds.Contains(b.Id))
            .ToListAsync();

        return topBooks.Select(tb => new TopSellingBooksResponse
        {
            Id = tb.BookId,
            Title = books.FirstOrDefault(b => b.Id == tb.BookId)?.Title ?? "Unknown",
            SoldCount = tb.SoldCount,
            TotalRevenue = tb.TotalRevenue
        });
    }

    // ===== DELIVERY MONITORING =====
    public async Task<PagedResult<DeliveryResponse>> GetDeliveriesAsync(string? status, int page = 1, int pageSize = 10)
    {
        var query = _context.Deliveries.AsNoTracking();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(d => d.Status == (DeliveryStatus)Enum.Parse(typeof(DeliveryStatus), status));

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(d => d.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<DeliveryResponse>
        {
            Items = items.Select(MapToDeliveryResponse),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<DeliveryResponse> GetDeliveryDetailAsync(Guid deliveryId)
    {
        var delivery = await _context.Deliveries.AsNoTracking().FirstOrDefaultAsync(d => d.Id == deliveryId);
        if (delivery == null)
            throw new Exception("Delivery not found");

        return MapToDeliveryResponse(delivery);
    }

    private static UserResponse MapToUserResponse(BookManagement.Repository.Entities.User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        FullName = user.FullName,
        Phone = user.Phone,
        Address = user.Address,
        Role = user.Role.ToString(),
        Status = user.Status.ToString(),
        ShopId = user.Shop?.Id,
        ShopName = user.Shop?.ShopName,
        ShopStatus = user.Shop?.Condition.ToString(),
        CreatedAt = user.CreatedAt
    };

    private static DisputeResponse MapToDisputeResponse(BookManagement.Repository.Entities.ReturnRequest rr) => new()
    {
        ReturnRequestId = rr.Id,
        OrderDetailId = rr.OrderDetailId,
        OrderId = rr.OrderDetail.OrderId,
        CustomerName = rr.OrderDetail?.Order?.User?.FullName ?? "Customer",
        ShopName = rr.OrderDetail?.Book?.Shop?.ShopName ?? "Shop",
        ReasonType = rr.ReasonType.ToString(),
        DetailedReason = rr.DetailedReason ?? string.Empty,
        EvidenceImageUrl = rr.ImageUrl,
        Status = rr.Status.ToString(),
        RefundAmount = rr.RefundAmount,
        CreatedAt = rr.CreatedAt
    };

    private static OrderResponse MapToOrderResponse(BookManagement.Repository.Entities.Order order) => new()
    {
        Id = order.Id,
        UserId = order.UserId,
        TotalAmount = order.TotalAmount,
        Status = order.OrderStatus.ToString(),
        ShippingAddress = order.ShippingAddress,
        CreatedAt = order.CreatedAt
    };

    private static BookResponse MapToBookResponse(BookManagement.Repository.Entities.Book book) => new()
    {
        Id = book.Id,
        Title = book.Title,
        Author = book.Author,
        Price = book.Price,
        ImageUrl = book.ImageUrl,
        Status = book.Status.ToString()
    };

    private static ShopResponse MapToShopResponse(BookManagement.Repository.Entities.Shop shop) => new()
    {
        Id = shop.Id,
        UserId = shop.UserId,
        ShopName = shop.ShopName,
        Status = shop.Condition.ToString(),
        Rating = (decimal)shop.Rating
    };

    private static DeliveryResponse MapToDeliveryResponse(BookManagement.Repository.Entities.Delivery delivery) => new()
    {
        Id = delivery.Id,
        OrderId = delivery.OrderId,
        TrackingNumber = delivery.TrackingNumber ?? string.Empty,
        Status = delivery.Status.ToString(),
        EstimatedDelivery = delivery.EstimatedDelivery,
        ActualDeliveredAt = delivery.ActualDeliveredAt
    };

    // ===== RESPONSE MODERATION =====
    public async Task<IEnumerable<ReportedResponseDto>> GetReportedResponsesAsync()
    {
        var notifications = await _context.Notifications
            .AsNoTracking()
            .Include(n => n.User)
            .Where(n => n.Type == NotificationType.SYSTEM && n.Content != null && n.Content.Contains("User reported shop response"))
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        var result = new List<ReportedResponseDto>();

        foreach (var n in notifications)
        {
            var content = n.Content ?? "";
            // Parse responseId from "User reported shop response {responseId}: {reason}"
            var parts = content.Split(new[] { "User reported shop response ", ":" }, StringSplitOptions.RemoveEmptyEntries);
            Guid responseId = Guid.Empty;
            string reason = content;

            if (parts.Length >= 2 && Guid.TryParse(parts[0].Trim(), out var parsedId))
            {
                responseId = parsedId;
                reason = string.Join(":", parts.Skip(1)).Trim();
            }

            var responseObj = responseId != Guid.Empty
                ? await _context.Responses
                    .AsNoTracking()
                    .Include(r => r.Shop)
                    .Include(r => r.Feedback)
                    .FirstOrDefaultAsync(r => r.Id == responseId)
                : null;

            result.Add(new ReportedResponseDto
            {
                NotificationId = n.Id,
                ResponseId = responseId != Guid.Empty ? responseId : null,
                CustomerUsername = n.User?.Username ?? n.UserId.ToString(),
                ShopName = responseObj?.Shop?.ShopName,
                FeedbackContent = responseObj?.Feedback?.Content,
                ResponseContent = responseObj?.Content,
                ReportReason = reason,
                CreatedAt = n.CreatedAt
            });
        }

        return result;
    }

    public async Task ModerateShopResponseAsync(Guid responseId, bool isDelete, string? adminNote)
    {
        var response = await _context.Responses
            .Include(r => r.Shop)
            .Include(r => r.Feedback)
                .ThenInclude(f => f.OrderDetail)
                    .ThenInclude(od => od.Order)
            .FirstOrDefaultAsync(r => r.Id == responseId);

        if (response == null)
        {
            throw new KeyNotFoundException("Không tìm thấy phản hồi của Shop.");
        }

        if (isDelete)
        {
            _context.Responses.Remove(response);

            // 1. Tự động tính 1 vi phạm cho Shop và gửi thông báo theo 3 nấc (1/3, 2/3, 3/3 khóa 1 tháng)
            if (response.Shop != null)
            {
                await HandleShopViolationAsync(response.Shop, "Admin xóa phản hồi do vi phạm tiêu chuẩn cộng đồng");
            }

            // 2. Gửi thông báo SYSTEM cho Khách hàng đã viết Đánh giá ban đầu
            var customerUserId = response.Feedback?.OrderDetail?.Order?.UserId;
            if (customerUserId.HasValue && customerUserId.Value != Guid.Empty)
            {
                var customerNotification = new BookManagement.Repository.Entities.Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = customerUserId.Value,
                    Type = NotificationType.SYSTEM,
                    ReferenceId = response.FeedbackId,
                    Content = $"Ban quản trị (Admin) đã xử lý gỡ bỏ phản hồi của Cửa hàng trên bài đánh giá của bạn do vi phạm tiêu chuẩn cộng đồng.",
                    IsRead = false,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                await _context.Notifications.AddAsync(customerNotification);
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task HandleShopViolationAsync(BookManagement.Repository.Entities.Shop shop, string violationReason)
    {
        if (shop == null || shop.UserId == Guid.Empty) return;

        shop.ViolationCount += 1;
        shop.UpdatedAt = DateTimeOffset.UtcNow;

        if (shop.ViolationCount == 1)
        {
            var warning1 = new BookManagement.Repository.Entities.Notification
            {
                Id = Guid.NewGuid(),
                UserId = shop.UserId,
                Type = NotificationType.SYSTEM,
                ReferenceId = shop.Id,
                Content = $"CẢNH BÁO VI PHẠM (1/3): Cửa hàng '{shop.ShopName}' của bạn vừa ghi nhận 1 lần vi phạm ({violationReason}). Nếu tái phạm đủ 3 lần, Cửa hàng sẽ bị hệ thống tự động tạm khóa 1 tháng!",
                IsRead = false,
                CreatedAt = DateTimeOffset.UtcNow
            };
            await _context.Notifications.AddAsync(warning1);
        }
        else if (shop.ViolationCount == 2)
        {
            var warning2 = new BookManagement.Repository.Entities.Notification
            {
                Id = Guid.NewGuid(),
                UserId = shop.UserId,
                Type = NotificationType.SYSTEM,
                ReferenceId = shop.Id,
                Content = $"CẢNH BÁO VI PHẠM NGHIÊM TRỌNG (2/3): Cửa hàng '{shop.ShopName}' của bạn đã ghi nhận 2 lần vi phạm ({violationReason}). Thêm 1 lần vi phạm nữa, Cửa hàng sẽ bị hệ thống tự động tạm khóa 1 tháng!",
                IsRead = false,
                CreatedAt = DateTimeOffset.UtcNow
            };
            await _context.Notifications.AddAsync(warning2);
        }
        else if (shop.ViolationCount >= 3)
        {
            shop.Condition = ShopCondition.CLOSED;
            shop.LockedUntil = DateTimeOffset.UtcNow.AddMonths(1);

            var shopUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == shop.UserId);
            if (shopUser != null)
            {
                shopUser.Status = UserStatus.LOCKED;
                shopUser.UpdatedAt = DateTimeOffset.UtcNow;
                await _sessionRepository.RevokeAllUserSessionsAsync(shop.UserId);
            }

            var lockWarning = new BookManagement.Repository.Entities.Notification
            {
                Id = Guid.NewGuid(),
                UserId = shop.UserId,
                Type = NotificationType.SYSTEM,
                ReferenceId = shop.Id,
                Content = $"THÔNG BÁO TẠM KHÓA CỬA HÀNG (3/3): Cửa hàng '{shop.ShopName}' đã cán mốc 3 lần vi phạm tiêu chuẩn cộng đồng ({violationReason}). Hệ thống đã tự động tạm khóa Cửa hàng 1 tháng (Tạm dừng đến ngày {shop.LockedUntil.Value:dd/MM/yyyy HH:mm}).",
                IsRead = false,
                CreatedAt = DateTimeOffset.UtcNow
            };
            await _context.Notifications.AddAsync(lockWarning);
        }
    }
}
