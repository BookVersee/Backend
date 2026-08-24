using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace BookManagement.Service.Services;

public class VnpayService
{
    private readonly IConfiguration _configuration;

    public VnpayService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string CreatePaymentUrl(int paymentId, decimal amount, string ipAddress, string? bankCode = null)
    {
        var vnpUrl = _configuration["VnPay:BaseUrl"] ?? _configuration["Vnpay:Url"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
        var tmnCode = _configuration["VnPay:TmnCode"] ?? _configuration["Vnpay:TmnCode"] ?? "DEMO_TMN_CODE";
        var hashSecret = _configuration["VnPay:HashSecret"] ?? _configuration["Vnpay:HashSecret"] ?? "DEMO_HASH_SECRET";
        var returnUrl = _configuration["VnPay:ReturnUrl"] ?? _configuration["Vnpay:ReturnUrl"] ?? "https://localhost:7000/api/payment/vnpay/callback";

        var vnpayParams = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            { "vnp_Version", "2.1.0" },
            { "vnp_Command", "pay" },
            { "vnp_TmnCode", tmnCode },
            { "vnp_Amount", ((long)(amount * 100)).ToString() },
            { "vnp_CreateDate", DateTime.UtcNow.AddHours(7).ToString("yyyyMMddHHmmss") },
            { "vnp_CurrCode", "VND" },
            { "vnp_IpAddr", string.IsNullOrEmpty(ipAddress) ? "127.0.0.1" : ipAddress },
            { "vnp_Locale", "vn" },
            { "vnp_OrderInfo", $"Payment for Order PaymentId #{paymentId}" },
            { "vnp_OrderType", "other" },
            { "vnp_ReturnUrl", returnUrl },
            { "vnp_TxnRef", paymentId.ToString() }
        };

        if (!string.IsNullOrEmpty(bankCode))
        {
            vnpayParams.Add("vnp_BankCode", bankCode);
        }

        var dataBuilder = new StringBuilder();
        var queryBuilder = new StringBuilder();

        foreach (var kv in vnpayParams)
        {
            if (dataBuilder.Length > 0)
            {
                dataBuilder.Append('&');
                queryBuilder.Append('&');
            }

            dataBuilder.Append(kv.Key).Append('=').Append(Uri.EscapeDataString(kv.Value));
            queryBuilder.Append(kv.Key).Append('=').Append(Uri.EscapeDataString(kv.Value));
        }

        var secureHash = HmacSha512(hashSecret, dataBuilder.ToString());
        queryBuilder.Append("&vnp_SecureHash=").Append(secureHash);

        return $"{vnpUrl}?{queryBuilder}";
    }

    public bool ValidateSignature(IDictionary<string, string> queryParams)
    {
        var hashSecret = _configuration["VnPay:HashSecret"] ?? _configuration["Vnpay:HashSecret"] ?? "DEMO_HASH_SECRET";
        if (!queryParams.TryGetValue("vnp_SecureHash", out var vnpSecureHash))
        {
            return false;
        }

        var sortedParams = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var kv in queryParams)
        {
            if (!kv.Key.Equals("vnp_SecureHash", StringComparison.OrdinalIgnoreCase) &&
                !kv.Key.Equals("vnp_SecureHashType", StringComparison.OrdinalIgnoreCase))
            {
                sortedParams.Add(kv.Key, kv.Value);
            }
        }

        var dataBuilder = new StringBuilder();
        foreach (var kv in sortedParams)
        {
            if (dataBuilder.Length > 0)
            {
                dataBuilder.Append('&');
            }

            dataBuilder.Append(kv.Key).Append('=').Append(Uri.EscapeDataString(kv.Value));
        }

        var calculatedHash = HmacSha512(hashSecret, dataBuilder.ToString());
        return calculatedHash.Equals(vnpSecureHash, StringComparison.OrdinalIgnoreCase);
    }

    public async Task<bool> ProcessRefundAsync(int paymentId, decimal refundAmount, string transactionNo, string createdBy)
    {
        await Task.Delay(100);
        return true;
    }

    private static string HmacSha512(string key, string inputData)
    {
        var hash = new StringBuilder();
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var inputBytes = Encoding.UTF8.GetBytes(inputData);
        using var hmac = new HMACSHA512(keyBytes);
        var hashValue = hmac.ComputeHash(inputBytes);
        foreach (var theByte in hashValue)
        {
            hash.Append(theByte.ToString("x2"));
        }
        return hash.ToString();
    }
}
