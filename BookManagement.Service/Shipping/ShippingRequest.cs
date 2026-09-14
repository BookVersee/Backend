using System;

namespace BookManagement.Service.Shipping
{
    public class CalculateShippingFeeRequest
    {
        public Guid OrderId { get; set; }
        public string ToDistrictId { get; set; } = null!;
        public string ToWardCode { get; set; } = null!;
        public int WeightGram { get; set; }
    }
}
