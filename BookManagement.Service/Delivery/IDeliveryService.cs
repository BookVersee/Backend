using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookManagement.Service.Delivery
{
    public interface IDeliveryService
    {
        Task<BookManagement.Repository.Entities.Delivery> CreateDeliveryAsync(CreateDeliveryDto dto);
        Task<DeliveryManifestDetailDto> GetDeliveryManifestDetailAsync(Guid deliveryId);
        Task UpdateDeliveryStatusAsync(Guid deliveryId, UpdateDeliveryStatusDto dto);
        Task<List<DeliveryManifestDetailDto>> GetDeliveryOrdersAsync(string? status);
    }
}
