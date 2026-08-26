using System;
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
}
