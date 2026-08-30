using System;

namespace BookManagement.Service.Shipping
{
    public class ShippingFeeResponse
    {
        public decimal TotalFee { get; set; }
        public DateTime EstimatedDeliveryDate { get; set; }
    }
}
