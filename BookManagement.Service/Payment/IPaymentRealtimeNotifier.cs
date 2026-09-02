using System;
using System.Threading.Tasks;

namespace BookManagement.Service.Payment;

public interface IPaymentRealtimeNotifier
{
    Task SendPaymentResultAsync(string orderId, bool isSuccess, string message, string? transactionCode = null);
}
