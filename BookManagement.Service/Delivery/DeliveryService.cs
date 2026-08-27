using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookManagement.Service.Dtos;
using BookManagement.Repository.Data;
using BookManagement.Repository.Entities;
using BookManagement.Repository.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace BookManagement.Service.Delivery;

public class DeliveryService
{
    private readonly AppDbContext _db;

    public DeliveryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<BookManagement.Repository.Entities.Delivery> CreateDeliveryAsync(CreateDeliveryDto dto)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == dto.OrderId);
        if (order == null)
        {
            throw new KeyNotFoundException("Order not found.");
        }

        // 4. KIỂM TRA CHỐNG TẠO VẬN ĐƠN TRÙNG LẶP (DUPLICATE DELIVERY)
        if (await _db.Deliveries.AnyAsync(d => d.OrderId == dto.OrderId))
        {
            throw new InvalidOperationException($"Delivery record already exists for Order #{dto.OrderId}.");
        }

        var delivery = new BookManagement.Repository.Entities.Delivery
        {
            OrderId = dto.OrderId,
            TrackingNumber = dto.TrackingNumber,
            CarrierName = dto.CarrierName,
            ShipFee = dto.ShipFee,
            Status = DeliveryStatus.PENDING,
            EstimatedDelivery = dto.EstimatedDelivery,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Deliveries.Add(delivery);
        order.OrderStatus = OrderStatus.SHIPPING;
        order.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();
        return delivery;
    }

    public async Task<DeliveryManifestDetailDto> GetDeliveryManifestDetailAsync(Guid deliveryId)
    {
        var delivery = await _db.Deliveries
            .Include(d => d.Order)
                .ThenInclude(o => o.User)
            .Include(d => d.Order)
                .ThenInclude(o => o.OrderDetails)
                    .ThenInclude(od => od.Book)
            .Include(d => d.Order)
                .ThenInclude(o => o.Payments)
            .FirstOrDefaultAsync(d => d.Id == deliveryId);

        if (delivery == null)
        {
            throw new KeyNotFoundException("Delivery manifest not found.");
        }

        var order = delivery.Order;
        var user = order?.User;

        decimal codAmount = 0m;
        var codPayment = order?.Payments.FirstOrDefault(p => p.Method == PaymentMethod.COD && p.Status == PaymentStatus.PENDING);
        if (codPayment != null)
        {
            codAmount = codPayment.Amount;
        }

        var items = order?.OrderDetails.Select(od => new ShopOrderItemDto
        {
            BookId = od.BookId,
            Title = od.Book != null ? od.Book.Title : "Unknown",
            Quantity = od.Quantity,
            UnitPrice = od.UnitPrice,
            ReturnStatus = od.ReturnStatus.ToString()
        }).ToList() ?? new List<ShopOrderItemDto>();

        return new DeliveryManifestDetailDto
        {
            DeliveryId = delivery.Id,
            OrderId = delivery.OrderId,
            TrackingNumber = delivery.TrackingNumber ?? string.Empty,
            CarrierName = delivery.CarrierName ?? string.Empty,
            ShipFee = delivery.ShipFee ?? 0m,
            Status = delivery.Status.ToString(),
            RecipientName = user != null ? (user.FullName ?? user.Username) : "Unknown",
            RecipientPhone = user != null ? (user.Phone ?? string.Empty) : string.Empty,
            RecipientAddress = order != null ? order.ShippingAddress : string.Empty,
            Weight = order != null ? (order.Weight ?? 0m) : 0m,
            CodAmount = codAmount,
            Items = items
        };
    }

    public async Task UpdateDeliveryStatusAsync(Guid deliveryId, UpdateDeliveryStatusDto dto)
    {
        var delivery = await _db.Deliveries
            .Include(d => d.Order)
                .ThenInclude(o => o.Payments)
            .FirstOrDefaultAsync(d => d.Id == deliveryId);

        if (delivery == null)
        {
            throw new KeyNotFoundException("Delivery not found.");
        }

        if (!Enum.TryParse<DeliveryStatus>(dto.Status, true, out var targetStatus))
        {
            throw new ArgumentException($"Invalid delivery status: {dto.Status}");
        }

        delivery.Status = targetStatus;
        delivery.UpdatedAt = DateTimeOffset.UtcNow;

        var order = delivery.Order;
        if (order != null)
        {
            if (targetStatus == DeliveryStatus.DELIVERED)
            {
                delivery.ActualDeliveredAt = DateTime.UtcNow;
                order.OrderStatus = OrderStatus.COMPLETED; // Cập nhật đơn hàng thành COMPLETED khi giao thành công

                // 3. DÒNG TIỀN COD TỰ ĐỘNG THU TIỀN MẶT & GHI TRANSACTION HISTORY
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
                        TransactionCode = $"COD_{delivery.TrackingNumber}_{DateTime.UtcNow.Ticks.ToString().Substring(0, 6)}",
                        Description = $"COD Cash collection for Order #{order.Id}",
                        CreatedAt = DateTimeOffset.UtcNow
                    };

                    _db.TransactionHistories.Add(codTransaction);
                }
            }
            else if (targetStatus == DeliveryStatus.RETURNED)
            {
                order.OrderStatus = OrderStatus.CANCELLED;
            }
            else if (targetStatus == DeliveryStatus.TRANSIT)
            {
                order.OrderStatus = OrderStatus.DELIVERING;
            }

            order.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _db.SaveChangesAsync();
    }

    public async Task<List<DeliveryManifestDetailDto>> GetDeliveryOrdersAsync(string? status)
    {
        var q = _db.Deliveries
            .Include(d => d.Order)
                .ThenInclude(o => o.User)
            .Include(d => d.Order)
                .ThenInclude(o => o.OrderDetails)
                    .ThenInclude(od => od.Book)
            .Include(d => d.Order)
                .ThenInclude(o => o.Payments)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<DeliveryStatus>(status, true, out var filterStatus))
        {
            q = q.Where(d => d.Status == filterStatus);
        }

        var deliveries = await q.OrderByDescending(d => d.CreatedAt).ToListAsync();

        return deliveries.Select(delivery =>
        {
            var order = delivery.Order;
            var user = order?.User;

            decimal codAmount = 0m;
            var codPayment = order?.Payments.FirstOrDefault(p => p.Method == PaymentMethod.COD && p.Status == PaymentStatus.PENDING);
            if (codPayment != null)
            {
                codAmount = codPayment.Amount;
            }

            var items = order?.OrderDetails.Select(od => new ShopOrderItemDto
            {
                BookId = od.BookId,
                Title = od.Book != null ? od.Book.Title : "Unknown",
                Quantity = od.Quantity,
                UnitPrice = od.UnitPrice,
                ReturnStatus = od.ReturnStatus.ToString()
            }).ToList() ?? new List<ShopOrderItemDto>();

            return new DeliveryManifestDetailDto
            {
                DeliveryId = delivery.Id,
                OrderId = delivery.OrderId,
                TrackingNumber = delivery.TrackingNumber ?? string.Empty,
                CarrierName = delivery.CarrierName ?? string.Empty,
                ShipFee = delivery.ShipFee ?? 0m,
                Status = delivery.Status.ToString(),
                RecipientName = user != null ? (user.FullName ?? user.Username) : "Unknown",
                RecipientPhone = user != null ? (user.Phone ?? string.Empty) : string.Empty,
                RecipientAddress = order != null ? order.ShippingAddress : string.Empty,
                Weight = order != null ? (order.Weight ?? 0m) : 0m,
                CodAmount = codAmount,
                Items = items
            };
        }).ToList();
    }
}
