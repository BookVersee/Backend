using System;
using System.Collections.Generic;
using BookManagement.Service.Common;

namespace BookManagement.Service.Admin;

public class UserDetailResponse
{
    public Guid Id { get; set; }
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string Role { get; set; } = null!;
    public string Status { get; set; } = null!;
    public Guid? ShopId { get; set; }
    public string? ShopName { get; set; }
    public string? ShopStatus { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<OrderSummaryResponse> RecentOrders { get; set; } = new();
    public List<TransactionSummaryResponse> FinancialTransactions { get; set; } = new();
}

public class OrderSummaryResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = null!;
    public string? ShippingAddress { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class TransactionSummaryResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string ReferenceType { get; set; } = null!;
    public Guid? ReferenceId { get; set; }
    public string TransactionType { get; set; } = null!;
    public decimal Amount { get; set; }
    public string TransactionCode { get; set; } = null!;
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class DisputeResponse
{
    public Guid ReturnRequestId { get; set; }
    public Guid OrderDetailId { get; set; }
    public Guid OrderId { get; set; }
    public string CustomerName { get; set; } = null!;
    public string ShopName { get; set; } = null!;
    public string ReasonType { get; set; } = null!;
    public string DetailedReason { get; set; } = null!;
    public string? EvidenceImageUrl { get; set; }
    public string Status { get; set; } = null!;
    public decimal? RefundAmount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class DashboardStatisticsResponse
{
    public int TotalOrders { get; set; }
    public int TotalUsers { get; set; }
    public int ActiveShops { get; set; }
    public decimal TotalRevenue { get; set; }
    public int DisputesCount { get; set; }
}

public class RevenueReportResponse
{
    public string Period { get; set; } = null!;
    public decimal TotalRevenue { get; set; }
    public decimal AvgOrderValue { get; set; }
    public int OrderCount { get; set; }
}

public class TopSellingBooksResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public int SoldCount { get; set; }
    public decimal TotalRevenue { get; set; }
}

public class ReportedResponseDto
{
    public Guid NotificationId { get; set; }
    public Guid? ResponseId { get; set; }
    public string CustomerUsername { get; set; } = null!;
    public string? ShopName { get; set; }
    public string? FeedbackContent { get; set; }
    public string? ResponseContent { get; set; }
    public string ReportReason { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
}
