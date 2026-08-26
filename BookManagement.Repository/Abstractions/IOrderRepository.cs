using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookManagement.Repository.Entities;
using BookManagement.Repository.Entities.Enums;

namespace BookManagement.Repository.Abstractions
{
    public interface IOrderRepository
    {
        Task<Order?> GetByIdAsync(Guid id);
        Task<IEnumerable<Order>> GetOrdersByUserIdAsync(Guid userId, OrderStatus? status = null);
        Task<IEnumerable<Order>> GetOrdersByShopUserIdAsync(Guid shopUserId, OrderStatus? status = null);
        Task<IQueryable<Order>> GetQueryableAsync();
        Task CreateOrderAsync(Order order);
        Task UpdateOrderAsync(Order order);
        Task CreateReturnRequestAsync(ReturnRequest returnRequest);
        Task<ReturnRequest?> GetReturnRequestByIdAsync(Guid id);
        Task UpdateReturnRequestAsync(ReturnRequest returnRequest);
        Task<IQueryable<ReturnRequest>> GetDisputesQueryableAsync();
        Task<OrderDetail?> GetOrderDetailByIdAsync(Guid orderDetailId);
    }
}
