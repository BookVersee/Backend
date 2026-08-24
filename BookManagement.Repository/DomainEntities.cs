using System;
using System.Collections.Generic;
using BookStore.BE2.Domain.Enums;

namespace BookStore.BE2.Domain.Entities;

public class User
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public UserStatus Status { get; set; }
    public string? Address { get; set; }
    public string? QrImageUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Shop? Shop { get; set; }
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<TransactionHistory> Transactions { get; set; } = new List<TransactionHistory>();
}

public class Category
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Status { get; set; }
    public ICollection<Book> Books { get; set; } = new List<Book>();
}

public class Shop
{
    public int ShopId { get; set; }
    public int UserId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public ShopCondition Condition { get; set; } = ShopCondition.OPEN;
    public float Rating { get; set; } = 5.0f;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public ICollection<Book> Books { get; set; } = new List<Book>();
    public ICollection<Chat> Chats { get; set; } = new List<Chat>();
    public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
}

public class Book
{
    public int BookId { get; set; }
    public int ShopId { get; set; }
    public int CategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Isbn { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int PublishedYear { get; set; }
    public BookStatus Status { get; set; } = BookStatus.ACTIVE;
    public float Rating { get; set; } = 5.0f;

    public Shop Shop { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}

public class Order
{
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus OrderStatus { get; set; } = OrderStatus.PENDING;
    public string ShippingAddress { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    public ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

public class OrderDetail
{
    public int OrderDetailId { get; set; }
    public int OrderId { get; set; }
    public int BookId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public ReturnStatus ReturnStatus { get; set; } = ReturnStatus.NONE;

    public Order Order { get; set; } = null!;
    public Book Book { get; set; } = null!;
    public ReturnRequest? ReturnRequest { get; set; }
    public Feedback? Feedback { get; set; }
}

public class Delivery
{
    public int DeliveryId { get; set; }
    public int OrderId { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public string CarrierName { get; set; } = string.Empty;
    public decimal ShipFee { get; set; }
    public DeliveryStatus Status { get; set; } = DeliveryStatus.PENDING;
    public DateTime? EstimatedDelivery { get; set; }
    public DateTime? ActualDeliveredAt { get; set; }

    public Order Order { get; set; } = null!;
}

public class Payment
{
    public int PaymentId { get; set; }
    public int OrderId { get; set; }
    public int? ReturnRequestId { get; set; }
    public PaymentType PaymentType { get; set; }
    public PaymentMethod Method { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.PENDING;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Order Order { get; set; } = null!;
    public ReturnRequest? ReturnRequest { get; set; }
}

public class ReturnRequest
{
    public int ReturnRequestId { get; set; }
    public int OrderDetailId { get; set; }
    public ReturnReasonType ReasonType { get; set; }
    public string DetailedReason { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public ReturnRequestStatus Status { get; set; } = ReturnRequestStatus.PENDING;
    public decimal RefundAmount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public OrderDetail OrderDetail { get; set; } = null!;
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

public class TransactionHistory
{
    public int TransactionId { get; set; }
    public int UserId { get; set; }
    public TransactionReferenceType ReferenceType { get; set; }
    public int ReferenceId { get; set; }
    public TransactionType TransactionType { get; set; }
    public decimal Amount { get; set; }
    public string TransactionCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}

public class Feedback
{
    public int FeedbackId { get; set; }
    public int ShopId { get; set; }
    public int OrderDetailId { get; set; }
    public int Rating { get; set; }
    public string Content { get; set; } = string.Empty;
    public FeedbackType Type { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Shop Shop { get; set; } = null!;
    public OrderDetail OrderDetail { get; set; } = null!;
    public Response? Response { get; set; }
}

public class Response
{
    public int ResponseId { get; set; }
    public int FeedbackId { get; set; }
    public int ShopId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Feedback Feedback { get; set; } = null!;
    public Shop Shop { get; set; } = null!;
}

public class Chat
{
    public int ChatId { get; set; }
    public int UserId { get; set; }
    public int ShopId { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Shop Shop { get; set; } = null!;
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}

public class Message
{
    public int MessageId { get; set; }
    public int ChatId { get; set; }
    public int SenderId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Chat Chat { get; set; } = null!;
    public User Sender { get; set; } = null!;
}
