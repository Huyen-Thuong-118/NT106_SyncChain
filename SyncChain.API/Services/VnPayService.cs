using System.Security.Cryptography;
using System.Text;

namespace SyncChain.API.Services;

public class VnPayService
{
    private readonly IConfiguration _config;

    public VnPayService(IConfiguration config) => _config = config;

    public (string url, string txnRef) CreatePaymentUrl(int orderId, decimal amount, string orderInfo, string ipAddress)
    {
        var vnp = _config.GetSection("VnPay");
        var tmnCode = vnp["TmnCode"]!;
        var hashSecret = vnp["HashSecret"]!;
        var baseUrl = vnp["BaseUrl"]!;
        var returnUrl = vnp["ReturnUrl"]!;

        var txnRef = $"{orderId}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        var now = DateTime.Now; 

        var @params = new SortedDictionary<string, string>
        {
            ["vnp_Version"]    = "2.1.0",
            ["vnp_Command"]    = "pay",
            ["vnp_TmnCode"]    = tmnCode,
            ["vnp_Amount"]     = ((long)(amount * 100)).ToString(),
            ["vnp_CreateDate"] = now.ToString("yyyyMMddHHmmss"),
            ["vnp_CurrCode"]   = "VND",
            ["vnp_IpAddr"]     = ipAddress,
            ["vnp_Locale"]     = "vn",
            ["vnp_OrderInfo"]  = orderInfo,
            ["vnp_OrderType"]  = "other",
            ["vnp_ReturnUrl"]  = returnUrl,
            ["vnp_TxnRef"]     = txnRef,
            ["vnp_ExpireDate"] = now.AddMinutes(15).ToString("yyyyMMddHHmmss")
        };

        var query = string.Join("&", @params.Select(kv =>
            $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
        var hash = HmacSha512(hashSecret, query);

        return ($"{baseUrl}?{query}&vnp_SecureHash={hash}", txnRef);
    }

    public bool ValidateCallback(IQueryCollection query)
    {
        var hashSecret = _config.GetSection("VnPay")["HashSecret"]!;
        var receivedHash = query["vnp_SecureHash"].ToString();

        var raw = string.Join("&", query
            .Where(kv => kv.Key.StartsWith("vnp_") &&
                         kv.Key != "vnp_SecureHash" &&
                         kv.Key != "vnp_SecureHashType")
            .OrderBy(kv => kv.Key)
            .Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value.ToString())}"));

        return string.Equals(HmacSha512(hashSecret, raw), receivedHash,
                             StringComparison.OrdinalIgnoreCase);
    }

    public (string txnRef, bool success, string responseCode) ParseCallback(IQueryCollection query)
    {
        var txnRef      = query["vnp_TxnRef"].ToString();
        var responseCode = query["vnp_ResponseCode"].ToString();
        return (txnRef, responseCode == "00", responseCode);
    }

    private static string HmacSha512(string key, string data)
    {
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
        return BitConverter.ToString(hmac.ComputeHash(Encoding.UTF8.GetBytes(data)))
                           .Replace("-", "").ToLowerInvariant();
    }
}
