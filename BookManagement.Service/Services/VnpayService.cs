using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
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
        var vnpUrl = _configuration["VnPay:BaseUrl"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
        var tmnCode = _configuration["VnPay:TmnCode"] ?? "CGXZLS0Z";
        var hashSecret = _configuration["VnPay:HashSecret"] ?? "XNBCJFAKAZQSGTARRLRAZSMHKGVAENMT";
        var returnUrl = _configuration["VnPay:ReturnUrl"] ?? "http://localhost:5226/api/payment/vnpay/callback";

        var clientIp = string.IsNullOrEmpty(ipAddress) || ipAddress == "::1" ? "127.0.0.1" : ipAddress;

        var vnpay = new VnPayLibrary();

        vnpay.AddRequestData("vnp_Version", "2.1.0");
        vnpay.AddRequestData("vnp_Command", "pay");
        vnpay.AddRequestData("vnp_TmnCode", tmnCode);
        vnpay.AddRequestData("vnp_Amount", ((long)(amount * 100)).ToString());
        vnpay.AddRequestData("vnp_CreateDate", DateTime.UtcNow.AddHours(7).ToString("yyyyMMddHHmmss"));
        vnpay.AddRequestData("vnp_CurrCode", "VND");
        vnpay.AddRequestData("vnp_IpAddr", clientIp);
        vnpay.AddRequestData("vnp_Locale", "vn");
        vnpay.AddRequestData("vnp_OrderInfo", $"Thanh toan don hang {paymentId}");
        vnpay.AddRequestData("vnp_OrderType", "other");
        vnpay.AddRequestData("vnp_ReturnUrl", returnUrl);
        vnpay.AddRequestData("vnp_TxnRef", paymentId.ToString());

        if (!string.IsNullOrEmpty(bankCode))
        {
            vnpay.AddRequestData("vnp_BankCode", bankCode);
        }

        return vnpay.CreateRequestUrl(vnpUrl, hashSecret);
    }

    public bool ValidateSignature(IDictionary<string, string> queryParams)
    {
        var hashSecret = _configuration["VnPay:HashSecret"] ?? "XNBCJFAKAZQSGTARRLRAZSMHKGVAENMT";
        if (!queryParams.TryGetValue("vnp_SecureHash", out var vnpSecureHash))
        {
            return false;
        }

        var vnpay = new VnPayLibrary();
        foreach (var kv in queryParams)
        {
            if (!string.IsNullOrEmpty(kv.Value) &&
                !kv.Key.Equals("vnp_SecureHash", StringComparison.OrdinalIgnoreCase) &&
                !kv.Key.Equals("vnp_SecureHashType", StringComparison.OrdinalIgnoreCase))
            {
                vnpay.AddResponseData(kv.Key, kv.Value);
            }
        }

        return vnpay.ValidateSignature(vnpSecureHash, hashSecret);
    }

    public async Task<bool> ProcessRefundAsync(int paymentId, decimal refundAmount, string transactionNo, string createdBy)
    {
        await Task.Delay(100);
        return true;
    }
}

public class VnPayLibrary
{
    private readonly SortedList<string, string> _requestData = new(new VnPayCompare());
    private readonly SortedList<string, string> _responseData = new(new VnPayCompare());

    public void AddRequestData(string key, string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            _requestData.Add(key, value);
        }
    }

    public void AddResponseData(string key, string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            _responseData.Add(key, value);
        }
    }

    public string CreateRequestUrl(string baseUrl, string vnpHashSecret)
    {
        var hashData = new StringBuilder();  // for HMAC: encoded key=encoded value
        var query = new StringBuilder();     // full query string for URL

        foreach (var kv in _requestData)
        {
            if (!string.IsNullOrEmpty(kv.Value))
            {
                if (hashData.Length > 0)
                {
                    hashData.Append('&');
                    query.Append('&');
                }
                // Both hash and URL use the same URL-encoded form
                var encodedKey = WebUtility.UrlEncode(kv.Key);
                var encodedValue = WebUtility.UrlEncode(kv.Value);
                hashData.Append(encodedKey).Append('=').Append(encodedValue);
                query.Append(encodedKey).Append('=').Append(encodedValue);
            }
        }

        var vnpSecureHash = HmacSha512(vnpHashSecret, hashData.ToString());
        return $"{baseUrl}?{query}&vnp_SecureHash={vnpSecureHash}";
    }

    public bool ValidateSignature(string inputHash, string secretKey)
    {
        var hashData = new StringBuilder();
        foreach (var kv in _responseData)
        {
            if (!string.IsNullOrEmpty(kv.Value))
            {
                if (hashData.Length > 0) hashData.Append('&');
                // Hash from encoded key=encoded value (matches what VNPay sent)
                hashData.Append(WebUtility.UrlEncode(kv.Key)).Append('=').Append(WebUtility.UrlEncode(kv.Value));
            }
        }

        var checkHash = HmacSha512(secretKey, hashData.ToString());
        return checkHash.Equals(inputHash, StringComparison.OrdinalIgnoreCase);
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

public class VnPayCompare : IComparer<string>
{
    public int Compare(string? x, string? y)
    {
        if (x == y) return 0;
        if (x == null) return -1;
        if (y == null) return 1;
        var compare = CompareInfo.GetCompareInfo("en-US");
        return compare.Compare(x, y, CompareOptions.Ordinal);
    }
}