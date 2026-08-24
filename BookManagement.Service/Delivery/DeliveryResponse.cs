namespace BookManagement.Service.Delivery;

public class DeliveryResponse
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string TrackingNumber { get; set; } = null!;
    public string? CarrierName { get; set; }
    public decimal ShipFee { get; set; }
    public string Status { get; set; } = null!; // PENDING, TRANSIT, DELIVERED, RETURNED
    public DateTime? EstimatedDelivery { get; set; }
    public DateTime? ActualDeliveredAt { get; set; }
}
