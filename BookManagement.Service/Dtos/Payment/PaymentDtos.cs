using System;
using BookManagement.Repository.Entities.Enums;

namespace BookManagement.Service.Dtos.Payment
{
    public class PaymentDto
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public Guid? ReturnRequestId { get; set; }
        public PaymentType PaymentType { get; set; }
        public PaymentMethod Method { get; set; }
        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }

    public class CreatePaymentUrlRequestDto
    {
        public Guid OrderId { get; set; }
        public string IpAddress { get; set; } = "127.0.0.1";
    }

    public class VnPayIpnResponseDto
    {
        public string RspCode { get; set; } = "00";
        public string Message { get; set; } = "Confirm Success";
    }
}
