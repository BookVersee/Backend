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
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUserSessionRepository _sessionRepository;
    private readonly IBookRepository _bookRepository;
    private readonly IShopRepository _shopRepository;
    private readonly IDeliveryRepository _deliveryRepository;

    public AdminService(
        AppDbContext context,
        IUserRepository userRepository,
        IOrderRepository orderRepository,
        ITransactionRepository transactionRepository,
        IUserSessionRepository sessionRepository,
        IBookRepository bookRepository,
        IShopRepository shopRepository,
        IDeliveryRepository deliveryRepository)
    {
        _context = context;
        _userRepository = userRepository;
        _orderRepository = orderRepository;
        _transactionRepository = transactionRepository;
        _sessionRepository = sessionRepository;
        _bookRepository = bookRepository;
        _shopRepository = shopRepository;
        _deliveryRepository = deliveryRepository;
    }

    // ===== USER MANAGEMENT =====
    public async Task<PagedResult<UserResponse>> GetUsersAsync(UserFilterRequest filter)
    {
        var query = _context.Users.AsNoTracking();

        if (filter.Role.HasValue)
            query = query.Where(u => u.Role == filter.Role.Value);

        if (filter.Status.HasValue)
            query = query.Where(u => u.Status == filter.Status.Value);

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var kw = filter.Keyword.Trim().ToLower();
            query = query.Where(u => u.Username.ToLower().Contains(kw) ||
                                     u.Email.ToLower().Contains(kw) ||
                                     (u.FullName != null && u.FullName.ToLower().Contains(kw)));
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

    public async Task<UserDetailResponse> GetUserDetailAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new Exception("User not found");

        var orders = await _orderRepository.GetOrdersByUserIdAsync(userId);
        var transactions = await _transactionRepository.GetTransactionsByUserIdAsync(userId);

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
                TransactionCode = t.TransactionCode,
                Description = t.Description,
                CreatedAt = t.CreatedAt
            }).ToList()
        };
    }

    public async Task UpdateUserStatusAsync(Guid userId, string status)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new Exception("User not found");

        user.Status = (UserStatus)Enum.Parse(typeof(UserStatus), status);
        await _userRepository.UpdateAsync(user);

        if (user.Status == UserStatus.LOCKED)
            await _sessionRepository.RevokeAllUserSessionsAsync(userId);
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
        var dispute = await _context.ReturnRequests.FindAsync(disputeId);
        if (dispute == null)
            throw new Exception("Dispute not found");

        dispute.Status = request.ApproveRefund ? ReturnRequestStatus.APPROVED : ReturnRequestStatus.REJECTED;
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
            .OrderByDescending(b => b.CreatedAt)
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
            .OrderByDescending(b => b.CreatedAt)
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
        var shop = await _shopRepository.GetShopByIdAsync(shopId);
        if (shop == null)
            throw new Exception("Shop not found");

        shop.Condition = ShopCondition.ACTIVE;
        await _shopRepository.UpdateAsync(shop);
    }

    public async Task LockShopAsync(Guid shopId, LockShopRequest request)
    {
        var shop = await _shopRepository.GetShopByIdAsync(shopId);
        if (shop == null)
            throw new Exception("Shop not found");

        shop.Condition = ShopCondition.LOCKED;
        await _shopRepository.UpdateAsync(shop);
    }

    // ===== DASHBOARD & STATISTICS =====
    public async Task<DashboardStatisticsResponse> GetDashboardStatisticsAsync(string period = "month")
    {
        var totalOrders = await _context.Orders.CountAsync();
        var totalUsers = await _context.Users.CountAsync();
        var activeShops = await _context.Shops.CountAsync(s => s.Condition == ShopCondition.ACTIVE);
        var totalRevenue = await _context.Orders.SumAsync(o => o.TotalAmount);
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
        var orders = await _context.Orders.AsNoTracking().ToListAsync();
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
            .OrderByDescending(d => d.CreatedAt)
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
        var delivery = await _deliveryRepository.GetByIdAsync(deliveryId);
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
        DetailedReason = rr.DetailedReason,
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
        TrackingNumber = delivery.TrackingNumber,
        Status = delivery.Status.ToString(),
        EstimatedDelivery = delivery.EstimatedDelivery,
        ActualDeliveredAt = delivery.ActualDeliveredAt
    };
}
