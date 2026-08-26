using System;
using System.Threading.Tasks;
using BookManagement.Service.Dtos;
using BookManagement.Repository.Data;
using BookManagement.Repository.Entities;
using DeliveryEntity = BookManagement.Repository.Entities.Delivery;
using BookManagement.Repository.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace BookManagement.Service.Shipping;

public class ShippingService
{
    private readonly AppDbContext _db;
    private readonly GhnService _ghnService;

    public ShippingService(AppDbContext db, GhnService ghnService)
    {
        _db = db;
        _ghnService = ghnService;
    }

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
            EstimatedDelivery = DateTime.UtcNow.AddDays(3),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Deliveries.Add(delivery);
        order.OrderStatus = OrderStatus.SHIPPING;
        order.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();
        return delivery;
    }

    public async Task ProcessGhnWebhookAsync(GhnWebhookPayload payload)
    {
        if (string.IsNullOrEmpty(payload.OrderCode)) return;

        var delivery = await _db.Deliveries
            .Include(d => d.Order)
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
                if (order != null) order.OrderStatus = OrderStatus.DELIVERED;
                break;
            case "return":
                delivery.Status = DeliveryStatus.RETURNED;
                if (order != null) order.OrderStatus = OrderStatus.APPROVED;
                break;
        }

        if (order != null) order.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
    }
}

