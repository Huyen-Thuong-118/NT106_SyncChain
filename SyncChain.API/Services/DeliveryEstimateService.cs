using SyncChain.API.DTOs.Shipping;

namespace SyncChain.API.Services;

public class DeliveryEstimateService
{
    private const decimal DailyShippingFee = 10_000m;
    private const decimal FreeShippingThreshold = 500_000m;

    private static readonly HashSet<string> MajorCities = new(StringComparer.OrdinalIgnoreCase)
    {
        "TP.HCM", "Hồ Chí Minh", "Ho Chi Minh", "Hà Nội", "Ha Noi",
        "Đà Nẵng", "Da Nang", "Hải Phòng", "Hai Phong", "Cần Thơ", "Can Tho"
    };

    private static readonly HashSet<string> IslandKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "phú quốc", "côn đảo", "lý sơn", "cô tô", "cát hải", "kiên hải", "vân đồn"
    };

    private static readonly HashSet<string> MountainProvinces = new(StringComparer.OrdinalIgnoreCase)
    {
        "Hà Giang", "Cao Bằng", "Lào Cai", "Lai Châu", "Điện Biên", "Sơn La",
        "Yên Bái", "Bắc Kạn", "Tuyên Quang", "Lạng Sơn", "Kon Tum", "Gia Lai", "Đắk Nông"
    };

    public DeliveryEstimateResponseDTO Estimate(EstimateDeliveryDTO dto)
    {
        var province = dto.Province?.Trim() ?? string.Empty;
        var ward = dto.Ward?.Trim() ?? string.Empty;
        var normalizedAddress = $"{ward}, {province}".Trim(' ', ',');
        var area = ClassifyArea(province, ward);
        var distance = EstimateDistance(province, area);
        var warehouseDelay = dto.OrderDate.TimeOfDay >= TimeSpan.FromHours(12) ? 0.5 : 0;
        var (minTransit, maxTransit) = TransitDays(dto.ServiceType, area, distance, dto.WeightKg);
        var start = dto.OrderDate.Date.AddDays(warehouseDelay);
        var minimumDeliveryDays = Math.Max(1, (int)Math.Ceiling(minTransit));
        var deliveryDays = Math.Max(1, (int)Math.Ceiling(maxTransit));
        var earliest = AddBusinessDays(start, minimumDeliveryDays);
        var latest = AddBusinessDays(start, deliveryDays);
        var shippingFee = dto.OrderTotal >= FreeShippingThreshold
            ? 0
            : deliveryDays * DailyShippingFee;
        var confidence = area switch
        {
            "Hải đảo" => 68,
            "Vùng núi" => 72,
            "Huyện nông thôn" => 78,
            _ => 86
        };

        return new DeliveryEstimateResponseDTO
        {
            Address = normalizedAddress,
            EarliestDelivery = earliest,
            LatestDelivery = latest,
            DeliveryDays = deliveryDays,
            ShippingFee = shippingFee,
            ConfidencePercent = confidence,
            EstimatedDistanceKm = distance,
            AreaType = area,
            WarehouseProcessing = warehouseDelay == 0
                ? "Đặt trước 12h: xử lý trong ngày"
                : "Đặt sau 12h: cộng 0,5 ngày xử lý",
            TransitTime = $"{minTransit:0.#}–{maxTransit:0.#} ngày làm việc",
            Factors = "Có xét cuối tuần; ngày lễ và thời tiết cần được kiểm tra lại tại thời điểm giao thực tế.",
            Assumption = string.IsNullOrWhiteSpace(ward)
                ? "Không có phường/xã chính xác, ước tính theo trung tâm tỉnh/thành."
                : "Phân loại theo tên tỉnh/thành và từ khóa khu vực; chưa kết nối bản đồ hoặc hãng vận chuyển."
        };
    }

    public bool SupportsExpress(string? province, string? ward)
    {
        var provinceName = province?.Trim() ?? string.Empty;
        var wardName = ward?.Trim() ?? string.Empty;
        return provinceName.Contains("Hồ Chí Minh", StringComparison.OrdinalIgnoreCase) &&
               ClassifyArea(provinceName, wardName) == "Nội thành đô thị lớn";
    }

    private static string ClassifyArea(string province, string ward)
    {
        var address = $"{ward} {province}".ToLowerInvariant();
        if (IslandKeywords.Any(address.Contains)) return "Hải đảo";
        if (MountainProvinces.Contains(province)) return "Vùng núi";
        if (IsMajorCity(province))
            return address.Contains("huyện") || address.Contains("xã") ? "Ngoại thành" : "Nội thành đô thị lớn";
        return address.Contains("thành phố") || address.Contains("phường")
            ? "Ngoại thành"
            : "Huyện nông thôn";
    }

    private static int EstimateDistance(string province, string area)
    {
        if (IsHoChiMinhCity(province))
            return area == "Nội thành đô thị lớn" ? 20 : 55;
        if (province.Contains("Hà Nội", StringComparison.OrdinalIgnoreCase)) return 1710;
        if (province.Contains("Đà Nẵng", StringComparison.OrdinalIgnoreCase)) return 960;
        if (province.Contains("Cần Thơ", StringComparison.OrdinalIgnoreCase)) return 170;
        if (province.Contains("Hải Phòng", StringComparison.OrdinalIgnoreCase)) return 1770;
        return area switch
        {
            "Hải đảo" => 650,
            "Vùng núi" => 1450,
            "Huyện nông thôn" => 550,
            _ => 400
        };
    }

    private static bool IsMajorCity(string province) =>
        MajorCities.Contains(province) ||
        IsHoChiMinhCity(province) ||
        province.Contains("Hà Nội", StringComparison.OrdinalIgnoreCase) ||
        province.Contains("Đà Nẵng", StringComparison.OrdinalIgnoreCase) ||
        province.Contains("Hải Phòng", StringComparison.OrdinalIgnoreCase) ||
        province.Contains("Cần Thơ", StringComparison.OrdinalIgnoreCase);

    private static bool IsHoChiMinhCity(string province) =>
        province.Contains("Hồ Chí Minh", StringComparison.OrdinalIgnoreCase) ||
        province.Contains("Ho Chi Minh", StringComparison.OrdinalIgnoreCase) ||
        province.Contains("TP.HCM", StringComparison.OrdinalIgnoreCase);

    private static (double Min, double Max) TransitDays(
        string? service, string area, int distance, decimal weight)
    {
        var normalized = service?.Trim().ToLowerInvariant() ?? string.Empty;
        var baseDays = normalized switch
        {
            "hoa toc" or "hỏa tốc" => distance <= 80 ? (0.5, 1.0) : (1.0, 2.0),
            "nhanh" => distance <= 300 ? (1.0, 2.0) : (2.0, 3.0),
            _ => distance <= 300 ? (2.0, 3.0) : (3.0, 5.0)
        };
        var areaExtra = area switch
        {
            "Hải đảo" => 2.0,
            "Vùng núi" => 1.5,
            "Huyện nông thôn" => 1.0,
            "Ngoại thành" => 0.5,
            _ => 0
        };
        var weightExtra = weight > 20 ? 1.0 : weight > 5 ? 0.5 : 0;
        return (baseDays.Item1 + areaExtra + weightExtra, baseDays.Item2 + areaExtra + weightExtra);
    }

    private static DateTime AddBusinessDays(DateTime date, int days)
    {
        var result = date;
        var added = 0;
        while (added < days)
        {
            result = result.AddDays(1);
            if (result.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
                added++;
        }
        return DateTime.SpecifyKind(result, DateTimeKind.Utc);
    }
}
