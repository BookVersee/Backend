using System.Threading.Tasks;
using BookManagement.Service.Payment;
using Microsoft.AspNetCore.SignalR;

namespace BookManagement.Api.Hubs;

/// <summary>
/// Infrastructure Notifier: Bắn tín hiệu kết quả giao dịch thanh toán MoMo / VNPay / QR qua AppHub.
/// </summary>
public class PaymentRealtimeNotifier : IPaymentRealtimeNotifier
{
    private readonly IHubContext<AppHub> _hubContext;

    public PaymentRealtimeNotifier(IHubContext<AppHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendPaymentResultAsync(string orderId, bool isSuccess, string message, string? transactionCode = null)
    {
        var payload = new
        {
            orderId = orderId,
            isSuccess = isSuccess,
            message = message,
            transactionCode = transactionCode
        };

        // Bắn trực tiếp vào Group theo dõi đơn hàng order_{orderId}
        await _hubContext.Clients.Group($"order_{orderId}").SendAsync("PaymentResult", payload);
    }
}
