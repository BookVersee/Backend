using System;
using System.Linq;
using System.Threading.Tasks;
using BookManagement.Service.Dtos;
using BookStore.BE2.Domain.Entities;
using BookStore.BE2.Domain.Enums;
using BookStore.BE2.Infrastructure.Persistence;
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
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.OrderId == dto.OrderId);
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
            EstimatedDelivery = dto.EstimatedDelivery
        };

        _db.Deliveries.Add(delivery);
        order.OrderStatus = OrderStatus.SHIPPING;
        order.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return delivery;
    }

    public async Task<Delivery> UpdateDeliveryAsync(int deliveryId, UpdateDeliveryDto dto)
    {
        var delivery = await _db.Deliveries.FirstOrDefaultAsync(d => d.DeliveryId == deliveryId);
        if (delivery == null)
        {
            throw new KeyNotFoundException("Delivery record not found.");
        }

        if (delivery.Status == DeliveryStatus.DELIVERED)
        {
            throw new InvalidOperationException("Cannot update delivery information after package is delivered.");
        }

        delivery.TrackingNumber = dto.TrackingNumber;
        delivery.CarrierName = dto.CarrierName;
        delivery.ShipFee = dto.ShipFee;
        delivery.EstimatedDelivery = dto.EstimatedDelivery;

        await _db.SaveChangesAsync();
        return delivery;
    }

    public async Task<PagedResultDto<Delivery>> GetDeliveryOrdersAsync(DeliveryStatus? status, int pageIndex, int pageSize)
    {
        var q = _db.Deliveries
            .Include(d => d.Order)
                .ThenInclude(o => o.User)
            .AsQueryable();

        if (status.HasValue)
        {
            q = q.Where(d => d.Status == status.Value);
        }

        var totalItems = await q.CountAsync();
        var items = await q
            .OrderByDescending(d => d.DeliveryId)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResultDto<Delivery>
        {
            TotalItems = totalItems,
            PageIndex = pageIndex,
            PageSize = pageSize,
            Items = items
        };
    }

    public async Task<DeliveryManifestDetailDto> GetDeliveryDetailAsync(int deliveryId)
    {
        var delivery = await _db.Deliveries
            .Include(d => d.Order)
                .ThenInclude(o => o.User)
            .Include(d => d.Order)
                .ThenInclude(o => o.OrderDetails)
                    .ThenInclude(od => od.Book)
            .Include(d => d.Order)
                .ThenInclude(o => o.Payments)
            .FirstOrDefaultAsync(d => d.DeliveryId == deliveryId);

        if (delivery == null)
        {
            throw new KeyNotFoundException("Delivery record not found.");
        }

        var order = delivery.Order;
        var codPayment = order.Payments.FirstOrDefault(p => p.Method == PaymentMethod.COD && p.Status == PaymentStatus.PENDING);
        decimal codAmount = codPayment != null ? codPayment.Amount : 0m;

        var items = order.OrderDetails.Select(od => new ShopOrderItemDto
        {
            BookId = od.BookId,
            Title = od.Book?.Title ?? string.Empty,
            Quantity = od.Quantity,
            UnitPrice = od.UnitPrice,
            ReturnStatus = od.ReturnStatus.ToString()
        }).ToList();

        return new DeliveryManifestDetailDto
        {
            DeliveryId = delivery.DeliveryId,
            OrderId = delivery.OrderId,
            TrackingNumber = delivery.TrackingNumber,
            CarrierName = delivery.CarrierName,
            ShipFee = delivery.ShipFee,
            Status = delivery.Status.ToString(),
            RecipientName = order.User?.FullName ?? string.Empty,
            RecipientPhone = order.User?.Phone ?? string.Empty,
            RecipientAddress = order.ShippingAddress,
            Weight = order.Weight,
            CodAmount = codAmount,
            Items = items
        };
    }

    public async Task UpdateDeliveryStatusAsync(int deliveryId, UpdateDeliveryStatusDto dto)
    {
        var delivery = await _db.Deliveries
            .Include(d => d.Order)
                .ThenInclude(o => o.Payments)
            .FirstOrDefaultAsync(d => d.DeliveryId == deliveryId);

        if (delivery == null)
        {
            throw new KeyNotFoundException("Delivery record not found.");
        }

        var order = delivery.Order;

        if (dto.Status.Equals("TRANSIT", StringComparison.OrdinalIgnoreCase))
        {
            delivery.Status = DeliveryStatus.TRANSIT;
            order.OrderStatus = OrderStatus.DELIVERING;
        }
        else if (dto.Status.Equals("DELIVERED", StringComparison.OrdinalIgnoreCase))
        {
            delivery.Status = DeliveryStatus.DELIVERED;
            delivery.ActualDeliveredAt = DateTime.UtcNow;
            order.OrderStatus = OrderStatus.DELIVERED;

            var codPayment = order.Payments.FirstOrDefault(p => p.Method == PaymentMethod.COD && p.Status == PaymentStatus.PENDING);
            if (codPayment != null)
            {
                codPayment.Status = PaymentStatus.SUCCESS;
                codPayment.UpdatedAt = DateTime.UtcNow;
            }
        }
        else if (dto.Status.Equals("RETURNED", StringComparison.OrdinalIgnoreCase))
        {
            delivery.Status = DeliveryStatus.RETURNED;
            order.OrderStatus = OrderStatus.APPROVED;
        }
        else if (dto.Status.Equals("FAILED", StringComparison.OrdinalIgnoreCase))
        {
            order.OrderStatus = OrderStatus.FAILED;
        }
        else
        {
            throw new ArgumentException($"Unsupported delivery status: {dto.Status}");
        }

        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}
