using BookManagement.Repository.Abstractions;
using BookManagement.Repository.Entities.Enums;

namespace BookManagement.Service.Delivery;

public class DeliveryService : IDeliveryService
{
    private readonly IDeliveryRepository _deliveryRepository;

    public DeliveryService(IDeliveryRepository deliveryRepository)
    {
        _deliveryRepository = deliveryRepository;
    }

    public async Task<DeliveryResponse> GetDeliveryByOrderIdAsync(Guid orderId)
    {
        var delivery = await _deliveryRepository.GetByOrderIdAsync(orderId);
        if (delivery == null)
            throw new Exception("Delivery not found");
        return MapToResponse(delivery);
    }

    public async Task<IEnumerable<DeliveryResponse>> GetDeliveriesByUserAsync(Guid userId)
    {
        var deliveries = await _deliveryRepository.GetByUserIdAsync(userId);
        return deliveries.Select(MapToResponse).ToList();
    }

    public async Task<DeliveryResponse> UpdateDeliveryStatusAsync(Guid deliveryId, UpdateDeliveryStatusRequest request)
    {
        var delivery = await _deliveryRepository.GetByIdAsync(deliveryId);
        if (delivery == null)
            throw new Exception("Delivery not found");

        delivery.Status = (DeliveryStatus)Enum.Parse(typeof(DeliveryStatus), request.Status);
        await _deliveryRepository.UpdateAsync(delivery);
        return MapToResponse(delivery);
    }

    private static DeliveryResponse MapToResponse(BookManagement.Repository.Entities.Delivery delivery)
    {
        return new DeliveryResponse
        {
            Id = delivery.Id,
            OrderId = delivery.OrderId,
            TrackingNumber = delivery.TrackingNumber,
            CarrierName = delivery.CarrierName,
            ShipFee = delivery.ShipFee ?? 0m,
            Status = delivery.Status.ToString(),
            EstimatedDelivery = delivery.EstimatedDelivery,
            ActualDeliveredAt = delivery.ActualDeliveredAt
        };
    }
}
