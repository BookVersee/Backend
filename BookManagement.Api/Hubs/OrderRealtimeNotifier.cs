using System;
using System.Threading.Tasks;
using BookManagement.Service.Order;
using Microsoft.AspNetCore.SignalR;

namespace BookManagement.Api.Hubs;

/// <summary>
/// Infrastructure Notifier: Bắn tín hiệu Realtime đơn hàng mới và cập nhật trạng thái đơn qua AppHub.
/// </summary>
public class OrderRealtimeNotifier : IOrderRealtimeNotifier
{
    private readonly IHubContext<AppHub> _hubContext;

    public OrderRealtimeNotifier(IHubContext<AppHub> hubContext)
    {
        _hubContext = hubContext;
    }

    /// <summary>
    /// Phát tín hiệu có đơn hàng mới cho Shop Dashboard
    /// </summary>
    public async Task SendNewOrderAlertAsync(Guid shopId, OrderResponse order)
    {
        await _hubContext.Clients.Group($"shop_{shopId}").SendAsync("NewOrderAlert", order);
        await _hubContext.Clients.User(shopId.ToString()).SendAsync("NewOrderAlert", order);
    }

    /// <summary>
    /// Phát tín hiệu cập nhật trạng thái đơn hàng & vận chuyển cho Khách hàng
    /// </summary>
    public async Task SendOrderStatusChangedAsync(Guid userId, Guid orderId, string newStatus, string message)
    {
        var payload = new
        {
            orderId = orderId,
            newStatus = newStatus,
            message = message,
            updatedAt = DateTimeOffset.UtcNow
        };

        await _hubContext.Clients.User(userId.ToString()).SendAsync("OrderStatusUpdated", payload);
        await _hubContext.Clients.Group($"user_{userId}").SendAsync("OrderStatusUpdated", payload);
        await _hubContext.Clients.Group($"order_{orderId}").SendAsync("OrderStatusUpdated", payload);
    }
}
