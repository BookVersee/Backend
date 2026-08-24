namespace BookManagement.Service.Delivery;

public interface IDeliveryService
{
    Task<DeliveryResponse> GetDeliveryByOrderIdAsync(Guid orderId);
    Task<IEnumerable<DeliveryResponse>> GetDeliveriesByUserAsync(Guid userId);
    Task<DeliveryResponse> UpdateDeliveryStatusAsync(Guid deliveryId, UpdateDeliveryStatusRequest request);
}
