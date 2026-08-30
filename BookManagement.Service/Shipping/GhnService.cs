using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
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
        var apiUrl = _config["Ghn:ApiUrl"] ?? "https://dev-online-gateway.ghn.vn/shiip/public-api/v2";

        if (string.IsNullOrEmpty(token) || token == "YOUR_GHN_API_TOKEN")
        {
            var mockOrderCode = "GHN" + Random.Shared.Next(10000000, 99999999).ToString();
            return (mockOrderCode, 30000m);
        }

        var endpoint = $"{apiUrl.TrimEnd('/')}/shipping-order/create";

        // Map danh sách hàng hóa
        var items = new List<object>();
        if (order.OrderDetails != null && order.OrderDetails.Any())
        {
            foreach (var od in order.OrderDetails)
            {
                items.Add(new
                {
                    name = !string.IsNullOrWhiteSpace(od.Book?.Title) ? od.Book.Title : "Sách",
                    quantity = od.Quantity > 0 ? od.Quantity : 1,
                    price = (int)od.UnitPrice,
                    weight = 200
                });
            }
        }
        else
        {
            items.Add(new
            {
                name = "Sách",
                quantity = 1,
                price = (int)order.TotalAmount,
                weight = 200
            });
        }

        // Tính trọng lượng (gram)
        int totalWeight = 500;
        if (order.Weight.HasValue && order.Weight.Value > 0)
        {
            totalWeight = order.Weight.Value <= 20 ? (int)(order.Weight.Value * 1000) : (int)order.Weight.Value;
        }

        var payload = new
        {
            payment_type_id = 2, // 1: Người bán trả ship, 2: Người mua trả ship
            note = order.Note ?? "Đơn hàng BookVerse",
            required_note = "CHOXEMHANGKHONGTHU",

            // Thông tin người gửi (Shop)
            from_name = !string.IsNullOrWhiteSpace(shop.ShopName) ? shop.ShopName : (!string.IsNullOrWhiteSpace(shop.FullName) ? shop.FullName : "Shop BookVerse"),
            from_phone = !string.IsNullOrWhiteSpace(shop.Phone) ? shop.Phone : "0901234567",
            from_address = !string.IsNullOrWhiteSpace(shop.Address) ? shop.Address : "72 Thành Thái, Phường 14, Quận 10, Hồ Chí Minh",
            from_ward_name = "Phường 14",
            from_district_name = "Quận 10",
            from_province_name = "Hồ Chí Minh",

            // Thông tin người nhận (Khách)
            to_name = !string.IsNullOrWhiteSpace(order.User?.FullName) ? order.User.FullName : (!string.IsNullOrWhiteSpace(order.User?.Username) ? order.User.Username : "Khách hàng"),
            to_phone = !string.IsNullOrWhiteSpace(order.User?.Phone) ? order.User.Phone : "0987654321",
            to_address = !string.IsNullOrWhiteSpace(order.ShippingAddress) ? order.ShippingAddress : "72 Thành Thái, Phường 14, Quận 10, TP.HCM",
            to_ward_code = "20311",
            to_district_id = 1444,

            // Thông tin gói hàng
            weight = totalWeight,
            length = 20,
            width = 15,
            height = 5,
            service_id = 53320,
            service_type_id = 2,

            // Danh sách hàng hóa
            items
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Add("Token", token);
        if (!string.IsNullOrEmpty(shopIdStr))
        {
            request.Headers.Add("ShopId", shopIdStr);
        }

        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(responseContent);
        var root = doc.RootElement;

        if (root.TryGetProperty("code", out var codeEl) && codeEl.GetInt32() == 200)
        {
            if (root.TryGetProperty("data", out var dataEl))
            {
                string orderCode = dataEl.GetProperty("order_code").GetString() ?? string.Empty;
                decimal totalFee = dataEl.TryGetProperty("total_fee", out var feeEl) ? feeEl.GetDecimal() : 30000m;
                return (orderCode, totalFee);
            }
        }

        string errorMsg = root.TryGetProperty("message", out var msgEl) ? (msgEl.GetString() ?? "Unknown error") : "Failed to create GHN order";
        throw new InvalidOperationException($"Lỗi kết nối tạo đơn Giao Hàng Nhanh (GHN): {errorMsg} - Phản hồi: {responseContent}");
    }
}
