using System.Threading.Tasks;

namespace BookManagement.Service.Shipping;

public interface IShippingService
{
    Task<BookStore.BE2.Domain.Entities.Delivery> CreateGhnOrderAsync(int shopId, CreateGhnOrderRequest dto);
    Task ProcessGhnWebhookAsync(GhnWebhookRequest payload);
}
