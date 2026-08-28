using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace BookManagement.Service.Payment;

public static class MomoSecurity
{
    public static string HmacSha256(string rawData, string secretKey)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secretKey);
        var messageBytes = Encoding.UTF8.GetBytes(rawData);
        using var hmac = new HMACSHA256(keyBytes);
        var hashValue = hmac.ComputeHash(messageBytes);
        var hash = new StringBuilder();
        foreach (var b in hashValue)
        {
            hash.Append(b.ToString("x2"));
        }
        return hash.ToString();
    }
}

public class MomoIpnRequest
{
    public string PartnerCode { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public long Amount { get; set; }
    public string OrderInfo { get; set; } = string.Empty;
    public string OrderType { get; set; } = string.Empty;
    public long TransId { get; set; }
    public int ResultCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public string PayType { get; set; } = string.Empty;
    public long ResponseTime { get; set; }
    public string ExtraData { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
}

public class MomoService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public MomoService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    public async Task<(string PayUrl, string? QrCodeUrl, string? Deeplink)> CreatePaymentAsync(Guid paymentId, decimal amount, string orderInfo, string? clientRedirectUrl = null)
    {
        string endpoint = _config["Momo:ApiUrl"] ?? "https://test-payment.momo.vn/v2/gateway/api/create";
        string partnerCode = _config["Momo:PartnerCode"] ?? "MOMO";
        string accessKey = _config["Momo:AccessKey"] ?? "F8BBA842ECF85";
        string secretKey = _config["Momo:SecretKey"] ?? "K951B6PE1waDMi640xX08PD3vg6EkVlz";

        // Tạo orderId duy nhất bằng paymentId + timestamp để tránh trùng trên MoMo Sandbox
        string orderId = $"{paymentId}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        string requestId = Guid.NewGuid().ToString();
        long amountLong = (long)amount;
        string redirectUrl = clientRedirectUrl ?? _config["Momo:RedirectUrl"] ?? "http://localhost:5226/api/payment/momo/callback";
        string ipnUrl = _config["Momo:IpnUrl"] ?? "http://localhost:5226/api/payment/momo/ipn";
        string requestType = _config["Momo:RequestType"] ?? "captureWallet";
        string extraData = "";

        string rawSignature = $"accessKey={accessKey}&amount={amountLong}&extraData={extraData}&ipnUrl={ipnUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={partnerCode}&redirectUrl={redirectUrl}&requestId={requestId}&requestType={requestType}";
        string signature = MomoSecurity.HmacSha256(rawSignature, secretKey);

        var payload = new
        {
            partnerCode,
            partnerName = "BookVerse Shop",
            storeId = "BookVerseStore",
            requestId,
            amount = amountLong,
            orderId,
            orderInfo,
            redirectUrl,
            ipnUrl,
            lang = "vi",
            extraData,
            requestType,
            signature
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(endpoint, content);
        var responseContent = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(responseContent);
        var root = doc.RootElement;

        if (root.TryGetProperty("resultCode", out var resCode) && resCode.GetInt32() == 0)
        {
            string payUrl = root.TryGetProperty("payUrl", out var pUrl) ? pUrl.GetString() ?? string.Empty : string.Empty;
            string? qrCodeUrl = root.TryGetProperty("qrCodeUrl", out var qUrl) ? qUrl.GetString() : null;
            string? deeplink = root.TryGetProperty("deeplink", out var dUrl) ? dUrl.GetString() : null;
            return (payUrl, qrCodeUrl, deeplink);
        }

        string message = root.TryGetProperty("message", out var msg) ? msg.GetString() ?? responseContent : responseContent;
        throw new InvalidOperationException($"Lỗi MoMo ({resCode}): {message}");
    }

    public bool ValidateIpnSignature(MomoIpnRequest req)
    {
        string accessKey = _config["Momo:AccessKey"] ?? "F8BBA842ECF85";
        string secretKey = _config["Momo:SecretKey"] ?? "K951B6PE1waDMi640xX08PD3vg6EkVlz";

        string rawSignature = $"accessKey={accessKey}&amount={req.Amount}&extraData={req.ExtraData}&message={req.Message}&orderId={req.OrderId}&orderInfo={req.OrderInfo}&orderType={req.OrderType}&partnerCode={req.PartnerCode}&payType={req.PayType}&requestId={req.RequestId}&responseTime={req.ResponseTime}&resultCode={req.ResultCode}&transId={req.TransId}";
        string expectedSignature = MomoSecurity.HmacSha256(rawSignature, secretKey);

        // Chỉ cho phép bypass nếu cấu hình môi trường Development bật cờ AllowTestBypass
        bool allowTestBypass = _config.GetValue<bool>("Momo:AllowTestBypassSignature", false);
        if (allowTestBypass && (req.Signature == "test" || req.Signature == "TEST"))
        {
            return true;
        }
        return !string.IsNullOrEmpty(req.Signature) && req.Signature.Equals(expectedSignature, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Chủ động truy vấn trạng thái thanh toán từ MoMo API (Query Transaction Status)
    /// </summary>
    public async Task<MomoQueryResponse?> QueryPaymentStatusAsync(string orderId)
    {
        string endpoint = _config["Momo:QueryUrl"] ?? "https://test-payment.momo.vn/v2/gateway/api/query";
        string partnerCode = _config["Momo:PartnerCode"] ?? "MOMO";
        string accessKey = _config["Momo:AccessKey"] ?? "F8BBA842ECF85";
        string secretKey = _config["Momo:SecretKey"] ?? "K951B6PE1waDMi640xX08PD3vg6EkVlz";

        string requestId = Guid.NewGuid().ToString();
        string rawSignature = $"accessKey={accessKey}&orderId={orderId}&partnerCode={partnerCode}&requestId={requestId}";
        string signature = MomoSecurity.HmacSha256(rawSignature, secretKey);

        var payload = new
        {
            partnerCode,
            requestId,
            orderId,
            lang = "vi",
            signature
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(endpoint, content);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<MomoQueryResponse>(responseContent, options);
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> ProcessRefundAsync(Guid returnRequestId, decimal refundAmount, string transactionNo, string createdBy)
    {
        await Task.Delay(100);
        return true;
    }
}

public class MomoQueryResponse
{
    public string PartnerCode { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public string ExtraData { get; set; } = string.Empty;
    public long Amount { get; set; }
    public long TransId { get; set; }
    public string PayType { get; set; } = string.Empty;
    public int ResultCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public long ResponseTime { get; set; }
}

