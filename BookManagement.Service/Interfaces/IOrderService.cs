using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BookManagement.Repository.Entities.Enums;
using BookManagement.Service.Dtos.Order;
using BookManagement.Service.Models;

namespace BookManagement.Service.Interfaces
{
    public interface IOrderService
    {
        Task<ApiResponse<OrderDto>> GetOrderByIdAsync(Guid orderId);
        Task<ApiResponse<IEnumerable<OrderDto>>> GetOrdersByShopAsync(Guid shopId, OrderFilterDto filter);
        Task<ApiResponse<OrderDto>> UpdateOrderStatusAsync(Guid shopId, Guid orderId, UpdateOrderStatusDto dto);
        Task<ApiResponse<bool>> CancelOrderAsync(Guid shopId, Guid orderId, string reason);
    }
}
