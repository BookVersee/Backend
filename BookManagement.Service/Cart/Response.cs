using System;
using System.Collections.Generic;

namespace BookManagement.Service.Cart
{
    public class CartItemResponse
    {
        public Guid CartDetailId { get; set; }
        public Guid BookId { get; set; }
        public string BookTitle { get; set; } = null!;
        public string? BookImage { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice => UnitPrice * Quantity;
    }

    public class ShopGroupResponse
    {
        public Guid ShopId { get; set; }
        public string ShopName { get; set; } = null!;
        public List<CartItemResponse> Items { get; set; } = new();
        public decimal ShopSubtotal { get; set; }
    }

    public class CartResponse
    {
        public Guid CartId { get; set; }
        public Guid UserId { get; set; }
        public List<ShopGroupResponse> ShopGroups { get; set; } = new();
        public decimal GrandTotal { get; set; }
    }
}
