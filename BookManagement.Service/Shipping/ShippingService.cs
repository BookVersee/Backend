using System;
using System.Linq;
using System.Threading.Tasks;
using BookManagement.Service.Delivery;
using BookManagement.Repository.Data;
using BookManagement.Repository.Entities;
using DeliveryEntity = BookManagement.Repository.Entities.Delivery;
using BookManagement.Repository.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace BookManagement.Service.Shipping;

/// Vị trí: Domain Service - Thực thi logic nghiệp vụ hệ thống, tích hợp đơn vị Giao Hàng Nhanh (GHN) và lưu DbContext.
public class ShippingService : IShippingService
{
    private readonly AppDbContext _db;
    private readonly GhnService _ghnService;

    public ShippingService(AppDbContext db, GhnService ghnService)
    {
        _db = db;
        _ghnService = ghnService;
    }

    /// Chức năng: Tạo vận đơn giao hàng qua API Giao Hàng Nhanh (GHN)
    public async Task<DeliveryEntity> CreateGhnOrderAsync(Guid shopId, CreateGhnOrderDto dto)
    {
        var order = await _db.Orders
            .Include(o => o.User)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Book)
            .FirstOrDefaultAsync(o => o.Id == dto.OrderId);

        if (order == null)
        {
            throw new KeyNotFoundException("Order not found.");
        }

        if (shopId != Guid.Empty && !order.OrderDetails.Any(od => od.Book != null && od.Book.ShopId == shopId))
        {
            throw new UnauthorizedAccessException("Shop does not have permission to create shipping order for this order.");
        }

        if (await _db.Deliveries.AnyAsync(d => d.OrderId == order.Id))
        {
            throw new InvalidOperationException($"Delivery already exists for Order #{order.Id}.");
        }

        var shop = await _db.Shops.FirstOrDefaultAsync(s => s.Id == shopId);
        if (shop == null)
        {
            throw new KeyNotFoundException("Shop not found.");
        }

        var (orderCode, totalFee) = await _ghnService.CreateShippingOrderAsync(shop, order);

        var delivery = new DeliveryEntity
        {
            OrderId = order.Id,
            TrackingNumber = orderCode,
            CarrierName = "GHN",
            ShipFee = totalFee,
            Status = DeliveryStatus.PENDING,
            EstimatedDelivery = DateTime.UtcNow.AddDays(3)
        };

        _db.Deliveries.Add(delivery);
        order.OrderStatus = OrderStatus.SHIPPING;
        order.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();
        return delivery;
    }

    /// Chức năng: Xử lý Webhook tự động cập nhật trạng thái vận đơn từ GHN
    public async Task ProcessGhnWebhookAsync(GhnWebhookPayload payload)
    {
        if (string.IsNullOrEmpty(payload.OrderCode)) return;

        var delivery = await _db.Deliveries
            .Include(d => d.Order)
                .ThenInclude(o => o.Payments)
            .FirstOrDefaultAsync(d => d.TrackingNumber == payload.OrderCode);

        if (delivery == null) return;

        var statusKey = payload.Status?.ToLowerInvariant();
        var order = delivery.Order;

        switch (statusKey)
        {
            case "picking":
            case "storing":
                delivery.Status = DeliveryStatus.PENDING;
                break;
            case "delivering":
                delivery.Status = DeliveryStatus.TRANSIT;
                if (order != null) order.OrderStatus = OrderStatus.DELIVERING;
                break;
            case "delivered":
                delivery.Status = DeliveryStatus.DELIVERED;
                delivery.ActualDeliveredAt = payload.Time ?? DateTime.UtcNow;
                if (order != null)
                {
                    order.OrderStatus = OrderStatus.DELIVERED;

                    var codPayment = order.Payments.FirstOrDefault(p => p.Method == PaymentMethod.COD && p.Status == PaymentStatus.PENDING);
                    if (codPayment != null)
                    {
                        codPayment.Status = PaymentStatus.SUCCESS;
                        codPayment.UpdatedAt = DateTimeOffset.UtcNow;

                        var codTransaction = new TransactionHistory
                        {
                            UserId = order.UserId,
                            ReferenceType = ReferenceType.ORDER_PAYMENT,
                            ReferenceId = order.Id,
                            TransactionType = TransactionType.IN,
                            Amount = codPayment.Amount,
                            TransactionCode = $"GHN_{delivery.TrackingNumber}_{DateTime.UtcNow.Ticks.ToString().Substring(0, 6)}",
                            Description = $"GHN COD Cash collection for Order #{order.Id}",
                            CreatedAt = DateTimeOffset.UtcNow
                        };

                        _db.TransactionHistories.Add(codTransaction);
                    }
                }
                break;
            case "return":
                delivery.Status = DeliveryStatus.RETURNED;
                if (order != null)
                {
                    if (order.OrderStatus != OrderStatus.CANCELLED)
                    {
                        order.OrderStatus = OrderStatus.CANCELLED;

                        var fullOrder = await _db.Orders
                            .Include(o => o.OrderDetails)
                                .ThenInclude(od => od.Book)
                            .FirstOrDefaultAsync(o => o.Id == order.Id);

                        if (fullOrder != null)
                        {
                            foreach (var detail in fullOrder.OrderDetails)
                            {
                                if (detail.Book != null)
                                {
                                    detail.Book.StockQuantity += detail.Quantity;
                                    if (detail.Book.Status == BookStatus.EMPTY && detail.Book.StockQuantity > 0)
                                    {
                                        detail.Book.Status = BookStatus.ACTIVE;
                                    }
                                }
                            }
                        }
                    }
                }
                break;
        }

        if (order != null) order.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
    }
}
