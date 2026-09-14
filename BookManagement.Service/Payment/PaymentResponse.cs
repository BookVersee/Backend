using System;

namespace BookManagement.Service.Payment
{
    public class PaymentUrlResponse
    {
        public string PaymentUrl { get; set; } = null!;
        public string? QrCodeUrl { get; set; }
        public string? Deeplink { get; set; }
    }

    public class PaymentStatusResponse
    {
        public Guid OrderId { get; set; }
        public bool IsPaid { get; set; }
        public string Message { get; set; } = null!;
        public string? TransactionCode { get; set; }
    }
}
