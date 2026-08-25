using System.Text.Json.Serialization;

namespace BookManagement.Service.Payment;

public class CreateVnpayUrlRequest
{
    [JsonPropertyName("order_id")]
    public int OrderId { get; set; }

    [JsonPropertyName("bank_code")]
    public string? BankCode { get; set; }
}

public class VnpayRefundRequest
{
    [JsonPropertyName("return_request_id")]
    public int ReturnRequestId { get; set; }

    [JsonPropertyName("order_id")]
    public int OrderId { get; set; }
}
