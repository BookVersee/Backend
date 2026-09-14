using System;

namespace BookManagement.Service.Payment
{
    public class CreatePaymentUrlDto
    {
        public Guid OrderId { get; set; }
        public string? OrderInfo { get; set; }
    }

    public class ProcessRefundDto
    {
        public Guid OrderId { get; set; }
        public Guid? ReturnRequestId { get; set; }
        public decimal? Amount { get; set; }
        public string? TransactionNo { get; set; }
        public string? RefundReason { get; set; }
    }

    public class CreateVnpayUrlDto : CreatePaymentUrlDto { }
    public class VnpayRefundDto : ProcessRefundDto { }
}
