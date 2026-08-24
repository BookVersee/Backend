using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookManagement.Repository.Abstractions;
using BookManagement.Repository.Entities.Enums;

namespace BookManagement.Service.Order
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;

        public OrderService(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public async Task<IEnumerable<OrderResponse>> GetUserOrdersAsync(Guid userId, OrderStatus? status = null)
        {
            var orders = await _orderRepository.GetOrdersByUserIdAsync(userId, status);
            return orders.Select(MapToResponse);
        }

        public async Task<OrderResponse> GetOrderDetailAsync(Guid userId, Guid orderId)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null || order.UserId != userId) throw new KeyNotFoundException("Order not found.");
            return MapToResponse(order);
        }

        public async Task CancelOrderAsync(Guid userId, Guid orderId)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null || order.UserId != userId) throw new KeyNotFoundException("Order not found.");
            if (order.OrderStatus != OrderStatus.PENDING) throw new InvalidOperationException("Only PENDING orders can be cancelled.");
            order.OrderStatus = OrderStatus.CANCELLED;
            await _orderRepository.UpdateOrderAsync(order);
        }

        public async Task<ReturnRequestResponse> CreateReturnRequestAsync(Guid userId, Guid orderDetailId, CreateReturnRequest input)
        {
            var returnRequest = new BookManagement.Repository.Entities.ReturnRequest
            {
                Id = Guid.NewGuid(),
                OrderDetailId = orderDetailId,
                ReasonType = input.ReasonType,
                DetailedReason = input.DetailedReason,
                ImageUrl = input.ImageUrl,
                Status = ReturnRequestStatus.PENDING,
                RefundAmount = input.RefundAmount
            };

            await _orderRepository.CreateReturnRequestAsync(returnRequest);

            return new ReturnRequestResponse
            {
                Id = returnRequest.Id,
                OrderDetailId = returnRequest.OrderDetailId,
                ReasonType = returnRequest.ReasonType,
                DetailedReason = returnRequest.DetailedReason,
                ImageUrl = returnRequest.ImageUrl,
                Status = returnRequest.Status,
                RefundAmount = returnRequest.RefundAmount,
                CreatedAt = returnRequest.CreatedAt
            };
        }

        private static OrderResponse MapToResponse(BookManagement.Repository.Entities.Order order) => new OrderResponse
        {
            Id = order.Id,
            UserId = order.UserId,
            UserFullName = order.User?.FullName ?? order.User?.Username ?? "Customer",
            TotalAmount = order.TotalAmount,
            OrderStatus = order.OrderStatus,
            ShippingAddress = order.ShippingAddress,
            Weight = order.Weight,
            Note = order.Note,
            CreatedAt = order.CreatedAt,
            OrderDetails = order.OrderDetails.Select(od => new OrderDetailResponse
            {
                OrderDetailId = od.Id,
                BookId = od.BookId,
                BookTitle = od.Book?.Title ?? "Unknown",
                BookImage = od.Book?.ImageUrl,
                Quantity = od.Quantity,
                UnitPrice = od.UnitPrice,
                ReturnStatus = od.ReturnStatus,
                ReturnRequest = od.ReturnRequest == null ? null : new ReturnRequestResponse
                {
                    Id = od.ReturnRequest.Id,
                    OrderDetailId = od.ReturnRequest.OrderDetailId,
                    ReasonType = od.ReturnRequest.ReasonType,
                    DetailedReason = od.ReturnRequest.DetailedReason,
                    ImageUrl = od.ReturnRequest.ImageUrl,
                    Status = od.ReturnRequest.Status,
                    RefundAmount = od.ReturnRequest.RefundAmount,
                    CreatedAt = od.ReturnRequest.CreatedAt
                }
            }).ToList(),
            Deliveries = order.Deliveries.Select(d => new DeliveryResponse
            {
                Id = d.Id,
                TrackingNumber = d.TrackingNumber,
                CarrierName = d.CarrierName,
                ShipFee = d.ShipFee,
                Status = d.Status,
                EstimatedDelivery = d.EstimatedDelivery,
                ActualDeliveredAt = d.ActualDeliveredAt
            }).ToList()
        };
    }
}
