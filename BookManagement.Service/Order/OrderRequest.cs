using System.Text.Json.Serialization;

namespace BookManagement.Service.Order;

public class UpdateOrderStatusRequest
{
    [JsonPropertyName("new_status")]
    public string NewStatus { get; set; } = string.Empty;

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}

public class OrderQueryRequest
{
    public string? Status { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
