namespace BookManagement.Service.Delivery;

public class UpdateDeliveryStatusRequest
{
    public required string Status { get; set; }
}

public class CreateDeliveryRequest
{
    public Guid OrderId { get; set; }
    public required string TrackingNumber { get; set; }
    public string? CarrierName { get; set; }
    public decimal ShipFee { get; set; }
}
