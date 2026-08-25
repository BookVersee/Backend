using System.Threading.Tasks;
using BookStore.BE2.Domain.Entities;
using BookStore.BE2.Domain.Enums;

namespace BookManagement.Service.Delivery;

public interface IDeliveryService
{
    Task<BookStore.BE2.Domain.Entities.Delivery> CreateDeliveryAsync(CreateDeliveryRequest dto);
    Task<BookStore.BE2.Domain.Entities.Delivery> UpdateDeliveryAsync(int deliveryId, UpdateDeliveryRequest dto);
    Task<DeliveryManifestResponse> GetDeliveryDetailAsync(int deliveryId);
    Task UpdateDeliveryStatusAsync(int deliveryId, UpdateDeliveryStatusRequest dto);
}
