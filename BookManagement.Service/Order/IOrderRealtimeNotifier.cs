using System;
using System.Threading.Tasks;

namespace BookManagement.Service.Order;

public interface IOrderRealtimeNotifier
{
    Task SendNewOrderAlertAsync(Guid shopId, OrderResponse order);
    Task SendOrderStatusChangedAsync(Guid userId, Guid orderId, string newStatus, string message);
}
