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
}
