using System;
using System.Linq;
using System.Threading.Tasks;
using BookManagement.Service.Delivery;
using BookManagement.Repository.Data;
using BookManagement.Repository.Entities;
using DeliveryEntity = BookManagement.Repository.Entities.Delivery;
using BookManagement.Repository.Entities.Enums;
using Microsoft.EntityFrameworkCore;

using BookManagement.Service.Order;

namespace BookManagement.Service.Shipping;

/// Vị trí: Domain Service - Thực thi logic nghiệp vụ hệ thống, tích hợp đơn vị Giao Hàng Nhanh (GHN) và lưu DbContext.
public class ShippingService : IShippingService
{
    private readonly AppDbContext _db;
    private readonly GhnService _ghnService;
    private readonly IOrderRealtimeNotifier? _orderNotifier;

    public ShippingService(AppDbContext db, GhnService ghnService, IOrderRealtimeNotifier? orderNotifier = null)
    {
        _db = db;
        _ghnService = ghnService;
        _orderNotifier = orderNotifier;
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

        var shopIds = await _db.Database
            .SqlQueryRaw<Guid>("SELECT Id FROM Shops WHERE UserId = {0} OR Id = {0}", shopId)
            .ToListAsync();
        if (!shopIds.Any() && shopId != Guid.Empty)
        {
            throw new KeyNotFoundException("Shop not found.");
        }

        var resolvedShopId = shopIds.FirstOrDefault();
        if (shopId != Guid.Empty && !order.OrderDetails.Any(od => od.Book != null && (od.Book.ShopId == resolvedShopId || od.Book.ShopId == shopId)))
        {
            throw new UnauthorizedAccessException("Shop does not have permission to create shipping order for this order.");
        }

        if (await _db.Deliveries.AnyAsync(d => d.OrderId == order.Id && d.CarrierName != "GHN_RETURN"))
        {
            throw new InvalidOperationException($"Delivery already exists for Order #{order.Id}.");
        }

        var shop = await _db.Shops.FirstOrDefaultAsync(s => s.Id == resolvedShopId || s.Id == shopId)
                   ?? new BookManagement.Repository.Entities.Shop { Id = resolvedShopId, ShopName = "Shop BookVerse" };

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

        if (_orderNotifier != null)
        {
            try
            {
                await _orderNotifier.SendOrderStatusChangedAsync(
                    order.UserId, 
                    order.Id, 
                    OrderStatus.SHIPPING.ToString(), 
                    $"Đơn hàng #{order.Id} đã được tạo vận đơn GHN ({delivery.TrackingNumber}) và đang chờ lấy hàng.");
            }
            catch
            {
            }
        }

        return delivery;
    }

    /// Chức năng: Tạo vận đơn trả hàng / thu hồi qua API Giao Hàng Nhanh (GHN) khi yêu cầu trả hàng được duyệt
    public async Task<DeliveryEntity> CreateReturnGhnOrderAsync(Guid returnRequestId)
    {
        var returnReq = await _db.ReturnRequests
            .Include(rr => rr.OrderDetail)
                .ThenInclude(od => od.Book)
            .Include(rr => rr.OrderDetail)
                .ThenInclude(od => od.Order)
                    .ThenInclude(o => o.User)
            .FirstOrDefaultAsync(rr => rr.Id == returnRequestId);

        if (returnReq == null)
        {
            throw new KeyNotFoundException("Return request not found.");
        }

        var order = returnReq.OrderDetail?.Order;
        if (order == null)
        {
            throw new InvalidOperationException("Associated order not found for this return request.");
        }

        // Kiểm tra xem đã có vận đơn trả hàng cho order này chưa để tránh tạo trùng
        var existingReturnDelivery = await _db.Deliveries
            .FirstOrDefaultAsync(d => d.OrderId == order.Id && (d.CarrierName == "GHN_RETURN" || (d.TrackingNumber != null && d.TrackingNumber.StartsWith("GHN_RET"))));

        if (existingReturnDelivery != null)
        {
            return existingReturnDelivery;
        }

        var shopId = returnReq.OrderDetail?.Book?.ShopId;
        var shop = shopId.HasValue 
            ? await _db.Shops.FirstOrDefaultAsync(s => s.Id == shopId.Value) 
            : null;

        if (shop == null)
        {
            shop = new BookManagement.Repository.Entities.Shop
            {
                Id = shopId ?? Guid.NewGuid(),
                ShopName = "Shop BookVerse",
                Address = "72 Thành Thái, Phường 14, Quận 10, Hồ Chí Minh",
                Phone = "0901234567"
            };
        }

        var (orderCode, totalFee) = await _ghnService.CreateReturnShippingOrderAsync(shop, order, returnReq);

        var returnDelivery = new DeliveryEntity
        {
            OrderId = order.Id,
            TrackingNumber = orderCode,
            CarrierName = "GHN_RETURN",
            ShipFee = totalFee,
            Status = DeliveryStatus.PENDING,
            EstimatedDelivery = DateTime.UtcNow.AddDays(3)
        };

        _db.Deliveries.Add(returnDelivery);

        if (returnReq.OrderDetail != null)
        {
            returnReq.OrderDetail.ReturnStatus = ReturnStatus.SHIPPED;
        }
        returnReq.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();

        if (_orderNotifier != null)
        {
            try
            {
                await _orderNotifier.SendOrderStatusChangedAsync(
                    order.UserId,
                    order.Id,
                    order.OrderStatus.ToString(),
                    $"Yêu cầu hoàn trả sách '{returnReq.OrderDetail?.Book?.Title}' đã được tạo vận đơn thu hồi GHN ({returnDelivery.TrackingNumber}). Shipper GHN sẽ liên hệ lấy hàng.");
            }
            catch
            {
            }
        }

        return returnDelivery;
    }

    /// Chức năng: Xử lý Webhook tự động cập nhật trạng thái vận đơn từ GHN
    public async Task ProcessGhnWebhookAsync(GhnWebhookPayload payload)
    {
        if (string.IsNullOrEmpty(payload.OrderCode)) return;

        var delivery = await _db.Deliveries
            .Include(d => d.Order)
                .ThenInclude(o => o.Payments)
            .Include(d => d.Order)
                .ThenInclude(o => o.OrderDetails)
                    .ThenInclude(od => od.Book)
            .Include(d => d.Order)
                .ThenInclude(o => o.OrderDetails)
                    .ThenInclude(od => od.ReturnRequest)
            .FirstOrDefaultAsync(d => d.TrackingNumber == payload.OrderCode);

        if (delivery == null) return;

        var isReturnDelivery = delivery.CarrierName == "GHN_RETURN" || (delivery.TrackingNumber != null && delivery.TrackingNumber.StartsWith("GHN_RET"));
        var statusKey = payload.Status?.ToLowerInvariant();
        var order = delivery.Order;

        if (isReturnDelivery)
        {
            switch (statusKey)
            {
                case "picking":
                case "storing":
                    delivery.Status = DeliveryStatus.PENDING;
                    break;
                case "delivering":
                    delivery.Status = DeliveryStatus.TRANSIT;
                    if (order?.OrderDetails != null)
                    {
                        foreach (var od in order.OrderDetails.Where(od => od.ReturnRequest != null && od.ReturnRequest.Status == ReturnRequestStatus.APPROVED))
                        {
                            od.ReturnStatus = ReturnStatus.SHIPPED;
                        }
                    }
                    break;
                case "delivered":
                    delivery.Status = DeliveryStatus.DELIVERED;
                    delivery.ActualDeliveredAt = payload.Time ?? DateTime.UtcNow;
                    if (order?.OrderDetails != null)
                    {
                        foreach (var od in order.OrderDetails.Where(od => od.ReturnRequest != null && od.ReturnRequest.Status == ReturnRequestStatus.APPROVED))
                        {
                            od.ReturnStatus = ReturnStatus.DELIVERED;
                            if (od.Book != null)
                            {
                                od.Book.StockQuantity += od.Quantity;
                                if (od.Book.Status == BookStatus.EMPTY && od.Book.StockQuantity > 0)
                                {
                                    od.Book.Status = BookStatus.ACTIVE;
                                }
                            }
                        }
                    }
                    break;
                case "return":
                case "cancel":
                    delivery.Status = DeliveryStatus.RETURNED;
                    break;
            }

            if (order != null)
            {
                order.UpdatedAt = DateTimeOffset.UtcNow;
                await _db.SaveChangesAsync();

                if (_orderNotifier != null)
                {
                    try
                    {
                        string statusMsg = statusKey switch
                        {
                            "delivering" => $"Đơn hoàn trả (#{delivery.TrackingNumber}) đang được shipper GHN vận chuyển về Shop.",
                            "delivered" => $"Shop đã nhận lại hàng hoàn trả (#{delivery.TrackingNumber}) thành công từ GHN. Hệ thống sẽ tiến hành hoàn tiền cho bạn.",
                            _ => $"Vận đơn hoàn trả #{delivery.TrackingNumber} đã được cập nhật trạng thái: {delivery.Status}."
                        };

                        await _orderNotifier.SendOrderStatusChangedAsync(order.UserId, order.Id, order.OrderStatus.ToString(), statusMsg);
                    }
                    catch
                    {
                    }
                }
            }
            else
            {
                await _db.SaveChangesAsync();
            }
            return;
        }

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

        if (order != null)
        {
            order.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync();

            if (_orderNotifier != null)
            {
                try
                {
                    string statusMsg = statusKey switch
                    {
                        "delivering" => $"Đơn hàng #{order.Id} đang trên đường giao tới bạn.",
                        "delivered" => $"Đơn hàng #{order.Id} đã được giao thành công!",
                        "return" => $"Đơn hàng #{order.Id} đã chuyển trạng thái hoàn trả.",
                        _ => $"Đơn hàng #{order.Id} đã được cập nhật trạng thái vận chuyển: {delivery.Status}."
                    };

                    await _orderNotifier.SendOrderStatusChangedAsync(order.UserId, order.Id, order.OrderStatus.ToString(), statusMsg);
                }
                catch
                {
                }
            }
        }
        else
        {
            await _db.SaveChangesAsync();
        }
    }
}
