using Microsoft.Maui.Graphics;

namespace SyncChain.Desktop.Models;

// ═══════════════════════════════════════════════════════════
//  Model API cho module khách hàng (giỏ/địa chỉ/thanh toán/tracking/thông báo)
//  — bổ sung những model mà AppModels của main chưa có.
// ═══════════════════════════════════════════════════════════

/// <summary>Địa chỉ từ API /api/address</summary>
public sealed class DiaChiApi
{
	public int MaDiaChi { get; set; }
	public string TenNguoiNhan { get; set; } = string.Empty;
	public string SoDienThoai { get; set; } = string.Empty;
	public string TinhThanh { get; set; } = string.Empty;
	public string QuanHuyen { get; set; } = string.Empty;
	public string PhuongXa { get; set; } = string.Empty;
	public string DiaChiChiTiet { get; set; } = string.Empty;
	public bool LaMacDinh { get; set; }
	public string DiaChi { get; set; } = string.Empty;
}

/// <summary>Giỏ hàng từ API /api/cart</summary>
public sealed class CartApi
{
	public int MaGioHang { get; set; }
	public List<CartItemApi> Items { get; set; } = new();
	public decimal TongTien { get; set; }
	public int SoLuong { get; set; }
}

public sealed class CartItemApi
{
	public int MaChiTietGio { get; set; }
	public int MaSanPham { get; set; }
	public string TenSanPham { get; set; } = string.Empty;
	public decimal GiaBan { get; set; }
	public string HinhAnhUrl { get; set; } = string.Empty;
	public int SoLuong { get; set; }
	public decimal ThanhTien { get; set; }
}

/// <summary>Bước timeline từ API /api/order/{id}/tracking</summary>
public sealed class TrackingTimelineStep
{
	public string Step { get; set; } = string.Empty;
	public string TrangThai { get; set; } = string.Empty; // hoanThanh | hienTai | choDoi | huyBo
	public Color Color => TrangThai switch
	{
		"hoanThanh" => Color.FromArgb("#22c55e"),
		"hienTai"   => Color.FromArgb("#3b82f6"),
		"huyBo"     => Color.FromArgb("#ef4444"),
		_           => Color.FromArgb("#9ca3af")
	};
	public string Label => Step switch
	{
		"pending"    => "Chờ xử lý",
		"processing" => "Đang xử lý",
		"shipping"   => "Đang giao",
		"done"       => "Hoàn tất",
		"cancel"     => "Đã hủy",
		_            => Step
	};
}

/// <summary>Thông tin thanh toán từ tracking endpoint</summary>
public sealed class ThanhToanInfo
{
	public int MaThanhToan { get; set; }
	public string PhuongThuc { get; set; } = string.Empty;
	public string TrangThaiThanhToan { get; set; } = string.Empty;
	public decimal SoTien { get; set; }
	public DateTime NgayTao { get; set; }
	public DateTime? NgayCapNhat { get; set; }
}

/// <summary>Response từ GET /api/order/{id}/tracking</summary>
public sealed class OrderTrackingResponse
{
	public DonHangTrackingApi Order { get; set; } = new();
	public ThanhToanInfo? Payment { get; set; }
	public List<ChiTietDonHangApi> ChiTiet { get; set; } = new();
	public List<TrackingTimelineStep> Timeline { get; set; } = new();
}

public sealed class DonHangTrackingApi
{
	public int MaDonHang { get; set; }
	public int MaNguoiDung { get; set; }
	public decimal TongTien { get; set; }
	public string TrangThai { get; set; } = "pending";
	public DateTime NgayTao { get; set; }
	public string? PhuongThucThanhToan { get; set; }
	public string? NguoiNhan { get; set; }
	public string? SoDienThoaiNhan { get; set; }
	public string? DiaChiGiao { get; set; }
}

/// <summary>Response từ POST /api/payment/initiate</summary>
public sealed class PaymentInitResponse
{
	public string? PaymentUrl { get; set; }
	public string PhuongThuc { get; set; } = string.Empty;
	public string? Message { get; set; }
}

/// <summary>Thông báo từ API /api/notification</summary>
public sealed class ThongBaoApi
{
	public int MaThongBao { get; set; }
	public string LoaiThongBao { get; set; } = string.Empty;
	public string TieuDe { get; set; } = string.Empty;
	public string NoiDung { get; set; } = string.Empty;
	public bool DaDoc { get; set; }
	public DateTime NgayTao { get; set; }
	public int? MaDonHang { get; set; }
	public Color BackgroundColor => DaDoc
		? Color.FromArgb("#f9fafb")
		: Color.FromArgb("#eff6ff");
}
