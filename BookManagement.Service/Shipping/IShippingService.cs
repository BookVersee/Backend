using System;
using System.Threading.Tasks;
using BookManagement.Service.Delivery;

namespace BookManagement.Service.Shipping
{
    public interface IShippingService
    {
        Task<BookManagement.Repository.Entities.Delivery> CreateGhnOrderAsync(Guid shopId, CreateGhnOrderDto dto);
        Task<BookManagement.Repository.Entities.Delivery> CreateReturnGhnOrderAsync(Guid returnRequestId);
        Task ProcessGhnWebhookAsync(GhnWebhookPayload payload);
    }
}
