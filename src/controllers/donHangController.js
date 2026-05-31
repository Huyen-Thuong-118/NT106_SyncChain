// src/controllers/donHangController.js
// Xử lý request/response cho đơn hàng; logic DB nằm ở donHangModel.
const donHangModel = require('../models/donHangModel');
const lichSuModel = require('../models/lichSuModel');
const { validateOrderTransition } = require('../middleware/validateStatusTransition');

// POST /api/donhang — khách hàng đã đăng nhập đặt hàng.
// MaKhachHang lấy từ token (không tin body) để tránh đặt hộ người khác.
async function create(req, res) {
  const { items, NguoiNhan, SoDienThoaiNhan, DiaChiGiao } = req.body;
  const MaKhachHang = req.nguoiDung.MaNguoiDung;

  if (!Array.isArray(items) || items.length === 0) {
    return res.status(400).json({ success: false, message: 'Cần mảng items không rỗng' });
  }
  for (const item of items) {
    if (!item || item.MaSanPham == null || !(item.SoLuong > 0)) {
      return res.status(400).json({ success: false, message: 'Mỗi item cần MaSanPham và SoLuong > 0' });
    }
  }

  try {
    const data = await donHangModel.taoDonHang({ MaKhachHang, items, NguoiNhan, SoDienThoaiNhan, DiaChiGiao });
    await lichSuModel.ghi(MaKhachHang, 'DAT_HANG', `Tạo đơn #${data.MaDonHang}`, req.ip);
    res.status(201).json({ success: true, data });
  } catch (err) {
    if (err.status) {
      return res.status(err.status).json({ success: false, message: err.message });
    }
    console.error('Lỗi tạo đơn hàng:', err.message);
    res.status(500).json({ success: false, message: 'Lỗi khi tạo đơn hàng' });
  }
}

// GET /api/donhang — khách chỉ thấy đơn của mình; nhân viên/admin thấy tất cả.
async function getAll(req, res) {
  try {
    const { VaiTro, MaNguoiDung } = req.nguoiDung;
    const maKhachHang = VaiTro === 'KhachHang' ? MaNguoiDung : null;
    const data = await donHangModel.layTatCa(maKhachHang);
    res.json({ success: true, data });
  } catch (err) {
    console.error('Lỗi truy vấn DonHang:', err.message);
    res.status(500).json({ success: false, message: 'Lỗi truy vấn đơn hàng' });
  }
}

// GET /api/donhang/:id
async function getById(req, res) {
  const maDonHang = Number(req.params.id);
  if (!Number.isInteger(maDonHang)) {
    return res.status(400).json({ success: false, message: 'Mã đơn hàng không hợp lệ' });
  }
  try {
    const data = await donHangModel.layTheoId(maDonHang);
    if (!data) return res.status(404).json({ success: false, message: 'Không tìm thấy đơn hàng' });
    // Khách chỉ được xem đơn của chính mình.
    const { VaiTro, MaNguoiDung } = req.nguoiDung;
    if (VaiTro === 'KhachHang' && data.MaKhachHang !== MaNguoiDung) {
      return res.status(403).json({ success: false, message: 'Bạn không có quyền xem đơn hàng này' });
    }
    res.json({ success: true, data });
  } catch (err) {
    console.error('Lỗi truy vấn chi tiết đơn hàng:', err.message);
    res.status(500).json({ success: false, message: 'Lỗi truy vấn chi tiết đơn hàng' });
  }
}

// PATCH /api/donhang/:id/trangthai — nhân viên/đại lý đẩy đơn sang trạng thái kế tiếp.
// Body: { TrangThaiMoi, MaVanDon?, DonViVanChuyen? }
async function capNhatTrangThai(req, res) {
  const maDonHang = Number(req.params.id);
  const { TrangThaiMoi, MaVanDon, DonViVanChuyen } = req.body;

  if (!Number.isInteger(maDonHang)) {
    return res.status(400).json({ success: false, message: 'Mã đơn hàng không hợp lệ' });
  }
  if (!TrangThaiMoi) {
    return res.status(400).json({ success: false, message: 'Cần TrangThaiMoi' });
  }

  try {
    const don = await donHangModel.layTheoId(maDonHang);
    if (!don) return res.status(404).json({ success: false, message: 'Không tìm thấy đơn hàng' });

    // Chặn chuyển trạng thái không hợp lệ (ném lỗi .status = 409).
    validateOrderTransition(don.TrangThaiDon, TrangThaiMoi);

    const data = await donHangModel.capNhatTrangThai(maDonHang, TrangThaiMoi, { MaVanDon, DonViVanChuyen });
    await lichSuModel.ghi(
      req.nguoiDung.MaNguoiDung,
      'CAP_NHAT_TRANG_THAI',
      `Đơn #${maDonHang}: ${don.TrangThaiDon} -> ${TrangThaiMoi}`,
      req.ip
    );
    res.json({ success: true, data });
  } catch (err) {
    if (err.status) {
      return res.status(err.status).json({ success: false, message: err.message });
    }
    console.error('Lỗi cập nhật trạng thái:', err.message);
    res.status(500).json({ success: false, message: 'Lỗi khi cập nhật trạng thái đơn' });
  }
}

module.exports = { create, getAll, getById, capNhatTrangThai };
