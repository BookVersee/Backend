using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookManagement.Service.Payment;

public interface IPaymentService
{
    Task<string> CreateVnpayUrlAsync(int userId, CreateVnpayUrlRequest dto, string ipAddress);
    Task<(string RspCode, string Message)> ProcessVnpayIpnAsync(IDictionary<string, string> queryParams);
    Task ProcessVnpayRefundAsync(int shopId, VnpayRefundRequest dto);
}
