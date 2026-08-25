using System.Text.Json.Serialization;

namespace BookManagement.Service.Payment;

public class VnpayCallbackResponse
{
    [JsonPropertyName("rsp_code")]
    public string RspCode { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}
