using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookManagement.Service.Dtos;
using BookManagement.Repository.Data;
using BookManagement.Repository.Entities;
using BookManagement.Repository.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace BookManagement.Service.Services;

public class DeliveryService
{
    private readonly AppDbContext _db;

    public DeliveryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Delivery> CreateDeliveryAsync(CreateDeliveryDto dto)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == dto.OrderId);
        if (order == null)
        {
            throw new KeyNotFoundException("Order not found.");
        }

        var delivery = new Delivery
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
                order.OrderStatus = OrderStatus.DELIVERED;

                var codPayment = order.Payments.FirstOrDefault(p => p.Method == PaymentMethod.COD && p.Status == PaymentStatus.PENDING);
                if (codPayment != null)
                {
                    codPayment.Status = PaymentStatus.SUCCESS;
                    codPayment.UpdatedAt = DateTimeOffset.UtcNow;
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
}
