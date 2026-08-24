using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using BookManagement.Service.Dtos.Payment;
using BookManagement.Service.Models;

namespace BookManagement.Service.Interfaces
{
    public interface IPaymentService
    {
        Task<ApiResponse<string>> CreateVnPayPaymentUrlAsync(CreatePaymentUrlRequestDto dto, HttpContext httpContext);
        Task<VnPayIpnResponseDto> ProcessVnPayIpnAsync(IQueryCollection queryParams);
        Task<ApiResponse<PaymentDto>> GetPaymentByOrderIdAsync(Guid orderId);
        Task<ApiResponse<IEnumerable<PaymentDto>>> GetPaymentsAsync();
    }
}
