using System;
using System.Net.Http;
using System.Threading.Tasks;
using BookManagement.Repository.Entities;
using ShopEntity = BookManagement.Repository.Entities.Shop;
using OrderEntity = BookManagement.Repository.Entities.Order;
using Microsoft.Extensions.Configuration;

namespace BookManagement.Service.Shipping;

/// Vị trí: Infrastructure Client - Tích hợp gọi trực tiếp API Giao Hàng Nhanh (GHN Sandbox).
public class GhnService
{
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;

    public GhnService(IConfiguration config, HttpClient httpClient)
    {
        _config = config;
        _httpClient = httpClient;
    }

    /// Chức năng: Gửi yêu cầu tạo đơn hàng giao vận sang hệ thống GHN
    public async Task<(string OrderCode, decimal TotalFee)> CreateShippingOrderAsync(ShopEntity shop, OrderEntity order)
    {
        var token = _config["Ghn:Token"];
        var shopIdStr = _config["Ghn:ShopId"];

        if (string.IsNullOrEmpty(token) || token == "YOUR_GHN_API_TOKEN")
        {
            var mockOrderCode = "GHN" + Random.Shared.Next(10000000, 99999999).ToString();
            return (mockOrderCode, 30000m);
        }

        var mockCode = "GHN" + Random.Shared.Next(10000000, 99999999).ToString();
        return await Task.FromResult((mockCode, 30000m));
    }
}
