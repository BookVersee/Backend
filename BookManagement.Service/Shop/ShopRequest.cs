using System;

namespace BookManagement.Service.Shop
{
    public class ShopRegisterDto
    {
        public string ShopName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? QrImageUrl { get; set; }
    }

    public class UpdateShopDto
    {
        public string? ShopName { get; set; }
        public string? Address { get; set; }
        public string? QrImageUrl { get; set; }
    }

    public class RevenueQueryRequest
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? PeriodType { get; set; }
    }
}
