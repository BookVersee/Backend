using System;
using System.Collections.Generic;
using BookManagement.Repository.Entities.Enums;

namespace BookManagement.Service.Shop
{
    public class ShopResponse
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string ShopName { get; set; } = null!;
        public ShopCondition Condition { get; set; }
        public double? Rating { get; set; }
        public string? OwnerName { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class ShopProfileDto
    {
        public Guid ShopId { get; set; }
        public string ShopName { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public float Rating { get; set; }
        public int TotalBooks { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class ShopRegisterResponseDto
    {
        public Guid ShopId { get; set; }
        public string ShopName { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class ShopOrderItemDto
    {
        public Guid BookId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string ReturnStatus { get; set; } = string.Empty;
    }

    public class ShopOrderDetailDto
    {
        public Guid OrderId { get; set; }
        public string OrderStatus { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public decimal Weight { get; set; }
        public string? Note { get; set; }
        public List<ShopOrderItemDto> Items { get; set; } = new();
    }

    public class RevenueDetailDto
    {
        public string Period { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int OrderCount { get; set; }
    }

    public class RevenueResponseDto
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrdersCompleted { get; set; }
        public List<RevenueDetailDto> Details { get; set; } = new();
    }
}
