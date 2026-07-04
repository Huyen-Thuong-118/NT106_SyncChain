using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SyncChain.API.Services;

public class MoMoService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<MoMoService> _logger;

    public MoMoService(HttpClient http, IConfiguration config, ILogger<MoMoService> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
    }

    public async Task<(bool success, string payUrl, string momoOrderId, string message)>
        CreatePaymentAsync(int orderId, decimal amount, string orderInfo)
    {
        var momo = _config.GetSection("MoMo");
        var partnerCode  = momo["PartnerCode"]!;
        var accessKey    = momo["AccessKey"]!;
        var secretKey    = momo["SecretKey"]!;
        var apiUrl       = momo["ApiUrl"]!;
        var redirectUrl  = momo["RedirectUrl"]!;
        var ipnUrl       = momo["IpnUrl"]!;

        var requestId    = $"{orderId}-{Guid.NewGuid():N}";
        var momoOrderId  = $"SC-{orderId}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        var requestType  = "payWithMethod";
        var extraData    = "";

        var rawSignature =
            $"accessKey={accessKey}&amount={(long)amount}&extraData={extraData}" +
            $"&ipnUrl={ipnUrl}&orderId={momoOrderId}&orderInfo={orderInfo}" +
            $"&partnerCode={partnerCode}&redirectUrl={redirectUrl}" +
            $"&requestId={requestId}&requestType={requestType}";

        var signature = HmacSha256(secretKey, rawSignature);

        var body = new
        {
            partnerCode,
            partnerName = "SyncChain",
            storeId     = partnerCode,
            requestId,
            amount      = (long)amount,
            orderId     = momoOrderId,
            orderInfo,
            redirectUrl,
            ipnUrl,
            lang        = "vi",
            requestType,
            autoCapture = true,
            extraData,
            signature
        };

        // Canh bao som neu con dung credential mau → nguyen nhan pho bien nhat khien
        // MoMo tu choi (sai chu ky / partner khong ton tai).
        if (partnerCode.Contains("SANDBOX", StringComparison.OrdinalIgnoreCase) ||
            accessKey.Contains("SANDBOX", StringComparison.OrdinalIgnoreCase) ||
            secretKey.Contains("SANDBOX", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "[MoMo] Dang dung credential MAU (PartnerCode={PartnerCode}). MoMo se tu choi. " +
                "Hay dat MoMo:PartnerCode/AccessKey/SecretKey that (sandbox) qua .env hoac user-secrets.",
                partnerCode);
        }

        _logger.LogInformation(
            "[MoMo] Tao thanh toan: orderId={OrderId} momoOrderId={MomoOrderId} amount={Amount} apiUrl={ApiUrl} ipnUrl={IpnUrl}",
            orderId, momoOrderId, (long)amount, apiUrl, ipnUrl);

        HttpResponseMessage resp;
        string respBody;
        try
        {
            resp = await _http.PostAsync(apiUrl,
                new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));
            respBody = await resp.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MoMo] Goi API that bai (khong ket noi duoc {ApiUrl})", apiUrl);
            return (false, "", momoOrderId, $"Khong ket noi duoc MoMo: {ex.Message}");
        }

        using var doc  = JsonDocument.Parse(respBody);
        var root       = doc.RootElement;
        var resultCode = root.TryGetProperty("resultCode", out var rc) ? rc.GetInt32() : -1;
        var payUrl     = root.TryGetProperty("payUrl",    out var pu) ? pu.GetString() ?? "" : "";
        var msg        = root.TryGetProperty("message",   out var m)  ? m.GetString()  ?? "" : "Unknown";

        if (resultCode == 0)
        {
            _logger.LogInformation(
                "[MoMo] Tao link OK: momoOrderId={MomoOrderId} resultCode=0 payUrl={PayUrl}",
                momoOrderId, payUrl);
        }
        else
        {
            // Log ca resultCode + message + raw response de chan doan chinh xac (khong sua mu).
            _logger.LogWarning(
                "[MoMo] Tao link THAT BAI: momoOrderId={MomoOrderId} resultCode={ResultCode} message={Message} raw={Raw}",
                momoOrderId, resultCode, msg, respBody);
        }

        return (resultCode == 0, payUrl, momoOrderId, msg);
    }

    public bool ValidateCallback(JsonElement body)
    {
        var momo      = _config.GetSection("MoMo");
        var secretKey = momo["SecretKey"]!;
        var accessKey = momo["AccessKey"]!;

        string Get(string key) => body.TryGetProperty(key, out var p) ? p.GetString() ?? "" : "";

        var partnerCode   = Get("partnerCode");
        var momoOrderId   = Get("orderId");
        var requestId     = Get("requestId");
        var amount        = Get("amount");
        var orderInfo     = Get("orderInfo");
        var orderType     = Get("orderType");
        var transId       = Get("transId");
        var resultCode    = Get("resultCode");
        var message       = Get("message");
        var payType       = Get("payType");
        var responseTime  = Get("responseTime");
        var extraData     = Get("extraData");
        var receivedSig   = Get("signature");

        var rawSignature =
            $"accessKey={accessKey}&amount={amount}&extraData={extraData}" +
            $"&message={message}&orderId={momoOrderId}&orderInfo={orderInfo}" +
            $"&orderType={orderType}&partnerCode={partnerCode}&payType={payType}" +
            $"&requestId={requestId}&responseTime={responseTime}" +
            $"&resultCode={resultCode}&transId={transId}";

        var expectedSig = HmacSha256(secretKey, rawSignature);
        var valid = string.Equals(expectedSig, receivedSig, StringComparison.OrdinalIgnoreCase);
        if (!valid)
        {
            _logger.LogWarning(
                "[MoMo] Callback CHU KY KHONG HOP LE: momoOrderId={MomoOrderId} resultCode={ResultCode}. " +
                "Kiem tra SecretKey va thu tu field ky.",
                momoOrderId, resultCode);
        }
        else
        {
            _logger.LogInformation(
                "[MoMo] Callback hop le: momoOrderId={MomoOrderId} resultCode={ResultCode}",
                momoOrderId, resultCode);
        }

        return valid;
    }

    public (string momoOrderId, bool success) ParseCallback(JsonElement body)
    {
        string Get(string key) => body.TryGetProperty(key, out var p) ? p.GetString() ?? "" : "";
        var momoOrderId = Get("orderId");
        var resultCode  = body.TryGetProperty("resultCode", out var rc) ? rc.GetInt32() : -1;
        return (momoOrderId, resultCode == 0);
    }

    private static string HmacSha256(string key, string data)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        return BitConverter.ToString(hmac.ComputeHash(Encoding.UTF8.GetBytes(data)))
                           .Replace("-", "").ToLowerInvariant();
    }
}
