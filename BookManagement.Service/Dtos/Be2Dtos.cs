using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using BookManagement.Repository.Entities.Enums;

namespace BookManagement.Service.Dtos;

// 1. Shop & Inventory DTOs
public class ShopRegisterDto
{
    [JsonPropertyName("shop_name")]
    public string ShopName { get; set; } = string.Empty;

    [JsonPropertyName("address")]
    public string Address { get; set; } = string.Empty;

    [JsonPropertyName("qr_image_url")]
    public string? QrImageUrl { get; set; }
}

public class ShopProfileDto
{
    [JsonPropertyName("shop_id")]
    public Guid ShopId { get; set; }

    [JsonPropertyName("shop_name")]
    public string ShopName { get; set; } = string.Empty;

    [JsonPropertyName("condition")]
    public string Condition { get; set; } = string.Empty;

    [JsonPropertyName("rating")]
    public float Rating { get; set; }

    [JsonPropertyName("total_books")]
    public int TotalBooks { get; set; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }
}

public class ShopRegisterResponseDto
{
    [JsonPropertyName("shop_id")]
    public Guid ShopId { get; set; }

    [JsonPropertyName("shop_name")]
    public string ShopName { get; set; } = string.Empty;

    [JsonPropertyName("condition")]
    public string Condition { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }
}

public class CreateBookRequestDto
{
    [JsonPropertyName("category_id")]
    public Guid CategoryId { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("isbn")]
    public string Isbn { get; set; } = string.Empty;

    [JsonPropertyName("author")]
    public string Author { get; set; } = string.Empty;

    [JsonPropertyName("publisher")]
    public string Publisher { get; set; } = string.Empty;

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("stock_quantity")]
    public int StockQuantity { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("image_url")]
    public string? ImageUrl { get; set; }

    [JsonPropertyName("published_year")]
    public int PublishedYear { get; set; }
}

public class UpdateBookRequestDto
{
    [JsonPropertyName("category_id")]
    public Guid CategoryId { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("stock_quantity")]
    public int StockQuantity { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("image_url")]
    public string? ImageUrl { get; set; }

    [JsonPropertyName("published_year")]
    public int PublishedYear { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

public class BookQueryDto
{
    public string? Keyword { get; set; }
    public Guid? CategoryId { get; set; }
    public BookStatus? Status { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class PagedResultDto<T>
{
    [JsonPropertyName("total_items")]
    public int TotalItems { get; set; }

    [JsonPropertyName("page_index")]
    public int PageIndex { get; set; }

    [JsonPropertyName("page_size")]
    public int PageSize { get; set; }

    [JsonPropertyName("items")]
    public IEnumerable<T> Items { get; set; } = new List<T>();
}

// Flat book DTO - no circular references
public class BookResponseDto
{
    [JsonPropertyName("book_id")]
    public Guid BookId { get; set; }

    [JsonPropertyName("shop_id")]
    public Guid ShopId { get; set; }

    [JsonPropertyName("category_id")]
    public Guid CategoryId { get; set; }

    [JsonPropertyName("category_name")]
    public string? CategoryName { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("isbn")]
    public string Isbn { get; set; } = string.Empty;

    [JsonPropertyName("author")]
    public string Author { get; set; } = string.Empty;

    [JsonPropertyName("publisher")]
    public string Publisher { get; set; } = string.Empty;

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("stock_quantity")]
    public int StockQuantity { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("image_url")]
    public string? ImageUrl { get; set; }

    [JsonPropertyName("published_year")]
    public int? PublishedYear { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("rating")]
    public float Rating { get; set; }
}

// Flat feedback DTO - no circular references
public class FeedbackResponseDataDto
{
    [JsonPropertyName("response_id")]
    public Guid ResponseId { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("image_url")]
    public string? ImageUrl { get; set; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }
}

public class FeedbackDto
{
    [JsonPropertyName("feedback_id")]
    public Guid FeedbackId { get; set; }

    [JsonPropertyName("order_detail_id")]
    public Guid OrderDetailId { get; set; }

    [JsonPropertyName("shop_id")]
    public Guid ShopId { get; set; }

    [JsonPropertyName("rating")]
    public int Rating { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("image_url")]
    public string? ImageUrl { get; set; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [JsonPropertyName("book_title")]
    public string? BookTitle { get; set; }

    [JsonPropertyName("response")]
    public FeedbackResponseDataDto? Response { get; set; }
}

public class ResponseCreatedDto
{
    [JsonPropertyName("response_id")]
    public Guid ResponseId { get; set; }

    [JsonPropertyName("feedback_id")]
    public Guid FeedbackId { get; set; }

    [JsonPropertyName("shop_id")]
    public Guid ShopId { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("image_url")]
    public string? ImageUrl { get; set; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }
}

// 2. Shop Orders, Revenue & Feedback DTOs
public class ShopOrderItemDto
{
    [JsonPropertyName("book_id")]
    public Guid BookId { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("unit_price")]
    public decimal UnitPrice { get; set; }

    [JsonPropertyName("return_status")]
    public string ReturnStatus { get; set; } = string.Empty;
}

public class ShopOrderDetailDto
{
    [JsonPropertyName("order_id")]
    public Guid OrderId { get; set; }

    [JsonPropertyName("order_status")]
    public string OrderStatus { get; set; } = string.Empty;

    [JsonPropertyName("shipping_address")]
    public string ShippingAddress { get; set; } = string.Empty;

    [JsonPropertyName("weight")]
    public decimal Weight { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("items")]
    public List<ShopOrderItemDto> Items { get; set; } = new();
}

public class UpdateOrderStatusDto
{
    [JsonPropertyName("order_status")]
    public string? OrderStatus { get; set; }

    [JsonPropertyName("new_status")]
    public string? NewStatus { get; set; }

    [JsonPropertyName("weight")]
    public decimal? Weight { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}

public class RevenueDetailDto
{
    [JsonPropertyName("period")]
    public string Period { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("order_count")]
    public int OrderCount { get; set; }
}

public class RevenueResponseDto
{
    [JsonPropertyName("total_revenue")]
    public decimal TotalRevenue { get; set; }

    [JsonPropertyName("total_orders_completed")]
    public int TotalOrdersCompleted { get; set; }

    [JsonPropertyName("details")]
    public List<RevenueDetailDto> Details { get; set; } = new();
}

public class FeedbackResponseRequestDto
{
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("image_url")]
    public string? ImageUrl { get; set; }
}

public class ProcessReturnRequestDto
{
    [JsonPropertyName("is_approved")]
    public bool? IsApproved { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("admin_note")]
    public string? AdminNote { get; set; }

    [JsonPropertyName("rejection_reason")]
    public string? RejectionReason { get; set; }
}

// 3. Delivery & Shipper DTOs
public class CreateDeliveryDto
{
    [JsonPropertyName("order_id")]
    public Guid OrderId { get; set; }

    [JsonPropertyName("tracking_number")]
    public string TrackingNumber { get; set; } = string.Empty;

    [JsonPropertyName("carrier_name")]
    public string CarrierName { get; set; } = string.Empty;

    [JsonPropertyName("shipfee")]
    public decimal ShipFee { get; set; }

    [JsonPropertyName("estimated_delivery")]
    public DateTime? EstimatedDelivery { get; set; }
}

public class UpdateDeliveryDto
{
    [JsonPropertyName("tracking_number")]
    public string TrackingNumber { get; set; } = string.Empty;

    [JsonPropertyName("carrier_name")]
    public string CarrierName { get; set; } = string.Empty;

    [JsonPropertyName("shipfee")]
    public decimal ShipFee { get; set; }

    [JsonPropertyName("estimated_delivery")]
    public DateTime? EstimatedDelivery { get; set; }
}

public class DeliveryManifestDetailDto
{
    [JsonPropertyName("delivery_id")]
    public Guid DeliveryId { get; set; }

    [JsonPropertyName("order_id")]
    public Guid OrderId { get; set; }

    [JsonPropertyName("tracking_number")]
    public string TrackingNumber { get; set; } = string.Empty;

    [JsonPropertyName("carrier_name")]
    public string CarrierName { get; set; } = string.Empty;

    [JsonPropertyName("ship_fee")]
    public decimal ShipFee { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("recipient_name")]
    public string RecipientName { get; set; } = string.Empty;

    [JsonPropertyName("recipient_phone")]
    public string RecipientPhone { get; set; } = string.Empty;

    [JsonPropertyName("recipient_address")]
    public string RecipientAddress { get; set; } = string.Empty;

    [JsonPropertyName("weight")]
    public decimal Weight { get; set; }

    [JsonPropertyName("cod_amount")]
    public decimal CodAmount { get; set; }

    [JsonPropertyName("items")]
    public List<ShopOrderItemDto> Items { get; set; } = new();
}

public class UpdateDeliveryStatusDto
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("failed_reason")]
    public string? FailedReason { get; set; }
}

// 4. Payment Gateway Integration DTOs
public class CreateVnpayUrlDto
{
    [JsonPropertyName("order_id")]
    public Guid OrderId { get; set; }

    [JsonPropertyName("order_info")]
    public string? OrderInfo { get; set; }

    [JsonPropertyName("bank_code")]
    public string? BankCode { get; set; }
}

public class VnpayRefundDto
{
    [JsonPropertyName("order_id")]
    public Guid OrderId { get; set; }

    [JsonPropertyName("return_request_id")]
    public Guid? ReturnRequestId { get; set; }

    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }

    [JsonPropertyName("transaction_no")]
    public string? TransactionNo { get; set; }

    [JsonPropertyName("transaction_date")]
    public string? TransactionDate { get; set; }

    [JsonPropertyName("refund_reason")]
    public string? RefundReason { get; set; }
}

// 5. GHN Shipping Integration DTOs
public class CreateGhnOrderDto
{
    [JsonPropertyName("order_id")]
    public Guid OrderId { get; set; }

    [JsonPropertyName("required_note")]
    public string? RequiredNote { get; set; }
}

public class GhnWebhookPayload
{
    [JsonPropertyName("OrderCode")]
    public string OrderCode { get; set; } = string.Empty;

    [JsonPropertyName("Status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("Time")]
    public DateTime? Time { get; set; }

    [JsonPropertyName("Description")]
    public string? Description { get; set; }
}

// 6. Realtime Communication DTOs
public class ChatThreadDto
{
    [JsonPropertyName("chat_id")]
    public Guid ChatId { get; set; }

    [JsonPropertyName("user_id")]
    public Guid UserId { get; set; }

    [JsonPropertyName("user_name")]
    public string UserName { get; set; } = string.Empty;

    [JsonPropertyName("shop_id")]
    public Guid ShopId { get; set; }

    [JsonPropertyName("last_message")]
    public string? LastMessage { get; set; }

    [JsonPropertyName("unread_count")]
    public int UnreadCount { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }
}

public class MessageDto
{
    [JsonPropertyName("message_id")]
    public Guid MessageId { get; set; }

    [JsonPropertyName("chat_id")]
    public Guid ChatId { get; set; }

    [JsonPropertyName("sender_id")]
    public Guid SenderId { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("image_url")]
    public string? ImageUrl { get; set; }

    [JsonPropertyName("is_read")]
    public bool IsRead { get; set; }

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }
}

public class SendMessageDto
{
    [JsonPropertyName("chat_id")]
    public Guid? ChatId { get; set; }

    [JsonPropertyName("shop_id")]
    public Guid? ShopId { get; set; }

    [JsonPropertyName("user_id")]
    public Guid? UserId { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("image_url")]
    public string? ImageUrl { get; set; }
}

