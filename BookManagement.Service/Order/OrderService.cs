using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookManagement.Repository.Abstractions;
using BookManagement.Repository.Data;
using BookManagement.Repository.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace BookManagement.Service.Order
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly AppDbContext _context;

        public OrderService(IOrderRepository orderRepository, AppDbContext context)
        {
            _orderRepository = orderRepository;
            _context = context;
        }

        public async Task<IEnumerable<OrderResponse>> GetUserOrdersAsync(Guid userId, OrderStatus? status = null)
        {
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            var isShop = user != null && user.Role == UserRole.SHOP;

            var orders = isShop
                ? await _orderRepository.GetOrdersByShopUserIdAsync(userId, status)
                : await _orderRepository.GetOrdersByUserIdAsync(userId, status);

            return orders.Select(MapToResponse);
        }

        public async Task<OrderResponse> GetOrderDetailAsync(Guid userId, Guid orderId)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null) throw new KeyNotFoundException("Order not found.");

            var isBuyer = order.UserId == userId;
            var isSeller = order.OrderDetails.Any(od => od.Book?.Shop?.UserId == userId);

            if (!isBuyer && !isSeller)
            {
                throw new KeyNotFoundException("Order not found.");
            }

            return MapToResponse(order);
        }

        public async Task<OrderResponse> CreateOrderAsync(Guid userId, CreateOrderRequest request)
        {
            var cart = await _context.Carts
                .Include(c => c.CartBookDetails)
                .ThenInclude(cbd => cbd.Book)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.CartBookDetails.Any())
            {
                throw new InvalidOperationException("Giỏ hàng của bạn đang trống. Vui lòng thêm sản phẩm trước khi thanh toán.");
            }

            var totalAmount = cart.CartBookDetails.Sum(cbd => cbd.Quantity * cbd.UnitPrice);

            var order = new BookManagement.Repository.Entities.Order
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TotalAmount = totalAmount,
                OrderStatus = OrderStatus.PENDING,
                ShippingAddress = request.ShippingAddress.Trim(),
                CreatedAt = DateTimeOffset.UtcNow
            };

            var orderDetails = cart.CartBookDetails.Select(cbd => new BookManagement.Repository.Entities.OrderDetail
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                BookId = cbd.BookId,
                Quantity = cbd.Quantity,
                UnitPrice = cbd.UnitPrice
            }).ToList();

            var payment = new BookManagement.Repository.Entities.Payment
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                PaymentType = PaymentType.PAYMENT,
                Method = request.PaymentMethod,
                Status = request.PaymentMethod == PaymentMethod.COD ? PaymentStatus.PENDING : PaymentStatus.SUCCESS,
                Amount = totalAmount
            };

            var delivery = new BookManagement.Repository.Entities.Delivery
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                CarrierName = "Giao Hàng Nhanh",
                TrackingNumber = "GHN" + Random.Shared.Next(10000000, 99999999).ToString(),
                Status = DeliveryStatus.PENDING
            };

            await _context.Orders.AddAsync(order);
            await _context.OrderDetails.AddRangeAsync(orderDetails);
            await _context.Payments.AddAsync(payment);
            await _context.Deliveries.AddAsync(delivery);

            // Clear items from cart after checkout
            _context.CartBookDetails.RemoveRange(cart.CartBookDetails);
            await _context.SaveChangesAsync();

            var createdOrder = await _orderRepository.GetByIdAsync(order.Id);
            return MapToResponse(createdOrder ?? order);
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
