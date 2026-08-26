using System;
using System.Net.Http;
using System.Threading.Tasks;
using BookManagement.Repository.Entities;
using Microsoft.Extensions.Configuration;

namespace BookManagement.Service.Services;

public class GhnService
{
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;

    public GhnService(IConfiguration config, HttpClient httpClient)
    {
        _config = config;
        _httpClient = httpClient;
    }

    public async Task<(string OrderCode, decimal TotalFee)> CreateShippingOrderAsync(Shop shop, BookManagement.Repository.Entities.Order order)
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
