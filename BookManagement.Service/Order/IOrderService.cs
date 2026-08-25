using System;
using System.Threading.Tasks;

namespace BookManagement.Service.Order;

public interface IOrderService
{
    Task<OrderDetailResponse> GetShopOrderDetailAsync(int shopId, int orderId);
    Task UpdateOrderStatusAsync(int shopId, int orderId, UpdateOrderStatusRequest dto);
    Task<RevenueResponse> GetShopRevenueAsync(int shopId, DateTime? fromDate, DateTime? toDate, string? periodType);
}
