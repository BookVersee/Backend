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
                        .ThenInclude(b => b.Shop)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.CartBookDetails.Any())
            {
                throw new InvalidOperationException("Giỏ hàng của bạn đang trống. Vui lòng thêm sản phẩm trước khi thanh toán.");
            }

            var itemsToOrder = cart.CartBookDetails.AsQueryable();
            if (request.SelectedCartItemIds != null && request.SelectedCartItemIds.Any())
            {
                itemsToOrder = itemsToOrder.Where(cbd => request.SelectedCartItemIds.Contains(cbd.Id));
            }

            var selectedCartList = itemsToOrder.ToList();
            if (!selectedCartList.Any())
            {
                throw new InvalidOperationException("Không tìm thấy sản phẩm hợp lệ nào được chọn trong giỏ hàng.");
            }

            // Validate status, shop condition, stock and update stock
            foreach (var item in selectedCartList)
            {
                var book = item.Book;
                if (book == null)
                {
                    throw new InvalidOperationException("Sản phẩm trong giỏ hàng không tồn tại.");
                }

                if (book.Status != BookStatus.ACTIVE)
                {
                    throw new InvalidOperationException($"Sản phẩm '{book.Title}' hiện không còn mở bán.");
                }

                if (book.Shop == null || book.Shop.Condition == ShopCondition.LOCKED || book.Shop.Condition == ShopCondition.CLOSED)
                {
                    throw new InvalidOperationException($"Cửa hàng cung cấp cuốn sách '{book.Title}' hiện đang đóng cửa hoặc bị khóa.");
                }

                if (book.StockQuantity < item.Quantity)
                {
                    throw new InvalidOperationException($"Sản phẩm '{book.Title}' chỉ còn {book.StockQuantity} cuốn trong kho (bạn đặt {item.Quantity} cuốn).");
                }

                // Deduct stock quantity
                book.StockQuantity -= item.Quantity;
                if (book.StockQuantity == 0)
                {
                    book.Status = BookStatus.EMPTY;
                }
            }

            // Calculate total using current Book.Price
            var totalAmount = selectedCartList.Sum(cbd => cbd.Quantity * cbd.Book.Price);

            var order = new BookManagement.Repository.Entities.Order
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TotalAmount = totalAmount,
                OrderStatus = OrderStatus.PENDING,
                ShippingAddress = request.ShippingAddress.Trim(),
                CreatedAt = DateTimeOffset.UtcNow
            };

            var orderDetails = selectedCartList.Select(cbd => new BookManagement.Repository.Entities.OrderDetail
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                BookId = cbd.BookId,
                Quantity = cbd.Quantity,
                UnitPrice = cbd.Book.Price
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

            // Automated Notification for Buyer
            var buyerNotification = new BookManagement.Repository.Entities.Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = NotificationType.ORDER_UPDATE,
                ReferenceId = order.Id,
                Content = $"Bạn đã đặt đơn hàng #{order.Id} thành công. Tổng tiền: {totalAmount:N0} VNĐ.",
                CreatedAt = DateTimeOffset.UtcNow
            };
            await _context.Notifications.AddAsync(buyerNotification);

            // Clear ONLY selected items from cart after checkout
            _context.CartBookDetails.RemoveRange(selectedCartList);
            await _context.SaveChangesAsync();

            var createdOrder = await _orderRepository.GetByIdAsync(order.Id);
            return MapToResponse(createdOrder ?? order);
        }

        public async Task CancelOrderAsync(Guid userId, Guid orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Book)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null || order.UserId != userId) throw new KeyNotFoundException("Order not found.");
            if (order.OrderStatus != OrderStatus.PENDING) throw new InvalidOperationException("Only PENDING orders can be cancelled.");

            order.OrderStatus = OrderStatus.CANCELLED;
            order.UpdatedAt = DateTimeOffset.UtcNow;

            // Restore book stock quantity
            foreach (var detail in order.OrderDetails)
            {
                if (detail.Book != null)
                {
                    detail.Book.StockQuantity += detail.Quantity;
                    if (detail.Book.Status == BookStatus.EMPTY && detail.Book.StockQuantity > 0)
                    {
                        detail.Book.Status = BookStatus.ACTIVE;
                    }
                }
            }

            // Notification
            var notification = new BookManagement.Repository.Entities.Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = NotificationType.ORDER_UPDATE,
                ReferenceId = order.Id,
                Content = $"Đơn hàng #{order.Id} đã được hủy thành công. Tồn kho sản phẩm đã được hoàn trả.",
                CreatedAt = DateTimeOffset.UtcNow
            };
            await _context.Notifications.AddAsync(notification);

            await _context.SaveChangesAsync();
        }

        public async Task<ReturnRequestResponse> CreateReturnRequestAsync(Guid userId, Guid orderDetailId, CreateReturnRequest input)
        {
            var orderDetail = await _context.OrderDetails
                .Include(od => od.Order)
                .Include(od => od.Book)
                .FirstOrDefaultAsync(od => od.Id == orderDetailId);

            if (orderDetail == null)
            {
                throw new KeyNotFoundException("Chi tiết đơn hàng không tồn tại.");
            }

            if (orderDetail.Order.UserId != userId)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền gửi yêu cầu trả hàng cho đơn này.");
            }

            if (orderDetail.Order.OrderStatus != OrderStatus.DELIVERED && orderDetail.Order.OrderStatus != OrderStatus.COMPLETED)
            {
                throw new InvalidOperationException("Chỉ có thể gửi yêu cầu trả hàng/hoàn tiền sau khi đơn hàng đã được giao thành công.");
            }

            var returnRequest = new BookManagement.Repository.Entities.ReturnRequest
            {
                Id = Guid.NewGuid(),
                OrderDetailId = orderDetailId,
                ReasonType = input.ReasonType,
                DetailedReason = input.DetailedReason,
                ImageUrl = input.ImageUrl,
                Status = ReturnRequestStatus.PENDING,
                RefundAmount = input.RefundAmount > 0 ? input.RefundAmount : (orderDetail.UnitPrice * orderDetail.Quantity)
            };

            orderDetail.ReturnStatus = ReturnStatus.REQUESTED;

            await _orderRepository.CreateReturnRequestAsync(returnRequest);

            // Notification
            var notification = new BookManagement.Repository.Entities.Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = NotificationType.ORDER_UPDATE,
                ReferenceId = returnRequest.Id,
                Content = $"Yêu cầu trả hàng cho cuốn '{orderDetail.Book?.Title}' đã được gửi tới Shop. Vui lòng chờ phản hồi.",
                CreatedAt = DateTimeOffset.UtcNow
            };
            await _context.Notifications.AddAsync(notification);
            await _context.SaveChangesAsync();

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

        public async Task EscalateReturnRequestAsync(Guid userId, Guid returnRequestId, string? reason)
        {
            var returnReq = await _context.ReturnRequests
                .Include(rr => rr.OrderDetail)
                    .ThenInclude(od => od.Order)
                .FirstOrDefaultAsync(rr => rr.Id == returnRequestId);

            if (returnReq == null || returnReq.OrderDetail.Order.UserId != userId)
            {
                throw new KeyNotFoundException("Không tìm thấy yêu cầu trả hàng.");
            }

            if (returnReq.Status != ReturnRequestStatus.REJECTED)
            {
                throw new InvalidOperationException("Chỉ có thể gửi khiếu nại lên Admin khi yêu cầu trả hàng bị Shop từ chối.");
            }

            // Mark as PENDING again for Admin escalation
            returnReq.Status = ReturnRequestStatus.PENDING;
            returnReq.DetailedReason = (returnReq.DetailedReason ?? "") + $" | [KHIẾU NẠI ADMIN: {reason}]";
            returnReq.UpdatedAt = DateTimeOffset.UtcNow;

            var notification = new BookManagement.Repository.Entities.Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = NotificationType.SYSTEM,
                ReferenceId = returnReq.Id,
                Content = $"Yêu cầu khiếu nại của bạn cho sản phẩm đã được gửi lên Ban quản trị (Admin) xử lý.",
                CreatedAt = DateTimeOffset.UtcNow
            };
            await _context.Notifications.AddAsync(notification);
            await _context.SaveChangesAsync();
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
