using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BookManagement.Service.Order;

public class OrderItemResponse
{
    [JsonPropertyName("book_id")]
    public int BookId { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("unit_price")]
    public decimal UnitPrice { get; set; }

    [JsonPropertyName("return_status")]
    public string ReturnStatus { get; set; } = string.Empty;
}

public class OrderDetailResponse
{
    [JsonPropertyName("order_id")]
    public int OrderId { get; set; }

    [JsonPropertyName("order_status")]
    public string OrderStatus { get; set; } = string.Empty;

    [JsonPropertyName("shipping_address")]
    public string ShippingAddress { get; set; } = string.Empty;

    [JsonPropertyName("weight")]
    public decimal Weight { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("items")]
    public List<OrderItemResponse> Items { get; set; } = new();
}

public class RevenueDetailResponse
{
    [JsonPropertyName("period")]
    public string Period { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("order_count")]
    public int OrderCount { get; set; }
}

public class RevenueResponse
{
    [JsonPropertyName("total_revenue")]
    public decimal TotalRevenue { get; set; }

    [JsonPropertyName("total_orders_completed")]
    public int TotalOrdersCompleted { get; set; }

    [JsonPropertyName("details")]
    public List<RevenueDetailResponse> Details { get; set; } = new();
}
