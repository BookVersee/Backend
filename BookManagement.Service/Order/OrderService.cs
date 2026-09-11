using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookManagement.Repository.Data;
using BookManagement.Repository.Entities.Enums;
using BookManagement.Service.Delivery;
using Microsoft.EntityFrameworkCore;

namespace BookManagement.Service.Order
{
    /// Vị trí: Domain Service - Thực thi logic nghiệp vụ hệ thống, xử lý giao dịch mua bán, thanh toán và lưu DbContext.
    public class OrderService : IOrderService
    {
        private readonly AppDbContext _context;
        private readonly IOrderRealtimeNotifier? _orderNotifier;
        private readonly BookManagement.Service.Notification.INotificationRealtimeNotifier? _notificationNotifier;

        public OrderService(
            AppDbContext context,
            IOrderRealtimeNotifier? orderNotifier = null,
            BookManagement.Service.Notification.INotificationRealtimeNotifier? notificationNotifier = null)
        {
            _context = context;
            _orderNotifier = orderNotifier;
            _notificationNotifier = notificationNotifier;
        }

        private IQueryable<BookManagement.Repository.Entities.Order> GetFullOrderQuery()
        {
            return _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Book)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ReturnRequest)
                .Include(o => o.Deliveries)
                .AsNoTracking();
        }

        /// Chức năng: Lấy danh sách lịch sử đơn hàng của người dùng hoặc Shop
        public async Task<IEnumerable<OrderResponse>> GetUserOrdersAsync(Guid userId, OrderStatus? status = null)
        {
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            var isShop = user != null && user.Role == UserRole.SHOP;

            var query = GetFullOrderQuery();

            if (isShop)
            {
                var shopIds = await _context.Database
                    .SqlQueryRaw<Guid>("SELECT Id FROM Shops WHERE UserId = {0} OR Id = {0}", userId)
                    .ToListAsync();

                if (!shopIds.Contains(userId))
                {
                    shopIds.Add(userId);
                }

                query = query.Where(o => o.OrderDetails.Any(od => od.Book != null && shopIds.Contains(od.Book.ShopId)));
            }
            else
            {
                query = query.Where(o => o.UserId == userId);
            }

            if (status.HasValue)
            {
                query = query.Where(o => o.OrderStatus == status.Value);
            }

            var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
            return orders.Select(MapToResponse);
        }

        /// Chức năng: Xem thông tin chi tiết toàn diện của 1 đơn hàng
        public async Task<OrderResponse> GetOrderDetailAsync(Guid userId, Guid orderId)
        {
            var order = await GetFullOrderQuery().FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null) throw new KeyNotFoundException("Order not found.");

            var shopIds = await _context.Database
                .SqlQueryRaw<Guid>("SELECT Id FROM Shops WHERE UserId = {0} OR Id = {0}", userId)
                .ToListAsync();

            if (!shopIds.Contains(userId))
            {
                shopIds.Add(userId);
            }

            var isBuyer = order.UserId == userId;
            var isSeller = order.OrderDetails.Any(od => od.Book != null && shopIds.Contains(od.Book.ShopId));

            if (!isBuyer && !isSeller)
            {
                throw new KeyNotFoundException("Order not found.");
            }

            return MapToResponse(order);
        }

        /// Chức năng: Đặt hàng thanh toán (Trừ kho nguyên tử chống Overselling Race Condition)
        public async Task<OrderResponse> CreateOrderAsync(Guid userId, CreateOrderRequest request)
        {
            var cart = await _context.Carts
                .Include(c => c.CartBookDetails)
                    .ThenInclude(cbd => cbd.Book)
                        .ThenInclude(b => b.Shop)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.CartBookDetails.Any(cbd => !cbd.IsDeleted))
            {
                throw new InvalidOperationException("Giỏ hàng của bạn đang trống. Vui lòng thêm sản phẩm trước khi thanh toán.");
            }

            var itemsToOrder = cart.CartBookDetails.Where(cbd => !cbd.IsDeleted).AsQueryable();
            if (request.SelectedCartItemIds != null && request.SelectedCartItemIds.Any())
            {
                itemsToOrder = itemsToOrder.Where(cbd => request.SelectedCartItemIds.Contains(cbd.Id));
            }

            var selectedCartList = itemsToOrder.ToList();
            if (!selectedCartList.Any())
            {
                throw new InvalidOperationException("Không tìm thấy sản phẩm hợp lệ nào được chọn trong giỏ hàng.");
            }

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var tx = await _context.Database.BeginTransactionAsync();
                try
                {
                    var now = DateTimeOffset.UtcNow;
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

                        if (book.Shop == null || book.Shop.Condition != ShopCondition.OPEN)
                        {
                            throw new InvalidOperationException($"Cửa hàng cung cấp cuốn sách '{book.Title}' hiện chưa được duyệt hoặc đang đóng cửa.");
                        }

                        int rowsAffected = await _context.Database.ExecuteSqlInterpolatedAsync(
                            $"UPDATE Books SET StockQuantity = StockQuantity - {item.Quantity}, Status = CASE WHEN StockQuantity - {item.Quantity} = 0 THEN 'EMPTY' ELSE Status END, UpdatedAt = {now} WHERE Id = {item.BookId} AND StockQuantity >= {item.Quantity} AND Status = 'ACTIVE'");

                        if (rowsAffected == 0)
                        {
                            throw new InvalidOperationException($"Sản phẩm '{book.Title}' đã hết hàng hoặc không đủ số lượng trong kho.");
                        }
                    }

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
                        UnitPrice = cbd.Book.Price,
                        ReturnStatus = ReturnStatus.NONE
                    }).ToList();

                    var payment = new BookManagement.Repository.Entities.Payment
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        PaymentType = PaymentType.PAYMENT,
                        Method = request.PaymentMethod,
                        Status = PaymentStatus.PENDING,
                        Amount = totalAmount,
                        CreatedAt = DateTimeOffset.UtcNow
                    };

                    await _context.Orders.AddAsync(order);
                    await _context.OrderDetails.AddRangeAsync(orderDetails);
                    await _context.Payments.AddAsync(payment);

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

                    foreach (var item in selectedCartList)
                    {
                        item.IsDeleted = true;
                        item.UpdatedAt = DateTimeOffset.UtcNow;
                    }
                    await _context.SaveChangesAsync();
                    await tx.CommitAsync();

                    var createdOrder = await GetFullOrderQuery().FirstOrDefaultAsync(o => o.Id == order.Id);
                    var responseDto = MapToResponse(createdOrder ?? order);

                    // Bắn Realtime thông báo Đơn hàng mới tới các Shop liên quan
                    if (_orderNotifier != null)
                    {
                        try
                        {
                            var shopIds = responseDto.OrderDetails
                                .Select(od => od.BookId)
                                .Join(_context.Books, bId => bId, b => b.Id, (bId, b) => b.ShopId)
                                .Distinct()
                                .ToList();

                            foreach (var shopId in shopIds)
                            {
                                await _orderNotifier.SendNewOrderAlertAsync(shopId, responseDto);
                            }
                        }
                        catch
                        {
                            // Tránh crash nếu SignalR gặp lỗi
                        }
                    }

                    // Bắn Realtime quả chuông cho người mua
                    if (_notificationNotifier != null)
                    {
                        try
                        {
                            await _notificationNotifier.SendNotificationAsync(userId, new BookManagement.Service.Notification.NotificationResponse
                            {
                                Id = buyerNotification.Id,
                                UserId = buyerNotification.UserId,
                                Type = buyerNotification.Type.ToString(),
                                ReferenceId = buyerNotification.ReferenceId,
                                Content = buyerNotification.Content ?? string.Empty,
                                IsRead = false,
                                CreatedAt = buyerNotification.CreatedAt
                            });
                        }
                        catch
                        {
                        }
                    }

                    return responseDto;
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }
            });
        }

        /// Chức năng: Hủy đơn hàng PENDING và hoàn trả tồn kho sản phẩm + giỏ hàng
        public async Task CancelOrderAsync(Guid userId, Guid orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Book)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null || order.UserId != userId) throw new KeyNotFoundException("Order not found.");
            if (order.OrderStatus != OrderStatus.PENDING) throw new InvalidOperationException("Only PENDING orders can be cancelled.");

            order.OrderStatus = OrderStatus.CANCELLED;
            order.UpdatedAt = DateTimeOffset.UtcNow;

            foreach (var detail in order.OrderDetails)
            {
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE Books SET StockQuantity = StockQuantity + {detail.Quantity}, Status = CASE WHEN Status = 'EMPTY' THEN 'ACTIVE' ELSE Status END, UpdatedAt = {DateTimeOffset.UtcNow} WHERE Id = {detail.BookId}");
            }

            foreach (var payment in order.Payments.Where(p => p.Status == PaymentStatus.PENDING))
            {
                payment.Status = PaymentStatus.FAILED;
                payment.UpdatedAt = DateTimeOffset.UtcNow;
            }

            // Hoàn trả các sản phẩm về giỏ hàng (IsDeleted = false) cho người mua
            var userCart = await _context.Carts
                .Include(c => c.CartBookDetails)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (userCart != null)
            {
                var orderBookIds = order.OrderDetails.Select(od => od.BookId).ToHashSet();
                foreach (var cbd in userCart.CartBookDetails.Where(cbd => orderBookIds.Contains(cbd.BookId) && cbd.IsDeleted))
                {
                    cbd.IsDeleted = false;
                    cbd.UpdatedAt = DateTimeOffset.UtcNow;
                }
            }

            var notification = new BookManagement.Repository.Entities.Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = NotificationType.ORDER_UPDATE,
                ReferenceId = order.Id,
                Content = $"Đơn hàng #{order.Id} đã được hủy thành công. Tồn kho sản phẩm và giỏ hàng đã được hoàn trả.",
                CreatedAt = DateTimeOffset.UtcNow
            };
            await _context.Notifications.AddAsync(notification);
            await _context.SaveChangesAsync();

            if (_orderNotifier != null)
            {
                try
                {
                    await _orderNotifier.SendOrderStatusChangedAsync(userId, order.Id, OrderStatus.CANCELLED.ToString(), "Đơn hàng đã được hủy thành công.");
                }
                catch
                {
                }
            }
        }

        /// Chức năng: Gửi yêu cầu khiếu nại trả hàng / hoàn tiền cho sản phẩm
        public async Task<ReturnRequestResponse> CreateReturnRequestAsync(Guid userId, Guid orderDetailId, CreateReturnRequest input)
        {
            var orderDetail = await _context.OrderDetails
                .Include(od => od.Order)
                .Include(od => od.Book)
                    .ThenInclude(b => b.Shop)
                .FirstOrDefaultAsync(od => od.Id == orderDetailId);

            if (orderDetail == null)
            {
                throw new KeyNotFoundException("Chi tiết đơn hàng không tồn tại.");
            }

            if (orderDetail.Order.UserId != userId)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền gửi yêu cầu trả hàng cho đơn này.");
            }

            if (orderDetail.Order.OrderStatus != OrderStatus.DELIVERED)
            {
                throw new InvalidOperationException("Chỉ có thể gửi yêu cầu trả hàng/hoàn tiền sau khi đơn hàng đã được giao thành công.");
            }

            var deliveredDate = orderDetail.Order.Deliveries.FirstOrDefault(d => d.ActualDeliveredAt.HasValue)?.ActualDeliveredAt ?? orderDetail.Order.UpdatedAt ?? orderDetail.Order.CreatedAt;
            if (DateTimeOffset.UtcNow - deliveredDate > TimeSpan.FromDays(5))
            {
                throw new InvalidOperationException("Quá thời hạn 5 ngày kể từ khi giao hàng thành công. Hệ thống không tiếp nhận yêu cầu trả hàng nữa.");
            }

            if (orderDetail.ReturnStatus != ReturnStatus.NONE)
            {
                throw new InvalidOperationException("Yêu cầu trả hàng cho sản phẩm này đã được gửi trước đó.");
            }

            var returnRequest = new BookManagement.Repository.Entities.ReturnRequest
            {
                Id = Guid.NewGuid(),
                OrderDetailId = orderDetailId,
                ReasonType = input.ReasonType,
                DetailedReason = input.DetailedReason,
                ImageUrl = input.ImageUrl,
                Status = ReturnRequestStatus.PENDING,
                RefundAmount = input.RefundAmount > 0 ? input.RefundAmount : (orderDetail.UnitPrice * orderDetail.Quantity),
                CreatedAt = DateTimeOffset.UtcNow
            };

            orderDetail.ReturnStatus = ReturnStatus.REQUESTED;

            await _context.ReturnRequests.AddAsync(returnRequest);

            var buyerNotification = new BookManagement.Repository.Entities.Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = NotificationType.ORDER_UPDATE,
                ReferenceId = returnRequest.Id,
                Content = $"Yêu cầu trả hàng cho cuốn '{orderDetail.Book?.Title}' đã được gửi tới Shop. Vui lòng chờ phản hồi trong vòng 3 ngày.",
                CreatedAt = DateTimeOffset.UtcNow
            };
            await _context.Notifications.AddAsync(buyerNotification);

            var shopUserId = orderDetail.Book?.ShopId;
            if (shopUserId.HasValue && shopUserId.Value != Guid.Empty)
            {
                var shopNotification = new BookManagement.Repository.Entities.Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = shopUserId.Value,
                    Type = NotificationType.ORDER_UPDATE,
                    ReferenceId = returnRequest.Id,
                    Content = $"Khách hàng gửi yêu cầu trả hàng cho sản phẩm '{orderDetail.Book?.Title}' (Đơn hàng #{orderDetail.OrderId}). Vui lòng xử lý trong vòng 3 ngày.",
                    CreatedAt = DateTimeOffset.UtcNow
                };
                await _context.Notifications.AddAsync(shopNotification);
            }

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

        /// Chức năng: Gửi khiếu nại lên Admin khi Shop từ chối yêu cầu trả hàng (trong vòng 3 ngày)
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

            if (returnReq.Status != ReturnRequestStatus.SHOP_REJECTED && returnReq.Status != ReturnRequestStatus.REJECTED)
            {
                throw new InvalidOperationException("Chỉ có thể gửi khiếu nại lên Admin khi yêu cầu trả hàng bị Shop từ chối.");
            }

            if (returnReq.ShopRespondedAt.HasValue && DateTimeOffset.UtcNow - returnReq.ShopRespondedAt.Value > TimeSpan.FromDays(3))
            {
                throw new InvalidOperationException("Đã quá thời hạn 3 ngày kể từ khi Shop từ chối. Bạn không thể gửi khiếu nại lên Admin.");
            }

            returnReq.Status = ReturnRequestStatus.ESCALATED;
            returnReq.EscalatedAt = DateTimeOffset.UtcNow;
            returnReq.DetailedReason = (returnReq.DetailedReason ?? "") + $" | [KHIẾU NẠI ADMIN: {reason}]";
            returnReq.UpdatedAt = DateTimeOffset.UtcNow;

            var notification = new BookManagement.Repository.Entities.Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = NotificationType.SYSTEM,
                ReferenceId = returnReq.Id,
                Content = $"Yêu cầu khiếu nại của bạn cho sản phẩm đã được gửi lên Ban quản trị (Admin) xử lý trong vòng 1 ngày.",
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
                OrderId = d.OrderId,
                TrackingNumber = d.TrackingNumber,
                CarrierName = d.CarrierName,
                ShipFee = d.ShipFee,
                Status = d.Status.ToString(),
                EstimatedDelivery = d.EstimatedDelivery,
                ActualDeliveredAt = d.ActualDeliveredAt
            }).ToList()
        };
    }
}
