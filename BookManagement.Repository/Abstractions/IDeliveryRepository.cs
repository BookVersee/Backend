using BookManagement.Repository.Entities;

namespace BookManagement.Repository.Abstractions;

public interface IDeliveryRepository
{
    Task<Delivery?> GetByIdAsync(Guid deliveryId);
    Task<Delivery?> GetByOrderIdAsync(Guid orderId);
    Task<IEnumerable<Delivery>> GetByUserIdAsync(Guid userId);
    Task AddAsync(Delivery delivery);
    Task UpdateAsync(Delivery delivery);
    Task<bool> ExistsByTrackingNumberAsync(string trackingNumber);
}
