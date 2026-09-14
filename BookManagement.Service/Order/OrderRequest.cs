using System;
using System.Collections.Generic;
using BookManagement.Repository.Entities.Enums;

namespace BookManagement.Service.Order
{
    public class CreateReturnRequest
    {
        public ReasonType ReasonType { get; set; }
        public string? DetailedReason { get; set; }
        public string? ImageUrl { get; set; }
        public decimal RefundAmount { get; set; }
    }

    public class CreateOrderRequest
    {
        public List<Guid>? SelectedCartItemIds { get; set; }
        public string ShippingAddress { get; set; } = null!;
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.COD;
        public string? Note { get; set; }
    }

    public class UpdateOrderStatusRequest
    {
        public string NewStatus { get; set; } = string.Empty;
        public string? Reason { get; set; }
    }

    public class UpdateOrderStatusDto
    {
        public string? OrderStatus { get; set; }
        public string? NewStatus { get; set; }
        public decimal? Weight { get; set; }
        public string? Note { get; set; }
        public string? Reason { get; set; }
    }

    public class ProcessReturnRequestDto
    {
        public bool? IsApproved { get; set; }
        public string? Status { get; set; }
        public string? AdminNote { get; set; }
        public string? RejectionReason { get; set; }
    }

    public class OrderQueryRequest
    {
        public string? Status { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
