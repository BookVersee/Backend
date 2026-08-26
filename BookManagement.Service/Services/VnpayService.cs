using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace BookManagement.Service.Services;

public class VnpayService
{
    private readonly IConfiguration _config;

    public VnpayService(IConfiguration config)
    {
        _config = config;
    }

    public string CreatePaymentUrl(Guid paymentId, decimal amount, string ipAddress, string? bankCode = null)
    {
        var vnpay = new VnPayLibrary();
        var tmnCode = _config["VnPay:TmnCode"];
        var hashSecret = _config["VnPay:HashSecret"];
        var baseUrl = _config["VnPay:BaseUrl"];
        var returnUrl = _config["VnPay:ReturnUrl"];

        vnpay.AddRequestData("vnp_Version", "2.1.0");
        vnpay.AddRequestData("vnp_Command", "pay");
        vnpay.AddRequestData("vnp_TmnCode", tmnCode!);
        vnpay.AddRequestData("vnp_Amount", ((long)(amount * 100)).ToString());

        if (!string.IsNullOrEmpty(bankCode))
        {
            vnpay.AddRequestData("vnp_BankCode", bankCode);
        }

        vnpay.AddRequestData("vnp_CreateDate", DateTime.UtcNow.AddHours(7).ToString("yyyyMMddHHmmss"));
        vnpay.AddRequestData("vnp_CurrCode", "VND");
        vnpay.AddRequestData("vnp_IpAddr", string.IsNullOrEmpty(ipAddress) ? "127.0.0.1" : ipAddress);
        vnpay.AddRequestData("vnp_Locale", "vn");
        vnpay.AddRequestData("vnp_OrderInfo", $"Payment for order with paymentId {paymentId}");
        vnpay.AddRequestData("vnp_OrderType", "other");
        vnpay.AddRequestData("vnp_ReturnUrl", returnUrl!);
        vnpay.AddRequestData("vnp_TxnRef", paymentId.ToString());

        return vnpay.CreateRequestUrl(baseUrl!, hashSecret!);
    }

    public bool ValidateSignature(IDictionary<string, string> queryParams)
    {
        var vnpay = new VnPayLibrary();
        var hashSecret = _config["VnPay:HashSecret"];

        foreach (var (key, value) in queryParams)
        {
            if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_") && key != "vnp_SecureHash")
            {
                vnpay.AddResponseData(key, value);
            }
        }

        var secureHash = queryParams.TryGetValue("vnp_SecureHash", out var hash) ? hash : string.Empty;
        return vnpay.ValidateSignature(secureHash, hashSecret!);
    }

    public async Task<bool> ProcessRefundAsync(Guid returnRequestId, decimal refundAmount, string transactionNo, string createdBy)
    {
        // Mock VNPAY Refund API call
        await Task.Delay(100);
        return true;
    }
}

internal class VnPayLibrary
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
        var data = new StringBuilder();
        foreach (var (key, value) in _requestData)
        {
            if (!string.IsNullOrEmpty(value))
            {
                data.Append(Uri.EscapeDataString(key) + "=" + Uri.EscapeDataString(value) + "&");
            }
        }

        string queryString = data.ToString();

        if (queryString.Length > 0)
        {
            queryString = queryString.Remove(queryString.Length - 1, 1);
        }

        string vnpSecureHash = HmacSHA512(vnpHashSecret, queryString);
        return $"{baseUrl}?{queryString}&vnp_SecureHash={vnpSecureHash}";
    }

    public bool ValidateSignature(string inputHash, string secretKey)
    {
        var data = new StringBuilder();
        foreach (var (key, value) in _responseData)
        {
            if (!string.IsNullOrEmpty(value))
            {
                data.Append(Uri.EscapeDataString(key) + "=" + Uri.EscapeDataString(value) + "&");
            }
        }

        string rawData = data.ToString();
        if (rawData.Length > 0)
        {
            rawData = rawData.Remove(rawData.Length - 1, 1);
        }

        string myChecksum = HmacSHA512(secretKey, rawData);
        return myChecksum.Equals(inputHash, StringComparison.OrdinalIgnoreCase);
    }

    private static string HmacSHA512(string key, string inputData)
    {
        var hash = new StringBuilder();
        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
        using (var hmac = new HMACSHA512(keyBytes))
        {
            byte[] hashValue = hmac.ComputeHash(inputBytes);
            foreach (byte theByte in hashValue)
            {
                hash.Append(theByte.ToString("x2"));
            }
        }
        return hash.ToString();
    }
}

internal class VnPayCompare : IComparer<string>
{
    public int Compare(string? x, string? y)
    {
        if (x == y) return 0;
        if (x == null) return -1;
        if (y == null) return 1;
        var vnpCompare = CultureInfo.InvariantCulture.CompareInfo;
        return vnpCompare.Compare(x, y, CompareOptions.Ordinal);
    }
}