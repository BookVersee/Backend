using System;
using System.Linq;
using System.Threading.Tasks;
using BookStore.BE2.Domain.Enums;
using BookStore.BE2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BookManagement.Service.Order;

public class OrderService : IOrderService
{
    private readonly AppDbContext _db;

    public OrderService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<OrderDetailResponse> GetShopOrderDetailAsync(int shopId, int orderId)
    {
        var order = await _db.Orders
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Book)
            .FirstOrDefaultAsync(o => o.OrderId == orderId
                && o.OrderDetails.Any(od => od.Book.ShopId == shopId));

        if (order == null)
            throw new KeyNotFoundException("Order not found for shop.");

        var shopItems = order.OrderDetails
            .Where(od => od.Book.ShopId == shopId)
            .Select(od => new OrderItemResponse
            {
                BookId = od.BookId,
                Title = od.Book.Title,
                Quantity = od.Quantity,
                UnitPrice = od.UnitPrice,
                ReturnStatus = od.ReturnStatus.ToString()
            }).ToList();

        return new OrderDetailResponse
        {
            OrderId = order.OrderId,
            OrderStatus = order.OrderStatus.ToString(),
            ShippingAddress = order.ShippingAddress,
            Weight = order.Weight,
            Note = order.Note,
            Items = shopItems
        };
    }

    public async Task UpdateOrderStatusAsync(int shopId, int orderId, UpdateOrderStatusRequest dto)
    {
        using var tx = await _db.Database.BeginTransactionAsync();

        var order = await _db.Orders
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Book)
            .FirstOrDefaultAsync(o => o.OrderId == orderId
                && o.OrderDetails.Any(od => od.Book.ShopId == shopId));

        if (order == null)
            throw new KeyNotFoundException("Order not found.");

        if (!Enum.TryParse<OrderStatus>(dto.NewStatus, true, out var targetStatus))
            throw new ArgumentException($"Invalid order status: {dto.NewStatus}");

        if (targetStatus == OrderStatus.CANCELLED)
        {
            foreach (var item in order.OrderDetails)
            {
                var book = item.Book;
                if (book != null)
                {
                    book.StockQuantity += item.Quantity;
                    if (book.Status == BookStatus.EMPTY && book.StockQuantity > 0)
                        book.Status = BookStatus.ACTIVE;
                }
            }
        }

        order.OrderStatus = targetStatus;
        order.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public async Task<RevenueResponse> GetShopRevenueAsync(int shopId, DateTime? fromDate, DateTime? toDate, string? periodType)
    {
        var query = _db.OrderDetails
            .Include(od => od.Order)
            .Where(od => od.Book.ShopId == shopId && od.Order.OrderStatus == OrderStatus.DELIVERED);

        if (fromDate.HasValue)
            query = query.Where(od => od.Order.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(od => od.Order.CreatedAt <= toDate.Value);

        var items = await query.ToListAsync();

        var totalRevenue = items.Sum(i => i.Quantity * i.UnitPrice);
        var totalCompletedOrders = items.Select(i => i.OrderId).Distinct().Count();

        var details = items
            .GroupBy(i => FormatPeriod(i.Order.CreatedAt, periodType))
            .Select(g => new RevenueDetailResponse
            {
                Period = g.Key,
                Amount = g.Sum(x => x.Quantity * x.UnitPrice),
                OrderCount = g.Select(x => x.OrderId).Distinct().Count()
            })
            .OrderBy(d => d.Period)
            .ToList();

        return new RevenueResponse
        {
            TotalRevenue = totalRevenue,
            TotalOrdersCompleted = totalCompletedOrders,
            Details = details
        };
    }

    private static string FormatPeriod(DateTime dt, string? periodType) =>
        (periodType?.ToUpperInvariant()) switch
        {
            "MONTH" => dt.ToString("yyyy-MM"),
            "YEAR" => dt.ToString("yyyy"),
            _ => dt.ToString("yyyy-MM-dd")
        };
}
