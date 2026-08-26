using System;
using System.Text.Json.Serialization;
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
        public string ShippingAddress { get; set; } = null!;
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.COD;
        public string? Note { get; set; }
    }

    public class UpdateOrderStatusRequest
    {
        [JsonPropertyName("new_status")]
        public string NewStatus { get; set; } = string.Empty;

        [JsonPropertyName("reason")]
        public string? Reason { get; set; }
    }

    public class OrderQueryRequest
    {
        public string? Status { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
