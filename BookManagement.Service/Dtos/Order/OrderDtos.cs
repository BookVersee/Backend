using System;
using BookManagement.Repository.Entities.Enums;

namespace BookManagement.Service.Dtos.Order
{
    public class OrderDetailDto
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public Guid BookId { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string? BookImageUrl { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public ReturnStatus ReturnStatus { get; set; }
    }

    public class OrderDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public decimal TotalAmount { get; set; }
        public OrderStatus OrderStatus { get; set; }
        public string ShippingAddress { get; set; } = null!;
        public decimal? Weight { get; set; }
        public string? Note { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public List<OrderDetailDto> OrderDetails { get; set; } = new();
    }

    public class UpdateOrderStatusDto
    {
        public OrderStatus Status { get; set; }
        public string? Note { get; set; }
    }

    public class OrderFilterDto
    {
        public OrderStatus? Status { get; set; }
        public Guid? ShopId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
