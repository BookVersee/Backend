using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BookManagement.Repository.Entities.Enums;

namespace BookManagement.Service.Order
{
    public interface IOrderService
    {
        Task<IEnumerable<OrderResponse>> GetUserOrdersAsync(Guid userId, OrderStatus? status = null);
        Task<OrderResponse> GetOrderDetailAsync(Guid userId, Guid orderId);
        Task<OrderResponse> CreateOrderAsync(Guid userId, CreateOrderRequest request);
        Task CancelOrderAsync(Guid userId, Guid orderId);
        Task<ReturnRequestResponse> CreateReturnRequestAsync(Guid userId, Guid orderDetailId, CreateReturnRequest input);
        Task EscalateReturnRequestAsync(Guid userId, Guid returnRequestId, string? reason);
    }
}
