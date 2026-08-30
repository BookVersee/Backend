using System;
using System.Threading.Tasks;

namespace BookManagement.Service.Payment
{
    public interface IPaymentService
    {
        Task<(string PaymentUrl, string? QrCodeUrl, string? Deeplink)> CreateMomoUrlAsync(Guid userId, CreatePaymentUrlDto dto, string ipAddress);
        Task<(int ResultCode, string Message)> ProcessMomoIpnAsync(MomoIpnRequest req);
        Task ProcessRefundAsync(Guid shopId, ProcessRefundDto dto);
        Task<(bool IsPaid, string Message, string? TransactionCode)> SyncPaymentStatusAsync(Guid orderId);
        Task<int> ExpirePendingOrdersAsync(int expiryMinutes = 15);
    }
}
